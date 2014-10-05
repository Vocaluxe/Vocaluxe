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
using System.Windows.Forms;
using System.Xml.Serialization;
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

    [XmlType("Playlist")]
    public struct SThemePlaylist
    {
        [XmlAttribute(AttributeName = "Name")] public string Name;

        [XmlElement("Rect")] public SRectF Rect;

        [XmlElement("EntryHeight")] public float EntryHeight;

        [XmlElement("SkinBackground")] public string TextureBackgroundName;
        [XmlElement("SkinBackgroundSelected")] public string SelTextureBackgroundName;

        [XmlElement("ColorBackground")] public SThemeColor ColorBackground;
        [XmlElement("SColorBackground")] public SThemeColor SelColorBackground;

        [XmlElement("Text1")] public SThemeText SText1;

        [XmlElement("StaticCover")] public SThemeStatic SStaticCover;
        [XmlElement("StaticPlaylistHeader")] public SThemeStatic SStaticPlaylistHeader;
        [XmlElement("StaticPlaylistFooter")] public SThemeStatic SStaticPlaylistFooter;

        [XmlElement("ButtonPlaylistName")] public SThemeButton SButtonPlaylistName;
        [XmlElement("ButtonPlaylistClose")] public SThemeButton SButtonPlaylistClose;
        [XmlElement("ButtonPlaylistSave")] public SThemeButton SButtonPlaylistSave;
        [XmlElement("ButtonPlaylistDelete")] public SThemeButton SButtonPlaylistDelete;
        [XmlElement("ButtonPlaylistSing")] public SThemeButton SButtonPlaylistSing;

        [XmlElement("SelectSlideGameMode")] public SThemeSelectSlide SSelectSlideGameMode;
    }

    public class CPlaylist : IMenuElement
    {
        private class CPlaylistElementContent
        {
            public IList<EGameMode> Modes;
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

        public bool ThemeLoaded
        {
            get { return _ThemeLoaded; }
        }

        private readonly CObjectInteractions _Interactions;

        private readonly List<CPlaylistElement> _PlaylistElements;
        private readonly List<CPlaylistElementContent> _PlaylistElementContents;

        public SRectF CompleteRect;
        public SRectF Rect;
        public SColorF BackgroundColor;
        public SColorF BackgroundSelColor;

        public CButton ButtonPlaylistName;
        public CButton ButtonPlaylistClose;
        public CButton ButtonPlaylistSave;
        public CButton ButtonPlaylistDelete;
        public CButton ButtonPlaylistSing;
        public CText Text1;
        public CSelectSlide SelectSlideGameMode;
        public CStatic StaticCover;
        public CStatic StaticPlaylistHeader;
        public CStatic StaticPlaylistFooter;

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
                        CBase.Playlist.DeleteSong(ActivePlaylistID, _PlaylistElements[_ChangeOrderSource].Content);
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

            Text1 = new CText(_PartyModeID);
            StaticCover = new CStatic(_PartyModeID);
            StaticPlaylistFooter = new CStatic(_PartyModeID);
            StaticPlaylistHeader = new CStatic(_PartyModeID);
            ButtonPlaylistName = new CButton(_PartyModeID);
            ButtonPlaylistClose = new CButton(_PartyModeID);
            ButtonPlaylistDelete = new CButton(_PartyModeID);
            ButtonPlaylistSave = new CButton(_PartyModeID);
            ButtonPlaylistSing = new CButton(_PartyModeID);
            SelectSlideGameMode = new CSelectSlide(_PartyModeID);

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

        public CPlaylist(SThemePlaylist theme, int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Theme = theme;

            CompleteRect = new SRectF();
            _PlaylistElements = new List<CPlaylistElement>();
            _PlaylistElementContents = new List<CPlaylistElementContent>();

            _Interactions = new CObjectInteractions();
            _ChangeOrderElement = new CPlaylistElement();

            Text1 = new CText(_Theme.SText1, _PartyModeID);
            StaticCover = new CStatic(_Theme.SStaticCover, _PartyModeID);
            StaticPlaylistFooter = new CStatic(_Theme.SStaticPlaylistFooter, _PartyModeID);
            StaticPlaylistHeader = new CStatic(_Theme.SStaticPlaylistHeader, _PartyModeID);
            ButtonPlaylistName = new CButton(_Theme.SButtonPlaylistName, _PartyModeID);
            ButtonPlaylistClose = new CButton(_Theme.SButtonPlaylistClose, _PartyModeID);
            ButtonPlaylistDelete = new CButton(_Theme.SButtonPlaylistDelete, _PartyModeID);
            ButtonPlaylistSave = new CButton(_Theme.SButtonPlaylistSave, _PartyModeID);
            ButtonPlaylistSing = new CButton(_Theme.SButtonPlaylistSing, _PartyModeID);
            SelectSlideGameMode = new CSelectSlide(_Theme.SSelectSlideGameMode, _PartyModeID);


            Visible = false;
            Selected = false;

            LoadTextures();
        }

        public void Init()
        {
            _Interactions.Clear();
            _PrepareList();
            _Interactions.AddStatic(StaticCover);
            _Interactions.AddStatic(StaticPlaylistFooter);
            _Interactions.AddStatic(StaticPlaylistHeader);
            _Interactions.AddButton(ButtonPlaylistName);
            _Interactions.AddButton(ButtonPlaylistClose);
            _Interactions.AddButton(ButtonPlaylistDelete);
            _Interactions.AddButton(ButtonPlaylistSave);
            _Interactions.AddButton(ButtonPlaylistSing);
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
            if (xmlReader.GetValue(item + "/ColorBackground", out _Theme.ColorBackground.Name, String.Empty))
                _ThemeLoaded &= _Theme.ColorBackground.Get(_PartyModeID, out BackgroundColor);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/BackgroundR", ref BackgroundColor.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/BackgroundG", ref BackgroundColor.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/BackgroundB", ref BackgroundColor.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/BackgroundA", ref BackgroundColor.A);
            }
            if (xmlReader.GetValue(item + "/SColorBackground", out _Theme.SelColorBackground.Name, String.Empty))
                _ThemeLoaded &= _Theme.SelColorBackground.Get(_PartyModeID, out BackgroundSelColor);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SBackgroundR", ref BackgroundSelColor.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SBackgroundG", ref BackgroundSelColor.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SBackgroundB", ref BackgroundSelColor.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SBackgroundA", ref BackgroundSelColor.A);
            }

            _ThemeLoaded &= Text1.LoadTheme(item, "TextPart1", xmlReader, skinIndex);

            _ThemeLoaded &= StaticCover.LoadTheme(item, "StaticCover", xmlReader, skinIndex);
            _ThemeLoaded &= StaticPlaylistHeader.LoadTheme(item, "StaticPlaylistHeader", xmlReader, skinIndex);
            _ThemeLoaded &= StaticPlaylistFooter.LoadTheme(item, "StaticPlaylistFooter", xmlReader, skinIndex);

            _ThemeLoaded &= ButtonPlaylistName.LoadTheme(item, "ButtonPlaylistName", xmlReader, skinIndex);
            _ThemeLoaded &= ButtonPlaylistSing.LoadTheme(item, "ButtonPlaylistSing", xmlReader, skinIndex);
            _ThemeLoaded &= ButtonPlaylistClose.LoadTheme(item, "ButtonPlaylistClose", xmlReader, skinIndex);
            _ThemeLoaded &= ButtonPlaylistSave.LoadTheme(item, "ButtonPlaylistSave", xmlReader, skinIndex);
            _ThemeLoaded &= ButtonPlaylistDelete.LoadTheme(item, "ButtonPlaylistDelete", xmlReader, skinIndex);

            _ThemeLoaded &= SelectSlideGameMode.LoadTheme(item, "SelectSlideGameMode", xmlReader, skinIndex);

            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/EntryHeight", ref _Theme.EntryHeight);
            if (_ThemeLoaded)
            {
                _Theme.Name = elementName;
                _Theme.Rect = Rect;
                _Theme.ColorBackground.Color = BackgroundColor;
                _Theme.SelColorBackground.Color = BackgroundSelColor;
                _Theme.SText1 = Text1.GetTheme();
                _Theme.SStaticCover = StaticCover.GetTheme();
                _Theme.SStaticPlaylistFooter = StaticPlaylistFooter.GetTheme();
                _Theme.SStaticPlaylistHeader = StaticPlaylistHeader.GetTheme();
                _Theme.SButtonPlaylistName = ButtonPlaylistName.GetTheme();
                _Theme.SButtonPlaylistSing = ButtonPlaylistSing.GetTheme();
                _Theme.SButtonPlaylistClose = ButtonPlaylistClose.GetTheme();
                _Theme.SButtonPlaylistSave = ButtonPlaylistSave.GetTheme();
                _Theme.SButtonPlaylistDelete = ButtonPlaylistDelete.GetTheme();

                LoadTextures();
            }
            return _ThemeLoaded;
        }

        public void Draw(bool forceDraw = false)
        {
            if (_PlaylistElements.Count <= 0)
                LoadPlaylist(0);
            if (!Visible && CBase.Settings.GetProgramState() != EProgramState.EditTheme && !forceDraw)
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
            return CHelper.IsInBounds(CompleteRect, mouseEvent) || _Interactions.IsMouseOver(mouseEvent);
        }

        public void UnloadTextures()
        {
            Text1.UnloadTextures();
            ButtonPlaylistClose.UnloadTextures();
            ButtonPlaylistDelete.UnloadTextures();
            ButtonPlaylistName.UnloadTextures();
            ButtonPlaylistSave.UnloadTextures();
            ButtonPlaylistSing.UnloadTextures();

            StaticCover.UnloadTextures();
            StaticPlaylistFooter.UnloadTextures();
            StaticPlaylistHeader.UnloadTextures();

            SelectSlideGameMode.UnloadTextures();
        }

        public void LoadTextures()
        {
            _Theme.ColorBackground.Get(_PartyModeID, out BackgroundColor);
            _Theme.SelColorBackground.Get(_PartyModeID, out BackgroundSelColor);

            Rect = _Theme.Rect;

            Text1.LoadTextures();
            ButtonPlaylistClose.LoadTextures();
            ButtonPlaylistDelete.LoadTextures();
            ButtonPlaylistName.LoadTextures();
            ButtonPlaylistSave.LoadTextures();
            ButtonPlaylistSing.LoadTextures();

            StaticCover.LoadTextures();
            StaticPlaylistFooter.LoadTextures();
            StaticPlaylistHeader.LoadTextures();

            SelectSlideGameMode.LoadTextures();

            _UpdateRect();

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
                        ButtonPlaylistName.Text.Text = ButtonPlaylistName.Text.Text + keyEvent.Unicode;
                    else
                    {
                        switch (keyEvent.Key)
                        {
                            case Keys.Enter:
                                CBase.Playlist.SetName(ActivePlaylistID, ButtonPlaylistName.Text.Text);
                                CBase.Playlist.Save(ActivePlaylistID);
                                EditMode = EEditMode.None;
                                ButtonPlaylistName.EditMode = false;
                                break;
                            case Keys.Escape:
                                ButtonPlaylistName.Text.Text = CBase.Playlist.GetName(ActivePlaylistID);
                                EditMode = EEditMode.None;
                                ButtonPlaylistName.EditMode = false;
                                break;
                            case Keys.Delete:
                            case Keys.Back:
                                if (!String.IsNullOrEmpty(ButtonPlaylistName.Text.Text))
                                    ButtonPlaylistName.Text.Text = ButtonPlaylistName.Text.Text.Remove(ButtonPlaylistName.Text.Text.Length - 1);
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
                            CBase.Playlist.DeleteSong(ActivePlaylistID, _PlaylistElements[CurrentPlaylistElement].Content);
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
                                CBase.Playlist.MoveSongUp(ActivePlaylistID, CurrentPlaylistElement + Offset);
                                UpdatePlaylist();

                                var key = new SKeyEvent {Key = Keys.Up};

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
                                CBase.Playlist.MoveSongDown(ActivePlaylistID, CurrentPlaylistElement + Offset);
                                UpdatePlaylist();

                                var key = new SKeyEvent {Key = Keys.Down};

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
                                CBase.Playlist.GetSong(ActivePlaylistID, CurrentPlaylistElement + Offset).GameMode =
                                    _PlaylistElementContents[CurrentPlaylistElement + Offset].Modes[_PlaylistElements[CurrentPlaylistElement].SelectSlide.Selection];
                                UpdatePlaylist();
                            }
                            break;

                        case Keys.Right:
                            _Interactions.HandleInput(keyEvent);
                            CurrentPlaylistElement = _GetSelectedSelectionNr();

                            if (CurrentPlaylistElement != -1)
                            {
                                CBase.Playlist.GetSong(ActivePlaylistID, CurrentPlaylistElement + Offset).GameMode =
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
                        if (ButtonPlaylistName.Selected)
                        {
                            EditMode = EEditMode.PlaylistName;
                            ButtonPlaylistName.EditMode = true;
                        }
                        else
                            ClosePlaylist();
                        break;

                    case Keys.Enter:
                        if (ButtonPlaylistClose.Selected)
                            ClosePlaylist();
                        else if (ButtonPlaylistSing.Selected)
                            _StartPlaylistSongs();
                        else if (ButtonPlaylistSave.Selected)
                            CBase.Playlist.Save(ActivePlaylistID);
                        else if (ButtonPlaylistDelete.Selected)
                        {
                            CBase.Playlist.Delete(ActivePlaylistID);
                            ClosePlaylist();
                        }
                        else if (ButtonPlaylistName.Selected)
                        {
                            if (EditMode != EEditMode.PlaylistName)
                            {
                                EditMode = EEditMode.PlaylistName;
                                ButtonPlaylistName.EditMode = true;
                            }
                            else
                            {
                                EditMode = EEditMode.None;
                                ButtonPlaylistName.EditMode = false;
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
                        CBase.Playlist.DeleteSong(ActivePlaylistID, _PlaylistElements[i].Content);
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
                                CBase.Playlist.GetSong(ActivePlaylistID, CurrentPlaylistElement + Offset).GameMode =
                                    _PlaylistElementContents[CurrentPlaylistElement + Offset].Modes[_PlaylistElements[CurrentPlaylistElement].SelectSlide.Selection];
                                UpdatePlaylist();
                            }
                            if (ButtonPlaylistClose.Selected)
                            {
                                ClosePlaylist();
                                return true;
                            }
                            if (ButtonPlaylistSing.Selected)
                            {
                                _StartPlaylistSongs();
                                return true;
                            }
                            if (ButtonPlaylistSave.Selected)
                            {
                                CBase.Playlist.Save(ActivePlaylistID);
                                return true;
                            }
                            if (ButtonPlaylistDelete.Selected)
                            {
                                CBase.Playlist.Delete(ActivePlaylistID);
                                ClosePlaylist();
                                return true;
                            }
                            if (ButtonPlaylistName.Selected)
                            {
                                EditMode = EEditMode.PlaylistName;
                                ButtonPlaylistName.EditMode = true;
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
                            CSong song = CBase.Songs.GetSongByID(DragAndDropSongID);

                            if (song != null)
                            {
                                var gm = EGameMode.TR_GAMEMODE_NORMAL;
                                if (song.IsDuet)
                                    gm = EGameMode.TR_GAMEMODE_DUET;

                                if (CurrentPlaylistElement != -1)
                                {
                                    CBase.Playlist.InsertSong(ActivePlaylistID, CurrentPlaylistElement + Offset, DragAndDropSongID, gm);
                                    UpdatePlaylist();
                                }
                                else
                                {
                                    if (mouseEvent.Y < _PlaylistElements[0].Background.Rect.Y && Offset == 0)
                                    {
                                        CBase.Playlist.InsertSong(ActivePlaylistID, 0, DragAndDropSongID, gm);
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
                                                CBase.Playlist.AddSong(ActivePlaylistID, DragAndDropSongID, gm);
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
                        _Interactions.SetInteractionToButton(ButtonPlaylistName);
                        CurrentPlaylistElement = -1;
                        if (mouseEvent.LB)
                        {
                            if (ButtonPlaylistName.Selected)
                            {
                                CBase.Playlist.SetName(ActivePlaylistID, ButtonPlaylistName.Text.Text);
                                CBase.Playlist.Save(ActivePlaylistID);
                                EditMode = EEditMode.None;
                                return true;
                            }
                        }
                        else if (mouseEvent.RB)
                        {
                            if (ButtonPlaylistName.Selected)
                            {
                                ButtonPlaylistName.Text.Text = CBase.Playlist.GetName(ActivePlaylistID);
                                EditMode = EEditMode.None;
                                ButtonPlaylistName.EditMode = false;
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
                                CBase.Playlist.MoveSong(ActivePlaylistID, _ChangeOrderSource, CurrentPlaylistElement + Offset);
                                UpdatePlaylist();
                            }
                            else if (CurrentPlaylistElement == -1)
                            {
                                if (mouseEvent.Y < _PlaylistElements[0].Background.Rect.Y && Offset == 0)
                                    CBase.Playlist.MoveSong(ActivePlaylistID, _ChangeOrderSource, 0);
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
                                            CBase.Playlist.MoveSong(ActivePlaylistID, _ChangeOrderSource, _PlaylistElementContents.Count - 1);
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
                var en = new CPlaylistElement
                    {
                        Background = new CStatic(_PartyModeID, _Theme.TextureBackgroundName, BackgroundColor,
                                                 new SRectF(Rect.X, Rect.Y + (i * _Theme.EntryHeight), Rect.W, _Theme.EntryHeight, Rect.Z)),
                        Cover = new CStatic(StaticCover)
                    };

                en.Cover.Rect.Y += Rect.Y + (i * _Theme.EntryHeight);
                en.Cover.Rect.X += Rect.X;

                en.Text1 = new CText(Text1);
                en.Text1.X += Rect.X;
                en.Text1.Y += Rect.Y + (i * _Theme.EntryHeight);

                en.SelectSlide = new CSelectSlide(SelectSlideGameMode);
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
            if (!CBase.Playlist.Exists(playlistID))
                return false;
            ActivePlaylistID = playlistID;
            ButtonPlaylistName.Text.Text = CBase.Playlist.GetName(ActivePlaylistID);
            _PlaylistElementContents.Clear();
            for (int i = 0; i < CBase.Playlist.GetSongCount(ActivePlaylistID); i++)
            {
                var pec = new CPlaylistElementContent
                    {
                        SongID = CBase.Playlist.GetSong(ActivePlaylistID, i).SongID,
                        Modes = CBase.Songs.GetSongByID(CBase.Playlist.GetSong(ActivePlaylistID, i).SongID).AvailableGameModes,
                        Mode = CBase.Playlist.GetSong(ActivePlaylistID, i).GameMode
                    };
                _PlaylistElementContents.Add(pec);
            }
            Update();
            return true;
        }

        public void UpdatePlaylist()
        {
            _PlaylistElementContents.Clear();
            for (int i = 0; i < CBase.Playlist.GetSongCount(ActivePlaylistID); i++)
            {
                var pec = new CPlaylistElementContent {SongID = CBase.Playlist.GetSong(ActivePlaylistID, i).SongID};
                pec.Modes = CBase.Songs.GetSongByID(pec.SongID).AvailableGameModes;
                pec.Mode = CBase.Playlist.GetSong(ActivePlaylistID, i).GameMode;
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

            if (CBase.Playlist.Exists(ActivePlaylistID))
            {
                for (int i = 0; i < CBase.Playlist.GetSongCount(ActivePlaylistID); i++)
                    CBase.Game.AddSong(CBase.Playlist.GetSong(ActivePlaylistID, i).SongID, CBase.Playlist.GetSong(ActivePlaylistID, i).GameMode);
                if (CBase.Game.GetNumSongs() > 0)
                    CBase.Graphics.FadeTo(EScreens.ScreenNames); //TODO: What is if someone uses that in PartyMode?
            }
        }

        private void _StartPlaylistSong(int selected)
        {
            CBase.Game.Reset();
            CBase.Game.ClearSongs();

            CBase.Game.AddSong(CBase.Playlist.GetSong(ActivePlaylistID, selected).SongID, CBase.Playlist.GetSong(ActivePlaylistID, selected).GameMode);

            if (CBase.Game.GetNumSongs() > 0)
                CBase.Graphics.FadeTo(EScreens.ScreenNames);
        }

        public void Update()
        {
            if (CBase.Playlist.Exists(ActivePlaylistID))
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
                        string t1 = CBase.Language.Translate(Text1.Text).Replace("%a", song.Artist).Replace("%t", song.Title);
                        _PlaylistElements[i].Text1.Text = /*(Offset + i + 1) + ") " + */ t1; //TODO: Add text field for the number
                        _PlaylistElements[i].SelectSlide.Clear();
                        for (int g = 0; g < pec.Modes.Count; g++)
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

        public SThemePlaylist GetTheme()
        {
            return _Theme;
        }

        private void _UpdateRect()
        {
            //Check for highest x/y-coords
            CompleteRect = Rect;
            //ButtonPlaylistClose
            if (ButtonPlaylistClose.Rect.X < CompleteRect.X)
                CompleteRect.X = ButtonPlaylistClose.Rect.X;
            if (ButtonPlaylistClose.Rect.Y < CompleteRect.Y)
                CompleteRect.Y = ButtonPlaylistClose.Rect.Y;
            if (ButtonPlaylistClose.Rect.W + ButtonPlaylistClose.Rect.X > CompleteRect.W + CompleteRect.X)
                CompleteRect.W = ButtonPlaylistClose.Rect.W + ButtonPlaylistClose.Rect.X - CompleteRect.X;
            if (ButtonPlaylistClose.Rect.Y + ButtonPlaylistClose.Rect.H > CompleteRect.Y + CompleteRect.H)
                CompleteRect.H = ButtonPlaylistClose.Rect.H + ButtonPlaylistClose.Rect.Y - CompleteRect.Y;
            //ButtonPlaylistName
            if (ButtonPlaylistName.Rect.X < CompleteRect.X)
                CompleteRect.X = ButtonPlaylistName.Rect.X;
            if (ButtonPlaylistName.Rect.Y < CompleteRect.Y)
                CompleteRect.Y = ButtonPlaylistName.Rect.Y;
            if (ButtonPlaylistName.Rect.W + ButtonPlaylistName.Rect.X > CompleteRect.W + CompleteRect.X)
                CompleteRect.W = ButtonPlaylistName.Rect.W + ButtonPlaylistName.Rect.X - CompleteRect.X;
            if (ButtonPlaylistName.Rect.Y + ButtonPlaylistName.Rect.H > CompleteRect.Y + CompleteRect.H)
                CompleteRect.H = ButtonPlaylistName.Rect.H + ButtonPlaylistName.Rect.Y - CompleteRect.Y;
            //ButtonPlaylistSing
            if (ButtonPlaylistSing.Rect.X < CompleteRect.X)
                CompleteRect.X = ButtonPlaylistSing.Rect.X;
            if (ButtonPlaylistSing.Rect.Y < CompleteRect.Y)
                CompleteRect.Y = ButtonPlaylistSing.Rect.Y;
            if (ButtonPlaylistSing.Rect.W + ButtonPlaylistSing.Rect.X > CompleteRect.W + CompleteRect.X)
                CompleteRect.W = ButtonPlaylistSing.Rect.W + ButtonPlaylistSing.Rect.X - CompleteRect.X;
            if (ButtonPlaylistSing.Rect.Y + ButtonPlaylistSing.Rect.H > CompleteRect.Y + CompleteRect.H)
                CompleteRect.H = ButtonPlaylistSing.Rect.H + ButtonPlaylistSing.Rect.Y - CompleteRect.Y;
            //ButtonPlaylistSave
            if (ButtonPlaylistSave.Rect.X < CompleteRect.X)
                CompleteRect.X = ButtonPlaylistSave.Rect.X;
            if (ButtonPlaylistSave.Rect.Y < CompleteRect.Y)
                CompleteRect.Y = ButtonPlaylistSave.Rect.Y;
            if (ButtonPlaylistSave.Rect.W + ButtonPlaylistSave.Rect.X > CompleteRect.W + CompleteRect.X)
                CompleteRect.W = ButtonPlaylistSave.Rect.W + ButtonPlaylistSave.Rect.X - CompleteRect.X;
            if (ButtonPlaylistSave.Rect.Y + ButtonPlaylistSave.Rect.H > CompleteRect.Y + CompleteRect.H)
                CompleteRect.H = ButtonPlaylistSave.Rect.H + ButtonPlaylistSave.Rect.Y - CompleteRect.Y;
            //ButtonPlaylistDelete
            if (ButtonPlaylistDelete.Rect.X < CompleteRect.X)
                CompleteRect.X = ButtonPlaylistDelete.Rect.X;
            if (ButtonPlaylistDelete.Rect.Y < CompleteRect.Y)
                CompleteRect.Y = ButtonPlaylistDelete.Rect.Y;
            if (ButtonPlaylistDelete.Rect.W + ButtonPlaylistDelete.Rect.X > CompleteRect.W + CompleteRect.X)
                CompleteRect.W = ButtonPlaylistDelete.Rect.W + ButtonPlaylistDelete.Rect.X - CompleteRect.X;
            if (ButtonPlaylistDelete.Rect.Y + ButtonPlaylistDelete.Rect.H > CompleteRect.Y + CompleteRect.H)
                CompleteRect.H = ButtonPlaylistDelete.Rect.H + ButtonPlaylistDelete.Rect.Y - CompleteRect.Y;
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