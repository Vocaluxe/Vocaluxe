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
using System.Linq;
using System.Threading.Tasks;
using Vocaluxe.Base;
using Vocaluxe.Base.Server;
using VocaluxeLib;
using VocaluxeLib.Game;
using VocaluxeLib.Menu;
using VocaluxeLib.Songs;
using Vocaluxe.Lib.Sound;

namespace Vocaluxe.Screens
{
    public class CScreenScore : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 4; }
        }

        private const string _TextSong = "TextSong";

        private const string _ScreenSettingShortScore = "ScreenSettingShortScore";
        private const string _ScreenSettingShortRating = "ScreenSettingShortRating";
        private const string _ScreenSettingShortDifficulty = "ScreenSettingShortDifficulty";

        private CBackground _SlideShowBG;

        private string[,] _TextNames;
        private string[,] _TextScores;
        private string[,] _TextRatings;
        private string[,] _TextDifficulty;
        private string[,] _ProgressBarPoints;
        private string[,] _StaticAvatar;
        private int _Round;
        private CPoints _Points;

        public override EMusicType CurrentMusicType
        {
            get { return EMusicType.BackgroundPreview; }
        }

        public override void Init()
        {
            base.Init();

            var texts = new List<string> {_TextSong};

            _BuildTextStrings(ref texts);

            _ThemeTexts = texts.ToArray();

            var statics = new List<string>();
            _BuildStaticStrings(ref statics);

            _ThemeStatics = statics.ToArray();

            var progressBars = new List<string>();
            _BuildProgressBarString(ref progressBars);

            _ThemeProgressBars = progressBars.ToArray();

            _ThemeScreenSettings = new string[] {_ScreenSettingShortScore, _ScreenSettingShortRating, _ScreenSettingShortDifficulty};

            _SlideShowBG = GetNewBackground();
            _AddBackground(_SlideShowBG);
            _SlideShowBG.Z--;
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
            //-1 --> Show average
            _Round = CGame.NumRounds > 1 ? -1 : 0;
            _Points = CGame.GetPoints();

            _SavePlayedSongs();

            _SetVisibility();
            _UpdateRatings();
            _SlideShowBG.Visible = _UpdateBackground();

            for (int p = 0; p < CGame.NumPlayers; p++)
                _Statics[_StaticAvatar[p, CGame.NumPlayers - 1]].Aspect = EAspect.Crop;
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
            _StaticAvatar = new string[CSettings.MaxNumPlayer,CSettings.MaxNumPlayer];

            for (int numplayer = 0; numplayer < CSettings.MaxNumPlayer; numplayer++)
            {
                for (int player = 0; player < CSettings.MaxNumPlayer; player++)
                {
                    if (player > numplayer)
                        continue;
                    string target = "P" + (player + 1) + "N" + (numplayer + 1);
                    _StaticAvatar[player, numplayer] = "StaticAvatar" + target;

                    statics.Add(_StaticAvatar[player, numplayer]);
                }
            }
        }

        private void _BuildProgressBarString(ref List<string> progressBars)
        {
            _ProgressBarPoints = new string[CSettings.MaxNumPlayer, CSettings.MaxNumPlayer];

            for (int numplayer = 0; numplayer < CSettings.MaxNumPlayer; numplayer++)
            {
                for (int player = 0; player < CSettings.MaxNumPlayer; player++)
                {
                    if (player > numplayer)
                        continue;
                    string target = "P" + (player + 1) + "N" + (numplayer + 1);
                    _ProgressBarPoints[player, numplayer] = "ProgressBarPoints" + target;

                    progressBars.Add(_ProgressBarPoints[player, numplayer]);
                }
            }
        }

        private int _ApplauseStream = -1;

        private void _PlayApplauseSound(int maxPoints)
        {
            // Play the appropriate applause sound based on maxPoints
            if (maxPoints >= 8000)
            {
                 _ApplauseStream = PlaySound(ESounds.ApplauseHigh, CConfig.GameMusicVolume);
            }
            else if (maxPoints >= 5000)
            {
                _ApplauseStream = PlaySound(ESounds.ApplauseMid, CConfig.GameMusicVolume);
            }
            else if (maxPoints >= 2000)
            {
            _ApplauseStream = PlaySound(ESounds.ApplauseLow, CConfig.GameMusicVolume);
            }
        }

        private static int PlaySound(ESounds sound, int volume)
        {
            int streamId = CSound.PlaySound(sound, false);
            CSound.SetStreamVolume(streamId, volume);

            return streamId;
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

                int maxPoints = (int)Math.Round(players.Max(player => player.Points));
                _PlayApplauseSound(maxPoints);
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

                int maxPoints = (int)Math.Round(players.Max(player => player.Points));
                _PlayApplauseSound(maxPoints);
            }

            for (int p = 0; p < players.Length; p++)
            {
                string name = CProfiles.GetPlayerName(players[p].ProfileID, p);
                if (song != null && song.IsDuet)
                {
                    if (song.Notes.VoiceNames.IsSet(players[p].VoiceNr))
                        name += " (" + song.Notes.VoiceNames[players[p].VoiceNr] + ")";
                }
                _Texts[_TextNames[p, CGame.NumPlayers - 1]].Text = name;

                if (CGame.NumPlayers < (int)_ScreenSettings[_ScreenSettingShortScore].GetValue())
                    _Texts[_TextScores[p, CGame.NumPlayers - 1]].Text = ((int)Math.Round(players[p].Points)).ToString("0000") + " " + CLanguage.Translate("TR_SCREENSCORE_POINTS");
                else
                    _Texts[_TextScores[p, CGame.NumPlayers - 1]].Text = ((int)Math.Round(players[p].Points)).ToString("0000");
                if (CGame.NumPlayers < (int)_ScreenSettings[_ScreenSettingShortDifficulty].GetValue())
                {
                    _Texts[_TextDifficulty[p, CGame.NumPlayers - 1]].Text = CLanguage.Translate("TR_SCREENSCORE_GAMEDIFFICULTY") + ": " +
                                                                            CLanguage.Translate(CProfiles.GetDifficulty(players[p].ProfileID).ToString());
                }
                else
                    _Texts[_TextDifficulty[p, CGame.NumPlayers - 1]].Text = CLanguage.Translate(CProfiles.GetDifficulty(players[p].ProfileID).ToString());
                if (CGame.NumPlayers < (int)_ScreenSettings[_ScreenSettingShortRating].GetValue())
                {
                    _Texts[_TextRatings[p, CGame.NumPlayers - 1]].Text = CLanguage.Translate("TR_SCREENSCORE_RATING") + ": " +
                                                                         CLanguage.Translate(_GetRating((int)Math.Round(players[p].Points)));
                }
                else
                    _Texts[_TextRatings[p, CGame.NumPlayers - 1]].Text = CLanguage.Translate(_GetRating((int)Math.Round(players[p].Points)));

                _ProgressBars[_ProgressBarPoints[p, CGame.NumPlayers - 1]].Progress = (float)players[p].Points / CSettings.MaxScore;

                if (CProfiles.IsProfileIDValid(players[p].ProfileID))
                    _Statics[_StaticAvatar[p, CGame.NumPlayers - 1]].Texture = CProfiles.GetAvatarTextureFromProfile(players[p].ProfileID);
            }
        }

        private void _SetVisibility()
        {
            for (int numplayer = 0; numplayer < CSettings.MaxNumPlayer; numplayer++)
            {
                for (int player = 0; player < CSettings.MaxNumPlayer; player++)
                {
                    if (player <= numplayer)
                    {
                        _Texts[_TextNames[player, numplayer]].Visible = numplayer + 1 == CGame.NumPlayers;
                        _Texts[_TextScores[player, numplayer]].Visible = numplayer + 1 == CGame.NumPlayers;
                        _Texts[_TextRatings[player, numplayer]].Visible = numplayer + 1 == CGame.NumPlayers;
                        _Texts[_TextDifficulty[player, numplayer]].Visible = numplayer + 1 == CGame.NumPlayers;
                        _ProgressBars[_ProgressBarPoints[player, numplayer]].Visible = numplayer + 1 == CGame.NumPlayers;
                        _ProgressBars[_ProgressBarPoints[player, numplayer]].Reset(true);
                        _Statics[_StaticAvatar[player, numplayer]].Visible = numplayer + 1 == CGame.NumPlayers;

                        _Statics[_StaticAvatar[player, numplayer]].Texture = null;
                    }
                }
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
            if (_ApplauseStream != -1)
            {
                 CSound.Close(_ApplauseStream);
                 _ApplauseStream = -1;
            }
            
            CParty.LeavingScore();
        }
    }
}
