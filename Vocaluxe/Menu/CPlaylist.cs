using System;
using System.Drawing;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.Lib.Draw;
using Vocaluxe.Lib.Song;
using Vocaluxe.GameModes;

namespace Vocaluxe.Menu
{

    struct SThemePlaylist
    {
        public string Name;

        public string ColorBackgroundName;
        public string SColorBackgroundName;

        public string TextureBackgroundName;
        public string STextureBackgroundName;

        public float EntryHeight;

        public CText Text1;
        public CText TextPlaylistHeader;

        public CStatic StaticCover;
        public CStatic StaticPlaylistHeader;
        public CStatic StaticPlaylistFooter;

        public CButton ButtonPlaylistClose;
        public CButton ButtonPlaylistSave;
        public CButton ButtonPlaylistDelete;
        public CButton ButtonPlaylistSing;

        public CSelectSlide SelectSlideGameMode;
    }

    class CPlaylist : IMenuElement
    {
        class PlaylistElementContent
        {
            public EGameMode[] Modes;
            public int SongID;
            public EGameMode Mode;
        }

        class PlaylistElement
        {
            public CStatic Cover;
            public CStatic Background;
            public CText Text1;
            public CSelectSlide SelectSlide;
            public int Content;
        }

        private SThemePlaylist _Theme;
        private bool _ThemeLoaded;

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        private CObjectInteractions _Interactions;

        private List<PlaylistElement> PlaylistElements;
        private List<PlaylistElementContent> PlaylistElementContents;

        public SRectF Rect;
        public SColorF BackgroundColor;
        public SColorF BackgroundSColor;

        public bool Visible;

        private bool _Selected;
        public bool Selected
        {
            get { return _Selected; }
            set
            {
                _Selected = value;
                _Interactions.Active = value;
                if (CurrentPlaylistElement == -1 && _Selected && PlaylistElementContents.Count > 0)
                {
                    CurrentPlaylistElement = 0;
                    ActiveHover = CurrentPlaylistElement;
                }
            }
        }

        public bool changeOrder = false;
        public int ActivePlaylistID = -1;
        public int Offset = 0;
        public int ActiveHover = -1;
        public int CurrentPlaylistElement = -1;

        public CPlaylist()
        {
            _Theme = new SThemePlaylist();
            _Theme.Text1 = new CText();
            _Theme.TextPlaylistHeader = new CText();
            _Theme.StaticCover = new CStatic();
            _Theme.StaticPlaylistFooter = new CStatic();
            _Theme.StaticPlaylistHeader = new CStatic();
            _Theme.ButtonPlaylistClose = new CButton();
            _Theme.ButtonPlaylistDelete = new CButton();
            _Theme.ButtonPlaylistSave = new CButton();
            _Theme.ButtonPlaylistSing = new CButton();
            _Theme.SelectSlideGameMode = new CSelectSlide();

            Rect = new SRectF();
            BackgroundColor = new SColorF();
            BackgroundSColor = new SColorF();

            PlaylistElements = new List<PlaylistElement>();
            PlaylistElementContents = new List<PlaylistElementContent>();

            _Interactions = new CObjectInteractions();

            Visible = false;
            Selected = false;    
        }

        public void Init()
        {
            _Interactions.Clear();
            PrepareList();
            _Interactions.AddText(_Theme.TextPlaylistHeader);
            _Interactions.AddStatic(_Theme.StaticCover);
            _Interactions.AddStatic(_Theme.StaticPlaylistFooter);
            _Interactions.AddStatic(_Theme.StaticPlaylistHeader);
            _Interactions.AddButton(_Theme.ButtonPlaylistClose);
            _Interactions.AddButton(_Theme.ButtonPlaylistDelete);
            _Interactions.AddButton(_Theme.ButtonPlaylistSave);
            _Interactions.AddButton(_Theme.ButtonPlaylistSing);
        }

        public bool LoadTheme(string XmlPath, string ElementName, XPathNavigator navigator, int SkinIndex)
        {
            string item = XmlPath + "/" + ElementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/SkinBackground", navigator, ref _Theme.TextureBackgroundName, String.Empty);
            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/SkinBackgroundSelected", navigator, ref _Theme.STextureBackgroundName, String.Empty);

            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/X", navigator, ref Rect.X);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/Y", navigator, ref Rect.Y);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/Z", navigator, ref Rect.Z);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/W", navigator, ref Rect.W);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/H", navigator, ref Rect.H);
            if (CHelper.GetValueFromXML(item + "/ColorBackground", navigator, ref _Theme.ColorBackgroundName, String.Empty))
            {
                _ThemeLoaded &= CTheme.GetColor(_Theme.ColorBackgroundName, SkinIndex, ref BackgroundColor);
            }
            else
            {
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/BackgroundR", navigator, ref BackgroundColor.R);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/BackgroundG", navigator, ref BackgroundColor.G);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/BackgroundB", navigator, ref BackgroundColor.B);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/BackgroundA", navigator, ref BackgroundColor.A);
            }
            if (CHelper.GetValueFromXML(item + "/SColorBackground", navigator, ref _Theme.SColorBackgroundName, String.Empty))
            {
                _ThemeLoaded &= CTheme.GetColor(_Theme.SColorBackgroundName, SkinIndex, ref BackgroundSColor);
            }
            else
            {
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SBackgroundR", navigator, ref BackgroundSColor.R);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SBackgroundG", navigator, ref BackgroundSColor.G);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SBackgroundB", navigator, ref BackgroundSColor.B);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SBackgroundA", navigator, ref BackgroundSColor.A);
            }

            _ThemeLoaded &= _Theme.Text1.LoadTheme(item, "TextPart1", navigator, SkinIndex);
            _ThemeLoaded &= _Theme.TextPlaylistHeader.LoadTheme(item, "TextPlaylistHeader", navigator, SkinIndex);

            _ThemeLoaded &= _Theme.StaticCover.LoadTheme(item, "StaticCover", navigator, SkinIndex);
            _ThemeLoaded &= _Theme.StaticPlaylistHeader.LoadTheme(item, "StaticPlaylistHeader", navigator, SkinIndex);
            _ThemeLoaded &= _Theme.StaticPlaylistFooter.LoadTheme(item, "StaticPlaylistFooter", navigator, SkinIndex);

            _ThemeLoaded &= _Theme.ButtonPlaylistSing.LoadTheme(item, "ButtonPlaylistSing", navigator, SkinIndex);
            _ThemeLoaded &= _Theme.ButtonPlaylistClose.LoadTheme(item, "ButtonPlaylistClose", navigator, SkinIndex);
            _ThemeLoaded &= _Theme.ButtonPlaylistSave.LoadTheme(item, "ButtonPlaylistSave", navigator, SkinIndex);
            _ThemeLoaded &= _Theme.ButtonPlaylistDelete.LoadTheme(item, "ButtonPlaylistDelete", navigator, SkinIndex);

            _ThemeLoaded &= _Theme.SelectSlideGameMode.LoadTheme(item, "SelectSlideGameMode", navigator, SkinIndex);

            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/EntryHeight", navigator, ref _Theme.EntryHeight);
            if (_ThemeLoaded)
            {
                _Theme.Name = ElementName;
                LoadTextures();
            }
            return _ThemeLoaded;
        }

        public bool SaveTheme(XmlWriter writer)
        {
            if (_ThemeLoaded)
            {
                writer.WriteStartElement(_Theme.Name);

                writer.WriteComment("<X>, <Y>, <Z>, <W>, <H>: Playlist position, width and height");
                writer.WriteElementString("X", Rect.X.ToString("#0"));
                writer.WriteElementString("Y", Rect.Y.ToString("#0"));
                writer.WriteElementString("Z", Rect.Z.ToString("#0.00"));
                writer.WriteElementString("W", Rect.W.ToString("#0"));
                writer.WriteElementString("H", Rect.H.ToString("#0"));

                writer.WriteElementString("EntryHeight", _Theme.EntryHeight.ToString("#0.00"));

                writer.WriteComment("<SkinBackground>: Texture name");
                writer.WriteElementString("SkinBackground", _Theme.TextureBackgroundName);

                writer.WriteComment("<SkinBackgroundSelected>: Texture name for selected playlist-entry");
                writer.WriteElementString("SkinBackgroundSelected", _Theme.STextureBackgroundName);

                writer.WriteComment("<ColorBackground>: Button color from ColorScheme (high priority)");
                writer.WriteComment("or <BackgroundR>, <BackgroundG>, <BackgroundB>, <BackgroundA> (lower priority)");
                if (_Theme.ColorBackgroundName != String.Empty)
                {
                    writer.WriteElementString("ColorBackground", _Theme.ColorBackgroundName);
                }
                else
                {
                    writer.WriteElementString("BackgroundR", BackgroundColor.R.ToString("#0.00"));
                    writer.WriteElementString("BackgroundG", BackgroundColor.G.ToString("#0.00"));
                    writer.WriteElementString("BackgroundB", BackgroundColor.B.ToString("#0.00"));
                    writer.WriteElementString("BackgroundA", BackgroundColor.A.ToString("#0.00"));
                }

                writer.WriteComment("<SColorBackground>: Selected paylist-entry color from ColorScheme (high priority)");
                writer.WriteComment("or <SBackgroundR>, <SBackgroundG>, <SBackgroundB>, <SBackgroundA> (lower priority)");
                if (_Theme.SColorBackgroundName != String.Empty)
                {
                    writer.WriteElementString("SColorBackground", _Theme.SColorBackgroundName);
                }
                else
                {
                    writer.WriteElementString("SBackgroundR", BackgroundSColor.R.ToString("#0.00"));
                    writer.WriteElementString("SBackgroundG", BackgroundSColor.G.ToString("#0.00"));
                    writer.WriteElementString("SBackgroundB", BackgroundSColor.B.ToString("#0.00"));
                    writer.WriteElementString("SBackgroundA", BackgroundSColor.A.ToString("#0.00"));
                }

                writer.WriteComment("Positions of <TextPart1>, <TextPart2>, <TextPart3>, <StaticCover> and <SelectSlideGameMode> are relative to playlist-entry!");
                writer.WriteComment("Use placeholders for Text of <TextPart1>, <TextPart2> and <TextPart3>: %t, %a, %l");
                _Theme.Text1.SaveTheme(writer);
                _Theme.TextPlaylistHeader.SaveTheme(writer);
                _Theme.StaticPlaylistFooter.SaveTheme(writer);
                _Theme.StaticPlaylistHeader.SaveTheme(writer);
                _Theme.StaticCover.SaveTheme(writer);
                _Theme.SelectSlideGameMode.SaveTheme(writer);
                _Theme.ButtonPlaylistClose.SaveTheme(writer);
                _Theme.ButtonPlaylistDelete.SaveTheme(writer);
                _Theme.ButtonPlaylistSave.SaveTheme(writer);
                _Theme.ButtonPlaylistSing.SaveTheme(writer);                    

                writer.WriteEndElement();

                return true;
            }
            return false;
        }

        public void Draw()
        {
            Draw(false);
        }

        public void ForceDraw()
        {
            Draw(true);
        }

        public void Draw(bool ForceDraw)
        {
            if (PlaylistElements.Count <= 0)
            {
                LoadPlaylist(0);
            }
            if (!Visible && CSettings.GameState != EGameState.EditTheme && !ForceDraw)
                return;

            for (int i = 0; i < PlaylistElements.Count; i++ )
            {
                if (i == ActiveHover && _Selected)
                {
                    PlaylistElements[i].Background.Texture = CTheme.GetSkinTexture(_Theme.STextureBackgroundName);
                    PlaylistElements[i].Background.Color = BackgroundSColor;
                }
                else
                {
                    PlaylistElements[i].Background.Texture = CTheme.GetSkinTexture(_Theme.TextureBackgroundName);
                    PlaylistElements[i].Background.Color = BackgroundColor;
                }
            }

            _Interactions.Draw();
        }

        public bool IsMouseOver(MouseEvent MouseEvent)
        {
            return CHelper.IsInBounds(Rect, MouseEvent.X, MouseEvent.Y) || _Interactions.IsMouseOver(MouseEvent);
        }

        public void UnloadTextures()
        {
        }

        public void LoadTextures()
        {
            _Theme.Text1.LoadTextures();

            if (_Theme.ColorBackgroundName != String.Empty)
                BackgroundColor = CTheme.GetColor(_Theme.ColorBackgroundName);

            if (_Theme.SColorBackgroundName != String.Empty)
                BackgroundSColor = CTheme.GetColor(_Theme.SColorBackgroundName);
        }

        public void ReloadTextures()
        {
            UnloadTextures();
            LoadTextures();
        }

        public bool HandleInput(KeyEvent KeyEvent)
        {
            if (Selected)
            {
                int PrevPlaylistElement = CurrentPlaylistElement;
                int CurrPlaylistElement = -1;
                //_Interactions.HandleInput(KeyEvent);

                for (int i = 0; i < PlaylistElements.Count; i++)
                {
                    //Hover for playlist-element
                    if (PlaylistElementContents.Count - 1 >= i && PlaylistElements[i].SelectSlide.Selected)
                        CurrPlaylistElement = i;
                }

                switch (KeyEvent.Key)
                {
                    case Keys.Up:
                        if (CurrentPlaylistElement == -1 && PlaylistElementContents.Count > 0)
                        {
                            _Interactions.HandleInput(KeyEvent);
                            SetSelectionToLastEntry();
                            return true;
                        }
                        else if (CurrentPlaylistElement == 0 || PlaylistElementContents.Count == 0 || KeyEvent.ModSHIFT)
                        {
                            CurrentPlaylistElement = -1;
                            ActiveHover = CurrentPlaylistElement;
                            _Interactions.SetInteractionToSelectSlide(PlaylistElements[0].SelectSlide);
                            _Interactions.HandleInput(KeyEvent);

                            int selection = GetSelectedSelectionNr();
                            if (selection != -1 && PlaylistElementContents.Count > selection)
                            {
                                CurrentPlaylistElement = selection;
                                ActiveHover = CurrentPlaylistElement;
                            }
                            return true;
                        }
                        else if (CurrentPlaylistElement - 1 > 0)
                        {
                            _Interactions.SetInteractionToSelectSlide(PlaylistElements[CurrentPlaylistElement].SelectSlide);
                            _Interactions.HandleInput(KeyEvent);

                            int selection = GetSelectedSelectionNr();
                            
                            if (selection != -1 && PlaylistElementContents.Count > selection)
                            {
                                CurrentPlaylistElement = selection;
                                ActiveHover = CurrentPlaylistElement;
                            }
                            return true;
                        }
                        else if (CurrentPlaylistElement - 1 == 0)
                        {
                            if (Offset > 0)
                            {
                                Offset--;
                                Update();
                            }

                            _Interactions.SetInteractionToSelectSlide(PlaylistElements[CurrentPlaylistElement].SelectSlide);
                            _Interactions.HandleInput(KeyEvent);

                            int selection = GetSelectedSelectionNr();
                            if (selection != -1 && PlaylistElementContents.Count > selection)
                            {
                                CurrentPlaylistElement = selection;
                                ActiveHover = CurrentPlaylistElement;
                            }

                            return true;
                        }
                        return false;

                    case Keys.Down:
                        if (CurrentPlaylistElement == -1 && PlaylistElementContents.Count > 0)
                        {
                            _Interactions.HandleInput(KeyEvent);
                            SetSelectionToFirstEntry();
                            return true;
                        }
                        else if (CurrentPlaylistElement == PlaylistElements.Count -1 || PlaylistElementContents.Count == 0 || KeyEvent.ModSHIFT)
                        {
                            CurrentPlaylistElement = -1;
                            ActiveHover = CurrentPlaylistElement;

                            int sel = PlaylistElementContents.Count - 1;
                            if (sel > PlaylistElements.Count - 1)
                                sel = PlaylistElements.Count - 1;

                            _Interactions.SetInteractionToSelectSlide(PlaylistElements[sel].SelectSlide);
                            _Interactions.HandleInput(KeyEvent);

                            int selection = GetSelectedSelectionNr();
                            if (selection != -1 && PlaylistElementContents.Count > selection)
                            {
                                CurrentPlaylistElement = selection;
                                ActiveHover = CurrentPlaylistElement;
                            }
                            return true;
                        }
                        else if (CurrentPlaylistElement + 1 < PlaylistElements.Count - 1)
                        {
                            _Interactions.SetInteractionToSelectSlide(PlaylistElements[CurrentPlaylistElement].SelectSlide);
                            _Interactions.HandleInput(KeyEvent);

                            int selection = GetSelectedSelectionNr();

                            if (selection != -1 && PlaylistElementContents.Count > selection)
                            {
                                CurrentPlaylistElement = selection;
                                ActiveHover = CurrentPlaylistElement;
                            }
                            return true;
                        }
                        else if (CurrentPlaylistElement + 1 >= PlaylistElements.Count - 1 && CurrentPlaylistElement + Offset + 1 < PlaylistElementContents.Count)
                        {
                            if (PlaylistElementContents[CurrentPlaylistElement + Offset + 1].SongID != -1)
                            {
                                Offset++;
                                Update();  
                            }

                            _Interactions.SetInteractionToSelectSlide(PlaylistElements[CurrentPlaylistElement].SelectSlide);
                            _Interactions.HandleInput(KeyEvent);

                            int selection = GetSelectedSelectionNr();
                            if (selection != -1 && PlaylistElementContents.Count > selection)
                            {
                                CurrentPlaylistElement = selection;
                                ActiveHover = CurrentPlaylistElement;
                            }

                            return true;
                        }
                        return false;

                    case Keys.Delete:
                        if (CurrentPlaylistElement != -1)
                        {
                            CPlaylists.Playlists[ActivePlaylistID].DeleteSong(PlaylistElements[CurrentPlaylistElement].Content);
                            UpdatePlaylist();
                            if (CurrentPlaylistElement != -1)
                                _Interactions.SetInteractionToSelectSlide(PlaylistElements[CurrentPlaylistElement].SelectSlide);
                            return true;
                        }
                        return false;

                    case Keys.Back:
                        ClosePlaylist();
                        break;

                    case Keys.Enter:
                        StartPlaylistSongs();
                        break;

                    case Keys.PageUp:
                        if (CurrentPlaylistElement != -1)
                        {
                            CPlaylists.Playlists[ActivePlaylistID].SongDown(CurrentPlaylistElement+Offset);
                            if (CurrentPlaylistElement - 1 > -1)
                            {
                                CurrentPlaylistElement--;
                                ActiveHover = CurrentPlaylistElement;
                            }
                            else if(Offset > 0)
                            {
                                Offset--;
                            }
                            UpdatePlaylist();
                            _Interactions.SetInteractionToSelectSlide(PlaylistElements[CurrentPlaylistElement].SelectSlide);
                            return true;
                        }
                        break;

                    case Keys.PageDown:
                        if (CurrentPlaylistElement != -1)
                        {
                            CPlaylists.Playlists[ActivePlaylistID].SongUp(CurrentPlaylistElement+Offset);
                            if (CurrentPlaylistElement + 1 < PlaylistElements.Count)
                            {
                                CurrentPlaylistElement++;
                                ActiveHover = CurrentPlaylistElement;
                            }
                            else if (Offset + CurrentPlaylistElement < PlaylistElementContents.Count) 
                            {
                                Offset++;
                            }
                            UpdatePlaylist();
                            _Interactions.SetInteractionToSelectSlide(PlaylistElements[CurrentPlaylistElement].SelectSlide);
                            return true;
                        }
                        break;

                    case Keys.Left:
                        _Interactions.HandleInput(KeyEvent);
                        if (CurrentPlaylistElement != -1)
                        {
                            CPlaylists.Playlists[ActivePlaylistID].Songs[CurrentPlaylistElement + Offset].GameMode = PlaylistElementContents[CurrentPlaylistElement + Offset].Modes[PlaylistElements[CurrentPlaylistElement].SelectSlide.Selection];
                            UpdatePlaylist();
                            return true;
                        }
                        return false;

                    case Keys.Right:
                        _Interactions.HandleInput(KeyEvent);
                        if (CurrentPlaylistElement != -1)
                        {
                            CPlaylists.Playlists[ActivePlaylistID].Songs[CurrentPlaylistElement + Offset].GameMode = PlaylistElementContents[CurrentPlaylistElement + Offset].Modes[PlaylistElements[CurrentPlaylistElement].SelectSlide.Selection];
                            UpdatePlaylist();
                            return true;
                        }
                        return false;
                }
            }
            return false;
        }

        private void SetSelectionToLastEntry()
        {
            if (PlaylistElementContents.Count == 0)
                return;

            int selection = GetSelectedSelectionNr();
            if (selection == -1)
                return;

            int off = PlaylistElementContents.Count - PlaylistElements.Count;
            if (off >= 0)
            {
                Offset = off;
                Update();
            }

            if (PlaylistElementContents.Count > selection)
            {
                CurrentPlaylistElement = selection;
                ActiveHover = CurrentPlaylistElement;
            }
        }

        private void SetSelectionToFirstEntry()
        {
            if (PlaylistElementContents.Count == 0)
                return;

            int selection = GetSelectedSelectionNr();
            if (selection == -1)
                return;

            Offset = 0;
            Update();

            if (PlaylistElementContents.Count > selection)
            {
                CurrentPlaylistElement = selection;
                ActiveHover = CurrentPlaylistElement;
            }
        }

        private int GetSelectedSelectionNr()
        {
            for (int i = 0; i < PlaylistElements.Count; i++)
            {
                if (PlaylistElements[i].SelectSlide.Selected)
                    return i;
            }
            return -1;
        }

        public bool HandleMouse(MouseEvent MouseEvent)
        {
            _Interactions.HandleMouse(MouseEvent);

            if (CHelper.IsInBounds(Rect, MouseEvent) && Visible)
            {
                if (MouseEvent.Wheel > 0)
                {
                    if (PlaylistElements.Count + Offset + MouseEvent.Wheel <= PlaylistElementContents.Count)
                    {
                        Offset += MouseEvent.Wheel;
                        Update();
                        return true;
                    }
                    return true;
                }
                else if (MouseEvent.Wheel < 0)
                {
                    if (Offset + MouseEvent.Wheel >= 0)
                    {
                        Offset += MouseEvent.Wheel;
                        Update();
                        return true;
                    }
                }

                if (MouseEvent.LB)
                {
                    if (CurrentPlaylistElement != -1)
                    {
                        CPlaylists.Playlists[ActivePlaylistID].Songs[CurrentPlaylistElement + Offset].GameMode = PlaylistElementContents[CurrentPlaylistElement + Offset].Modes[PlaylistElements[CurrentPlaylistElement].SelectSlide.Selection];
                        UpdatePlaylist();
                        return true;
                    }
                }

                for (int i = 0; i < PlaylistElements.Count; i++)
                { 
                    //Hover for playlist-element
                    if (PlaylistElementContents.Count - 1 >= i && CHelper.IsInBounds(PlaylistElements[i].Background.Rect, MouseEvent))
                    {
                        ActiveHover = i;
                        CurrentPlaylistElement = i;
                    }

                    //Delete Entry with RB
                    if (CHelper.IsInBounds(PlaylistElements[i].Background.Rect, MouseEvent) && MouseEvent.RB && PlaylistElements[i].Content != -1)
                    {
                        CPlaylists.Playlists[ActivePlaylistID].DeleteSong(PlaylistElements[i].Content);
                        UpdatePlaylist();
                        return true;
                    }
                    //Change order with holding LB
                    else if (CHelper.IsInBounds(PlaylistElements[i].Background.Rect, MouseEvent) && MouseEvent.LBH && PlaylistElements[i].Content != -1 && !changeOrder)
                    {
                        changeOrder = true;
                        CurrentPlaylistElement = i;
                    }
                    else if (CHelper.IsInBounds(PlaylistElements[i].Background.Rect, MouseEvent) && MouseEvent.LBH && PlaylistElements[i].Content != -1 && changeOrder)
                    {
                        /*
                        if (PlaylistElements.Count < CurrentPlaylistElement + 1)
                        {
                        }
                        else if (PlaylistElementContents.Count < CurrentPlaylistElement + 1 + Offset)
                        {
                        }
                        if (mevent.Y < PlaylistElements[CurrentPlaylistElement + 1].Background.Rect.Y)
                        {
                        }
                         */
                    }
                }
            }
            else 
            {
                if (ActiveHover != -1)
                {
                    ActiveHover = -1;
                    CurrentPlaylistElement = -1;
                }
            }

            if (ActivePlaylistID > -1)
            {
                if (MouseEvent.LB)
                {
                    if (_Theme.ButtonPlaylistClose.Selected)
                    {
                        ClosePlaylist();
                    }
                    else if (_Theme.ButtonPlaylistSing.Selected)
                    {
                        StartPlaylistSongs();
                    }
                    else if (_Theme.ButtonPlaylistSave.Selected)
                    {
                        CPlaylists.SavePlaylist(ActivePlaylistID);
                    }
                    else if (_Theme.ButtonPlaylistDelete.Selected)
                    {
                        CPlaylists.DeletePlaylist(ActivePlaylistID);
                        ClosePlaylist();
                    }
                }
            }
            return false;
        }

        private void PrepareList()
        {
            PlaylistElements.Clear();
            for (int i = 0; i < Math.Floor(Rect.H / _Theme.EntryHeight); i++)
            {
                PlaylistElement en = new PlaylistElement();
                en.Background = new CStatic(_Theme.TextureBackgroundName, BackgroundColor, new SRectF(Rect.X, Rect.Y + (i * _Theme.EntryHeight), Rect.W, _Theme.EntryHeight, Rect.Z));
                en.Cover = new CStatic(new STexture(), _Theme.StaticCover.Color, new SRectF(Rect.X + _Theme.StaticCover.Rect.X, Rect.Y + _Theme.StaticCover.Rect.Y + (i * _Theme.EntryHeight), _Theme.StaticCover.Rect.W, _Theme.StaticCover.Rect.H, _Theme.StaticCover.Rect.Z));
                en.Text1 = new CText(Rect.X + _Theme.Text1.X, Rect.Y + _Theme.Text1.Y + (i * _Theme.EntryHeight), _Theme.Text1.Z, _Theme.Text1.Height, _Theme.Text1.MaxWidth, _Theme.Text1.Align, _Theme.Text1.Style, _Theme.Text1.Fon, _Theme.Text1.Color, "", _Theme.Text1.ReflectionSpace, _Theme.Text1.ReflectionHeight);
                
                en.SelectSlide = new CSelectSlide(_Theme.SelectSlideGameMode);
                en.SelectSlide.Rect = new SRectF(Rect.X + _Theme.SelectSlideGameMode.Rect.X, Rect.Y + _Theme.SelectSlideGameMode.Rect.Y + (i * _Theme.EntryHeight), _Theme.SelectSlideGameMode.Rect.W, _Theme.SelectSlideGameMode.Rect.H, _Theme.SelectSlideGameMode.Rect.Z);
                en.SelectSlide.RectArrowLeft = new SRectF(Rect.X + _Theme.SelectSlideGameMode.RectArrowLeft.X, Rect.Y + _Theme.SelectSlideGameMode.RectArrowLeft.Y + (i * _Theme.EntryHeight), _Theme.SelectSlideGameMode.RectArrowLeft.W, _Theme.SelectSlideGameMode.RectArrowLeft.H, _Theme.SelectSlideGameMode.RectArrowLeft.Z);
                en.SelectSlide.RectArrowRight = new SRectF(Rect.X + _Theme.SelectSlideGameMode.RectArrowRight.X, Rect.Y + _Theme.SelectSlideGameMode.RectArrowRight.Y + (i * _Theme.EntryHeight), _Theme.SelectSlideGameMode.RectArrowRight.W, _Theme.SelectSlideGameMode.RectArrowRight.H, _Theme.SelectSlideGameMode.RectArrowRight.Z);
                en.Content = -1;
                PlaylistElements.Add(en);
                _Interactions.AddSelectSlide(en.SelectSlide);
                _Interactions.AddText(en.Text1);
                _Interactions.AddStatic(en.Background);
                _Interactions.AddStatic(en.Cover);
            }
        }

        public bool LoadPlaylist(int PlaylistID) 
        {
            if (PlaylistID > -1 && PlaylistID < CPlaylists.NumPlaylists)
            {
                _Theme.TextPlaylistHeader.Text = CPlaylists.Playlists[ActivePlaylistID].PlaylistName;
                ActivePlaylistID = PlaylistID;
                PlaylistElementContents.Clear();
                for (int i = 0; i < CPlaylists.Playlists[ActivePlaylistID].Songs.Count; i++)
                {
                    PlaylistElementContent pec = new PlaylistElementContent();
                    pec.SongID = CPlaylists.Playlists[ActivePlaylistID].Songs[i].SongID;
                    pec.Modes = CSongs.AllSongs[CPlaylists.Playlists[ActivePlaylistID].Songs[i].SongID].AvailableGameModes;
                    pec.Mode = CPlaylists.Playlists[ActivePlaylistID].Songs[i].GameMode;
                    PlaylistElementContents.Add(pec);
                }
                Update();
                return true;
            }
            else
                return false;
        }

        public void UpdatePlaylist()
        {
            PlaylistElementContents.Clear();
            for (int i = 0; i < CPlaylists.Playlists[ActivePlaylistID].Songs.Count; i++)
            {
                PlaylistElementContent pec = new PlaylistElementContent();
                pec.SongID = CPlaylists.Playlists[ActivePlaylistID].Songs[i].SongID;
                pec.Modes = CSongs.AllSongs[pec.SongID].AvailableGameModes;
                pec.Mode = CPlaylists.Playlists[ActivePlaylistID].Songs[i].GameMode;
                PlaylistElementContents.Add(pec);
            }
            Update();
            if (PlaylistElementContents.Count - 1 < ActiveHover || PlaylistElementContents.Count - 1 < CurrentPlaylistElement)
            {
                ActiveHover = PlaylistElementContents.Count - 1;
                CurrentPlaylistElement = PlaylistElementContents.Count - 1;
            }
        }

        private void StartPlaylistSongs()
        {
            CGame.Reset();
            CGame.ClearSongs();

            if (ActivePlaylistID > -1 && ActivePlaylistID < CPlaylists.NumPlaylists)
            {
                for (int i = 0; i < CPlaylists.Playlists[ActivePlaylistID].Songs.Count; i++)
                {
                    CGame.AddSong(CPlaylists.Playlists[ActivePlaylistID].Songs[i].SongID, CPlaylists.Playlists[ActivePlaylistID].Songs[i].GameMode);
                }
                if (CGame.GetNumSongs() > 0)
                    CGraphics.FadeTo(EScreens.ScreenNames);
            }
        }

        private void ClosePlaylist()
        {
            Visible = false;
            Selected = false;
            ActivePlaylistID = -1;
        }

        public void Update()
        {
            if (ActivePlaylistID > -1 && ActivePlaylistID < CPlaylists.NumPlaylists)
            {
                int e = 0 + Offset;
                for (int i = 0; i < PlaylistElements.Count; i++ )
                {
                    if (e + i < PlaylistElementContents.Count)
                    {
                        PlaylistElements[i].Content = e+i;
                        PlaylistElements[i].Background.Visible = true;
                        PlaylistElements[i].Cover.Visible = true;
                        PlaylistElements[i].SelectSlide.Visible = true;
                        PlaylistElements[i].Text1.Visible = true;
                        PlaylistElementContent pec = PlaylistElementContents[e + i];
                        CSong song = CSongs.GetSong(pec.SongID);
                        PlaylistElements[i].Cover.Texture = song.CoverTextureSmall;
                        string t1 = _Theme.Text1.Text.Replace("%a", song.Artist).Replace("%t", song.Title);
                        PlaylistElements[i].Text1.Text = song.Artist + " - " + song.Title;
                        PlaylistElements[i].SelectSlide.Clear();
                        for (int g = 0; g < pec.Modes.Length; g++)
                        {
                            PlaylistElements[i].SelectSlide.AddValue(Enum.GetName(typeof(GameModes.EGameMode), pec.Modes[g]));
                            if (pec.Modes[g] == pec.Mode)
                                PlaylistElements[i].SelectSlide.SetSelectionByValueIndex(g);
                        }                     
                    }
                    else
                    {
                        PlaylistElements[i].Background.Visible = false;
                        PlaylistElements[i].Cover.Visible = false;
                        PlaylistElements[i].SelectSlide.Visible = false;
                        PlaylistElements[i].Text1.Visible = false;
                        PlaylistElements[i].Content = -1;
                    }
                }
            }
        }

        #region ThemeEdit
        public void MoveElement(int stepX, int stepY)
        {
            Rect.X += stepX;
            Rect.Y += stepY;
        }

        public void ResizeElement(int stepW, int stepH)
        {
            Rect.W += stepW;
            if (Rect.W <= 0)
                Rect.W = 1;

            Rect.H += stepH;
            if (Rect.H <= 0)
                Rect.H = 1;
        }
        #endregion ThemeEdit
    }
}
