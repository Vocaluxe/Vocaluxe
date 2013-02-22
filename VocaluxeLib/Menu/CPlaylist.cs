using System;
using System.Drawing;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Xml;

using Vocaluxe.Menu.SongMenu;

namespace Vocaluxe.Menu
{
    public enum EEditMode
    {
        None,
        PlaylistName,
        ChangeOrder
    }

    public class CPlaylistSong
    {
        public int SongID;
        public EGameMode GameMode;

        public CPlaylistSong(int SongID, EGameMode gm)
        {
            this.SongID = SongID;
            this.GameMode = gm;
        }

        public CPlaylistSong()
        {
            GameMode = EGameMode.TR_GAMEMODE_NORMAL;
        }

        public CPlaylistSong(CPlaylistSong ps)
        {
            SongID = ps.SongID;
            GameMode = ps.GameMode;
        }
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

    public class CPlaylist : IMenuElement
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
        private int _PartyModeID;
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
                        CBase.Playlist.DeletePlaylistSong(ActivePlaylistID, PlaylistElements[ChangeOrderSource].Content);
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

        private PlaylistElement ChangeOrderElement;
        private int ChangeOrderSource = -1;
        private int OldMousePosX = 0;
        private int OldMousePosY = 0;

        public int DragAndDropSongID = -1;

        //private static

        public CPlaylist(int PartyModeID)
        {
            _PartyModeID = PartyModeID;
            _Theme = new SThemePlaylist();
            _Theme.Text1 = new CText(_PartyModeID);
            _Theme.StaticCover = new CStatic(_PartyModeID);
            _Theme.StaticPlaylistFooter = new CStatic(_PartyModeID);
            _Theme.StaticPlaylistHeader = new CStatic(_PartyModeID);
            _Theme.ButtonPlaylistName = new CButton(_PartyModeID);
            _Theme.ButtonPlaylistClose = new CButton(_PartyModeID);
            _Theme.ButtonPlaylistDelete = new CButton(_PartyModeID);
            _Theme.ButtonPlaylistSave = new CButton(_PartyModeID);
            _Theme.ButtonPlaylistSing = new CButton(_PartyModeID);
            _Theme.SelectSlideGameMode = new CSelectSlide(_PartyModeID);

            CompleteRect = new SRectF();
            Rect = new SRectF();
            BackgroundColor = new SColorF();
            BackgroundSColor = new SColorF();

            PlaylistElements = new List<PlaylistElement>();
            PlaylistElementContents = new List<PlaylistElementContent>();

            _Interactions = new CObjectInteractions();
            ChangeOrderElement = new PlaylistElement();

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

        public bool LoadTheme(string XmlPath, string ElementName, CXMLReader xmlReader, int SkinIndex)
        {
            string item = XmlPath + "/" + ElementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinBackground", ref _Theme.TextureBackgroundName, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinBackgroundSelected", ref _Theme.STextureBackgroundName, String.Empty);

            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/X", ref Rect.X);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Y", ref Rect.Y);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Z", ref Rect.Z);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/W", ref Rect.W);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/H", ref Rect.H);
            if (xmlReader.GetValue(item + "/ColorBackground", ref _Theme.ColorBackgroundName, String.Empty))
            {
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.ColorBackgroundName, SkinIndex, ref BackgroundColor);
            }
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/BackgroundR", ref BackgroundColor.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/BackgroundG", ref BackgroundColor.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/BackgroundB", ref BackgroundColor.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/BackgroundA", ref BackgroundColor.A);
            }
            if (xmlReader.GetValue(item + "/SColorBackground", ref _Theme.SColorBackgroundName, String.Empty))
            {
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.SColorBackgroundName, SkinIndex, ref BackgroundSColor);
            }
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SBackgroundR", ref BackgroundSColor.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SBackgroundG", ref BackgroundSColor.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SBackgroundB", ref BackgroundSColor.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SBackgroundA", ref BackgroundSColor.A);
            }

            _ThemeLoaded &= _Theme.Text1.LoadTheme(item, "TextPart1", xmlReader, SkinIndex);

            _ThemeLoaded &= _Theme.StaticCover.LoadTheme(item, "StaticCover", xmlReader, SkinIndex);
            _ThemeLoaded &= _Theme.StaticPlaylistHeader.LoadTheme(item, "StaticPlaylistHeader", xmlReader, SkinIndex);
            _ThemeLoaded &= _Theme.StaticPlaylistFooter.LoadTheme(item, "StaticPlaylistFooter", xmlReader, SkinIndex);

            _ThemeLoaded &= _Theme.ButtonPlaylistName.LoadTheme(item, "ButtonPlaylistName", xmlReader, SkinIndex);
            _ThemeLoaded &= _Theme.ButtonPlaylistSing.LoadTheme(item, "ButtonPlaylistSing", xmlReader, SkinIndex);
            _ThemeLoaded &= _Theme.ButtonPlaylistClose.LoadTheme(item, "ButtonPlaylistClose", xmlReader, SkinIndex);
            _ThemeLoaded &= _Theme.ButtonPlaylistSave.LoadTheme(item, "ButtonPlaylistSave", xmlReader, SkinIndex);
            _ThemeLoaded &= _Theme.ButtonPlaylistDelete.LoadTheme(item, "ButtonPlaylistDelete", xmlReader, SkinIndex);

            _ThemeLoaded &= _Theme.SelectSlideGameMode.LoadTheme(item, "SelectSlideGameMode", xmlReader, SkinIndex);

            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/EntryHeight", ref _Theme.EntryHeight);
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
            if (!Visible && CBase.Settings.GetGameState() != EGameState.EditTheme && !ForceDraw)
                return;

            for (int i = 0; i < PlaylistElements.Count; i++ )
            {
                if (i == CurrentPlaylistElement && _Selected)
                {
                    PlaylistElements[i].Background.Texture = CBase.Theme.GetSkinTexture(_Theme.STextureBackgroundName, _PartyModeID);
                    PlaylistElements[i].Background.Color = BackgroundSColor;
                }
                else
                {
                    PlaylistElements[i].Background.Texture = CBase.Theme.GetSkinTexture(_Theme.TextureBackgroundName, _PartyModeID);
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
                BackgroundColor = CBase.Theme.GetColor(_Theme.ColorBackgroundName, _PartyModeID);

            if (_Theme.SColorBackgroundName != String.Empty)
                BackgroundSColor = CBase.Theme.GetColor(_Theme.SColorBackgroundName, _PartyModeID);

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
                        CBase.Playlist.SetPlaylistName(ActivePlaylistID, _Theme.ButtonPlaylistName.Text.Text);
                        CBase.Playlist.SavePlaylist(ActivePlaylistID);
                        _EditMode = EEditMode.None;
                        _Theme.ButtonPlaylistName.EditMode = false;
                    }
                    else if (KeyEvent.Key == Keys.Escape)
                    {
                        _Theme.ButtonPlaylistName.Text.Text = CBase.Playlist.GetPlaylistName(ActivePlaylistID);
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
                                CBase.Playlist.DeletePlaylistSong(ActivePlaylistID, PlaylistElements[CurrentPlaylistElement].Content);
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
                                StartPlaylistSong(CurrentPlaylistElement);
                                break;

                            case Keys.Add:   //move the selected song up
                                if (PlaylistElementContents.Count > 1 && (CurrentPlaylistElement > 0 || Offset > 0))
                                {
                                    CBase.Playlist.MovePlaylistSongUp(ActivePlaylistID, CurrentPlaylistElement + Offset);
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
                                    CBase.Playlist.MovePlaylistSongDown(ActivePlaylistID, CurrentPlaylistElement + Offset);
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
                                    CBase.Playlist.GetPlaylistSong(ActivePlaylistID, CurrentPlaylistElement + Offset).GameMode = PlaylistElementContents[CurrentPlaylistElement + Offset].Modes[PlaylistElements[CurrentPlaylistElement].SelectSlide.Selection];
                                    UpdatePlaylist();
                                }
                                break;

                            case Keys.Right:
                                _Interactions.HandleInput(KeyEvent);
                                CurrentPlaylistElement = GetSelectedSelectionNr();

                                if (CurrentPlaylistElement != -1)
                                {
                                    CBase.Playlist.GetPlaylistSong(ActivePlaylistID, CurrentPlaylistElement + Offset).GameMode = PlaylistElementContents[CurrentPlaylistElement + Offset].Modes[PlaylistElements[CurrentPlaylistElement].SelectSlide.Selection];
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
                                CBase.Playlist.SavePlaylist(ActivePlaylistID);
                            }
                            else if (_Theme.ButtonPlaylistDelete.Selected)
                            {
                                CBase.Playlist.DeletePlaylist(ActivePlaylistID);
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
                        CBase.Playlist.DeletePlaylistSong(ActivePlaylistID, PlaylistElements[i].Content);
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
                                CBase.Playlist.GetPlaylistSong(ActivePlaylistID, CurrentPlaylistElement + Offset).GameMode = PlaylistElementContents[CurrentPlaylistElement + Offset].Modes[PlaylistElements[CurrentPlaylistElement].SelectSlide.Selection];
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
                                CBase.Playlist.SavePlaylist(ActivePlaylistID);
                                return true;
                            }
                            else if (_Theme.ButtonPlaylistDelete.Selected)
                            {
                                CBase.Playlist.DeletePlaylist(ActivePlaylistID);
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

                        //Start selected song with double click
                        if (MouseEvent.LD && CurrentPlaylistElement != -1)
                        {
                            StartPlaylistSong(CurrentPlaylistElement);
                        }

                        //Change order with holding LB
                        if (MouseEvent.LBH && CurrentPlaylistElement != -1 && PlaylistElementContents.Count > 0 && DragAndDropSongID == -1)
                        {

                            ChangeOrderSource = CurrentPlaylistElement + Offset;

                            //Update of Drag/Drop-Texture
                            if (ChangeOrderSource >= PlaylistElementContents.Count)
                                return true;

                            ChangeOrderElement = new PlaylistElement(PlaylistElements[CurrentPlaylistElement]);
                            ChangeOrderElement.Background.Rect.Z = CBase.Settings.GetZNear();
                            ChangeOrderElement.Cover.Rect.Z = CBase.Settings.GetZNear();
                            ChangeOrderElement.SelectSlide.Rect.Z = CBase.Settings.GetZNear();
                            ChangeOrderElement.SelectSlide.RectArrowLeft.Z = CBase.Settings.GetZNear();
                            ChangeOrderElement.SelectSlide.RectArrowRight.Z = CBase.Settings.GetZNear();
                            ChangeOrderElement.Text1.Z = CBase.Settings.GetZNear();

                            ChangeOrderElement.Background.Texture = CBase.Theme.GetSkinTexture(_Theme.TextureBackgroundName, _PartyModeID);
                            ChangeOrderElement.Background.Color = BackgroundColor;

                            OldMousePosX = MouseEvent.X;
                            OldMousePosY = MouseEvent.Y;

                            _EditMode = EEditMode.ChangeOrder;
                        }

                        if (!MouseEvent.LBH && DragAndDropSongID != -1)
                        {
                            EGameMode gm = EGameMode.TR_GAMEMODE_NORMAL;
                            CSong song = CBase.Songs.GetSongByID(DragAndDropSongID);

                            if (song != null)
                            {
                                if (song.IsDuet)
                                    gm = EGameMode.TR_GAMEMODE_DUET;

                                if (CurrentPlaylistElement != -1)
                                {
                                    CBase.Playlist.InsertPlaylistSong(ActivePlaylistID, CurrentPlaylistElement + Offset, DragAndDropSongID, gm);
                                    UpdatePlaylist();
                                }
                                else
                                {
                                    if (MouseEvent.Y < PlaylistElements[0].Background.Rect.Y && Offset == 0)
                                    {
                                        CBase.Playlist.InsertPlaylistSong(ActivePlaylistID, 0, DragAndDropSongID, gm);
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
                                                CBase.Playlist.AddPlaylistSong(ActivePlaylistID, DragAndDropSongID, gm);
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
                                CBase.Playlist.SetPlaylistName(ActivePlaylistID, _Theme.ButtonPlaylistName.Text.Text);
                                CBase.Playlist.SavePlaylist(ActivePlaylistID);
                                _EditMode = EEditMode.None;
                                return true;
                            }
                        }
                        else if (MouseEvent.RB)
                        {
                            if (_Theme.ButtonPlaylistName.Selected)
                            {
                                _Theme.ButtonPlaylistName.Text.Text = CBase.Playlist.GetPlaylistName(ActivePlaylistID);
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
                                CBase.Playlist.MovePlaylistSong(ActivePlaylistID, ChangeOrderSource, CurrentPlaylistElement + Offset);
                                UpdatePlaylist();
                            }
                            else if (CurrentPlaylistElement == -1)
                            {
                                if (MouseEvent.Y < PlaylistElements[0].Background.Rect.Y && Offset == 0)
                                {
                                    CBase.Playlist.MovePlaylistSong(ActivePlaylistID, ChangeOrderSource, 0);
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
                                            CBase.Playlist.MovePlaylistSong(ActivePlaylistID, ChangeOrderSource, PlaylistElementContents.Count - 1);
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

                en.Background = new CStatic(_PartyModeID, _Theme.TextureBackgroundName, BackgroundColor, new SRectF(Rect.X, Rect.Y + (i * _Theme.EntryHeight), Rect.W, _Theme.EntryHeight, Rect.Z));

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
            if (PlaylistID > -1 && PlaylistID < CBase.Playlist.GetNumPlaylists())
            {
                _Theme.ButtonPlaylistName.Text.Text = CBase.Playlist.GetPlaylistName(ActivePlaylistID);
                ActivePlaylistID = PlaylistID;
                PlaylistElementContents.Clear();
                for (int i = 0; i < CBase.Playlist.GetPlaylistSongCount(ActivePlaylistID); i++)
                {
                    PlaylistElementContent pec = new PlaylistElementContent();
                    pec.SongID = CBase.Playlist.GetPlaylistSong(ActivePlaylistID, i).SongID;
                    pec.Modes = CBase.Songs.GetSongByID(CBase.Playlist.GetPlaylistSong(ActivePlaylistID, i).SongID).AvailableGameModes;
                    pec.Mode = CBase.Playlist.GetPlaylistSong(ActivePlaylistID, i).GameMode;
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
            for (int i = 0; i < CBase.Playlist.GetPlaylistSongCount(ActivePlaylistID); i++)
            {
                PlaylistElementContent pec = new PlaylistElementContent();
                pec.SongID = CBase.Playlist.GetPlaylistSong(ActivePlaylistID, i).SongID;
                pec.Modes = CBase.Songs.GetSongByID(pec.SongID).AvailableGameModes;
                pec.Mode = CBase.Playlist.GetPlaylistSong(ActivePlaylistID, i).GameMode;
                PlaylistElementContents.Add(pec);
            }
            
            Update();
        }


        public void ClosePlaylist()
        {
            Visible = false;
            Selected = false;
            ActivePlaylistID = -1;
        }


        private void StartPlaylistSongs()
        {
            CBase.Game.Reset();
            CBase.Game.ClearSongs();

            if (ActivePlaylistID > -1 && ActivePlaylistID < CBase.Playlist.GetNumPlaylists())
            {
                for (int i = 0; i < CBase.Playlist.GetPlaylistSongCount(ActivePlaylistID); i++)
                {
                    CBase.Game.AddSong(CBase.Playlist.GetPlaylistSong(ActivePlaylistID, i).SongID, CBase.Playlist.GetPlaylistSong(ActivePlaylistID, i).GameMode);
                }
                if (CBase.Game.GetNumSongs() > 0)
                    CBase.Graphics.FadeTo(EScreens.ScreenNames);    //TODO: What is if someone uses that in PartyMode?
            }
        }

        private void StartPlaylistSong(int selected)
        {
            CBase.Game.Reset();
            CBase.Game.ClearSongs();

            CBase.Game.AddSong(CBase.Playlist.GetPlaylistSong(ActivePlaylistID, selected).SongID, CBase.Playlist.GetPlaylistSong(ActivePlaylistID, selected).GameMode);

            if (CBase.Game.GetNumSongs() > 0)
                CBase.Graphics.FadeTo(EScreens.ScreenNames); 
        }

        public void Update()
        {
            if (ActivePlaylistID > -1 && ActivePlaylistID < CBase.Playlist.GetNumPlaylists())
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
                        CSong song = CBase.Songs.GetSongByID(pec.SongID);
                        PlaylistElements[i].Cover.Texture = song.CoverTextureSmall;
                        string t1 = _Theme.Text1.Text.Replace("%a", song.Artist).Replace("%t", song.Title);
                        PlaylistElements[i].Text1.Text = /*(Offset + i + 1).ToString() + ") " + */song.Artist + " - " + song.Title; //TODO: Add text field for the number
                        PlaylistElements[i].SelectSlide.Clear();
                        for (int g = 0; g < pec.Modes.Length; g++)
                        {
                            PlaylistElements[i].SelectSlide.AddValue(Enum.GetName(typeof(EGameMode), pec.Modes[g]));
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
