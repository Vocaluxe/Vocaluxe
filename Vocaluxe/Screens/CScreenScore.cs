﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.Menu;
using Vocaluxe.GameModes;

using Vocaluxe.Lib.Song;

namespace Vocaluxe.Screens
{
    class CScreenScore : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        const int ScreenVersion = 2;

        private const string TextSong = "TextSong";

        private string[,] TextNames;
        private string[,] TextScores;
        private string[,] TextRatings;
        private string[,] TextDifficulty;
        private string[,] StaticPointsBarBG;
        private string[,] StaticPointsBar;
        private string[,] StaticAvatar;
        private double[] StaticPointsBarDrawnPoints;
        private int _Round;
        private CPoints _Points;
        private Stopwatch timer;

        public CScreenScore()
        {
            Init();
        }

        protected override void Init()
        {
            base.Init();

            _ThemeName = "ScreenScore";
            _ScreenVersion = ScreenVersion;

            List<string> texts = new List<string>();
            texts.Add(TextSong);

            BuildTextStrings(ref texts);

            _ThemeTexts = texts.ToArray();

            List<string> statics = new List<string>();
            BuildStaticStrings(ref statics);

            _ThemeStatics = statics.ToArray();

            StaticPointsBarDrawnPoints = new double[CSettings.MaxNumPlayer];
        }

        public override bool HandleInput(KeyEvent KeyEvent)
        {
            if (KeyEvent.KeyPressed)
            {
            }
            else
            {
                switch (KeyEvent.Key)
                {
                    case Keys.Escape:
                    case Keys.Back:
                        CGraphics.FadeTo(EScreens.ScreenHighscore);
                        break;

                    case Keys.Enter:
                        CGraphics.FadeTo(EScreens.ScreenHighscore);
                        break;

                    case Keys.Left:
                        ChangeRound(-1);
                        break;

                    case Keys.Right:
                        ChangeRound(1);
                        break;
                }
            }
            return true;
        }

        public override bool HandleMouse(MouseEvent MouseEvent)
        {
            base.HandleMouse(MouseEvent);

            if (MouseEvent.LB)
            {
                CGraphics.FadeTo(EScreens.ScreenHighscore);
            }

            if (MouseEvent.RB)
            {
                CGraphics.FadeTo(EScreens.ScreenHighscore);
            }

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();
            _Round = 1;
            _Points = CGame.GetPoints();

            SetVisuability();
            UpdateRatings();
        }

        public override bool UpdateGame()
        {

            SPlayer[] player = new SPlayer[CGame.NumPlayer];
            player = _Points.GetPlayer(_Round - 1, CGame.NumPlayer);
            for (int p = 0; p < player.Length; p++)
            {
                if (StaticPointsBarDrawnPoints[p] < player[p].Points)
                {
                    if (CConfig.ScoreAnimationTime >= 1)
                    {
                        StaticPointsBarDrawnPoints[p] = (timer.ElapsedMilliseconds / 1000f) / CConfig.ScoreAnimationTime * 10000;


                        if (StaticPointsBarDrawnPoints[p] > player[p].Points)
                        {
                            StaticPointsBarDrawnPoints[p] = player[p].Points;
                        }
                        Statics[htStatics(StaticPointsBar[p, CGame.NumPlayer - 1])].Rect.H = ((float)StaticPointsBarDrawnPoints[p]) * (Statics[htStatics(StaticPointsBarBG[p, CGame.NumPlayer - 1])].Rect.H / 10000);
                        Statics[htStatics(StaticPointsBar[p, CGame.NumPlayer - 1])].Rect.Y = Statics[htStatics(StaticPointsBarBG[p, CGame.NumPlayer - 1])].Rect.H + Statics[htStatics(StaticPointsBarBG[p, CGame.NumPlayer - 1])].Rect.Y - Statics[htStatics(StaticPointsBar[p, CGame.NumPlayer - 1])].Rect.H;
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
            {
                rating = "TR_RATING_SUPERSTAR";
            }
            else if (points < 8400 && points >= 7000)
            {
                rating = "TR_RATING_LEAD_SINGER";
            }
            else if (points < 7000 && points >= 5600)
            {
                rating = "TR_RATING_RISING_STAR";
            }
            else if (points < 5600 && points >= 4200)
            {
                rating = "TR_RATING_HOPEFUL";
            }
            else if (points < 4200 && points >= 2800)
            {
                rating = "TR_RATING_WANNABE";
            }
            else if (points < 2800 && points >= 1400)
            {
                rating = "TR_RATING_AMATEUR";
            }
            else if (points < 1400)
            {
                rating = "TR_RATING_TONE_DEAF";
            }

            return rating;
        }

        private void BuildTextStrings(ref List<string> texts)
        {
            TextNames = new string[CSettings.MaxNumPlayer, CSettings.MaxNumPlayer];
            TextScores = new string[CSettings.MaxNumPlayer, CSettings.MaxNumPlayer];
            TextRatings = new string[CSettings.MaxNumPlayer, CSettings.MaxNumPlayer];
            TextDifficulty = new string[CSettings.MaxNumPlayer, CSettings.MaxNumPlayer];

            for (int numplayer = 0; numplayer < CSettings.MaxNumPlayer; numplayer++)
            {
                for (int player = 0; player < CSettings.MaxNumPlayer; player++)
                {
                    if (player <= numplayer)
                    {
                        string target = "P" + (player + 1).ToString() + "N" + (numplayer + 1).ToString();
                        TextNames[player, numplayer] = "TextName" + target;
                        TextScores[player, numplayer] = "TextScore" + target;
                        TextRatings[player, numplayer] = "TextRating" + target;
                        TextDifficulty[player, numplayer] = "TextDifficulty" + target;

                        texts.Add(TextNames[player, numplayer]);
                        texts.Add(TextScores[player, numplayer]);
                        texts.Add(TextRatings[player, numplayer]);
                        texts.Add(TextDifficulty[player, numplayer]);
                    }
                }
            }
        }

        private void BuildStaticStrings(ref List<string> statics)
        {
            StaticPointsBar = new string[CSettings.MaxNumPlayer, CSettings.MaxNumPlayer];
            StaticPointsBarBG = new string[CSettings.MaxNumPlayer, CSettings.MaxNumPlayer];
            StaticAvatar = new string[CSettings.MaxNumPlayer, CSettings.MaxNumPlayer];

            for (int numplayer = 0; numplayer < CSettings.MaxNumPlayer; numplayer++)
            {
                for (int player = 0; player < CSettings.MaxNumPlayer; player++)
                {
                    if (player <= numplayer)
                    {
                        string target = "P" + (player + 1).ToString() + "N" + (numplayer + 1).ToString();
                        StaticPointsBarBG[player, numplayer] = "StaticPointsBarBG" + target;
                        StaticPointsBar[player, numplayer] = "StaticPointsBar" + target;
                        StaticAvatar[player, numplayer] = "StaticAvatar" + target;

                        statics.Add(StaticPointsBarBG[player, numplayer]);
                        statics.Add(StaticPointsBar[player, numplayer]);
                        statics.Add(StaticAvatar[player, numplayer]);

                    }
                }
            }
        }

        private void UpdateRatings()
        {
            CSong song = CGame.GetSong(_Round);
            if (song == null)
                return;

            Texts[htTexts(TextSong)].Text = song.Artist + " - " + song.Title;
            if (_Points.NumRounds > 1)
            {
                Texts[htTexts(TextSong)].Text += " (" + _Round + "/" + _Points.NumRounds + ")";
            }

            SPlayer[] player = new SPlayer[CGame.NumPlayer];
            player = _Points.GetPlayer(_Round - 1, CGame.NumPlayer);
            for (int p = 0; p < player.Length; p++)
            {
                if (!song.IsDuet)
                    Texts[htTexts(TextNames[p, CGame.NumPlayer - 1])].Text = player[p].Name;
                else
                    if(player[p].LineNr == 0 && song.DuetPart1 != "Part 1")
                        Texts[htTexts(TextNames[p, CGame.NumPlayer - 1])].Text = player[p].Name + " (" + song.DuetPart1 + ")";
                    else if(player[p].LineNr == 1 && song.DuetPart2 != "Part 2")
                        Texts[htTexts(TextNames[p, CGame.NumPlayer - 1])].Text = player[p].Name + " (" + song.DuetPart2 + ")";
                    else
                        Texts[htTexts(TextNames[p, CGame.NumPlayer - 1])].Text = player[p].Name;

                Texts[htTexts(TextScores[p, CGame.NumPlayer - 1])].Text = ((int)Math.Round(player[p].Points)).ToString("0000") + " " + CLanguage.Translate("TR_SCREENSCORE_POINTS");
                if (CGame.NumPlayer <= 3)
                {
                    Texts[htTexts(TextRatings[p, CGame.NumPlayer - 1])].Text = CLanguage.Translate("TR_SCREENSCORE_RATING") + ": " + CLanguage.Translate(GetRating((int)Math.Round(player[p].Points)));
                    Texts[htTexts(TextDifficulty[p, CGame.NumPlayer - 1])].Text = CLanguage.Translate("TR_SCREENSCORE_GAMEDIFFICULTY") + ": " + CLanguage.Translate(player[p].Difficulty.ToString());
                }
                else
                {
                    Texts[htTexts(TextRatings[p, CGame.NumPlayer - 1])].Text = CLanguage.Translate(GetRating((int)Math.Round(player[p].Points)));
                    Texts[htTexts(TextDifficulty[p, CGame.NumPlayer - 1])].Text = CLanguage.Translate(player[p].Difficulty.ToString());
                }
                StaticPointsBarDrawnPoints[p] = 0.0;
                Statics[htStatics(StaticPointsBar[p, CGame.NumPlayer - 1])].Rect.H = 0;
                Statics[htStatics(StaticPointsBar[p, CGame.NumPlayer - 1])].Rect.Y = Statics[htStatics(StaticPointsBarBG[p, CGame.NumPlayer - 1])].Rect.H + Statics[htStatics(StaticPointsBarBG[p, CGame.NumPlayer - 1])].Rect.Y - Statics[htStatics(StaticPointsBar[p, CGame.NumPlayer - 1])].Rect.H;
                if (player[p].ProfileID >= 0 && player[p].ProfileID < CProfiles.NumProfiles)
                    Statics[htStatics(StaticAvatar[p, CGame.NumPlayer - 1])].Texture = CProfiles.Profiles[player[p].ProfileID].Avatar.Texture;
            }

            if (CConfig.ScoreAnimationTime < 1)
            {
                for (int p = 0; p < CGame.NumPlayer; p++)
                {
                    Statics[htStatics(StaticPointsBar[p, CGame.NumPlayer - 1])].Rect.H = ((float)player[p].Points) * (Statics[htStatics(StaticPointsBarBG[p, CGame.NumPlayer - 1])].Rect.H / 10000);
                    Statics[htStatics(StaticPointsBar[p, CGame.NumPlayer - 1])].Rect.Y = Statics[htStatics(StaticPointsBarBG[p, CGame.NumPlayer - 1])].Rect.H + Statics[htStatics(StaticPointsBarBG[p, CGame.NumPlayer - 1])].Rect.Y - Statics[htStatics(StaticPointsBar[p, CGame.NumPlayer - 1])].Rect.H;
                    StaticPointsBarDrawnPoints[p] = player[p].Points;
                }
            }

            timer = new Stopwatch();
            timer.Start();
        }
        
        private void SetVisuability()
        {
            for (int numplayer = 0; numplayer < CSettings.MaxNumPlayer; numplayer++)
            {
                for (int player = 0; player < CSettings.MaxNumPlayer; player++)
                {
                    if (player <= numplayer)
                    {
                        Texts[htTexts(TextNames[player, numplayer])].Visible = (numplayer + 1 == CGame.NumPlayer);
                        Texts[htTexts(TextScores[player, numplayer])].Visible = (numplayer + 1 == CGame.NumPlayer);
                        Texts[htTexts(TextRatings[player, numplayer])].Visible = (numplayer + 1 == CGame.NumPlayer);
                        Texts[htTexts(TextDifficulty[player, numplayer])].Visible = (numplayer + 1 == CGame.NumPlayer);
                        Statics[htStatics(StaticPointsBar[player, numplayer])].Visible = (numplayer + 1 == CGame.NumPlayer);
                        Statics[htStatics(StaticPointsBarBG[player, numplayer])].Visible = (numplayer + 1 == CGame.NumPlayer);
                        Statics[htStatics(StaticAvatar[player, numplayer])].Visible = (numplayer + 1 == CGame.NumPlayer);

                        Statics[htStatics(StaticAvatar[player, numplayer])].Texture = new Lib.Draw.STexture(-1);
                    }
                }
            }
        }

        private void ChangeRound(int Num)
        {
            if ((_Round + Num) <= _Points.NumRounds && (_Round + Num) > 0)
            {
                _Round += Num;
                UpdateRatings();
            }
        }
    }
}