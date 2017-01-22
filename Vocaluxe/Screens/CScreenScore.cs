#region license
// This file is part of Vocaluxe.
// 
// Vocaluxe is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Vocaluxe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using Vocaluxe.Base;
using Vocaluxe.Base.Server;
using VocaluxeLib;
using VocaluxeLib.Game;
using VocaluxeLib.Menu;
using VocaluxeLib.Songs;

namespace Vocaluxe.Screens
{
    public class CScreenScore : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 3; }
        }

        private const string _TextSong = "TextSong";

        private const string _ScreenSettingShortScore = "ScreenSettingShortScore";
        private const string _ScreenSettingShortRating = "ScreenSettingShortRating";
        private const string _ScreenSettingShortDifficulty = "ScreenSettingShortDifficulty";
        private const string _ScreenSettingAnimationDirection = "ScreenSettingAnimationDirection";

        private CBackground _SlideShowBG;

        private string[,,] _TextNames;
        private string[,,] _TextScores;
        private string[,,] _TextRatings;
        private string[,,] _TextDifficulty;
        private string[,,] _StaticPointsBarBG;
        private string[,,] _StaticPointsBar;
        private string[,,] _StaticAvatar;
        private double[] _StaticPointsBarDrawnPoints;
        private int _Round;
        private CPoints _Points;
        private Stopwatch _Timer;

        private string[] _PlayerTextName;
        private string[] _PlayerTextScore;
        private string[] _PlayerTextRating;
        private string[] _PlayerTextDifficulty;
        private string[] _PlayerStaticPointsBarBG;
        private string[] _PlayerStaticPointsBar;
        private string[] _PlayerStaticAvatar;

        public override EMusicType CurrentMusicType
        {
            get { return EMusicType.BackgroundPreview; }
        }

        public override void Init()
        {
            base.Init();

            var texts = new List<string> { _TextSong };

            _BuildTextStrings(ref texts);

            _CreatePlayerStrings();

            _ThemeTexts = texts.ToArray();

            var statics = new List<string>();
            _BuildStaticStrings(ref statics);

            _CreatePlayerStatics();
            
            _AssignPlayerElements();

            _ThemeStatics = statics.ToArray();

            _ThemeScreenSettings = new string[] { _ScreenSettingShortScore, _ScreenSettingShortRating, _ScreenSettingShortDifficulty, _ScreenSettingAnimationDirection };

            _StaticPointsBarDrawnPoints = new double[CSettings.MaxNumPlayer];

            _SlideShowBG = GetNewBackground();
            _AddBackground(_SlideShowBG);
            _SlideShowBG.Z--;
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            if (keyEvent.KeyPressed) { }
            else
            {
                switch (keyEvent.Key)
                {
                    case Keys.Escape:
                    case Keys.Back:
                    case Keys.Enter:
                        _LeaveScreen();
                        break;

                    case Keys.Left:
                        _ChangeRound(-1);
                        break;

                    case Keys.Right:
                        _ChangeRound(1);
                        break;
                }
            }
            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);

            if (mouseEvent.Wheel != 0)
                _ChangeRound(mouseEvent.Wheel);

            if (mouseEvent.LB)
                _LeaveScreen();

            if (mouseEvent.RB)
                _LeaveScreen();

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            _InitiatePlayerStatics();
            _InitiatePlayerStrings();
            _AssignPlayerElements();

            //-1 --> Show average
            _Round = CGame.NumRounds > 1 ? -1 : 0;
            _Points = CGame.GetPoints();

            _SavePlayedSongs();

            _SetVisibility();
            _UpdateRatings();
            _SlideShowBG.Visible = _UpdateBackground();
        }

        public override bool UpdateGame()
        {
            var players = new SPlayer[CGame.NumPlayers];
            if (_Round >= 0)
                players = _Points.GetPlayer(_Round, CGame.NumPlayers);
            else
            {
                for (int i = 0; i < CGame.NumRounds; i++)
                {
                    SPlayer[] points = _Points.GetPlayer(i, CGame.NumPlayers);
                    for (int p = 0; p < players.Length; p++)
                        players[p].Points += points[p].Points;
                }
                for (int p = 0; p < players.Length; p++)
                    players[p].Points = (int)(players[p].Points / CGame.NumRounds);
            }
            for (int p = 0; p < players.Length; p++)
            {
                if (_StaticPointsBarDrawnPoints[p] < players[p].Points)
                {
                    if (CConfig.Config.Game.ScoreAnimationTime >= 1)
                    {
                        _StaticPointsBarDrawnPoints[p] = (_Timer.ElapsedMilliseconds / 1000f) / CConfig.Config.Game.ScoreAnimationTime * 10000;


                        if (_StaticPointsBarDrawnPoints[p] > players[p].Points)
                            _StaticPointsBarDrawnPoints[p] = players[p].Points;
                        var direction = (string)_ScreenSettings[_ScreenSettingAnimationDirection].GetValue();
                        if (direction.ToLower() == "vertical")
                        {
                            _Statics[_PlayerStaticPointsBar[p]].W = ((float)_StaticPointsBarDrawnPoints[p]) *
                                                                                    (_Statics[_PlayerStaticPointsBarBG[p]].W / CSettings.MaxScore);
                        }
                        else
                        {
                            _Statics[_PlayerStaticPointsBar[p]].H = ((float)_StaticPointsBarDrawnPoints[p]) *
                                                                                    (_Statics[_PlayerStaticPointsBarBG[p]].H / CSettings.MaxScore);
                            _Statics[_PlayerStaticPointsBar[p]].Y = _Statics[_PlayerStaticPointsBarBG[p]].H +
                                                                                    _Statics[_PlayerStaticPointsBarBG[p]].Y -
                                                                                    _Statics[_PlayerStaticPointsBar[p]].H;
                        }
                    }
                }
            }
            return true;
        }

        private static string _GetRating(double points)
        {
            string rating;

            if (points >= 9800)
                rating = "TR_RATING_VOCAL_HERO";
            else if (points >= 8400)
                rating = "TR_RATING_SUPERSTAR";
            else if (points >= 7000)
                rating = "TR_RATING_LEAD_SINGER";
            else if (points >= 5600)
                rating = "TR_RATING_RISING_STAR";
            else if (points >= 4200)
                rating = "TR_RATING_HOPEFUL";
            else if (points >= 2800)
                rating = "TR_RATING_WANNABE";
            else if (points >= 1400)
                rating = "TR_RATING_AMATEUR";
            else
                rating = "TR_RATING_TONE_DEAF";

            return rating;
        }

        private void _BuildTextStrings(ref List<string> texts)
        {
            _TextNames = new string[CSettings.MaxNumScreens, CSettings.MaxScreenPlayer, CSettings.MaxScreenPlayer];
            _TextScores = new string[CSettings.MaxNumScreens, CSettings.MaxScreenPlayer, CSettings.MaxScreenPlayer];
            _TextRatings = new string[CSettings.MaxNumScreens, CSettings.MaxScreenPlayer, CSettings.MaxScreenPlayer];
            _TextDifficulty = new string[CSettings.MaxNumScreens, CSettings.MaxScreenPlayer, CSettings.MaxScreenPlayer];

            for (int numplayer = 0; numplayer < CSettings.MaxScreenPlayer; numplayer++)
            {
                for (int player = 0; player < CSettings.MaxScreenPlayer; player++)
                {
                    if (player <= numplayer)
                    {
                        string target = "P" + (player + 1) + "N" + (numplayer + 1);
                        for (int screen = 0; screen < CSettings.MaxNumScreens; screen++)
                        {
                            _TextNames[screen, player, numplayer] = "TextNameS" + (screen + 1) + target;
                            _TextScores[screen, player, numplayer] = "TextScoreS" + (screen + 1) + target;
                            _TextRatings[screen, player, numplayer] = "TextRatingS" + (screen + 1) + target;
                            _TextDifficulty[screen, player, numplayer] = "TextDifficultyS" + (screen + 1) + target;
                        }

                        texts.Add("TextNames" + target);
                        texts.Add("TextScores" + target);
                        texts.Add("TextRatings" + target);
                        texts.Add("TextDifficulty" + target);
                    }
                }
            }
        }

        private void _CreatePlayerStrings()
        {
            for (int screen = 0; screen < CSettings.MaxNumScreens; screen++)
            {
                for (int numplayer = 0; numplayer < CSettings.MaxScreenPlayer; numplayer++)
                {
                    for (int player = 0; player < CSettings.MaxScreenPlayer; player++)
                    {
                        if (player <= numplayer)
                        {
                            string target = "P" + (player + 1) + "N" + (numplayer + 1);
                            _AddText(GetNewText(), "TextNameS" + (screen + 1) + target);
                            _AddText(GetNewText(), "TextScoreS" + (screen + 1) + target);
                            _AddText(GetNewText(), "TextRatingS" + (screen + 1) + target);
                            _AddText(GetNewText(), "TextDifficultyS" + (screen + 1) + target);
                        }
                    }
                }
            }
        }

        private void _InitiatePlayerStrings()
        {
            for (int screen = 0; screen < CSettings.MaxNumScreens; screen++)
            {
                for (int numplayer = 0; numplayer < CSettings.MaxScreenPlayer; numplayer++)
                {
                    for (int player = 0; player < CSettings.MaxScreenPlayer; player++)
                    {
                        if (player <= numplayer)
                        {
                            string target = "P" + (player + 1) + "N" + (numplayer + 1);
                            _Texts["TextName" + target].Visible = false;
                            _Texts["TextNameS" + (screen + 1) + target] = GetNewText(_Texts["TextName" + target]);
                            _Texts["TextNameS" + (screen + 1) + target].X += screen * CSettings.RenderW;

                            _Texts["TextScore" + target].Visible = false;
                            _Texts["TextScoreS" + (screen + 1) + target] = GetNewText(_Texts["TextScore" + target]);
                            _Texts["TextScoreS" + (screen + 1) + target].X += screen * CSettings.RenderW;

                            _Texts["TextRating" + target].Visible = false;
                            _Texts["TextRatingS" + (screen + 1) + target] = GetNewText(_Texts["TextRating" + target]);
                            _Texts["TextRatingS" + (screen + 1) + target].X += screen * CSettings.RenderW;

                            _Texts["TextDifficulty" + target].Visible = false;
                            _Texts["TextDifficultyS" + (screen + 1) + target] = GetNewText(_Texts["TextDifficulty" + target]);
                            _Texts["TextDifficultyS" + (screen + 1) + target].X += screen * CSettings.RenderW;
                        }
                    }
                }
            }
        }

        private void _BuildStaticStrings(ref List<string> statics)
        {
            _StaticPointsBar = new string[CSettings.MaxNumScreens, CSettings.MaxScreenPlayer, CSettings.MaxScreenPlayer];
            _StaticPointsBarBG = new string[CSettings.MaxNumScreens, CSettings.MaxScreenPlayer, CSettings.MaxScreenPlayer];
            _StaticAvatar = new string[CSettings.MaxNumScreens, CSettings.MaxScreenPlayer, CSettings.MaxScreenPlayer];

            for (int numplayer = 0; numplayer < CSettings.MaxScreenPlayer; numplayer++)
            {
                for (int player = 0; player < CSettings.MaxScreenPlayer; player++)
                {
                    if (player > numplayer)
                        continue;
                    string target = "P" + (player + 1) + "N" + (numplayer + 1);
                    for (int screen = 0; screen < CSettings.MaxNumScreens; screen++)
                    {
                        _StaticPointsBarBG[screen, player, numplayer] = "StaticPointsBarBGS" + (screen + 1) + target;
                        _StaticPointsBar[screen, player, numplayer] = "StaticPointsBarS" + (screen + 1) + target;
                        _StaticAvatar[screen, player, numplayer] = "StaticAvatarS" + (screen + 1) + target;
                    }
                    statics.Add("StaticPointsBarBG" + target);
                    statics.Add("StaticPointsBar" + target);
                    statics.Add("StaticAvatar" + target);
                }
            }
        }

        private void _CreatePlayerStatics()
        {
            for (int screen = 0; screen < CSettings.MaxNumScreens; screen++)
            {
                for (int numplayer = 0; numplayer < CSettings.MaxScreenPlayer; numplayer++)
                {
                    for (int player = 0; player < CSettings.MaxScreenPlayer; player++)
                    {
                        if (player <= numplayer)
                        {
                            string target = "P" + (player + 1) + "N" + (numplayer + 1);
                            _AddStatic(GetNewStatic(), "StaticPointsBarBGS" + (screen + 1) + target);
                            _AddStatic(GetNewStatic(), "StaticPointsBarS" + (screen + 1) + target);
                            _AddStatic(GetNewStatic(), "StaticAvatarS" + (screen + 1) + target);
                        }
                    }
                }
            }
        }

        private void _InitiatePlayerStatics()
        {
            for (int screen = 0; screen < CSettings.MaxNumScreens; screen++)
            {
                for (int numplayer = 0; numplayer < CSettings.MaxScreenPlayer; numplayer++)
                {
                    for (int player = 0; player < CSettings.MaxScreenPlayer; player++)
                    {
                        if (player <= numplayer)
                        {
                            string target = "P" + (player + 1) + "N" + (numplayer + 1);
                            _Statics["StaticPointsBarBG" + target].Visible = false;
                            _Statics["StaticPointsBarBGS" + (screen + 1) + target] = GetNewStatic(_Statics["StaticPointsBarBG" + target]);
                            _Statics["StaticPointsBarBGS" + (screen + 1) + target].X += screen * CSettings.RenderW;

                            _Statics["StaticPointsBar" + target].Visible = false;
                            _Statics["StaticPointsBarS" + (screen + 1) + target] = GetNewStatic(_Statics["StaticPointsBar" + target]);
                            _Statics["StaticPointsBarS" + (screen + 1) + target].X += screen * CSettings.RenderW;

                            _Statics["StaticAvatar" + target].Visible = false;
                            _Statics["StaticAvatarS" + (screen + 1) + target] = GetNewStatic(_Statics["StaticAvatar" + target]);
                            _Statics["StaticAvatarS" + (screen + 1) + target].X += screen * CSettings.RenderW;
                        }
                    }
                }
            }
        }

        private void _UpdateRatings()
        {
            CSong song = null;
            var players = new SPlayer[CGame.NumPlayers];
            if (_Round >= 0)
            {
                song = CGame.GetSong(_Round);
                if (song == null)
                    return;

                _Texts[_TextSong].Text = song.Artist + " - " + song.Title;
                if (_Points.NumRounds > 1)
                    _Texts[_TextSong].Text += " (" + (_Round + 1) + "/" + _Points.NumRounds + ")";
                players = _Points.GetPlayer(_Round, CGame.NumPlayers);
            }
            else
            {
                _Texts[_TextSong].Text = "TR_SCREENSCORE_OVERALLSCORE";
                for (int i = 0; i < CGame.NumRounds; i++)
                {
                    SPlayer[] points = _Points.GetPlayer(i, CGame.NumPlayers);
                    for (int p = 0; p < players.Length; p++)
                    {
                        if (i < 1)
                            players[p].ProfileID = points[p].ProfileID;
                        players[p].Points += points[p].Points;
                    }
                }
                for (int p = 0; p < players.Length; p++)
                    players[p].Points = (int)Math.Round(players[p].Points / CGame.NumRounds);
            }

            var pointAnimDirection = (string)_ScreenSettings[_ScreenSettingAnimationDirection].GetValue();
            for (int p = 0; p < players.Length; p++)
            {
                string name = CProfiles.GetPlayerName(players[p].ProfileID, p);
                if (song != null && song.IsDuet)
                {
                    if (song.Notes.VoiceNames.IsSet(players[p].VoiceNr))
                        name += " (" + song.Notes.VoiceNames[players[p].VoiceNr] + ")";
                }
                _Texts[_PlayerTextName[p]].Text = name;

                if (CGame.NumPlayers < (int)_ScreenSettings[_ScreenSettingShortScore].GetValue())
                    _Texts[_PlayerTextScore[p]].Text = ((int)Math.Round(players[p].Points)).ToString("0000") + " " + CLanguage.Translate("TR_SCREENSCORE_POINTS");
                else
                    _Texts[_PlayerTextScore[p]].Text = ((int)Math.Round(players[p].Points)).ToString("0000");
                if (CGame.NumPlayers < (int)_ScreenSettings[_ScreenSettingShortDifficulty].GetValue())
                {
                    _Texts[_PlayerTextDifficulty[p]].Text = CLanguage.Translate("TR_SCREENSCORE_GAMEDIFFICULTY") + ": " +
                                                                            CLanguage.Translate(CProfiles.GetDifficulty(players[p].ProfileID).ToString());
                }
                else
                    _Texts[_PlayerTextDifficulty[p]].Text = CLanguage.Translate(CProfiles.GetDifficulty(players[p].ProfileID).ToString());
                if (CGame.NumPlayers < (int)_ScreenSettings[_ScreenSettingShortRating].GetValue())
                {
                    _Texts[_PlayerTextRating[p]].Text = CLanguage.Translate("TR_SCREENSCORE_RATING") + ": " +
                                                                         CLanguage.Translate(_GetRating((int)Math.Round(players[p].Points)));
                }
                else
                    _Texts[_PlayerTextRating[p]].Text = CLanguage.Translate(_GetRating((int)Math.Round(players[p].Points)));

                _StaticPointsBarDrawnPoints[p] = 0.0;
                if (pointAnimDirection.ToLower() == "vertical")
                    _Statics[_PlayerStaticPointsBar[p]].W = 0;
                else
                {
                    _Statics[_PlayerStaticPointsBar[p]].H = 0;
                    _Statics[_PlayerStaticPointsBar[p]].Y = _Statics[_PlayerStaticPointsBarBG[p]].H +
                                                                            _Statics[_PlayerStaticPointsBarBG[p]].Y -
                                                                            _Statics[_PlayerStaticPointsBar[p]].H;
                }
                if (CProfiles.IsProfileIDValid(players[p].ProfileID))
                    _Statics[_PlayerStaticAvatar[p]].Texture = CProfiles.GetAvatarTextureFromProfile(players[p].ProfileID);
            }

            if (CConfig.Config.Game.ScoreAnimationTime < 1)
            {
                for (int p = 0; p < CGame.NumPlayers; p++)
                {
                    if (pointAnimDirection.ToLower() == "vertical")
                    {
                        _Statics[_PlayerStaticPointsBar[p]].W = ((float)players[p].Points) *
                                                                                (_Statics[_PlayerStaticPointsBarBG[p]].W / CSettings.MaxScore);
                    }
                    else
                    {
                        _Statics[_PlayerStaticPointsBar[p]].H = ((float)players[p].Points) *
                                                                                (_Statics[_PlayerStaticPointsBarBG[p]].H / CSettings.MaxScore);
                        _Statics[_PlayerStaticPointsBar[p]].Y = _Statics[_PlayerStaticPointsBarBG[p]].H +
                                                                                _Statics[_PlayerStaticPointsBarBG[p]].Y -
                                                                                _Statics[_PlayerStaticPointsBar[p]].H;
                    }
                    _StaticPointsBarDrawnPoints[p] = players[p].Points;
                }
            }

            _Timer = new Stopwatch();
            _Timer.Start();
        }

        private void _AssignPlayerElements()
        {
            _PlayerTextName = new String[CGame.NumPlayers];
            _PlayerTextScore = new String[CGame.NumPlayers];
            _PlayerTextRating = new String[CGame.NumPlayers];
            _PlayerTextDifficulty = new String[CGame.NumPlayers];
            _PlayerStaticPointsBarBG = new String[CGame.NumPlayers];
            _PlayerStaticPointsBar = new String[CGame.NumPlayers];
            _PlayerStaticAvatar = new String[CGame.NumPlayers];

            int screenPlayers = CGame.NumPlayers / CConfig.GetNumScreens();
            int remainingPlayers = CGame.NumPlayers - (screenPlayers * CConfig.GetNumScreens());
            int player = 0;

            for (int s = 0; s < CConfig.GetNumScreens(); s++)
            {
                for (int p = 0; p < screenPlayers; p++)
                {
                    if (remainingPlayers > 0)
                    {
                        _PlayerTextName[player] = _TextNames[s, p, screenPlayers];
                        _PlayerTextScore[player] = _TextScores[s, p, screenPlayers];
                        _PlayerTextRating[player] = _TextRatings[s, p, screenPlayers];
                        _PlayerTextDifficulty[player] = _TextDifficulty[s, p, screenPlayers];
                        _PlayerStaticPointsBarBG[player] = _StaticPointsBarBG[s, p, screenPlayers];
                        _PlayerStaticPointsBar[player] = _StaticPointsBar[s, p, screenPlayers];
                        _PlayerStaticAvatar[player++] = _StaticAvatar[s, p, screenPlayers];
                        if (p == screenPlayers - 1)
                        {
                            _PlayerTextName[player] = _TextNames[s, p + 1, screenPlayers];
                            _PlayerTextScore[player] = _TextScores[s, p + 1, screenPlayers];
                            _PlayerTextRating[player] = _TextRatings[s, p + 1, screenPlayers];
                            _PlayerTextDifficulty[player] = _TextDifficulty[s, p + 1, screenPlayers];
                            _PlayerStaticPointsBarBG[player] = _StaticPointsBarBG[s, p + 1, screenPlayers];
                            _PlayerStaticPointsBar[player] = _StaticPointsBar[s, p + 1, screenPlayers];
                            _PlayerStaticAvatar[player++] = _StaticAvatar[s, p + 1, screenPlayers];
                            remainingPlayers--;
                        }
                    }
                    else
                    {
                        _PlayerTextName[player] = _TextNames[s, p, screenPlayers - 1];
                        _PlayerTextScore[player] = _TextScores[s, p, screenPlayers - 1];
                        _PlayerTextRating[player] = _TextRatings[s, p, screenPlayers - 1];
                        _PlayerTextDifficulty[player] = _TextDifficulty[s, p, screenPlayers - 1];
                        _PlayerStaticPointsBarBG[player] = _StaticPointsBarBG[s, p, screenPlayers - 1];
                        _PlayerStaticPointsBar[player] = _StaticPointsBar[s, p, screenPlayers - 1];
                        _PlayerStaticAvatar[player++] = _StaticAvatar[s, p, screenPlayers - 1];
                    }

                }
                //Handle when players < screens
                if (screenPlayers == 0 && remainingPlayers > 0)
                {
                    _PlayerTextName[player] = _TextNames[s, 0, 0];
                    _PlayerTextScore[player] = _TextScores[s, 0, 0];
                    _PlayerTextRating[player] = _TextRatings[s, 0, 0];
                    _PlayerTextDifficulty[player] = _TextDifficulty[s, 0, 0];
                    _PlayerStaticPointsBarBG[player] = _StaticPointsBarBG[s, 0, 0];
                    _PlayerStaticPointsBar[player] = _StaticPointsBar[s, 0, 0];
                    _PlayerStaticAvatar[player++] = _StaticAvatar[s, 0, 0];
                    remainingPlayers--;
                }
            }
        }

        private void _SetVisibility()
        {
            for (int screen = 0; screen < CSettings.MaxNumScreens; screen++)
            {
                for (int numplayer = 0; numplayer < CSettings.MaxScreenPlayer; numplayer++)
                {
                    for (int player = 0; player < CSettings.MaxScreenPlayer; player++)
                    {
                        if (player <= numplayer)
                        {
                            _Texts[_TextNames[screen, player, numplayer]].AllMonitors = false;
                            _Texts[_TextScores[screen, player, numplayer]].AllMonitors = false;
                            _Texts[_TextRatings[screen, player, numplayer]].AllMonitors = false;
                            _Texts[_TextDifficulty[screen, player, numplayer]].AllMonitors = false;
                            _Statics[_StaticPointsBar[screen, player, numplayer]].AllMonitors = false;
                            _Statics[_StaticPointsBarBG[screen, player, numplayer]].AllMonitors = false;
                            _Statics[_StaticAvatar[screen, player, numplayer]].AllMonitors = false;

                            _Texts[_TextNames[screen, player, numplayer]].Visible = false;
                            _Texts[_TextScores[screen, player, numplayer]].Visible = false;
                            _Texts[_TextRatings[screen, player, numplayer]].Visible = false;
                            _Texts[_TextDifficulty[screen, player, numplayer]].Visible = false;
                            _Statics[_StaticPointsBar[screen, player, numplayer]].Visible = false;
                            _Statics[_StaticPointsBarBG[screen, player, numplayer]].Visible = false;
                            _Statics[_StaticAvatar[screen, player, numplayer]].Visible = false;

                            _Statics[_StaticAvatar[screen, player, numplayer]].Texture = null;
                        }
                    }
                }
            }
            for (int p = 0; p < CGame.NumPlayers; p++)
            {
                _Texts[_PlayerTextName[p]].Visible = true;
                _Texts[_PlayerTextScore[p]].Visible = true;
                _Texts[_PlayerTextRating[p]].Visible = true;
                _Texts[_PlayerTextDifficulty[p]].Visible = true;
                _Statics[_PlayerStaticPointsBarBG[p]].Visible = true;
                _Statics[_PlayerStaticPointsBar[p]].Visible = true;
                _Statics[_PlayerStaticAvatar[p]].Visible = true;

                SColorF color = CBase.Themes.GetPlayerColor(p + 1);
                _Statics[_PlayerStaticPointsBarBG[p]].Color = new SColorF(color.R, color.G, color.B, _Statics[_PlayerStaticPointsBarBG[p]].Color.A);
                _Statics[_PlayerStaticPointsBar[p]].Color = new SColorF(color.R, color.G, color.B, _Statics[_PlayerStaticPointsBarBG[p]].Color.A);
            }
        }

        private void _ChangeRound(int num)
        {
            _Round += num;
            _Round = _Round.Clamp(-1, _Points.NumRounds - 1);

            _UpdateRatings();
        }

        private void _SavePlayedSongs()
        {
            for (int round = 0; round < _Points.NumRounds; round++)
            {
                SPlayer[] players = _Points.GetPlayer(round, CGame.NumPlayers);

                for (int p = 0; p < players.Length; p++)
                {
                    if (players[p].Points > CSettings.MinScoreForDB && players[p].SongFinished)
                    {
                        CSong song = CSongs.GetSong(players[p].SongID);
                        CDataBase.IncreaseSongCounter(song.DataBaseSongID);
                        song.NumPlayed++;
                        song.NumPlayedSession++;
                        break;
                    }
                }
            }
        }

        private bool _UpdateBackground()
        {
            string[] photos = CVocaluxeServer.GetPhotosOfThisRound();
            _SlideShowBG.RemoveSlideShowTextures();
            foreach (string photo in photos)
                _SlideShowBG.AddSlideShowTexture(photo);
            return photos.Length > 0;
        }

        private void _LeaveScreen()
        {
            CParty.LeavingScore();
        }
    }
}