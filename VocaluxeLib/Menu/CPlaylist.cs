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
using System.Windows.Forms;
using System.Xml;
using VocaluxeLib.Songs;

namespace VocaluxeLib.Menu
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

        public CPlaylistSong(int songID, EGameMode gm)
        {
            SongID = songID;
            GameMode = gm;
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
        public string SelColorBackgroundName;

        public string TextureBackgroundName;
        public string SelTextureBackgroundName;

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
        private class CPlaylistElementContent
        {
            public EGameMode[] Modes;
            public int SongID;
            public EGameMode Mode;
        }

        private class CPlaylistElement
        {
            public CStatic Cover;
            public CStatic Background;
            public CText Text1;
            public CSelectSlide SelectSlide;
            public int Content;

            public CPlaylistElement() {}

            public CPlaylistElement(CPlaylistElement pe)
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

            public void MouseMove(int posX, int posY, int oldPosX, int oldPosY)
            {
                int diffX = posX - oldPosX;
                int diffY = posY - oldPosY;

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

        private readonly int _PartyModeID;
        private SThemePlaylist _Theme;
        private bool _ThemeLoaded;

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        private readonly CObjectInteractions _Interactions;

        private readonly List<CPlaylistElement> _PlaylistElements;
        private readonly List<CPlaylistElementContent> _PlaylistElementContents;

        public SRectF CompleteRect;
        public SRectF Rect;
        public SColorF BackgroundColor;
        public SColorF BackgroundSelColor;

        public bool Visible;

        private bool _Selected;
        public bool Selected
        {
            get { return _Selected; }
            set
            {
                _Selected = value;
                _Interactions.Active = value;
                CurrentPlaylistElement = _GetSelectedSelectionNr();

                if (!value)
                {
                    if (EditMode == EEditMode.ChangeOrder && _ChangeOrderSource != -1 && _PlaylistElements.Count > _ChangeOrderSource)
                    {
                        CBase.Playlist.DeletePlaylistSong(ActivePlaylistID, _PlaylistElements[_ChangeOrderSource].Content);
                        UpdatePlaylist();
                    }
                    _ChangeOrderSource = -1;
                    EditMode = EEditMode.None;
                }
            }
        }

        public EEditMode EditMode;
        public int ActivePlaylistID = -1;
        public int Offset;
        public int CurrentPlaylistElement = -1;

        private CPlaylistElement _ChangeOrderElement;
        private int _ChangeOrderSource = -1;
        private int _OldMousePosX;
        private int _OldMousePosY;

        public int DragAndDropSongID = -1;

        //private static
        public CPlaylist(int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Theme = new SThemePlaylist
                {
                    Text1 = new CText(_PartyModeID),
                    StaticCover = new CStatic(_PartyModeID),
                    StaticPlaylistFooter = new CStatic(_PartyModeID),
                    StaticPlaylistHeader = new CStatic(_PartyModeID),
                    ButtonPlaylistName = new CButton(_PartyModeID),
                    ButtonPlaylistClose = new CButton(_PartyModeID),
                    ButtonPlaylistDelete = new CButton(_PartyModeID),
                    ButtonPlaylistSave = new CButton(_PartyModeID),
                    ButtonPlaylistSing = new CButton(_PartyModeID),
                    SelectSlideGameMode = new CSelectSlide(_PartyModeID)
                };

            CompleteRect = new SRectF();
            Rect = new SRectF();
            BackgroundColor = new SColorF();
            BackgroundSelColor = new SColorF();

            _PlaylistElements = new List<CPlaylistElement>();
            _PlaylistElementContents = new List<CPlaylistElementContent>();

            _Interactions = new CObjectInteractions();
            _ChangeOrderElement = new CPlaylistElement();

            Visible = false;
            Selected = false;
        }

        public void Init()
        {
            _Interactions.Clear();
            _PrepareList();
            _Interactions.AddStatic(_Theme.StaticCover);
            _Interactions.AddStatic(_Theme.StaticPlaylistFooter);
            _Interactions.AddStatic(_Theme.StaticPlaylistHeader);
            _Interactions.AddButton(_Theme.ButtonPlaylistName);
            _Interactions.AddButton(_Theme.ButtonPlaylistClose);
            _Interactions.AddButton(_Theme.ButtonPlaylistDelete);
            _Interactions.AddButton(_Theme.ButtonPlaylistSave);
            _Interactions.AddButton(_Theme.ButtonPlaylistSing);
        }

        public bool LoadTheme(string xmlPath, string elementName, CXMLReader xmlReader, int skinIndex)
        {
            string item = xmlPath + "/" + elementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinBackground", out _Theme.TextureBackgroundName, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinBackgroundSelected", out _Theme.SelTextureBackgroundName, String.Empty);

            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/X", ref Rect.X);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Y", ref Rect.Y);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Z", ref Rect.Z);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/W", ref Rect.W);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/H", ref Rect.H);
            if (xmlReader.GetValue(item + "/ColorBackground", out _Theme.ColorBackgroundName, String.Empty))
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.ColorBackgroundName, skinIndex, out BackgroundColor);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/BackgroundR", ref BackgroundColor.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/BackgroundG", ref BackgroundColor.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/BackgroundB", ref BackgroundColor.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/BackgroundA", ref BackgroundColor.A);
            }
            if (xmlReader.GetValue(item + "/SColorBackground", out _Theme.SelColorBackgroundName, String.Empty))
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.SelColorBackgroundName, skinIndex, out BackgroundSelColor);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SBackgroundR", ref BackgroundSelColor.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SBackgroundG", ref BackgroundSelColor.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SBackgroundB", ref BackgroundSelColor.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SBackgroundA", ref BackgroundSelColor.A);
            }

            _ThemeLoaded &= _Theme.Text1.LoadTheme(item, "TextPart1", xmlReader, skinIndex);

            _ThemeLoaded &= _Theme.StaticCover.LoadTheme(item, "StaticCover", xmlReader, skinIndex);
            _ThemeLoaded &= _Theme.StaticPlaylistHeader.LoadTheme(item, "StaticPlaylistHeader", xmlReader, skinIndex);
            _ThemeLoaded &= _Theme.StaticPlaylistFooter.LoadTheme(item, "StaticPlaylistFooter", xmlReader, skinIndex);

            _ThemeLoaded &= _Theme.ButtonPlaylistName.LoadTheme(item, "ButtonPlaylistName", xmlReader, skinIndex);
            _ThemeLoaded &= _Theme.ButtonPlaylistSing.LoadTheme(item, "ButtonPlaylistSing", xmlReader, skinIndex);
            _ThemeLoaded &= _Theme.ButtonPlaylistClose.LoadTheme(item, "ButtonPlaylistClose", xmlReader, skinIndex);
            _ThemeLoaded &= _Theme.ButtonPlaylistSave.LoadTheme(item, "ButtonPlaylistSave", xmlReader, skinIndex);
            _ThemeLoaded &= _Theme.ButtonPlaylistDelete.LoadTheme(item, "ButtonPlaylistDelete", xmlReader, skinIndex);

            _ThemeLoaded &= _Theme.SelectSlideGameMode.LoadTheme(item, "SelectSlideGameMode", xmlReader, skinIndex);

            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/EntryHeight", ref _Theme.EntryHeight);
            if (_ThemeLoaded)
            {
                _Theme.Name = elementName;

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
                writer.WriteElementString("SkinBackgroundSelected", _Theme.SelTextureBackgroundName);

                writer.WriteComment("<ColorBackground>: Button color from ColorScheme (high priority)");
                writer.WriteComment("or <BackgroundR>, <BackgroundG>, <BackgroundB>, <BackgroundA> (lower priority)");
                if (!String.IsNullOrEmpty(_Theme.ColorBackgroundName))
                    writer.WriteElementString("ColorBackground", _Theme.ColorBackgroundName);
                else
                {
                    writer.WriteElementString("BackgroundR", BackgroundColor.R.ToString("#0.00"));
                    writer.WriteElementString("BackgroundG", BackgroundColor.G.ToString("#0.00"));
                    writer.WriteElementString("BackgroundB", BackgroundColor.B.ToString("#0.00"));
                    writer.WriteElementString("BackgroundA", BackgroundColor.A.ToString("#0.00"));
                }

                writer.WriteComment("<SColorBackground>: Selected paylist-entry color from ColorScheme (high priority)");
                writer.WriteComment("or <SBackgroundR>, <SBackgroundG>, <SBackgroundB>, <SBackgroundA> (lower priority)");
                if (!String.IsNullOrEmpty(_Theme.SelColorBackgroundName))
                    writer.WriteElementString("SColorBackground", _Theme.SelColorBackgroundName);
                else
                {
                    writer.WriteElementString("SBackgroundR", BackgroundSelColor.R.ToString("#0.00"));
                    writer.WriteElementString("SBackgroundG", BackgroundSelColor.G.ToString("#0.00"));
                    writer.WriteElementString("SBackgroundB", BackgroundSelColor.B.ToString("#0.00"));
                    writer.WriteElementString("SBackgroundA", BackgroundSelColor.A.ToString("#0.00"));
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

        public void Draw(bool forceDraw = false)
        {
            if (_PlaylistElements.Count <= 0)
                LoadPlaylist(0);
            if (!Visible && CBase.Settings.GetGameState() != EGameState.EditTheme && !forceDraw)
                return;

            for (int i = 0; i < _PlaylistElements.Count; i++)
            {
                if (i == CurrentPlaylistElement && _Selected)
                {
                    _PlaylistElements[i].Background.Texture = CBase.Theme.GetSkinTexture(_Theme.SelTextureBackgroundName, _PartyModeID);
                    _PlaylistElements[i].Background.Color = BackgroundSelColor;
                }
                else
                {
                    _PlaylistElements[i].Background.Texture = CBase.Theme.GetSkinTexture(_Theme.TextureBackgroundName, _PartyModeID);
                    _PlaylistElements[i].Background.Color = BackgroundColor;
                }
            }
            _Interactions.Draw();

            if (EditMode == EEditMode.ChangeOrder)
                _ChangeOrderElement.Draw();
        }

        public bool IsMouseOver(SMouseEvent mouseEvent)
        {
            return CHelper.IsInBounds(CompleteRect, mouseEvent.X, mouseEvent.Y) || _Interactions.IsMouseOver(mouseEvent);
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
            if (!String.IsNullOrEmpty(_Theme.ColorBackgroundName))
                BackgroundColor = CBase.Theme.GetColor(_Theme.ColorBackgroundName, _PartyModeID);

            if (!String.IsNullOrEmpty(_Theme.SelColorBackgroundName))
                BackgroundSelColor = CBase.Theme.GetColor(_Theme.SelColorBackgroundName, _PartyModeID);

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

        public bool HandleInput(SKeyEvent keyEvent)
        {
            if (Selected)
            {
                //Active EditMode ignores other input!
                if (EditMode == EEditMode.PlaylistName)
                {
                    if (keyEvent.KeyPressed && !Char.IsControl(keyEvent.Unicode))
                        _Theme.ButtonPlaylistName.Text.Text = _Theme.ButtonPlaylistName.Text.Text + keyEvent.Unicode;
                    else
                    {
                        switch (keyEvent.Key)
                        {
                            case Keys.Enter:
                                CBase.Playlist.SetPlaylistName(ActivePlaylistID, _Theme.ButtonPlaylistName.Text.Text);
                                CBase.Playlist.SavePlaylist(ActivePlaylistID);
                                EditMode = EEditMode.None;
                                _Theme.ButtonPlaylistName.EditMode = false;
                                break;
                            case Keys.Escape:
                                _Theme.ButtonPlaylistName.Text.Text = CBase.Playlist.GetPlaylistName(ActivePlaylistID);
                                EditMode = EEditMode.None;
                                _Theme.ButtonPlaylistName.EditMode = false;
                                break;
                            case Keys.Delete:
                            case Keys.Back:
                                if (!String.IsNullOrEmpty(_Theme.ButtonPlaylistName.Text.Text))
                                    _Theme.ButtonPlaylistName.Text.Text = _Theme.ButtonPlaylistName.Text.Text.Remove(_Theme.ButtonPlaylistName.Text.Text.Length - 1);
                                break;
                        }
                    }
                    return true;
                }
                if (CurrentPlaylistElement == -1 || _PlaylistElementContents.Count == 0)
                {
                    //no song is selected
                    _Interactions.HandleInput(keyEvent);
                    CurrentPlaylistElement = _GetSelectedSelectionNr();

                    if (CurrentPlaylistElement != -1)
                        return true;
                }
                else if (CurrentPlaylistElement != -1)
                {
                    //a song is selected
                    int scrollLimit = _PlaylistElements.Count / 2;

                    //special actions if a song is selected
                    switch (keyEvent.Key)
                    {
                        case Keys.Up:
                            if (keyEvent.ModShift)
                            {
                                _Interactions.SetInteractionToSelectSlide(_PlaylistElements[0].SelectSlide);
                                _Interactions.HandleInput(keyEvent);
                                CurrentPlaylistElement = _GetSelectedSelectionNr();
                            }
                            else if (CurrentPlaylistElement > scrollLimit || _PlaylistElementContents.Count == 0)
                            {
                                _Interactions.HandleInput(keyEvent);
                                CurrentPlaylistElement = _GetSelectedSelectionNr();
                            }
                            else if (CurrentPlaylistElement <= scrollLimit)
                            {
                                if (Offset > 0)
                                {
                                    Offset--;
                                    Update();
                                }
                                else
                                {
                                    _Interactions.HandleInput(keyEvent);
                                    CurrentPlaylistElement = _GetSelectedSelectionNr();
                                }
                            }
                            break;

                        case Keys.Down:
                            if (keyEvent.ModShift)
                            {
                                for (int i = _PlaylistElements.Count - 1; i >= 0; i--)
                                {
                                    if (_PlaylistElements[i].SelectSlide.Visible)
                                    {
                                        _Interactions.SetInteractionToSelectSlide(_PlaylistElements[0].SelectSlide);
                                        _Interactions.HandleInput(keyEvent);
                                        CurrentPlaylistElement = _GetSelectedSelectionNr();
                                    }
                                }
                            }
                            else if (CurrentPlaylistElement >= scrollLimit)
                            {
                                if (Offset < _PlaylistElementContents.Count - _PlaylistElements.Count)
                                {
                                    Offset++;
                                    Update();
                                }
                                else
                                {
                                    _Interactions.HandleInput(keyEvent);
                                    CurrentPlaylistElement = _GetSelectedSelectionNr();
                                }
                            }
                            else if (CurrentPlaylistElement < scrollLimit)
                            {
                                _Interactions.HandleInput(keyEvent);
                                CurrentPlaylistElement = _GetSelectedSelectionNr();
                            }
                            break;

                        case Keys.Delete:
                            CBase.Playlist.DeletePlaylistSong(ActivePlaylistID, _PlaylistElements[CurrentPlaylistElement].Content);
                            UpdatePlaylist();

                            if (Offset > 0)
                                Offset--;

                            Update();

                            if (_PlaylistElementContents.Count - 1 < CurrentPlaylistElement)
                                CurrentPlaylistElement = _PlaylistElementContents.Count - 1;

                            if (CurrentPlaylistElement != -1)
                                _Interactions.SetInteractionToSelectSlide(_PlaylistElements[CurrentPlaylistElement].SelectSlide);
                            break;

                        case Keys.Back:
                            ClosePlaylist(); //really? or better global?
                            break;

                        case Keys.Enter:
                            _StartPlaylistSong(CurrentPlaylistElement);
                            break;

                        case Keys.Add: //move the selected song up
                            if (_PlaylistElementContents.Count > 1 && (CurrentPlaylistElement > 0 || Offset > 0))
                            {
                                CBase.Playlist.MovePlaylistSongUp(ActivePlaylistID, CurrentPlaylistElement + Offset);
                                UpdatePlaylist();

                                SKeyEvent key = new SKeyEvent {Key = Keys.Up};

                                if (CurrentPlaylistElement > scrollLimit)
                                {
                                    _Interactions.HandleInput(key);
                                    CurrentPlaylistElement = _GetSelectedSelectionNr();
                                }
                                else if (CurrentPlaylistElement <= scrollLimit)
                                {
                                    if (Offset > 0)
                                    {
                                        Offset--;
                                        Update();
                                    }
                                    else
                                    {
                                        _Interactions.HandleInput(key);
                                        CurrentPlaylistElement = _GetSelectedSelectionNr();
                                    }
                                }
                            }
                            break;

                        case Keys.Subtract: //move the selected song down
                            if (_PlaylistElementContents.Count > 1 && CurrentPlaylistElement + Offset < _PlaylistElementContents.Count - 1)
                            {
                                CBase.Playlist.MovePlaylistSongDown(ActivePlaylistID, CurrentPlaylistElement + Offset);
                                UpdatePlaylist();

                                SKeyEvent key = new SKeyEvent {Key = Keys.Down};

                                if (CurrentPlaylistElement >= scrollLimit)
                                {
                                    if (Offset < _PlaylistElementContents.Count - _PlaylistElements.Count)
                                    {
                                        Offset++;
                                        Update();
                                    }
                                    else
                                    {
                                        _Interactions.HandleInput(key);
                                        CurrentPlaylistElement = _GetSelectedSelectionNr();
                                    }
                                }
                                else if (CurrentPlaylistElement < scrollLimit)
                                {
                                    _Interactions.HandleInput(key);
                                    CurrentPlaylistElement = _GetSelectedSelectionNr();
                                }
                            }
                            break;

                        case Keys.PageUp: //scroll up
                            if (_PlaylistElementContents.Count > 0)
                            {
                                Offset -= _PlaylistElements.Count;

                                if (Offset < 0)
                                    Offset = 0;

                                Update();
                                CurrentPlaylistElement = 0;
                            }
                            break;

                        case Keys.PageDown: //scroll down
                            if (_PlaylistElementContents.Count > 0)
                            {
                                Offset += _PlaylistElements.Count;

                                if (Offset > _PlaylistElementContents.Count - _PlaylistElements.Count)
                                    Offset = _PlaylistElementContents.Count - _PlaylistElements.Count;

                                if (Offset < 0)
                                    Offset = 0;

                                Update();

                                for (int i = _PlaylistElements.Count - 1; i >= 0; i--)
                                {
                                    if (_PlaylistElements[i].SelectSlide.Visible)
                                    {
                                        CurrentPlaylistElement = i;
                                        break;
                                    }
                                }
                            }
                            break;

                        case Keys.Left:
                            _Interactions.HandleInput(keyEvent);
                            CurrentPlaylistElement = _GetSelectedSelectionNr();

                            if (CurrentPlaylistElement != -1)
                            {
                                CBase.Playlist.GetPlaylistSong(ActivePlaylistID, CurrentPlaylistElement + Offset).GameMode =
                                    _PlaylistElementContents[CurrentPlaylistElement + Offset].Modes[_PlaylistElements[CurrentPlaylistElement].SelectSlide.Selection];
                                UpdatePlaylist();
                            }
                            break;

                        case Keys.Right:
                            _Interactions.HandleInput(keyEvent);
                            CurrentPlaylistElement = _GetSelectedSelectionNr();

                            if (CurrentPlaylistElement != -1)
                            {
                                CBase.Playlist.GetPlaylistSong(ActivePlaylistID, CurrentPlaylistElement + Offset).GameMode =
                                    _PlaylistElementContents[CurrentPlaylistElement + Offset].Modes[_PlaylistElements[CurrentPlaylistElement].SelectSlide.Selection];
                                UpdatePlaylist();
                            }
                            break;
                    }
                    return true;
                }

                //default actions
                switch (keyEvent.Key)
                {
                    case Keys.Back:
                        if (_Theme.ButtonPlaylistName.Selected)
                        {
                            EditMode = EEditMode.PlaylistName;
                            _Theme.ButtonPlaylistName.EditMode = true;
                        }
                        else
                            ClosePlaylist();
                        break;

                    case Keys.Enter:
                        if (_Theme.ButtonPlaylistClose.Selected)
                            ClosePlaylist();
                        else if (_Theme.ButtonPlaylistSing.Selected)
                            _StartPlaylistSongs();
                        else if (_Theme.ButtonPlaylistSave.Selected)
                            CBase.Playlist.SavePlaylist(ActivePlaylistID);
                        else if (_Theme.ButtonPlaylistDelete.Selected)
                        {
                            CBase.Playlist.DeletePlaylist(ActivePlaylistID);
                            ClosePlaylist();
                        }
                        else if (_Theme.ButtonPlaylistName.Selected)
                        {
                            if (EditMode != EEditMode.PlaylistName)
                            {
                                EditMode = EEditMode.PlaylistName;
                                _Theme.ButtonPlaylistName.EditMode = true;
                            }
                            else
                            {
                                EditMode = EEditMode.None;
                                _Theme.ButtonPlaylistName.EditMode = false;
                            }
                        }
                        break;
                    case Keys.PageDown:
                        _SetSelectionToLastEntry();
                        break;
                    case Keys.PageUp:
                        _SetSelectionToFirstEntry();
                        break;
                }
            }
            return true;
        }

        private void _SetSelectionToLastEntry()
        {
            if (_PlaylistElementContents.Count == 0)
                return;

            int off = _PlaylistElementContents.Count - _PlaylistElements.Count;
            Offset = off >= 0 ? off : 0;

            Update();

            for (int i = _PlaylistElements.Count - 1; i >= 0; i--)
            {
                if (_PlaylistElements[i].SelectSlide.Visible)
                {
                    CurrentPlaylistElement = i;
                    return;
                }
            }
        }

        private void _SetSelectionToFirstEntry()
        {
            if (_PlaylistElementContents.Count == 0)
                return;

            Offset = 0;
            Update();

            CurrentPlaylistElement = 0;
        }

        private int _GetSelectedSelectionNr()
        {
            for (int i = 0; i < _PlaylistElements.Count; i++)
            {
                if (_PlaylistElements[i].SelectSlide.Selected)
                    return i;
            }
            return -1;
        }

        public void ScrollToBottom()
        {
            if (_PlaylistElementContents.Count == 0)
                return;

            int off = _PlaylistElementContents.Count - _PlaylistElements.Count;
            Offset = off >= 0 ? off : 0;

            Update();
        }

        public bool HandleMouse(SMouseEvent mouseEvent)
        {
            _Interactions.HandleMouse(mouseEvent);

            if (CHelper.IsInBounds(CompleteRect, mouseEvent) && Visible)
            {
                //Scroll
                if (mouseEvent.Wheel > 0)
                {
                    if (_PlaylistElements.Count + Offset + mouseEvent.Wheel <= _PlaylistElementContents.Count)
                    {
                        Offset += mouseEvent.Wheel;
                        Update();
                    }
                    return true;
                }
                if (mouseEvent.Wheel < 0)
                {
                    if (Offset + mouseEvent.Wheel >= 0)
                    {
                        Offset += mouseEvent.Wheel;
                        Update();
                    }
                    return true;
                }

                bool hoverSet = false;
                for (int i = 0; i < _PlaylistElements.Count; i++)
                {
                    //Hover for playlist-element
                    if (_PlaylistElementContents.Count - 1 >= i && CHelper.IsInBounds(_PlaylistElements[i].Background.Rect, mouseEvent))
                    {
                        hoverSet = true;
                        CurrentPlaylistElement = i;
                        _Interactions.SetInteractionToSelectSlide(_PlaylistElements[CurrentPlaylistElement].SelectSlide);
                        _Interactions.ProcessMouseMove(mouseEvent.X, mouseEvent.Y);
                    }

                    //Delete Entry with RB
                    if (CHelper.IsInBounds(_PlaylistElements[i].Background.Rect, mouseEvent) && mouseEvent.RB && _PlaylistElements[i].Content != -1)
                    {
                        CBase.Playlist.DeletePlaylistSong(ActivePlaylistID, _PlaylistElements[i].Content);
                        UpdatePlaylist();
                        return true;
                    }
                }
                if (!hoverSet)
                    CurrentPlaylistElement = -1;

                switch (EditMode)
                {
                        //Normal mode
                    case EEditMode.None:

                        //LB actions
                        if (mouseEvent.LB)
                        {
                            if (CurrentPlaylistElement != -1)
                            {
                                CBase.Playlist.GetPlaylistSong(ActivePlaylistID, CurrentPlaylistElement + Offset).GameMode =
                                    _PlaylistElementContents[CurrentPlaylistElement + Offset].Modes[_PlaylistElements[CurrentPlaylistElement].SelectSlide.Selection];
                                UpdatePlaylist();
                            }
                            if (_Theme.ButtonPlaylistClose.Selected)
                            {
                                ClosePlaylist();
                                return true;
                            }
                            if (_Theme.ButtonPlaylistSing.Selected)
                            {
                                _StartPlaylistSongs();
                                return true;
                            }
                            if (_Theme.ButtonPlaylistSave.Selected)
                            {
                                CBase.Playlist.SavePlaylist(ActivePlaylistID);
                                return true;
                            }
                            if (_Theme.ButtonPlaylistDelete.Selected)
                            {
                                CBase.Playlist.DeletePlaylist(ActivePlaylistID);
                                ClosePlaylist();
                                return true;
                            }
                            if (_Theme.ButtonPlaylistName.Selected)
                            {
                                EditMode = EEditMode.PlaylistName;
                                _Theme.ButtonPlaylistName.EditMode = true;
                                return true;
                            }
                        }

                        //Start selected song with double click
                        if (mouseEvent.LD && CurrentPlaylistElement != -1)
                            _StartPlaylistSong(CurrentPlaylistElement);

                        //Change order with holding LB
                        if (mouseEvent.LBH && CurrentPlaylistElement != -1 && _PlaylistElementContents.Count > 0 && DragAndDropSongID == -1)
                        {
                            _ChangeOrderSource = CurrentPlaylistElement + Offset;

                            //Update of Drag/Drop-Texture
                            if (_ChangeOrderSource >= _PlaylistElementContents.Count)
                                return true;

                            _ChangeOrderElement = new CPlaylistElement(_PlaylistElements[CurrentPlaylistElement]);
                            _ChangeOrderElement.Background.Rect.Z = CBase.Settings.GetZNear();
                            _ChangeOrderElement.Cover.Rect.Z = CBase.Settings.GetZNear();
                            _ChangeOrderElement.SelectSlide.Rect.Z = CBase.Settings.GetZNear();
                            _ChangeOrderElement.SelectSlide.RectArrowLeft.Z = CBase.Settings.GetZNear();
                            _ChangeOrderElement.SelectSlide.RectArrowRight.Z = CBase.Settings.GetZNear();
                            _ChangeOrderElement.Text1.Z = CBase.Settings.GetZNear();

                            _ChangeOrderElement.Background.Texture = CBase.Theme.GetSkinTexture(_Theme.TextureBackgroundName, _PartyModeID);
                            _ChangeOrderElement.Background.Color = BackgroundColor;

                            _OldMousePosX = mouseEvent.X;
                            _OldMousePosY = mouseEvent.Y;

                            EditMode = EEditMode.ChangeOrder;
                        }

                        if (!mouseEvent.LBH && DragAndDropSongID != -1)
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
                                    if (mouseEvent.Y < _PlaylistElements[0].Background.Rect.Y && Offset == 0)
                                    {
                                        CBase.Playlist.InsertPlaylistSong(ActivePlaylistID, 0, DragAndDropSongID, gm);
                                        UpdatePlaylist();
                                    }
                                    else
                                    {
                                        if (_PlaylistElements.Count + Offset >= _PlaylistElementContents.Count)
                                        {
                                            float min = 0f;
                                            for (int i = _PlaylistElements.Count - 1; i >= 0; i--)
                                            {
                                                if (_PlaylistElements[i].SelectSlide.Visible)
                                                {
                                                    min = _PlaylistElements[i].SelectSlide.Rect.Y + _PlaylistElements[i].SelectSlide.Rect.H;
                                                    break;
                                                }
                                            }

                                            if (mouseEvent.Y > min)
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
                        if (mouseEvent.LB)
                        {
                            if (_Theme.ButtonPlaylistName.Selected)
                            {
                                CBase.Playlist.SetPlaylistName(ActivePlaylistID, _Theme.ButtonPlaylistName.Text.Text);
                                CBase.Playlist.SavePlaylist(ActivePlaylistID);
                                EditMode = EEditMode.None;
                                return true;
                            }
                        }
                        else if (mouseEvent.RB)
                        {
                            if (_Theme.ButtonPlaylistName.Selected)
                            {
                                _Theme.ButtonPlaylistName.Text.Text = CBase.Playlist.GetPlaylistName(ActivePlaylistID);
                                EditMode = EEditMode.None;
                                _Theme.ButtonPlaylistName.EditMode = false;
                                return true;
                            }
                        }
                        break;

                    case EEditMode.ChangeOrder:
                        //Actions according to playlist-element

                        //Update coords for Drag/Drop-Texture
                        _ChangeOrderElement.MouseMove(mouseEvent.X, mouseEvent.Y, _OldMousePosX, _OldMousePosY);
                        _OldMousePosX = mouseEvent.X;
                        _OldMousePosY = mouseEvent.Y;

                        if (!mouseEvent.LBH)
                        {
                            if (CurrentPlaylistElement != -1 && CurrentPlaylistElement + Offset != _ChangeOrderSource)
                            {
                                CBase.Playlist.MovePlaylistSong(ActivePlaylistID, _ChangeOrderSource, CurrentPlaylistElement + Offset);
                                UpdatePlaylist();
                            }
                            else if (CurrentPlaylistElement == -1)
                            {
                                if (mouseEvent.Y < _PlaylistElements[0].Background.Rect.Y && Offset == 0)
                                    CBase.Playlist.MovePlaylistSong(ActivePlaylistID, _ChangeOrderSource, 0);
                                else
                                {
                                    if (_PlaylistElements.Count + Offset >= _PlaylistElementContents.Count)
                                    {
                                        float min = 0f;
                                        for (int i = _PlaylistElements.Count - 1; i >= 0; i--)
                                        {
                                            if (_PlaylistElements[i].SelectSlide.Visible)
                                            {
                                                min = _PlaylistElements[i].SelectSlide.Rect.Y + _PlaylistElements[i].SelectSlide.Rect.H;
                                                break;
                                            }
                                        }

                                        if (mouseEvent.Y > min)
                                            CBase.Playlist.MovePlaylistSong(ActivePlaylistID, _ChangeOrderSource, _PlaylistElementContents.Count - 1);
                                    }
                                }

                                UpdatePlaylist();
                            }
                            EditMode = EEditMode.None;
                        }
                        break;
                }
            }
            return false;
        }

        private void _PrepareList()
        {
            _PlaylistElements.Clear();

            for (int i = 0; i < Math.Floor(Rect.H / _Theme.EntryHeight); i++)
            {
                CPlaylistElement en = new CPlaylistElement
                    {
                        Background = new CStatic(_PartyModeID, _Theme.TextureBackgroundName, BackgroundColor,
                                                 new SRectF(Rect.X, Rect.Y + (i * _Theme.EntryHeight), Rect.W, _Theme.EntryHeight, Rect.Z)),
                        Cover = new CStatic(_Theme.StaticCover)
                    };

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

                _PlaylistElements.Add(en);
                _Interactions.AddSelectSlide(en.SelectSlide);
                _Interactions.AddText(en.Text1);
                _Interactions.AddStatic(en.Background);
                _Interactions.AddStatic(en.Cover);
            }
        }

        public bool LoadPlaylist(int playlistID)
        {
            if (playlistID > -1 && playlistID < CBase.Playlist.GetNumPlaylists())
            {
                ActivePlaylistID = playlistID;
                _Theme.ButtonPlaylistName.Text.Text = CBase.Playlist.GetPlaylistName(ActivePlaylistID);
                _PlaylistElementContents.Clear();
                for (int i = 0; i < CBase.Playlist.GetPlaylistSongCount(ActivePlaylistID); i++)
                {
                    CPlaylistElementContent pec = new CPlaylistElementContent
                        {
                            SongID = CBase.Playlist.GetPlaylistSong(ActivePlaylistID, i).SongID,
                            Modes = CBase.Songs.GetSongByID(CBase.Playlist.GetPlaylistSong(ActivePlaylistID, i).SongID).AvailableGameModes,
                            Mode = CBase.Playlist.GetPlaylistSong(ActivePlaylistID, i).GameMode
                        };
                    _PlaylistElementContents.Add(pec);
                }
                Update();
                return true;
            }
            return false;
        }

        public void UpdatePlaylist()
        {
            _PlaylistElementContents.Clear();
            for (int i = 0; i < CBase.Playlist.GetPlaylistSongCount(ActivePlaylistID); i++)
            {
                CPlaylistElementContent pec = new CPlaylistElementContent {SongID = CBase.Playlist.GetPlaylistSong(ActivePlaylistID, i).SongID};
                pec.Modes = CBase.Songs.GetSongByID(pec.SongID).AvailableGameModes;
                pec.Mode = CBase.Playlist.GetPlaylistSong(ActivePlaylistID, i).GameMode;
                _PlaylistElementContents.Add(pec);
            }

            Update();
        }

        public void ClosePlaylist()
        {
            Visible = false;
            Selected = false;
            ActivePlaylistID = -1;
        }

        private void _StartPlaylistSongs()
        {
            CBase.Game.Reset();
            CBase.Game.ClearSongs();

            if (ActivePlaylistID > -1 && ActivePlaylistID < CBase.Playlist.GetNumPlaylists())
            {
                for (int i = 0; i < CBase.Playlist.GetPlaylistSongCount(ActivePlaylistID); i++)
                    CBase.Game.AddSong(CBase.Playlist.GetPlaylistSong(ActivePlaylistID, i).SongID, CBase.Playlist.GetPlaylistSong(ActivePlaylistID, i).GameMode);
                if (CBase.Game.GetNumSongs() > 0)
                    CBase.Graphics.FadeTo(EScreens.ScreenNames); //TODO: What is if someone uses that in PartyMode?
            }
        }

        private void _StartPlaylistSong(int selected)
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
                for (int i = 0; i < _PlaylistElements.Count; i++)
                {
                    if (Offset + i < _PlaylistElementContents.Count)
                    {
                        _PlaylistElements[i].Content = Offset + i;
                        _PlaylistElements[i].Background.Visible = true;
                        _PlaylistElements[i].Cover.Visible = true;
                        _PlaylistElements[i].SelectSlide.Visible = true;
                        _PlaylistElements[i].Text1.Visible = true;
                        CPlaylistElementContent pec = _PlaylistElementContents[Offset + i];
                        CSong song = CBase.Songs.GetSongByID(pec.SongID);
                        _PlaylistElements[i].Cover.Texture = song.CoverTextureSmall;
                        string t1 = CBase.Language.Translate(_Theme.Text1.Text).Replace("%a", song.Artist).Replace("%t", song.Title);
                        _PlaylistElements[i].Text1.Text = /*(Offset + i + 1) + ") " + */ t1; //TODO: Add text field for the number
                        _PlaylistElements[i].SelectSlide.Clear();
                        for (int g = 0; g < pec.Modes.Length; g++)
                        {
                            _PlaylistElements[i].SelectSlide.AddValue(Enum.GetName(typeof(EGameMode), pec.Modes[g]));
                            if (pec.Modes[g] == pec.Mode)
                                _PlaylistElements[i].SelectSlide.SetSelectionByValueIndex(g);
                        }
                    }
                    else
                    {
                        _PlaylistElements[i].Background.Visible = false;
                        _PlaylistElements[i].Cover.Visible = false;
                        _PlaylistElements[i].SelectSlide.Visible = false;
                        _PlaylistElements[i].Text1.Visible = false;
                        _PlaylistElements[i].Content = -1;
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