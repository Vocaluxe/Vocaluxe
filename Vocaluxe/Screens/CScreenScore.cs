#region license
// /*
//     This file is part of Vocaluxe.
// 
//     Vocaluxe is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     Vocaluxe is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
//  */
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib.Menu;
using VocaluxeLib.Menu.SongMenu;

namespace Vocaluxe.Screens
{
    class CScreenScore : CMenu
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

        private string[,] _TextNames;
        private string[,] _TextScores;
        private string[,] _TextRatings;
        private string[,] _TextDifficulty;
        private string[,] _StaticPointsBarBG;
        private string[,] _StaticPointsBar;
        private string[,] _StaticAvatar;
        private double[] _StaticPointsBarDrawnPoints;
        private int _Round;
        private CPoints _Points;
        private Stopwatch _Timer;

        public override void Init()
        {
            base.Init();

            List<string> texts = new List<string> {_TextSong};

            _BuildTextStrings(ref texts);

            _ThemeTexts = texts.ToArray();

            List<string> statics = new List<string>();
            _BuildStaticStrings(ref statics);

            _ThemeStatics = statics.ToArray();

            _ThemeScreenSettings = new string[] {_ScreenSettingShortScore, _ScreenSettingShortRating, _ScreenSettingShortDifficulty, _ScreenSettingAnimationDirection};

            _StaticPointsBarDrawnPoints = new double[CSettings.MaxNumPlayer];
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            if (keyEvent.KeyPressed) {}
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
            _Round = CGame.NumRounds > 1 ? 0 : 1;
            _Points = CGame.GetPoints();

            _SetVisibility();
            _UpdateRatings();
        }

        public override bool UpdateGame()
        {
            SPlayer[] players = new SPlayer[CGame.NumPlayer];
            if (_Round != 0)
                players = _Points.GetPlayer(_Round - 1, CGame.NumPlayer);
            else
            {
                for (int i = 0; i < CGame.NumRounds; i++)
                {
                    SPlayer[] points = _Points.GetPlayer(i, CGame.NumPlayer);
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
                    if (CConfig.ScoreAnimationTime >= 1)
                    {
                        _StaticPointsBarDrawnPoints[p] = (_Timer.ElapsedMilliseconds / 1000f) / CConfig.ScoreAnimationTime * 10000;


                        if (_StaticPointsBarDrawnPoints[p] > players[p].Points)
                            _StaticPointsBarDrawnPoints[p] = players[p].Points;
                        string direction = (string)_ScreenSettings[_ScreenSettingAnimationDirection].GetValue();
                        if (direction.ToLower() == "vertical")
                        {
                            _Statics[_StaticPointsBar[p, CGame.NumPlayer - 1]].Rect.W = ((float)_StaticPointsBarDrawnPoints[p]) *
                                                                                        (_Statics[_StaticPointsBarBG[p, CGame.NumPlayer - 1]].Rect.W / 10000);
                        }
                        else
                        {
                            _Statics[_StaticPointsBar[p, CGame.NumPlayer - 1]].Rect.H = ((float)_StaticPointsBarDrawnPoints[p]) *
                                                                                        (_Statics[_StaticPointsBarBG[p, CGame.NumPlayer - 1]].Rect.H / 10000);
                            _Statics[_StaticPointsBar[p, CGame.NumPlayer - 1]].Rect.Y = _Statics[_StaticPointsBarBG[p, CGame.NumPlayer - 1]].Rect.H +
                                                                                        _Statics[_StaticPointsBarBG[p, CGame.NumPlayer - 1]].Rect.Y -
                                                                                        _Statics[_StaticPointsBar[p, CGame.NumPlayer - 1]].Rect.H;
                        }
                    }
                }
            }
            return true;
        }

        public override bool Draw()
        {
            base.Draw();
            return true;
        }

        private static string _GetRating(double points)
        {
            string rating = String.Empty;

            if (points >= 9800)
            {
                //in case of the game names so:
                rating = "TR_RATING_VOCAL_HERO";
            }
            else if (points < 9800 && points >= 8400)
                rating = "TR_RATING_SUPERSTAR";
            else if (points < 8400 && points >= 7000)
                rating = "TR_RATING_LEAD_SINGER";
            else if (points < 7000 && points >= 5600)
                rating = "TR_RATING_RISING_STAR";
            else if (points < 5600 && points >= 4200)
                rating = "TR_RATING_HOPEFUL";
            else if (points < 4200 && points >= 2800)
                rating = "TR_RATING_WANNABE";
            else if (points < 2800 && points >= 1400)
                rating = "TR_RATING_AMATEUR";
            else if (points < 1400)
                rating = "TR_RATING_TONE_DEAF";

            return rating;
        }

        private void _BuildTextStrings(ref List<string> texts)
        {
            _TextNames = new string[CSettings.MaxNumPlayer,CSettings.MaxNumPlayer];
            _TextScores = new string[CSettings.MaxNumPlayer,CSettings.MaxNumPlayer];
            _TextRatings = new string[CSettings.MaxNumPlayer,CSettings.MaxNumPlayer];
            _TextDifficulty = new string[CSettings.MaxNumPlayer,CSettings.MaxNumPlayer];

            for (int numplayer = 0; numplayer < CSettings.MaxNumPlayer; numplayer++)
            {
                for (int player = 0; player < CSettings.MaxNumPlayer; player++)
                {
                    if (player <= numplayer)
                    {
                        string target = "P" + (player + 1) + "N" + (numplayer + 1);
                        _TextNames[player, numplayer] = "TextName" + target;
                        _TextScores[player, numplayer] = "TextScore" + target;
                        _TextRatings[player, numplayer] = "TextRating" + target;
                        _TextDifficulty[player, numplayer] = "TextDifficulty" + target;

                        texts.Add(_TextNames[player, numplayer]);
                        texts.Add(_TextScores[player, numplayer]);
                        texts.Add(_TextRatings[player, numplayer]);
                        texts.Add(_TextDifficulty[player, numplayer]);
                    }
                }
            }
        }

        private void _BuildStaticStrings(ref List<string> statics)
        {
            _StaticPointsBar = new string[CSettings.MaxNumPlayer,CSettings.MaxNumPlayer];
            _StaticPointsBarBG = new string[CSettings.MaxNumPlayer,CSettings.MaxNumPlayer];
            _StaticAvatar = new string[CSettings.MaxNumPlayer,CSettings.MaxNumPlayer];

            for (int numplayer = 0; numplayer < CSettings.MaxNumPlayer; numplayer++)
            {
                for (int player = 0; player < CSettings.MaxNumPlayer; player++)
                {
                    if (player > numplayer)
                        continue;
                    string target = "P" + (player + 1) + "N" + (numplayer + 1);
                    _StaticPointsBarBG[player, numplayer] = "StaticPointsBarBG" + target;
                    _StaticPointsBar[player, numplayer] = "StaticPointsBar" + target;
                    _StaticAvatar[player, numplayer] = "StaticAvatar" + target;

                    statics.Add(_StaticPointsBarBG[player, numplayer]);
                    statics.Add(_StaticPointsBar[player, numplayer]);
                    statics.Add(_StaticAvatar[player, numplayer]);
                }
            }
        }

        private void _UpdateRatings()
        {
            CSong song = null;
            SPlayer[] players = new SPlayer[CGame.NumPlayer];
            if (_Round != 0)
            {
                song = CGame.GetSong(_Round);
                if (song == null)
                    return;

                _Texts[_TextSong].Text = song.Artist + " - " + song.Title;
                if (_Points.NumRounds > 1)
                    _Texts[_TextSong].Text += " (" + _Round + "/" + _Points.NumRounds + ")";
                players = _Points.GetPlayer(_Round - 1, CGame.NumPlayer);
            }
            else
            {
                _Texts[_TextSong].Text = "TR_SCREENSCORE_OVERALLSCORE";
                for (int i = 0; i < CGame.NumRounds; i++)
                {
                    SPlayer[] points = _Points.GetPlayer(i, CGame.NumPlayer);
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

            for (int p = 0; p < players.Length; p++)
            {
                string name = CProfiles.GetPlayerName(players[p].ProfileID, p);
                if (song != null && song.IsDuet)
                {
                    if (players[p].LineNr == 0 && song.DuetPart1 != "Part 1")
                        name += " (" + song.DuetPart1 + ")";
                    else if (players[p].LineNr == 1 && song.DuetPart2 != "Part 2")
                        name += " (" + song.DuetPart2 + ")";
                }
                _Texts[_TextNames[p, CGame.NumPlayer - 1]].Text = name;

                if (CGame.NumPlayer < (int)_ScreenSettings[_ScreenSettingShortScore].GetValue())
                    _Texts[_TextScores[p, CGame.NumPlayer - 1]].Text = ((int)Math.Round(players[p].Points)).ToString("0000") + " " + CLanguage.Translate("TR_SCREENSCORE_POINTS");
                else
                    _Texts[_TextScores[p, CGame.NumPlayer - 1]].Text = ((int)Math.Round(players[p].Points)).ToString("0000");
                if (CGame.NumPlayer < (int)_ScreenSettings[_ScreenSettingShortDifficulty].GetValue())
                {
                    _Texts[_TextDifficulty[p, CGame.NumPlayer - 1]].Text = CLanguage.Translate("TR_SCREENSCORE_GAMEDIFFICULTY") + ": " +
                                                                           CLanguage.Translate(CProfiles.GetDifficulty(players[p].ProfileID).ToString());
                }
                else
                    _Texts[_TextDifficulty[p, CGame.NumPlayer - 1]].Text = CLanguage.Translate(CProfiles.GetDifficulty(players[p].ProfileID).ToString());
                if (CGame.NumPlayer < (int)_ScreenSettings[_ScreenSettingShortRating].GetValue())
                {
                    _Texts[_TextRatings[p, CGame.NumPlayer - 1]].Text = CLanguage.Translate("TR_SCREENSCORE_RATING") + ": " +
                                                                        CLanguage.Translate(_GetRating((int)Math.Round(players[p].Points)));
                }
                else
                    _Texts[_TextRatings[p, CGame.NumPlayer - 1]].Text = CLanguage.Translate(_GetRating((int)Math.Round(players[p].Points)));

                _StaticPointsBarDrawnPoints[p] = 0.0;
                string direction = (string)_ScreenSettings[_ScreenSettingAnimationDirection].GetValue();
                if (direction.ToLower() == "vertical")
                    _Statics[_StaticPointsBar[p, CGame.NumPlayer - 1]].Rect.W = 0;
                else
                {
                    _Statics[_StaticPointsBar[p, CGame.NumPlayer - 1]].Rect.H = 0;
                    _Statics[_StaticPointsBar[p, CGame.NumPlayer - 1]].Rect.Y = _Statics[_StaticPointsBarBG[p, CGame.NumPlayer - 1]].Rect.H +
                                                                                _Statics[_StaticPointsBarBG[p, CGame.NumPlayer - 1]].Rect.Y -
                                                                                _Statics[_StaticPointsBar[p, CGame.NumPlayer - 1]].Rect.H;
                }
                if (CProfiles.IsProfileIDValid(players[p].ProfileID))
                    _Statics[_StaticAvatar[p, CGame.NumPlayer - 1]].Texture = CProfiles.Profiles[players[p].ProfileID].Avatar.Texture;
            }

            if (CConfig.ScoreAnimationTime < 1)
            {
                for (int p = 0; p < CGame.NumPlayer; p++)
                {
                    _Statics[_StaticPointsBar[p, CGame.NumPlayer - 1]].Rect.H = ((float)players[p].Points) * (_Statics[_StaticPointsBarBG[p, CGame.NumPlayer - 1]].Rect.H / 10000);
                    _Statics[_StaticPointsBar[p, CGame.NumPlayer - 1]].Rect.Y = _Statics[_StaticPointsBarBG[p, CGame.NumPlayer - 1]].Rect.H +
                                                                                _Statics[_StaticPointsBarBG[p, CGame.NumPlayer - 1]].Rect.Y -
                                                                                _Statics[_StaticPointsBar[p, CGame.NumPlayer - 1]].Rect.H;
                    _StaticPointsBarDrawnPoints[p] = players[p].Points;
                }
            }

            _Timer = new Stopwatch();
            _Timer.Start();
        }

        private void _SetVisibility()
        {
            for (int numplayer = 0; numplayer < CSettings.MaxNumPlayer; numplayer++)
            {
                for (int player = 0; player < CSettings.MaxNumPlayer; player++)
                {
                    if (player <= numplayer)
                    {
                        _Texts[_TextNames[player, numplayer]].Visible = numplayer + 1 == CGame.NumPlayer;
                        _Texts[_TextScores[player, numplayer]].Visible = numplayer + 1 == CGame.NumPlayer;
                        _Texts[_TextRatings[player, numplayer]].Visible = numplayer + 1 == CGame.NumPlayer;
                        _Texts[_TextDifficulty[player, numplayer]].Visible = numplayer + 1 == CGame.NumPlayer;
                        _Statics[_StaticPointsBar[player, numplayer]].Visible = numplayer + 1 == CGame.NumPlayer;
                        _Statics[_StaticPointsBarBG[player, numplayer]].Visible = numplayer + 1 == CGame.NumPlayer;
                        _Statics[_StaticAvatar[player, numplayer]].Visible = numplayer + 1 == CGame.NumPlayer;

                        _Statics[_StaticAvatar[player, numplayer]].Texture = new STexture(-1);
                    }
                }
            }
        }

        private void _ChangeRound(int num)
        {
            if ((_Round + num) <= _Points.NumRounds && (_Round + num > 0))
            {
                _Round += num;
                _UpdateRatings();
            }
            else if (num < 0 && _Round == 1 && CGame.NumRounds > 1)
            {
                _Round = 0;
                _UpdateRatings();
            }
        }

        private void _LeaveScreen()
        {
            CParty.LeavingScore();
        }
    }
}