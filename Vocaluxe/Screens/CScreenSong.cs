using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.Lib.Draw;
using Vocaluxe.Menu;
using Vocaluxe.Menu.SongMenu;

namespace Vocaluxe.Screens
{
    class CScreenSong : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        const int ScreenVersion = 1;

        private const string TextCategory = "TextCategory";
        private const string TextSelection = "TextSelection";
        private const string TextSearchBarTitle = "TextSearchBarTitle";
        private const string TextSearchBar = "TextSearchBar";

        private const string StaticSearchBar = "StaticSearchBar";
        private const string SongMenu = "SongMenu";

        private string _SearchText = String.Empty;
        private bool _SearchActive = false;

        public CScreenSong()
        {
            Init();
        }

        protected override void Init()
        {
            base.Init();

            _ThemeName = "ScreenSong";
            _ScreenVersion = ScreenVersion;
            _ThemeStatics = new string[] { StaticSearchBar };
            _ThemeTexts = new string[] { TextCategory, TextSelection, TextSearchBarTitle, TextSearchBar };
            _ThemeSongMenus = new string[] { SongMenu };
        }

        public override void LoadTheme()
        {
            base.LoadTheme();
        }

        public override bool HandleInput(KeyEvent KeyEvent)
        {
            base.HandleInput(KeyEvent);

            if (KeyEvent.KeyPressed && !Char.IsControl(KeyEvent.Unicode) && _SearchActive)
            {
                ApplyNewSearchFilter(_SearchText + KeyEvent.Unicode);
            }
            else
            {
                SongMenus[htSongMenus(SongMenu)].HandleInput(ref KeyEvent);

                if (KeyEvent.Handled)
                    return true;

                switch (KeyEvent.Key)
                {
                    case Keys.Escape:
                        if (CSongs.Category < 0 || CConfig.Tabs == EOffOn.TR_CONFIG_OFF)
                            CGraphics.FadeTo(EScreens.ScreenMain);
                        break;
                        
                    case Keys.Enter:
                        if (CSongs.NumVisibleSongs > 0)
                            StartSong(SongMenus[htSongMenus(SongMenu)].GetSelectedSong());
                        break;

                    case Keys.Back:
                        if (_SearchText.Length > 0)
                        {
                            ApplyNewSearchFilter(_SearchText.Remove(_SearchText.Length - 1));
                        }

                        if (!_SearchActive && CSongs.Category < 0)
                            CGraphics.FadeTo(EScreens.ScreenMain);

                        break;

                    case Keys.F3:
                        if (_SearchActive)
                        {
                            _SearchActive = false;
                            _SearchText = String.Empty;
                            ApplyNewSearchFilter(_SearchText);
                        }
                        else
                        {
                            _SearchActive = true;
                        }
                        break;

                    case Keys.A:
                        if (!_SearchActive && KeyEvent.Mod == Modifier.None)
                        {
                            StartRandomAllSongs();
                        }
                        if (KeyEvent.Mod == Modifier.Ctrl)
                        {
                            StartRandomVisibleSongs();
                        }
                        break;

                    case Keys.F:
                        if (KeyEvent.Mod == Modifier.Ctrl){
                            if (_SearchActive)
                            {
                                _SearchActive = false;
                                _SearchText = String.Empty;
                                ApplyNewSearchFilter(_SearchText);
                            }
                            else
                            {
                                _SearchActive = true;
                            }
                        }
                        break;

                    case Keys.R:
                        if (CSongs.Category != -1)
                        {
                            SongMenus[htSongMenus(SongMenu)].SetSelectedSong(CSongs.GetRandomSong());
                        }
                        break;
                }
            }

            return true;
        }

        public override bool HandleMouse(MouseEvent MouseEvent)
        {
            base.HandleMouse(MouseEvent);

            if ((MouseEvent.RB) && (CSongs.Category < 0))
            {
                CGraphics.FadeTo(EScreens.ScreenMain);
            }

            SongMenus[htSongMenus(SongMenu)].HandleMouse(ref MouseEvent);
            if (MouseEvent.LB && CSongs.NumVisibleSongs > 0 && SongMenus[htSongMenus(SongMenu)].GetActualSelection() != -1)
            {
                StartSong(SongMenus[htSongMenus(SongMenu)].GetSelectedSong());
            }
             
            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            SongMenus[htSongMenus(SongMenu)].OnShow();
        }

        public override bool UpdateGame()
        {
            SongMenus[htSongMenus(SongMenu)].Update();
            Texts[htTexts(TextCategory)].Text = CSongs.GetActualCategoryName();

            int song = SongMenus[htSongMenus(SongMenu)].GetActualSelection();
            if ((CSongs.Category >= 0) && (song >= 0) && song < CSongs.VisibleSongs.Length)
            {
                Texts[htTexts(TextSelection)].Text = CSongs.VisibleSongs[song].Artist + " - " + CSongs.VisibleSongs[song].Title;
                CBackgroundMusic.Disable();
            }
            else if ((CSongs.Category == -1) && (song >= 0) && song < CSongs.Categories.Length)
            {
                Texts[htTexts(TextSelection)].Text = CSongs.Categories[song].Name;
                CBackgroundMusic.Enable();
            }
            else
                Texts[htTexts(TextSelection)].Text = String.Empty;

            Texts[htTexts(TextSearchBar)].Text = _SearchText;
            if (_SearchActive)
            {
                Texts[htTexts(TextSearchBar)].Text += '|';

                Texts[htTexts(TextSearchBar)].Visible = true;
                Texts[htTexts(TextSearchBarTitle)].Visible = true;
                Statics[htStatics(StaticSearchBar)].Visible = true;
            }
            else
            {
                Texts[htTexts(TextSearchBar)].Visible = false;
                Texts[htTexts(TextSearchBarTitle)].Visible = false;
                Statics[htStatics(StaticSearchBar)].Visible = false;
            }

            return true;
        }

        public override bool Draw()
        {
            base.Draw();
            SongMenus[htSongMenus(SongMenu)].Draw();

            return true;
        }

        public override void OnClose()
        {
            base.OnClose();
            CBackgroundMusic.Enable();
            SongMenus[htSongMenus(SongMenu)].OnHide();
        }

        private void StartSong(int SongNr)
        {
            if ((CSongs.Category >= 0) && (SongNr >= 0))
            {
                if (CSongs.VisibleSongs[SongNr].IsDuet)
                    CGame.SetGameMode(GameModes.EGameMode.Duet);
                else
                    CGame.SetGameMode(GameModes.EGameMode.Normal);

                CGame.Reset();
                CGame.ClearSongs();
                CGame.AddVisibleSong(SongNr);
                //CGame.AddSong(SongNr+1);

                CGraphics.FadeTo(EScreens.ScreenNames);
            }
        }

        private void StartRandomAllSongs()
        {
            CGame.Reset();
            CGame.ClearSongs();
            CGame.SetGameMode(GameModes.EGameMode.Normal);

            List<int> IDs = new List<int>();
            for (int i = 0; i < CSongs.AllSongs.Length; i++)
            {
                IDs.Add(i);
            }

            while (IDs.Count > 0)
            {
                int SongNr = IDs[CGame.Rand.Next(IDs.Count)];

                if (!CSongs.AllSongs[SongNr].IsDuet)
                {
                    CGame.AddSong(SongNr);
                }
                IDs.Remove(SongNr);    
            }

            if (CGame.GetNumSongs() > 0)
                CGraphics.FadeTo(EScreens.ScreenNames);
        }

        private void StartRandomVisibleSongs()
        {
            CGame.Reset();
            CGame.ClearSongs();
            CGame.SetGameMode(GameModes.EGameMode.Normal);

            List<int> IDs = new List<int>();
            for (int i = 0; i < CSongs.VisibleSongs.Length; i++)
            {
                IDs.Add(CSongs.VisibleSongs[i].ID);
            }

            while (IDs.Count > 0)
            {
                int SongNr = IDs[CGame.Rand.Next(IDs.Count)];

                if (!CSongs.AllSongs[SongNr].IsDuet)
                {
                    CGame.AddSong(SongNr);
                }
                IDs.Remove(SongNr);
            }

            if (CGame.GetNumSongs() > 0)
                CGraphics.FadeTo(EScreens.ScreenNames);
        }

        private void ApplyNewSearchFilter(string NewFilterString)
        {
            int song = SongMenus[htSongMenus(SongMenu)].GetSelectedSong();
            int id = -1;
            if (song > -1 && song < CSongs.NumVisibleSongs)
            {
                id = CSongs.VisibleSongs[song].ID;
            }

            _SearchText = NewFilterString;
            CSongs.SearchFilter = _SearchText;

            if (NewFilterString != String.Empty)
                CSongs.Category = 0;
            else
                CSongs.Category = -1;

            if (id > -1)
            {
                if (CSongs.NumVisibleSongs > 0)
                {
                    if (id != CSongs.VisibleSongs[0].ID)
                        SongMenus[htSongMenus(SongMenu)].OnHide();
                }
            }

            if (CSongs.NumVisibleSongs == 0)
                SongMenus[htSongMenus(SongMenu)].OnHide();

            SongMenus[htSongMenus(SongMenu)].OnShow();
        }
    }
}
