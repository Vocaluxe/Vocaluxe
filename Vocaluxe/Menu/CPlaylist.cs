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
    enum EEditMode
    {
        None,
        PlaylistName,
        ChangeOrder
    }

    struct SThemePlaylist
    {
        public string Name;

        public string ColorBackgroundName;
        public string SColorBackgroundName;

        public string TextureBackgroundName;
        public string STextureBackgroundName;

        public float EntryHeight;

        public CText Text1;

        public CStatic StaticCover;
        public CStatic StaticPlaylistHeader;
        public CStatic StaticPlaylistFooter;

        public CButton ButtonPlaylistName;
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

            public PlaylistElement()
            {
            }

            public PlaylistElement(PlaylistElement pe)
            {
                Cover = new CStatic(pe.Cover);
                Background = new CStatic(pe.Background);
                Text1 = new CText(pe.Text1);
                SelectSlide = new CSelectSlide(pe.SelectSlide);
                Content = pe.Content;
            }

            public void Draw()
            {
                Background.Draw();
                SelectSlide.Draw();
                Cover.Draw();
                Text1.Draw();
            }

            public void MouseMove(int PosX, int PosY, int OldPosX, int OldPosY)
            {
                int diffX = PosX - OldPosX;
                int diffY = PosY - OldPosY;

                Cover.Rect.X += diffX;
                Cover.Rect.Y += diffY;

                Background.Rect.X += diffX;
                Background.Rect.Y += diffY;

                SelectSlide.Rect.X += diffX;
                SelectSlide.Rect.Y += diffY;

                SelectSlide.RectArrowLeft.X += diffX;
                SelectSlide.RectArrowLeft.Y += diffY;

                SelectSlide.RectArrowRight.X += diffX;
                SelectSlide.RectArrowRight.Y += diffY;

                Text1.X += diffX;
                Text1.Y += diffY;
            }
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

        public SRectF CompleteRect;
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
                CurrentPlaylistElement = GetSelectedSelectionNr();

                if (!value)
                {
                    if (_EditMode == EEditMode.ChangeOrder && ChangeOrderSource != -1 && PlaylistElements.Count > ChangeOrderSource)
                    {
                        CPlaylists.Playlists[ActivePlaylistID].DeleteSong(PlaylistElements[ChangeOrderSource].Content);
                        UpdatePlaylist();                       
                    }
                    ChangeOrderSource = -1;
                    _EditMode = EEditMode.None;
                }
            }
        }

        public EEditMode _EditMode;
        public int ActivePlaylistID = -1;
        public int Offset = 0;
        public int CurrentPlaylistElement = -1;

        private PlaylistElement ChangeOrderElement = new PlaylistElement();
        private int ChangeOrderSource = -1;
        private int OldMousePosX = 0;
        private int OldMousePosY = 0;

        public int DragAndDropSongID = -1;

        //private static

        public CPlaylist()
        {
            _Theme = new SThemePlaylist();
            _Theme.Text1 = new CText();
            _Theme.StaticCover = new CStatic();
            _Theme.StaticPlaylistFooter = new CStatic();
            _Theme.StaticPlaylistHeader = new CStatic();
            _Theme.ButtonPlaylistName = new CButton();
            _Theme.ButtonPlaylistClose = new CButton();
            _Theme.ButtonPlaylistDelete = new CButton();
            _Theme.ButtonPlaylistSave = new CButton();
            _Theme.ButtonPlaylistSing = new CButton();
            _Theme.SelectSlideGameMode = new CSelectSlide();

            CompleteRect = new SRectF();
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
            _Interactions.AddStatic(_Theme.StaticCover);
            _Interactions.AddStatic(_Theme.StaticPlaylistFooter);
            _Interactions.AddStatic(_Theme.StaticPlaylistHeader);
            _Interactions.AddButton(_Theme.ButtonPlaylistName);
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

            _ThemeLoaded &= _Theme.StaticCover.LoadTheme(item, "StaticCover", navigator, SkinIndex);
            _ThemeLoaded &= _Theme.StaticPlaylistHeader.LoadTheme(item, "StaticPlaylistHeader", navigator, SkinIndex);
            _ThemeLoaded &= _Theme.StaticPlaylistFooter.LoadTheme(item, "StaticPlaylistFooter", navigator, SkinIndex);

            _ThemeLoaded &= _Theme.ButtonPlaylistName.LoadTheme(item, "ButtonPlaylistName", navigator, SkinIndex);
            _ThemeLoaded &= _Theme.ButtonPlaylistSing.LoadTheme(item, "ButtonPlaylistSing", navigator, SkinIndex);
            _ThemeLoaded &= _Theme.ButtonPlaylistClose.LoadTheme(item, "ButtonPlaylistClose", navigator, SkinIndex);
            _ThemeLoaded &= _Theme.ButtonPlaylistSave.LoadTheme(item, "ButtonPlaylistSave", navigator, SkinIndex);
            _ThemeLoaded &= _Theme.ButtonPlaylistDelete.LoadTheme(item, "ButtonPlaylistDelete", navigator, SkinIndex);

            _ThemeLoaded &= _Theme.SelectSlideGameMode.LoadTheme(item, "SelectSlideGameMode", navigator, SkinIndex);

            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/EntryHeight", navigator, ref _Theme.EntryHeight);
            if (_ThemeLoaded)
            {
                _Theme.Name = ElementName;

                //Check for highest x/y-coords
                CompleteRect = Rect;
                //ButtonPlaylistClose
                if (_Theme.ButtonPlaylistClose.Rect.X < CompleteRect.X)
                    CompleteRect.X = _Theme.ButtonPlaylistClose.Rect.X;
                if (_Theme.ButtonPlaylistClose.Rect.Y < CompleteRect.Y)
                    CompleteRect.Y = _Theme.ButtonPlaylistClose.Rect.Y;
                if (_Theme.ButtonPlaylistClose.Rect.W + _Theme.ButtonPlaylistClose.Rect.X > CompleteRect.W + CompleteRect.X)
                    CompleteRect.W = _Theme.ButtonPlaylistClose.Rect.W + _Theme.ButtonPlaylistClose.Rect.X - CompleteRect.X;
                if (_Theme.ButtonPlaylistClose.Rect.Y + _Theme.ButtonPlaylistClose.Rect.H > CompleteRect.Y + CompleteRect.H)
                    CompleteRect.H = _Theme.ButtonPlaylistClose.Rect.H + _Theme.ButtonPlaylistClose.Rect.Y - CompleteRect.Y;
                //ButtonPlaylistName
                if (_Theme.ButtonPlaylistName.Rect.X < CompleteRect.X)
                    CompleteRect.X = _Theme.ButtonPlaylistName.Rect.X;
                if (_Theme.ButtonPlaylistName.Rect.Y < CompleteRect.Y)
                    CompleteRect.Y = _Theme.ButtonPlaylistName.Rect.Y;
                if (_Theme.ButtonPlaylistName.Rect.W + _Theme.ButtonPlaylistName.Rect.X > CompleteRect.W + CompleteRect.X)
                    CompleteRect.W = _Theme.ButtonPlaylistName.Rect.W + _Theme.ButtonPlaylistName.Rect.X - CompleteRect.X;
                if (_Theme.ButtonPlaylistName.Rect.Y + _Theme.ButtonPlaylistName.Rect.H > CompleteRect.Y + CompleteRect.H)
                    CompleteRect.H = _Theme.ButtonPlaylistName.Rect.H + _Theme.ButtonPlaylistName.Rect.Y - CompleteRect.Y;
                //ButtonPlaylistSing
                if (_Theme.ButtonPlaylistSing.Rect.X < CompleteRect.X)
                    CompleteRect.X = _Theme.ButtonPlaylistSing.Rect.X;
                if (_Theme.ButtonPlaylistSing.Rect.Y < CompleteRect.Y)
                    CompleteRect.Y = _Theme.ButtonPlaylistSing.Rect.Y;
                if (_Theme.ButtonPlaylistSing.Rect.W + _Theme.ButtonPlaylistSing.Rect.X > CompleteRect.W + CompleteRect.X)
                    CompleteRect.W = _Theme.ButtonPlaylistSing.Rect.W + _Theme.ButtonPlaylistSing.Rect.X - CompleteRect.X;
                if (_Theme.ButtonPlaylistSing.Rect.Y + _Theme.ButtonPlaylistSing.Rect.H > CompleteRect.Y + CompleteRect.H)
                    CompleteRect.H = _Theme.ButtonPlaylistSing.Rect.H + _Theme.ButtonPlaylistSing.Rect.Y - CompleteRect.Y;
                //ButtonPlaylistSave
                if (_Theme.ButtonPlaylistSave.Rect.X < CompleteRect.X)
                    CompleteRect.X = _Theme.ButtonPlaylistSave.Rect.X;
                if (_Theme.ButtonPlaylistSave.Rect.Y < CompleteRect.Y)
                    CompleteRect.Y = _Theme.ButtonPlaylistSave.Rect.Y;
                if (_Theme.ButtonPlaylistSave.Rect.W + _Theme.ButtonPlaylistSave.Rect.X > CompleteRect.W + CompleteRect.X)
                    CompleteRect.W = _Theme.ButtonPlaylistSave.Rect.W + _Theme.ButtonPlaylistSave.Rect.X - CompleteRect.X;
                if (_Theme.ButtonPlaylistSave.Rect.Y + _Theme.ButtonPlaylistSave.Rect.H > CompleteRect.Y + CompleteRect.H)
                    CompleteRect.H = _Theme.ButtonPlaylistSave.Rect.H + _Theme.ButtonPlaylistSave.Rect.Y - CompleteRect.Y;
                //ButtonPlaylistDelete
                if (_Theme.ButtonPlaylistDelete.Rect.X < CompleteRect.X)
                    CompleteRect.X = _Theme.ButtonPlaylistDelete.Rect.X;
                if (_Theme.ButtonPlaylistDelete.Rect.Y < CompleteRect.Y)
                    CompleteRect.Y = _Theme.ButtonPlaylistDelete.Rect.Y;
                if (_Theme.ButtonPlaylistDelete.Rect.W + _Theme.ButtonPlaylistDelete.Rect.X > CompleteRect.W + CompleteRect.X)
                    CompleteRect.W = _Theme.ButtonPlaylistDelete.Rect.W + _Theme.ButtonPlaylistDelete.Rect.X - CompleteRect.X;
                if (_Theme.ButtonPlaylistDelete.Rect.Y + _Theme.ButtonPlaylistDelete.Rect.H > CompleteRect.Y + CompleteRect.H)
                    CompleteRect.H = _Theme.ButtonPlaylistDelete.Rect.H + _Theme.ButtonPlaylistDelete.Rect.Y - CompleteRect.Y;
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
                _Theme.StaticPlaylistFooter.SaveTheme(writer);
                _Theme.StaticPlaylistHeader.SaveTheme(writer);
                _Theme.StaticCover.SaveTheme(writer);
                _Theme.SelectSlideGameMode.SaveTheme(writer);
                _Theme.ButtonPlaylistName.SaveTheme(writer);
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
                if (i == CurrentPlaylistElement && _Selected)
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

            if (_EditMode == EEditMode.ChangeOrder)
                ChangeOrderElement.Draw();
        }

        public bool IsMouseOver(MouseEvent MouseEvent)
        {
            return CHelper.IsInBounds(CompleteRect, MouseEvent.X, MouseEvent.Y) || _Interactions.IsMouseOver(MouseEvent);
        }

        public void UnloadTextures()
        {
            _Theme.Text1.UnloadTextures();
            _Theme.ButtonPlaylistClose.UnloadTextures();
            _Theme.ButtonPlaylistDelete.UnloadTextures();
            _Theme.ButtonPlaylistName.UnloadTextures();
            _Theme.ButtonPlaylistSave.UnloadTextures();
            _Theme.ButtonPlaylistSing.UnloadTextures();

            _Theme.StaticCover.UnloadTextures();
            _Theme.StaticPlaylistFooter.UnloadTextures();
            _Theme.StaticPlaylistHeader.UnloadTextures();

            _Theme.SelectSlideGameMode.UnloadTextures();
        }

        public void LoadTextures()
        {
            if (_Theme.ColorBackgroundName != String.Empty)
                BackgroundColor = CTheme.GetColor(_Theme.ColorBackgroundName);

            if (_Theme.SColorBackgroundName != String.Empty)
                BackgroundSColor = CTheme.GetColor(_Theme.SColorBackgroundName);

            _Theme.Text1.LoadTextures();
            _Theme.ButtonPlaylistClose.LoadTextures();
            _Theme.ButtonPlaylistDelete.LoadTextures();
            _Theme.ButtonPlaylistName.LoadTextures();
            _Theme.ButtonPlaylistSave.LoadTextures();
            _Theme.ButtonPlaylistSing.LoadTextures();

            _Theme.StaticCover.LoadTextures();
            _Theme.StaticPlaylistFooter.LoadTextures();
            _Theme.StaticPlaylistHeader.LoadTextures();

            _Theme.SelectSlideGameMode.LoadTextures();

            Init();
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
                //Active EditMode ignores other input!
                if (_EditMode == EEditMode.PlaylistName)
                {
                    if (KeyEvent.KeyPressed && !Char.IsControl(KeyEvent.Unicode))
                    {
                        _Theme.ButtonPlaylistName.Text.Text = _Theme.ButtonPlaylistName.Text.Text + KeyEvent.Unicode;
                    }
                    else if (KeyEvent.Key == Keys.Enter)
                    {
                        CPlaylists.Playlists[ActivePlaylistID].PlaylistName = _Theme.ButtonPlaylistName.Text.Text;
                        CPlaylists.Playlists[ActivePlaylistID].SavePlaylist();
                        _EditMode = EEditMode.None;
                        _Theme.ButtonPlaylistName.EditMode = false;
                    }
                    else if (KeyEvent.Key == Keys.Escape)
                    {
                        _Theme.ButtonPlaylistName.Text.Text = CPlaylists.Playlists[ActivePlaylistID].PlaylistName;
                        _EditMode = EEditMode.None;
                        _Theme.ButtonPlaylistName.EditMode = false;
                    }
                    else if (KeyEvent.Key == Keys.Back || KeyEvent.Key == Keys.Delete)
                    {
                        if (_Theme.ButtonPlaylistName.Text.Text.Length > 0)
                            _Theme.ButtonPlaylistName.Text.Text = _Theme.ButtonPlaylistName.Text.Text.Remove(_Theme.ButtonPlaylistName.Text.Text.Length - 1);
                    }
                }
                else
                {
                    if (CurrentPlaylistElement == -1 || PlaylistElementContents.Count == 0)    //no song is selected
                    {
                        _Interactions.HandleInput(KeyEvent);
                        CurrentPlaylistElement = GetSelectedSelectionNr();

                        if (CurrentPlaylistElement != -1)
                            return true;
                    }
                    else if (CurrentPlaylistElement != -1)   //a song is selected
                    {
                        int ScrollLimit = PlaylistElements.Count / 2;

                        //special actions if a song is selected
                        switch (KeyEvent.Key)
                        {
                            case Keys.Up:
                                if (KeyEvent.ModSHIFT)
                                {
                                    _Interactions.SetInteractionToSelectSlide(PlaylistElements[0].SelectSlide);
                                    _Interactions.HandleInput(KeyEvent);
                                    CurrentPlaylistElement = GetSelectedSelectionNr();
                                }
                                else if (CurrentPlaylistElement > ScrollLimit || PlaylistElementContents.Count == 0)
                                {
                                    _Interactions.HandleInput(KeyEvent);
                                    CurrentPlaylistElement = GetSelectedSelectionNr();
                                }
                                else if (CurrentPlaylistElement <= ScrollLimit)
                                {
                                    if (Offset > 0)
                                    {
                                        Offset--;
                                        Update();
                                    }
                                    else
                                    {
                                        _Interactions.HandleInput(KeyEvent);
                                        CurrentPlaylistElement = GetSelectedSelectionNr();
                                    }
                                }
                                break;

                            case Keys.Down:
                                if (KeyEvent.ModSHIFT)
                                {
                                    for (int i = PlaylistElements.Count - 1; i >= 0; i--)
                                    {
                                        if (PlaylistElements[i].SelectSlide.Visible)
                                        {
                                            _Interactions.SetInteractionToSelectSlide(PlaylistElements[0].SelectSlide);
                                            _Interactions.HandleInput(KeyEvent);
                                            CurrentPlaylistElement = GetSelectedSelectionNr();
                                        }
                                    }
                                }
                                else if (CurrentPlaylistElement >= ScrollLimit)
                                {
                                    if (Offset < PlaylistElementContents.Count - PlaylistElements.Count)
                                    {
                                        Offset++;
                                        Update();
                                    }
                                    else
                                    {
                                        _Interactions.HandleInput(KeyEvent);
                                        CurrentPlaylistElement = GetSelectedSelectionNr();
                                    }
                                }
                                else if (CurrentPlaylistElement < ScrollLimit)
                                {
                                    _Interactions.HandleInput(KeyEvent);
                                    CurrentPlaylistElement = GetSelectedSelectionNr();
                                }
                                break;

                            case Keys.Delete:
                                CPlaylists.Playlists[ActivePlaylistID].DeleteSong(PlaylistElements[CurrentPlaylistElement].Content);
                                UpdatePlaylist();

                                if (Offset > 0)
                                    Offset--;

                                Update();

                                if (PlaylistElementContents.Count - 1 < CurrentPlaylistElement)
                                {
                                    CurrentPlaylistElement = PlaylistElementContents.Count - 1;
                                }

                                if (CurrentPlaylistElement != -1)
                                    _Interactions.SetInteractionToSelectSlide(PlaylistElements[CurrentPlaylistElement].SelectSlide);
                                break;

                            case Keys.Back:
                                ClosePlaylist(); //really? or better global?
                                break;

                            case Keys.Enter:
                                StartPlaylistSongs();
                                break;

                            case Keys.Add:   //move the selected song up
                                if (PlaylistElementContents.Count > 1 && (CurrentPlaylistElement > 0 || Offset > 0))
                                {
                                    CPlaylists.Playlists[ActivePlaylistID].SongUp(CurrentPlaylistElement + Offset);
                                    UpdatePlaylist();

                                    KeyEvent key = new KeyEvent();
                                    key.Key = Keys.Up;

                                    if (CurrentPlaylistElement > ScrollLimit)
                                    {
                                        _Interactions.HandleInput(key);
                                        CurrentPlaylistElement = GetSelectedSelectionNr();
                                    }
                                    else if (CurrentPlaylistElement <= ScrollLimit)
                                    {
                                        if (Offset > 0)
                                        {
                                            Offset--;
                                            Update();
                                        }
                                        else
                                        {
                                            _Interactions.HandleInput(key);
                                            CurrentPlaylistElement = GetSelectedSelectionNr();
                                        }
                                    }
                                }
                                break;

                            case Keys.Subtract: //move the selected song down
                                if (PlaylistElementContents.Count > 1 && CurrentPlaylistElement + Offset < PlaylistElementContents.Count - 1)
                                {
                                    CPlaylists.Playlists[ActivePlaylistID].SongDown(CurrentPlaylistElement + Offset);
                                    UpdatePlaylist();

                                    KeyEvent key = new KeyEvent();
                                    key.Key = Keys.Down;

                                    if (CurrentPlaylistElement >= ScrollLimit)
                                    {
                                        if (Offset < PlaylistElementContents.Count - PlaylistElements.Count)
                                        {
                                            Offset++;
                                            Update();
                                        }
                                        else
                                        {
                                            _Interactions.HandleInput(key);
                                            CurrentPlaylistElement = GetSelectedSelectionNr();
                                        }
                                    }
                                    else if (CurrentPlaylistElement < ScrollLimit)
                                    {
                                        _Interactions.HandleInput(key);
                                        CurrentPlaylistElement = GetSelectedSelectionNr();
                                    }
                                }
                                break;

                            case Keys.PageUp:   //scroll up
                                if (PlaylistElementContents.Count > 0)
                                {
                                    Offset -= PlaylistElements.Count;

                                    if (Offset < 0)
                                        Offset = 0;

                                    Update();
                                    CurrentPlaylistElement = 0;
                                }
                                break;
                            
                            case Keys.PageDown: //scroll down
                                if (PlaylistElementContents.Count > 0)
                                {
                                    Offset += PlaylistElements.Count;

                                    if (Offset > PlaylistElementContents.Count - PlaylistElements.Count)
                                        Offset = PlaylistElementContents.Count - PlaylistElements.Count;

                                    if (Offset < 0)
                                        Offset = 0;

                                    Update();

                                    for (int i = PlaylistElements.Count - 1; i >= 0; i--)
                                    {
                                        if (PlaylistElements[i].SelectSlide.Visible)
                                        {
                                            CurrentPlaylistElement = i;
                                            break;
                                        }
                                    }
                                }
                                break;

                            case Keys.Left:
                                _Interactions.HandleInput(KeyEvent);
                                CurrentPlaylistElement = GetSelectedSelectionNr();

                                if (CurrentPlaylistElement != -1)
                                {
                                    CPlaylists.Playlists[ActivePlaylistID].Songs[CurrentPlaylistElement + Offset].GameMode = PlaylistElementContents[CurrentPlaylistElement + Offset].Modes[PlaylistElements[CurrentPlaylistElement].SelectSlide.Selection];
                                    UpdatePlaylist();
                                }
                                break;

                            case Keys.Right:
                                _Interactions.HandleInput(KeyEvent);
                                CurrentPlaylistElement = GetSelectedSelectionNr();

                                if (CurrentPlaylistElement != -1)
                                {
                                    CPlaylists.Playlists[ActivePlaylistID].Songs[CurrentPlaylistElement + Offset].GameMode = PlaylistElementContents[CurrentPlaylistElement + Offset].Modes[PlaylistElements[CurrentPlaylistElement].SelectSlide.Selection];
                                    UpdatePlaylist();
                                }
                                break;
                        }
                        return true;
                    }
   
                    //default actions
                    switch (KeyEvent.Key)
                    {
                        case Keys.Back:
                            if (_Theme.ButtonPlaylistName.Selected)
                            {
                                _EditMode = EEditMode.PlaylistName;
                                _Theme.ButtonPlaylistName.EditMode = true;
                            }
                            else
                                ClosePlaylist();
                            break;

                        case Keys.Enter:
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
                            else if (_Theme.ButtonPlaylistName.Selected)
                            {
                                if (_EditMode != EEditMode.PlaylistName)
                                {
                                    _EditMode = EEditMode.PlaylistName;
                                    _Theme.ButtonPlaylistName.EditMode = true;
                                }
                                else
                                {
                                    _EditMode = EEditMode.None;
                                    _Theme.ButtonPlaylistName.EditMode = false;
                                }
                            }
                            break;
                    }
                }
            }
            return true;
        }

        private void SetSelectionToLastEntry()
        {
            if (PlaylistElementContents.Count == 0)
                return;

            int off = PlaylistElementContents.Count - PlaylistElements.Count;
            if (off >= 0)
                Offset = off;
            else
                Offset = 0;

            Update();

            for (int i = PlaylistElements.Count - 1; i >= 0; i--)
            {
                if (PlaylistElements[i].SelectSlide.Visible)
                {
                    CurrentPlaylistElement = i;
                    return;
                }
            }
        }

        private void SetSelectionToFirstEntry()
        {
            if (PlaylistElementContents.Count == 0)
                return;

            Offset = 0;
            Update();

            CurrentPlaylistElement = 0;
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

        public void ScrollToBottom()
        {
            if (PlaylistElementContents.Count == 0)
                return;

            int off = PlaylistElementContents.Count - PlaylistElements.Count;
            if (off >= 0)
                Offset = off;
            else
                Offset = 0;

            Update();
        }

        public bool HandleMouse(MouseEvent MouseEvent)
        {
            _Interactions.HandleMouse(MouseEvent);

            if (CHelper.IsInBounds(CompleteRect, MouseEvent) && Visible)
            {
                //Scroll
                if (MouseEvent.Wheel > 0)
                {
                    if (PlaylistElements.Count + Offset + MouseEvent.Wheel <= PlaylistElementContents.Count)
                    {
                        Offset += MouseEvent.Wheel;
                        Update();
                    }
                    return true;
                }
                else if (MouseEvent.Wheel < 0)
                {
                    if (Offset + MouseEvent.Wheel >= 0)
                    {
                        Offset += MouseEvent.Wheel;
                        Update();
                    }
                    return true;
                }

                bool hover_set = false;
                for (int i = 0; i < PlaylistElements.Count; i++)
                {
                    //Hover for playlist-element
                    if (PlaylistElementContents.Count - 1 >= i && CHelper.IsInBounds(PlaylistElements[i].Background.Rect, MouseEvent))
                    {
                        hover_set = true;
                        CurrentPlaylistElement = i;
                        _Interactions.SetInteractionToSelectSlide(PlaylistElements[CurrentPlaylistElement].SelectSlide);
                        _Interactions.ProcessMouseMove(MouseEvent.X, MouseEvent.Y);
                    }

                    //Delete Entry with RB
                    if (CHelper.IsInBounds(PlaylistElements[i].Background.Rect, MouseEvent) && MouseEvent.RB && PlaylistElements[i].Content != -1)
                    {
                        CPlaylists.Playlists[ActivePlaylistID].DeleteSong(PlaylistElements[i].Content);
                        UpdatePlaylist();
                        return true;
                    }
                }
                if (!hover_set)
                {
                    CurrentPlaylistElement = -1;
                }

                switch (_EditMode)
                {
                    //Normal mode
                    case EEditMode.None:

                        //LB actions
                        if (MouseEvent.LB)
                        {
                            if (CurrentPlaylistElement != -1)
                            {
                                CPlaylists.Playlists[ActivePlaylistID].Songs[CurrentPlaylistElement + Offset].GameMode = PlaylistElementContents[CurrentPlaylistElement + Offset].Modes[PlaylistElements[CurrentPlaylistElement].SelectSlide.Selection];
                                UpdatePlaylist();
                            }
                            if (_Theme.ButtonPlaylistClose.Selected)
                            {
                                ClosePlaylist();
                                return true;
                            }
                            else if (_Theme.ButtonPlaylistSing.Selected)
                            {
                                StartPlaylistSongs();
                                return true;
                            }
                            else if (_Theme.ButtonPlaylistSave.Selected)
                            {
                                CPlaylists.SavePlaylist(ActivePlaylistID);
                                return true;
                            }
                            else if (_Theme.ButtonPlaylistDelete.Selected)
                            {
                                CPlaylists.DeletePlaylist(ActivePlaylistID);
                                ClosePlaylist();
                                return true;
                            }
                            else if (_Theme.ButtonPlaylistName.Selected)
                            {
                                _EditMode = EEditMode.PlaylistName;
                                _Theme.ButtonPlaylistName.EditMode = true;
                                return true;
                            }
                        }

                        //Change order with holding LB
                        if (MouseEvent.LBH && CurrentPlaylistElement != -1 && PlaylistElementContents.Count > 1 && DragAndDropSongID == -1)
                        {

                            ChangeOrderSource = CurrentPlaylistElement + Offset;

                            //Update of Drag/Drop-Texture
                            if (ChangeOrderSource >= PlaylistElementContents.Count)
                                return true;

                            ChangeOrderElement = new PlaylistElement(PlaylistElements[CurrentPlaylistElement]);
                            ChangeOrderElement.Background.Rect.Z = CSettings.zNear;
                            ChangeOrderElement.Cover.Rect.Z = CSettings.zNear;
                            ChangeOrderElement.SelectSlide.Rect.Z = CSettings.zNear;
                            ChangeOrderElement.SelectSlide.RectArrowLeft.Z = CSettings.zNear;
                            ChangeOrderElement.SelectSlide.RectArrowRight.Z = CSettings.zNear;
                            ChangeOrderElement.Text1.Z = CSettings.zNear;

                            ChangeOrderElement.Background.Texture = CTheme.GetSkinTexture(_Theme.TextureBackgroundName);
                            ChangeOrderElement.Background.Color = BackgroundColor;

                            OldMousePosX = MouseEvent.X;
                            OldMousePosY = MouseEvent.Y;

                            _EditMode = EEditMode.ChangeOrder;
                        }

                        if (!MouseEvent.LBH && DragAndDropSongID != -1)
                        {
                            EGameMode gm = EGameMode.TR_GAMEMODE_NORMAL;
                            CSong song = CSongs.GetSong(DragAndDropSongID);

                            if (song != null)
                            {
                                if (song.IsDuet)
                                    gm = EGameMode.TR_GAMEMODE_DUET;

                                if (CurrentPlaylistElement != -1)
                                {
                                    CPlaylists.Playlists[ActivePlaylistID].SongInsert(CurrentPlaylistElement + Offset, DragAndDropSongID, gm);
                                    UpdatePlaylist();
                                }
                                else
                                {
                                    if (MouseEvent.Y < PlaylistElements[0].Background.Rect.Y && Offset == 0)
                                    {
                                        CPlaylists.Playlists[ActivePlaylistID].SongInsert(0, DragAndDropSongID, gm);
                                        UpdatePlaylist();
                                    }
                                    else
                                    {
                                        if (PlaylistElements.Count + Offset >= PlaylistElementContents.Count)
                                        {
                                            float min = 0f;
                                            for (int i = PlaylistElements.Count - 1; i >= 0; i--)
                                            {
                                                if (PlaylistElements[i].SelectSlide.Visible)
                                                {
                                                    min = PlaylistElements[i].SelectSlide.Rect.Y + PlaylistElements[i].SelectSlide.Rect.H;
                                                    break;
                                                }
                                            }

                                            if (MouseEvent.Y > min)
                                            {
                                                CPlaylists.Playlists[ActivePlaylistID].AddSong(DragAndDropSongID, gm);
                                                UpdatePlaylist();
                                                ScrollToBottom();
                                            }
                                        }
                                    }
                                    DragAndDropSongID = -1;
                                    UpdatePlaylist();
                                }
                            }
                        }

                        break;

                    case EEditMode.PlaylistName:
                        _Interactions.SetInteractionToButton(_Theme.ButtonPlaylistName);
                        CurrentPlaylistElement = -1;
                        if (MouseEvent.LB)
                        {
                            if (_Theme.ButtonPlaylistName.Selected)
                            {
                                CPlaylists.Playlists[ActivePlaylistID].PlaylistName = _Theme.ButtonPlaylistName.Text.Text;
                                CPlaylists.Playlists[ActivePlaylistID].SavePlaylist();
                                _EditMode = EEditMode.None;
                                return true;
                            }
                        }
                        else if (MouseEvent.RB)
                        {
                            if (_Theme.ButtonPlaylistName.Selected)
                            {
                                _Theme.ButtonPlaylistName.Text.Text = CPlaylists.Playlists[ActivePlaylistID].PlaylistName;
                                _EditMode = EEditMode.None;
                                _Theme.ButtonPlaylistName.EditMode = false;
                                return true;
                            }
                        }
                        break;

                    case EEditMode.ChangeOrder:
                        //Actions according to playlist-element

                        //Update coords for Drag/Drop-Texture
                        ChangeOrderElement.MouseMove(MouseEvent.X, MouseEvent.Y, OldMousePosX, OldMousePosY);
                        OldMousePosX = MouseEvent.X;
                        OldMousePosY = MouseEvent.Y;

                        if (!MouseEvent.LBH)
                        {
                            if (CurrentPlaylistElement != -1 && CurrentPlaylistElement + Offset != ChangeOrderSource)
                            {
                                CPlaylists.Playlists[ActivePlaylistID].SongMove(ChangeOrderSource, CurrentPlaylistElement + Offset);
                                UpdatePlaylist();
                            }
                            else if (CurrentPlaylistElement == -1)
                            {
                                if (MouseEvent.Y < PlaylistElements[0].Background.Rect.Y && Offset == 0)
                                {
                                    CPlaylists.Playlists[ActivePlaylistID].SongMove(ChangeOrderSource, 0);
                                }
                                else
                                {
                                    if (PlaylistElements.Count + Offset >= PlaylistElementContents.Count)
                                    {
                                        float min = 0f;
                                        for (int i = PlaylistElements.Count - 1; i >= 0; i--)
                                        {
                                            if (PlaylistElements[i].SelectSlide.Visible)
                                            {
                                                min = PlaylistElements[i].SelectSlide.Rect.Y + PlaylistElements[i].SelectSlide.Rect.H;
                                                break;
                                            }
                                        }

                                        if (MouseEvent.Y > min)
                                        {
                                            CPlaylists.Playlists[ActivePlaylistID].SongMove(ChangeOrderSource, PlaylistElementContents.Count - 1);
                                        }
                                    }
                                }

                                UpdatePlaylist();
                            }
                            _EditMode = EEditMode.None;
                        }
                        break;
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

                en.Cover = new CStatic(_Theme.StaticCover);
                en.Cover.Rect.Y += Rect.Y + (i * _Theme.EntryHeight);
                en.Cover.Rect.X += Rect.X;

                en.Text1 = new CText(_Theme.Text1);
                en.Text1.X += Rect.X;
                en.Text1.Y += Rect.Y + (i * _Theme.EntryHeight);

                en.SelectSlide = new CSelectSlide(_Theme.SelectSlideGameMode);
                en.SelectSlide.Rect.X += Rect.X;
                en.SelectSlide.Rect.Y += Rect.Y + (i * _Theme.EntryHeight);
                en.SelectSlide.RectArrowLeft.X += Rect.X;
                en.SelectSlide.RectArrowLeft.Y += Rect.Y + (i * _Theme.EntryHeight);
                en.SelectSlide.RectArrowRight.X += Rect.X;
                en.SelectSlide.RectArrowRight.Y += Rect.Y + (i * _Theme.EntryHeight);

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
                _Theme.ButtonPlaylistName.Text.Text = CPlaylists.Playlists[ActivePlaylistID].PlaylistName;
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
                for (int i = 0; i < PlaylistElements.Count; i++ )
                {
                    if (Offset + i < PlaylistElementContents.Count)
                    {
                        PlaylistElements[i].Content = Offset + i;
                        PlaylistElements[i].Background.Visible = true;
                        PlaylistElements[i].Cover.Visible = true;
                        PlaylistElements[i].SelectSlide.Visible = true;
                        PlaylistElements[i].Text1.Visible = true;
                        PlaylistElementContent pec = PlaylistElementContents[Offset + i];
                        CSong song = CSongs.GetSong(pec.SongID);
                        PlaylistElements[i].Cover.Texture = song.CoverTextureSmall;
                        string t1 = _Theme.Text1.Text.Replace("%a", song.Artist).Replace("%t", song.Title);
                        PlaylistElements[i].Text1.Text = /*(Offset + i + 1).ToString() + ") " + */song.Artist + " - " + song.Title; //TODO: Add text field for the number
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
