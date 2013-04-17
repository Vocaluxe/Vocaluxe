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

            List<string> texts = new List<string>();
            texts.Add(_TextSong);

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
            if (CGame.NumRounds > 1)
                _Round = 0;
            else
                _Round = 1;
            _Points = CGame.GetPoints();

            _SetVisibility();
            _UpdateRatings();
        }

        public override bool UpdateGame()
        {
            SPlayer[] player = new SPlayer[CGame.NumPlayer];
            if (_Round != 0)
                player = _Points.GetPlayer(_Round - 1, CGame.NumPlayer);
            else
            {
                for (int i = 0; i < CGame.NumRounds; i++)
                {
                    SPlayer[] points = _Points.GetPlayer(i, CGame.NumPlayer);
                    for (int p = 0; p < player.Length; p++)
                        player[p].Points += points[p].Points;
                }
                for (int p = 0; p < player.Length; p++)
                    player[p].Points = (int)(player[p].Points / CGame.NumRounds);
            }
            for (int p = 0; p < player.Length; p++)
            {
                if (_StaticPointsBarDrawnPoints[p] < player[p].Points)
                {
                    if (CConfig.ScoreAnimationTime >= 1)
                    {
                        _StaticPointsBarDrawnPoints[p] = (_Timer.ElapsedMilliseconds / 1000f) / CConfig.ScoreAnimationTime * 10000;


                        if (_StaticPointsBarDrawnPoints[p] > player[p].Points)
                            _StaticPointsBarDrawnPoints[p] = player[p].Points;
                        string direction = (string)ScreenSettings[_ScreenSettingAnimationDirection].GetValue();
                        if (direction.ToLower() == "vertical")
                        {
                            Statics[_StaticPointsBar[p, CGame.NumPlayer - 1]].Rect.W = ((float)_StaticPointsBarDrawnPoints[p]) *
                                                                                      (Statics[_StaticPointsBarBG[p, CGame.NumPlayer - 1]].Rect.W / 10000);
                        }
                        else
                        {
                            Statics[_StaticPointsBar[p, CGame.NumPlayer - 1]].Rect.H = ((float)_StaticPointsBarDrawnPoints[p]) *
                                                                                      (Statics[_StaticPointsBarBG[p, CGame.NumPlayer - 1]].Rect.H / 10000);
                            Statics[_StaticPointsBar[p, CGame.NumPlayer - 1]].Rect.Y = Statics[_StaticPointsBarBG[p, CGame.NumPlayer - 1]].Rect.H +
                                                                                      Statics[_StaticPointsBarBG[p, CGame.NumPlayer - 1]].Rect.Y -
                                                                                      Statics[_StaticPointsBar[p, CGame.NumPlayer - 1]].Rect.H;
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

        public string GetRating(double points)
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
                        string target = "P" + (player + 1).ToString() + "N" + (numplayer + 1).ToString();
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
                    if (player <= numplayer)
                    {
                        string target = "P" + (player + 1).ToString() + "N" + (numplayer + 1).ToString();
                        _StaticPointsBarBG[player, numplayer] = "StaticPointsBarBG" + target;
                        _StaticPointsBar[player, numplayer] = "StaticPointsBar" + target;
                        _StaticAvatar[player, numplayer] = "StaticAvatar" + target;

                        statics.Add(_StaticPointsBarBG[player, numplayer]);
                        statics.Add(_StaticPointsBar[player, numplayer]);
                        statics.Add(_StaticAvatar[player, numplayer]);
                    }
                }
            }
        }

        private void _UpdateRatings()
        {
            CSong song = null;
            SPlayer[] player = new SPlayer[CGame.NumPlayer];
            if (_Round != 0)
            {
                song = CGame.GetSong(_Round);
                if (song == null)
                    return;

                Texts[_TextSong].Text = song.Artist + " - " + song.Title;
                if (_Points.NumRounds > 1)
                    Texts[_TextSong].Text += " (" + _Round + "/" + _Points.NumRounds + ")";
                player = _Points.GetPlayer(_Round - 1, CGame.NumPlayer);
            }
            else
            {
                Texts[_TextSong].Text = "TR_SCREENSCORE_OVERALLSCORE";
                for (int i = 0; i < CGame.NumRounds; i++)
                {
                    SPlayer[] points = _Points.GetPlayer(i, CGame.NumPlayer);
                    for (int p = 0; p < player.Length; p++)
                    {
                        if (i < 1)
                        {
                            player[p].ProfileID = points[p].ProfileID;
                            player[p].Name = points[p].Name;
                            player[p].Difficulty = points[p].Difficulty;
                        }
                        player[p].Points += points[p].Points;
                    }
                }
                for (int p = 0; p < player.Length; p++)
                    player[p].Points = (int)Math.Round(player[p].Points / CGame.NumRounds);
            }

            for (int p = 0; p < player.Length; p++)
            {
                if (song != null)
                {
                    if (!song.IsDuet)
                        Texts[_TextNames[p, CGame.NumPlayer - 1]].Text = player[p].Name;
                    else if (player[p].LineNr == 0 && song.DuetPart1 != "Part 1")
                        Texts[_TextNames[p, CGame.NumPlayer - 1]].Text = player[p].Name + " (" + song.DuetPart1 + ")";
                    else if (player[p].LineNr == 1 && song.DuetPart2 != "Part 2")
                        Texts[_TextNames[p, CGame.NumPlayer - 1]].Text = player[p].Name + " (" + song.DuetPart2 + ")";
                    else
                        Texts[_TextNames[p, CGame.NumPlayer - 1]].Text = player[p].Name;
                }
                else
                    Texts[_TextNames[p, CGame.NumPlayer - 1]].Text = player[p].Name;

                if (CGame.NumPlayer < (int)ScreenSettings[_ScreenSettingShortScore].GetValue())
                    Texts[_TextScores[p, CGame.NumPlayer - 1]].Text = ((int)Math.Round(player[p].Points)).ToString("0000") + " " + CLanguage.Translate("TR_SCREENSCORE_POINTS");
                else
                    Texts[_TextScores[p, CGame.NumPlayer - 1]].Text = ((int)Math.Round(player[p].Points)).ToString("0000");
                if (CGame.NumPlayer < (int)ScreenSettings[_ScreenSettingShortDifficulty].GetValue())
                {
                    Texts[_TextDifficulty[p, CGame.NumPlayer - 1]].Text = CLanguage.Translate("TR_SCREENSCORE_GAMEDIFFICULTY") + ": " +
                                                                         CLanguage.Translate(player[p].Difficulty.ToString());
                }
                else
                    Texts[_TextDifficulty[p, CGame.NumPlayer - 1]].Text = CLanguage.Translate(player[p].Difficulty.ToString());
                if (CGame.NumPlayer < (int)ScreenSettings[_ScreenSettingShortRating].GetValue())
                {
                    Texts[_TextRatings[p, CGame.NumPlayer - 1]].Text = CLanguage.Translate("TR_SCREENSCORE_RATING") + ": " +
                                                                      CLanguage.Translate(GetRating((int)Math.Round(player[p].Points)));
                }
                else
                    Texts[_TextRatings[p, CGame.NumPlayer - 1]].Text = CLanguage.Translate(GetRating((int)Math.Round(player[p].Points)));

                _StaticPointsBarDrawnPoints[p] = 0.0;
                string direction = (string)ScreenSettings[_ScreenSettingAnimationDirection].GetValue();
                if (direction.ToLower() == "vertical")
                    Statics[_StaticPointsBar[p, CGame.NumPlayer - 1]].Rect.W = 0;
                else
                {
                    Statics[_StaticPointsBar[p, CGame.NumPlayer - 1]].Rect.H = 0;
                    Statics[_StaticPointsBar[p, CGame.NumPlayer - 1]].Rect.Y = Statics[_StaticPointsBarBG[p, CGame.NumPlayer - 1]].Rect.H +
                                                                              Statics[_StaticPointsBarBG[p, CGame.NumPlayer - 1]].Rect.Y -
                                                                              Statics[_StaticPointsBar[p, CGame.NumPlayer - 1]].Rect.H;
                }
                if (player[p].ProfileID >= 0 && player[p].ProfileID < CProfiles.NumProfiles)
                    Statics[_StaticAvatar[p, CGame.NumPlayer - 1]].Texture = CProfiles.Profiles[player[p].ProfileID].Avatar.Texture;
            }

            if (CConfig.ScoreAnimationTime < 1)
            {
                for (int p = 0; p < CGame.NumPlayer; p++)
                {
                    Statics[_StaticPointsBar[p, CGame.NumPlayer - 1]].Rect.H = ((float)player[p].Points) * (Statics[_StaticPointsBarBG[p, CGame.NumPlayer - 1]].Rect.H / 10000);
                    Statics[_StaticPointsBar[p, CGame.NumPlayer - 1]].Rect.Y = Statics[_StaticPointsBarBG[p, CGame.NumPlayer - 1]].Rect.H +
                                                                              Statics[_StaticPointsBarBG[p, CGame.NumPlayer - 1]].Rect.Y -
                                                                              Statics[_StaticPointsBar[p, CGame.NumPlayer - 1]].Rect.H;
                    _StaticPointsBarDrawnPoints[p] = player[p].Points;
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
                        Texts[_TextNames[player, numplayer]].Visible = numplayer + 1 == CGame.NumPlayer;
                        Texts[_TextScores[player, numplayer]].Visible = numplayer + 1 == CGame.NumPlayer;
                        Texts[_TextRatings[player, numplayer]].Visible = numplayer + 1 == CGame.NumPlayer;
                        Texts[_TextDifficulty[player, numplayer]].Visible = numplayer + 1 == CGame.NumPlayer;
                        Statics[_StaticPointsBar[player, numplayer]].Visible = numplayer + 1 == CGame.NumPlayer;
                        Statics[_StaticPointsBarBG[player, numplayer]].Visible = numplayer + 1 == CGame.NumPlayer;
                        Statics[_StaticAvatar[player, numplayer]].Visible = numplayer + 1 == CGame.NumPlayer;

                        Statics[_StaticAvatar[player, numplayer]].Texture = new STexture(-1);
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