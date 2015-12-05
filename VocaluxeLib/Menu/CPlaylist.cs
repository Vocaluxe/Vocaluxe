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
using VocaluxeLib.Xml;

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

        public SPlaylistSong ToStruct()
        {
            CSong song = CBase.Songs.GetSongByID(SongID);
            if (song == null)
                throw new Exception("Can't find Song. This should never happen!");
            return new SPlaylistSong {Artist = song.Artist, Title = song.Title, GameMode = GameMode};
        }
    }

    [XmlType("Playlist")]
    public struct SThemePlaylist
    {
        [XmlAttribute(AttributeName = "Name")] public string Name;

        public SRectF Rect;

        public float EntryHeight;

        public string SkinBackground;
        public string SkinBackgroundSelected;

        public SThemeColor ColorBackground;
        public SThemeColor SelColorBackground;

        public SThemeText Text1;

        public SThemeStatic StaticCover;
        public SThemeStatic StaticPlaylistHeader;
        public SThemeStatic StaticPlaylistFooter;

        public SThemeButton ButtonPlaylistName;
        public SThemeButton ButtonPlaylistClose;
        public SThemeButton ButtonPlaylistSave;
        public SThemeButton ButtonPlaylistDelete;
        public SThemeButton ButtonPlaylistSing;

        public SThemeSelectSlide SelectSlideGameMode;
    }

    //TODO: Refactor this class, it is almost unreadable and may not work in some occasions
    public class CPlaylist : CObjectInteractions, IMenuElement, IThemeable
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

                Cover.X += diffX;
                Cover.Y += diffY;

                Background.X += diffX;
                Background.Y += diffY;

                SelectSlide.X += diffX;
                SelectSlide.Y += diffY;

                Text1.X += diffX;
                Text1.Y += diffY;
            }
        }

        private readonly int _PartyModeID;
        private SThemePlaylist _Theme;

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        public bool ThemeLoaded { get; private set; }

        private readonly List<CPlaylistElement> _PlaylistElements = new List<CPlaylistElement>();
        private readonly List<CPlaylistElementContent> _PlaylistElementContents = new List<CPlaylistElementContent>();

        private SRectF _CompleteRect;
        private SColorF _BackgroundColor;
        private SColorF _BackgroundSelColor;

        private readonly CButton _ButtonPlaylistName;
        private readonly CButton _ButtonPlaylistClose;
        private readonly CButton _ButtonPlaylistSave;
        private readonly CButton _ButtonPlaylistDelete;
        private readonly CButton _ButtonPlaylistSing;
        private readonly CText _Text1;
        private readonly CSelectSlide _SelectSlideGameMode;
        private readonly CStatic _StaticPlaylistHeader;
        private readonly CStatic _StaticPlaylistFooter;

        public SRectF Rect
        {
            get { return _CompleteRect; }
        }
        public SRectF MaxRect { get; set; }
        private bool _Selectable = true;
        public bool Selectable
        {
            get { return Visible && _Selectable; }
            set { _Selectable = value; }
        }
        public bool Visible
        {
            get { return _Active; }
            set { _Active = value; }
        }
        public bool Highlighted { get; set; }
        private bool _Selected;
        public bool Selected
        {
            get { return _Selected; }
            set
            {
                _Selected = value;
                _CurrentPlaylistElement = _GetSelectedElementNr();

                if (!value)
                {
                    if (_EditMode == EEditMode.ChangeOrder && _ChangeOrderSource != -1 && _PlaylistElements.Count > _ChangeOrderSource)
                    {
                        CBase.Playlist.DeleteSong(ActivePlaylistID, _PlaylistElements[_ChangeOrderSource].Content);
                        UpdatePlaylist();
                    }
                    _ChangeOrderSource = -1;
                    _ChangeOrderElement = null;
                    _EditMode = EEditMode.None;
                }
                else if (_CurrentPlaylistElement == -1 && _PlaylistElements.Count > 0)
                    _SelectElement(_PlaylistElements[0].SelectSlide);
            }
        }

        private EEditMode _EditMode;
        public int ActivePlaylistID = -1;
        private int _Offset;
        private int _CurrentPlaylistElement = -1;

        private CPlaylistElement _ChangeOrderElement;
        private int _ChangeOrderSource = -1;
        private int _OldMousePosX;
        private int _OldMousePosY;

        public int DragAndDropSongID = -1;

        //private static
        public CPlaylist(int partyModeID)
        {
            _PartyModeID = partyModeID;

            _Text1 = new CText(_PartyModeID);
            _StaticPlaylistFooter = new CStatic(_PartyModeID);
            _StaticPlaylistHeader = new CStatic(_PartyModeID);
            _ButtonPlaylistName = new CButton(_PartyModeID);
            _ButtonPlaylistClose = new CButton(_PartyModeID);
            _ButtonPlaylistDelete = new CButton(_PartyModeID);
            _ButtonPlaylistSave = new CButton(_PartyModeID);
            _ButtonPlaylistSing = new CButton(_PartyModeID);
            _SelectSlideGameMode = new CSelectSlide(_PartyModeID);

            Visible = false;
            Selected = false;
        }

        public CPlaylist(SThemePlaylist theme, int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Theme = theme;

            _Text1 = new CText(_Theme.Text1, _PartyModeID);
            _StaticPlaylistFooter = new CStatic(_Theme.StaticPlaylistFooter, _PartyModeID);
            _StaticPlaylistHeader = new CStatic(_Theme.StaticPlaylistHeader, _PartyModeID);
            _ButtonPlaylistName = new CButton(_Theme.ButtonPlaylistName, _PartyModeID);
            _ButtonPlaylistClose = new CButton(_Theme.ButtonPlaylistClose, _PartyModeID);
            _ButtonPlaylistDelete = new CButton(_Theme.ButtonPlaylistDelete, _PartyModeID);
            _ButtonPlaylistSave = new CButton(_Theme.ButtonPlaylistSave, _PartyModeID);
            _ButtonPlaylistSing = new CButton(_Theme.ButtonPlaylistSing, _PartyModeID);
            _SelectSlideGameMode = new CSelectSlide(_Theme.SelectSlideGameMode, _PartyModeID);

            _AddStatic(_StaticPlaylistFooter);
            _AddStatic(_StaticPlaylistHeader);
            _AddButton(_ButtonPlaylistName);
            _AddButton(_ButtonPlaylistClose);
            _AddButton(_ButtonPlaylistDelete);
            _AddButton(_ButtonPlaylistSave);
            _AddButton(_ButtonPlaylistSing);

            Visible = false;
            Selected = false;
            ThemeLoaded = true;
        }

        public bool LoadTheme(string xmlPath, string elementName, CXmlReader xmlReader)
        {
            string item = xmlPath + "/" + elementName;
            ThemeLoaded = true;

            ThemeLoaded &= xmlReader.GetValue(item + "/SkinBackground", out _Theme.SkinBackground, String.Empty);
            ThemeLoaded &= xmlReader.GetValue(item + "/SkinBackgroundSelected", out _Theme.SkinBackgroundSelected, String.Empty);

            ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/X", ref _Theme.Rect.X);
            ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Y", ref _Theme.Rect.Y);
            ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Z", ref _Theme.Rect.Z);
            ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/W", ref _Theme.Rect.W);
            ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/H", ref _Theme.Rect.H);
            if (xmlReader.GetValue(item + "/ColorBackground", out _Theme.ColorBackground.Name, String.Empty))
                ThemeLoaded &= _Theme.ColorBackground.Get(_PartyModeID, out _BackgroundColor);
            else
            {
                ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/BackgroundR", ref _BackgroundColor.R);
                ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/BackgroundG", ref _BackgroundColor.G);
                ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/BackgroundB", ref _BackgroundColor.B);
                ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/BackgroundA", ref _BackgroundColor.A);
            }
            if (xmlReader.GetValue(item + "/SColorBackground", out _Theme.SelColorBackground.Name, String.Empty))
                ThemeLoaded &= _Theme.SelColorBackground.Get(_PartyModeID, out _BackgroundSelColor);
            else
            {
                ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SBackgroundR", ref _BackgroundSelColor.R);
                ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SBackgroundG", ref _BackgroundSelColor.G);
                ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SBackgroundB", ref _BackgroundSelColor.B);
                ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SBackgroundA", ref _BackgroundSelColor.A);
            }

            ThemeLoaded &= _Text1.LoadTheme(item, "TextPart1", xmlReader);

            CStatic tmpStatic = new CStatic(_PartyModeID);
            ThemeLoaded &= tmpStatic.LoadTheme(item, "StaticCover", xmlReader);
            ThemeLoaded &= _StaticPlaylistHeader.LoadTheme(item, "StaticPlaylistHeader", xmlReader);
            ThemeLoaded &= _StaticPlaylistFooter.LoadTheme(item, "StaticPlaylistFooter", xmlReader);

            ThemeLoaded &= _ButtonPlaylistName.LoadTheme(item, "ButtonPlaylistName", xmlReader);
            ThemeLoaded &= _ButtonPlaylistSing.LoadTheme(item, "ButtonPlaylistSing", xmlReader);
            ThemeLoaded &= _ButtonPlaylistClose.LoadTheme(item, "ButtonPlaylistClose", xmlReader);
            ThemeLoaded &= _ButtonPlaylistSave.LoadTheme(item, "ButtonPlaylistSave", xmlReader);
            ThemeLoaded &= _ButtonPlaylistDelete.LoadTheme(item, "ButtonPlaylistDelete", xmlReader);

            ThemeLoaded &= _SelectSlideGameMode.LoadTheme(item, "SelectSlideGameMode", xmlReader);

            ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/EntryHeight", ref _Theme.EntryHeight);
            if (ThemeLoaded)
            {
                _Theme.Name = elementName;
                _Theme.ColorBackground.Color = _BackgroundColor;
                _Theme.SelColorBackground.Color = _BackgroundSelColor;
                _Theme.StaticCover = (SThemeStatic)tmpStatic.GetTheme();
                _ReadSubThemeElements();

                LoadSkin();
            }
            return ThemeLoaded;
        }

        private void _ReadSubThemeElements()
        {
            _Theme.Text1 = (SThemeText)_Text1.GetTheme();
            _Theme.StaticPlaylistFooter = (SThemeStatic)_StaticPlaylistFooter.GetTheme();
            _Theme.StaticPlaylistHeader = (SThemeStatic)_StaticPlaylistHeader.GetTheme();
            _Theme.ButtonPlaylistName = (SThemeButton)_ButtonPlaylistName.GetTheme();
            _Theme.ButtonPlaylistSing = (SThemeButton)_ButtonPlaylistSing.GetTheme();
            _Theme.ButtonPlaylistClose = (SThemeButton)_ButtonPlaylistClose.GetTheme();
            _Theme.ButtonPlaylistSave = (SThemeButton)_ButtonPlaylistSave.GetTheme();
            _Theme.ButtonPlaylistDelete = (SThemeButton)_ButtonPlaylistDelete.GetTheme();
        }

        public void UpdateGame()
        {
            if (_PlaylistElements.Count <= 0)
                LoadPlaylist(0);
            for (int i = 0; i < _PlaylistElements.Count; i++)
            {
                if (i == _CurrentPlaylistElement && _Selected)
                {
                    _PlaylistElements[i].Background.Texture = CBase.Themes.GetSkinTexture(_Theme.SkinBackgroundSelected, _PartyModeID);
                    _PlaylistElements[i].Background.Color = _BackgroundSelColor;
                }
                else
                {
                    _PlaylistElements[i].Background.Texture = CBase.Themes.GetSkinTexture(_Theme.SkinBackground, _PartyModeID);
                    _PlaylistElements[i].Background.Color = _BackgroundColor;
                }
            }
        }

        public override void Draw()
        {
            base.Draw();

            if (_ChangeOrderElement != null)
                _ChangeOrderElement.Draw();
        }

        public bool IsMouseOver(SMouseEvent mouseEvent)
        {
            return CHelper.IsInBounds(_CompleteRect, mouseEvent);
        }

        public void UnloadSkin()
        {
            _Text1.UnloadSkin();
            _ButtonPlaylistClose.UnloadSkin();
            _ButtonPlaylistDelete.UnloadSkin();
            _ButtonPlaylistName.UnloadSkin();
            _ButtonPlaylistSave.UnloadSkin();
            _ButtonPlaylistSing.UnloadSkin();

            _StaticPlaylistFooter.UnloadSkin();
            _StaticPlaylistHeader.UnloadSkin();

            _SelectSlideGameMode.UnloadSkin();
            _ClearElements();
        }

        public void LoadSkin()
        {
            _Theme.ColorBackground.Get(_PartyModeID, out _BackgroundColor);
            _Theme.SelColorBackground.Get(_PartyModeID, out _BackgroundSelColor);

            MaxRect = _Theme.Rect;

            _Text1.LoadSkin();
            _ButtonPlaylistClose.LoadSkin();
            _ButtonPlaylistDelete.LoadSkin();
            _ButtonPlaylistName.LoadSkin();
            _ButtonPlaylistSave.LoadSkin();
            _ButtonPlaylistSing.LoadSkin();

            _StaticPlaylistFooter.LoadSkin();
            _StaticPlaylistHeader.LoadSkin();

            _SelectSlideGameMode.LoadSkin();

            _UpdateRect();

            _PrepareList();
        }

        public void ReloadSkin()
        {
            UnloadSkin();
            LoadSkin();
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            if (!Selected)
                return false;
            //Active EditMode ignores other input!
            if (_EditMode == EEditMode.PlaylistName)
            {
                if (keyEvent.KeyPressed && !Char.IsControl(keyEvent.Unicode))
                    _ButtonPlaylistName.Text.Text = _ButtonPlaylistName.Text.Text + keyEvent.Unicode;
                else
                {
                    switch (keyEvent.Key)
                    {
                        case Keys.Enter:
                            CBase.Playlist.SetName(ActivePlaylistID, _ButtonPlaylistName.Text.Text);
                            CBase.Playlist.Save(ActivePlaylistID);
                            _EditMode = EEditMode.None;
                            _ButtonPlaylistName.EditMode = false;
                            break;
                        case Keys.Escape:
                            _ButtonPlaylistName.Text.Text = CBase.Playlist.GetName(ActivePlaylistID);
                            _EditMode = EEditMode.None;
                            _ButtonPlaylistName.EditMode = false;
                            break;
                        case Keys.Delete:
                        case Keys.Back:
                            if (!String.IsNullOrEmpty(_ButtonPlaylistName.Text.Text))
                                _ButtonPlaylistName.Text.Text = _ButtonPlaylistName.Text.Text.Remove(_ButtonPlaylistName.Text.Text.Length - 1);
                            break;
                        default:
                            return false;
                    }
                }
                return true;
            }
            if (_CurrentPlaylistElement == -1 || _PlaylistElementContents.Count == 0)
            {
                //no song is selected
                bool handled = base.HandleInput(keyEvent);
                _CurrentPlaylistElement = _GetSelectedElementNr();

                if (_CurrentPlaylistElement != -1 || handled)
                    return true;
            }
            else if (_CurrentPlaylistElement != -1)
            {
                //a song is selected
                int scrollLimit = _PlaylistElements.Count / 2;

                //special actions if a song is selected
                switch (keyEvent.Key)
                {
                    case Keys.Up:
                        if (keyEvent.ModShift)
                        {
                            _SelectElement(_PlaylistElements[0].SelectSlide);
                            base.HandleInput(keyEvent);
                            _CurrentPlaylistElement = _GetSelectedElementNr();
                        }
                        else if (_CurrentPlaylistElement > scrollLimit || _PlaylistElementContents.Count == 0)
                        {
                            base.HandleInput(keyEvent);
                            _CurrentPlaylistElement = _GetSelectedElementNr();
                        }
                        else if (_CurrentPlaylistElement <= scrollLimit)
                        {
                            if (_Offset > 0)
                            {
                                _Offset--;
                                _Update();
                            }
                            else
                            {
                                base.HandleInput(keyEvent);
                                _CurrentPlaylistElement = _GetSelectedElementNr();
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
                                    _SelectElement(_PlaylistElements[0].SelectSlide);
                                    base.HandleInput(keyEvent);
                                    _CurrentPlaylistElement = _GetSelectedElementNr();
                                }
                            }
                        }
                        else if (_CurrentPlaylistElement >= scrollLimit)
                        {
                            if (_Offset < _PlaylistElementContents.Count - _PlaylistElements.Count)
                            {
                                _Offset++;
                                _Update();
                            }
                            else
                            {
                                base.HandleInput(keyEvent);
                                _CurrentPlaylistElement = _GetSelectedElementNr();
                            }
                        }
                        else if (_CurrentPlaylistElement < scrollLimit)
                        {
                            base.HandleInput(keyEvent);
                            _CurrentPlaylistElement = _GetSelectedElementNr();
                        }
                        break;

                    case Keys.Delete:
                        CBase.Playlist.DeleteSong(ActivePlaylistID, _PlaylistElements[_CurrentPlaylistElement].Content);
                        UpdatePlaylist();

                        if (_Offset > 0)
                            _Offset--;

                        _Update();

                        if (_PlaylistElementContents.Count - 1 < _CurrentPlaylistElement)
                            _CurrentPlaylistElement = _PlaylistElementContents.Count - 1;

                        if (_CurrentPlaylistElement != -1)
                            _SelectElement(_PlaylistElements[_CurrentPlaylistElement].SelectSlide);
                        break;

                    case Keys.Back:
                        ClosePlaylist(); //really? or better global?
                        break;

                    case Keys.Enter:
                        _StartPlaylistSong(_CurrentPlaylistElement);
                        break;

                    case Keys.Add: //move the selected song up
                        if (_PlaylistElementContents.Count > 1 && (_CurrentPlaylistElement > 0 || _Offset > 0))
                        {
                            CBase.Playlist.MoveSongUp(ActivePlaylistID, _CurrentPlaylistElement + _Offset);
                            UpdatePlaylist();

                            var key = new SKeyEvent {Key = Keys.Up};

                            if (_CurrentPlaylistElement > scrollLimit)
                            {
                                base.HandleInput(key);
                                _CurrentPlaylistElement = _GetSelectedElementNr();
                            }
                            else if (_CurrentPlaylistElement <= scrollLimit)
                            {
                                if (_Offset > 0)
                                {
                                    _Offset--;
                                    _Update();
                                }
                                else
                                {
                                    base.HandleInput(key);
                                    _CurrentPlaylistElement = _GetSelectedElementNr();
                                }
                            }
                        }
                        break;

                    case Keys.Subtract: //move the selected song down
                        if (_PlaylistElementContents.Count > 1 && _CurrentPlaylistElement + _Offset < _PlaylistElementContents.Count - 1)
                        {
                            CBase.Playlist.MoveSongDown(ActivePlaylistID, _CurrentPlaylistElement + _Offset);
                            UpdatePlaylist();

                            var key = new SKeyEvent {Key = Keys.Down};

                            if (_CurrentPlaylistElement >= scrollLimit)
                            {
                                if (_Offset < _PlaylistElementContents.Count - _PlaylistElements.Count)
                                {
                                    _Offset++;
                                    _Update();
                                }
                                else
                                {
                                    base.HandleInput(key);
                                    _CurrentPlaylistElement = _GetSelectedElementNr();
                                }
                            }
                            else if (_CurrentPlaylistElement < scrollLimit)
                            {
                                base.HandleInput(key);
                                _CurrentPlaylistElement = _GetSelectedElementNr();
                            }
                        }
                        break;

                    case Keys.PageUp: //scroll up
                        if (_PlaylistElementContents.Count > 0)
                        {
                            _Offset -= _PlaylistElements.Count;

                            if (_Offset < 0)
                                _Offset = 0;

                            _Update();
                            _CurrentPlaylistElement = 0;
                        }
                        break;

                    case Keys.PageDown: //scroll down
                        if (_PlaylistElementContents.Count > 0)
                        {
                            _Offset += _PlaylistElements.Count;

                            if (_Offset > _PlaylistElementContents.Count - _PlaylistElements.Count)
                                _Offset = _PlaylistElementContents.Count - _PlaylistElements.Count;

                            if (_Offset < 0)
                                _Offset = 0;

                            _Update();

                            for (int i = _PlaylistElements.Count - 1; i >= 0; i--)
                            {
                                if (_PlaylistElements[i].SelectSlide.Visible)
                                {
                                    _CurrentPlaylistElement = i;
                                    break;
                                }
                            }
                        }
                        break;

                    case Keys.Left:
                        base.HandleInput(keyEvent);
                        _CurrentPlaylistElement = _GetSelectedElementNr();

                        if (_CurrentPlaylistElement != -1)
                        {
                            CBase.Playlist.GetSong(ActivePlaylistID, _CurrentPlaylistElement + _Offset).GameMode =
                                _PlaylistElementContents[_CurrentPlaylistElement + _Offset].Modes[_PlaylistElements[_CurrentPlaylistElement].SelectSlide.Selection];
                            UpdatePlaylist();
                        }
                        break;

                    case Keys.Right:
                        base.HandleInput(keyEvent);
                        _CurrentPlaylistElement = _GetSelectedElementNr();

                        if (_CurrentPlaylistElement != -1)
                        {
                            CBase.Playlist.GetSong(ActivePlaylistID, _CurrentPlaylistElement + _Offset).GameMode =
                                _PlaylistElementContents[_CurrentPlaylistElement + _Offset].Modes[_PlaylistElements[_CurrentPlaylistElement].SelectSlide.Selection];
                            UpdatePlaylist();
                        }
                        break;
                    default:
                        return false;
                }
                return true;
            }

            //default actions
            switch (keyEvent.Key)
            {
                case Keys.Back:
                    if (_ButtonPlaylistName.Selected)
                    {
                        _EditMode = EEditMode.PlaylistName;
                        _ButtonPlaylistName.EditMode = true;
                        _ChangeOrderElement = null;
                    }
                    else
                        ClosePlaylist();
                    break;

                case Keys.Enter:
                    if (_ButtonPlaylistClose.Selected)
                        ClosePlaylist();
                    else if (_ButtonPlaylistSing.Selected)
                        _StartPlaylistSongs();
                    else if (_ButtonPlaylistSave.Selected)
                        CBase.Playlist.Save(ActivePlaylistID);
                    else if (_ButtonPlaylistDelete.Selected)
                    {
                        CBase.Playlist.Delete(ActivePlaylistID);
                        ClosePlaylist();
                    }
                    else if (_ButtonPlaylistName.Selected)
                    {
                        _ChangeOrderElement = null;
                        if (_EditMode != EEditMode.PlaylistName)
                        {
                            _EditMode = EEditMode.PlaylistName;
                            _ButtonPlaylistName.EditMode = true;
                        }
                        else
                        {
                            _EditMode = EEditMode.None;
                            _ButtonPlaylistName.EditMode = false;
                        }
                    }
                    break;
                case Keys.PageDown:
                    _SetSelectionToLastEntry();
                    break;
                case Keys.PageUp:
                    _SetSelectionToFirstEntry();
                    break;
                default:
                    return false;
            }
            return true;
        }

        private void _SetSelectionToLastEntry()
        {
            if (_PlaylistElementContents.Count == 0)
                return;

            int off = _PlaylistElementContents.Count - _PlaylistElements.Count;
            _Offset = off >= 0 ? off : 0;

            _Update();

            for (int i = _PlaylistElements.Count - 1; i >= 0; i--)
            {
                if (_PlaylistElements[i].SelectSlide.Visible)
                {
                    _CurrentPlaylistElement = i;
                    return;
                }
            }
        }

        private void _SetSelectionToFirstEntry()
        {
            if (_PlaylistElementContents.Count == 0)
                return;

            _Offset = 0;
            _Update();

            _CurrentPlaylistElement = 0;
        }

        private int _GetSelectedElementNr()
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
            _Offset = off >= 0 ? off : 0;

            _Update();
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);

            if (CHelper.IsInBounds(_CompleteRect, mouseEvent) && Visible)
            {
                //Scroll
                if (mouseEvent.Wheel > 0)
                {
                    if (_PlaylistElements.Count + _Offset + mouseEvent.Wheel <= _PlaylistElementContents.Count)
                    {
                        _Offset += mouseEvent.Wheel;
                        _Update();
                    }
                    return true;
                }
                if (mouseEvent.Wheel < 0)
                {
                    if (_Offset + mouseEvent.Wheel >= 0)
                    {
                        _Offset += mouseEvent.Wheel;
                        _Update();
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
                        _CurrentPlaylistElement = i;
                        _SelectElement(_PlaylistElements[_CurrentPlaylistElement].SelectSlide);
                        ProcessMouseMove(mouseEvent.X, mouseEvent.Y);
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
                    _CurrentPlaylistElement = -1;

                switch (_EditMode)
                {
                        //Normal mode
                    case EEditMode.None:

                        //LB actions
                        if (mouseEvent.LB)
                        {
                            if (_CurrentPlaylistElement != -1)
                            {
                                CBase.Playlist.GetSong(ActivePlaylistID, _CurrentPlaylistElement + _Offset).GameMode =
                                    _PlaylistElementContents[_CurrentPlaylistElement + _Offset].Modes[_PlaylistElements[_CurrentPlaylistElement].SelectSlide.Selection];
                                UpdatePlaylist();
                            }
                            if (_ButtonPlaylistClose.Selected)
                            {
                                ClosePlaylist();
                                return true;
                            }
                            if (_ButtonPlaylistSing.Selected)
                            {
                                _StartPlaylistSongs();
                                return true;
                            }
                            if (_ButtonPlaylistSave.Selected)
                            {
                                CBase.Playlist.Save(ActivePlaylistID);
                                return true;
                            }
                            if (_ButtonPlaylistDelete.Selected)
                            {
                                CBase.Playlist.Delete(ActivePlaylistID);
                                ClosePlaylist();
                                return true;
                            }
                            if (_ButtonPlaylistName.Selected)
                            {
                                _EditMode = EEditMode.PlaylistName;
                                _ButtonPlaylistName.EditMode = true;
                                return true;
                            }
                        }

                        //Start selected song with double click
                        if (mouseEvent.LD && _CurrentPlaylistElement != -1)
                            _StartPlaylistSong(_CurrentPlaylistElement);

                        //Change order with holding LB
                        if (mouseEvent.LBH && _CurrentPlaylistElement != -1 && _PlaylistElementContents.Count > 0 && DragAndDropSongID == -1)
                        {
                            _ChangeOrderSource = _CurrentPlaylistElement + _Offset;

                            //Update of Drag/Drop-Texture
                            if (_ChangeOrderSource >= _PlaylistElementContents.Count)
                                return true;

                            _ChangeOrderElement = new CPlaylistElement(_PlaylistElements[_CurrentPlaylistElement])
                                {
                                    Background = {Z = CBase.Settings.GetZNear()},
                                    Cover = {Z = CBase.Settings.GetZNear()},
                                    SelectSlide = {Z = CBase.Settings.GetZNear()},
                                    Text1 = {Z = CBase.Settings.GetZNear()}
                                };

                            _ChangeOrderElement.Background.Texture = CBase.Themes.GetSkinTexture(_Theme.SkinBackground, _PartyModeID);
                            _ChangeOrderElement.Background.Color = _BackgroundColor;

                            _OldMousePosX = mouseEvent.X;
                            _OldMousePosY = mouseEvent.Y;

                            _EditMode = EEditMode.ChangeOrder;
                        }

                        if (!mouseEvent.LBH && DragAndDropSongID != -1)
                        {
                            CSong song = CBase.Songs.GetSongByID(DragAndDropSongID);

                            if (song != null)
                            {
                                var gm = EGameMode.TR_GAMEMODE_NORMAL;
                                if (song.IsDuet)
                                    gm = EGameMode.TR_GAMEMODE_DUET;

                                if (_CurrentPlaylistElement != -1)
                                {
                                    CBase.Playlist.InsertSong(ActivePlaylistID, _CurrentPlaylistElement + _Offset, DragAndDropSongID, gm);
                                    UpdatePlaylist();
                                }
                                else
                                {
                                    if (mouseEvent.Y < _PlaylistElements[0].Background.Rect.Y && _Offset == 0)
                                    {
                                        CBase.Playlist.InsertSong(ActivePlaylistID, 0, DragAndDropSongID, gm);
                                        UpdatePlaylist();
                                    }
                                    else
                                    {
                                        if (_PlaylistElements.Count + _Offset >= _PlaylistElementContents.Count)
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
                        _SelectElement(_ButtonPlaylistName);
                        _CurrentPlaylistElement = -1;
                        if (mouseEvent.LB)
                        {
                            if (_ButtonPlaylistName.Selected)
                            {
                                CBase.Playlist.SetName(ActivePlaylistID, _ButtonPlaylistName.Text.Text);
                                CBase.Playlist.Save(ActivePlaylistID);
                                _EditMode = EEditMode.None;
                                return true;
                            }
                        }
                        else if (mouseEvent.RB)
                        {
                            if (_ButtonPlaylistName.Selected)
                            {
                                _ButtonPlaylistName.Text.Text = CBase.Playlist.GetName(ActivePlaylistID);
                                _EditMode = EEditMode.None;
                                _ButtonPlaylistName.EditMode = false;
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
                            if (_CurrentPlaylistElement != -1 && _CurrentPlaylistElement + _Offset != _ChangeOrderSource)
                            {
                                CBase.Playlist.MoveSong(ActivePlaylistID, _ChangeOrderSource, _CurrentPlaylistElement + _Offset);
                                UpdatePlaylist();
                            }
                            else if (_CurrentPlaylistElement == -1)
                            {
                                if (mouseEvent.Y < _PlaylistElements[0].Background.Rect.Y && _Offset == 0)
                                    CBase.Playlist.MoveSong(ActivePlaylistID, _ChangeOrderSource, 0);
                                else
                                {
                                    if (_PlaylistElements.Count + _Offset >= _PlaylistElementContents.Count)
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
                            _EditMode = EEditMode.None;
                            _ChangeOrderElement = null;
                        }
                        break;
                }
            }
            return false;
        }

        private void _PrepareList()
        {
            _PlaylistElements.Clear();

            for (int i = 0; i < Math.Floor(_Theme.Rect.H / _Theme.EntryHeight); i++)
            {
                var en = new CPlaylistElement
                    {
                        Background = new CStatic(_PartyModeID, _Theme.SkinBackground, _BackgroundColor,
                                                 new SRectF(_Theme.Rect.X, _Theme.Rect.Y + (i * _Theme.EntryHeight), _Theme.Rect.W, _Theme.EntryHeight, _Theme.Rect.Z)),
                        Cover = new CStatic(_Theme.StaticCover, _PartyModeID)
                    };

                en.Cover.LoadSkin();
                en.Cover.Y += _Theme.Rect.Y + (i * _Theme.EntryHeight);
                en.Cover.X += _Theme.Rect.X;

                en.Text1 = new CText(_Text1);
                en.Text1.X += _Theme.Rect.X;
                en.Text1.Y += _Theme.Rect.Y + (i * _Theme.EntryHeight);

                en.SelectSlide = new CSelectSlide(_SelectSlideGameMode);
                en.SelectSlide.LoadSkin();
                en.SelectSlide.X += _Theme.Rect.X;
                en.SelectSlide.Y += _Theme.Rect.Y + (i * _Theme.EntryHeight);

                en.Content = -1;

                _PlaylistElements.Add(en);
                _AddSelectSlide(en.SelectSlide);
                _AddText(en.Text1);
                _AddStatic(en.Background);
                _AddStatic(en.Cover);
            }
        }

        public bool LoadPlaylist(int playlistID)
        {
            if (!CBase.Playlist.Exists(playlistID))
                return false;
            ActivePlaylistID = playlistID;
            _ButtonPlaylistName.Text.Text = CBase.Playlist.GetName(ActivePlaylistID);
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
            _Update();
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

            _Update();
        }

        public void ClosePlaylist()
        {
            Visible = false;
            Selected = false;
            ActivePlaylistID = -1;
        }

        private void _StartPlaylistSongs()
        {
            //TODO: What is if someone uses that in PartyMode?
            CBase.Game.Reset();
            CBase.Game.ClearSongs();

            if (CBase.Playlist.Exists(ActivePlaylistID))
            {
                for (int i = 0; i < CBase.Playlist.GetSongCount(ActivePlaylistID); i++)
                    CBase.Game.AddSong(CBase.Playlist.GetSong(ActivePlaylistID, i).SongID, CBase.Playlist.GetSong(ActivePlaylistID, i).GameMode);
                if (CBase.Game.GetNumSongs() > 0)
                    CBase.Graphics.FadeTo(EScreen.Names);
            }
        }

        private void _StartPlaylistSong(int selected)
        {
            CBase.Game.Reset();
            CBase.Game.ClearSongs();

            CBase.Game.AddSong(CBase.Playlist.GetSong(ActivePlaylistID, selected).SongID, CBase.Playlist.GetSong(ActivePlaylistID, selected).GameMode);

            if (CBase.Game.GetNumSongs() > 0)
                CBase.Graphics.FadeTo(EScreen.Names);
        }

        private void _Update()
        {
            if (!CBase.Playlist.Exists(ActivePlaylistID))
                return;
            for (int i = 0; i < _PlaylistElements.Count; i++)
            {
                if (_Offset + i < _PlaylistElementContents.Count)
                {
                    _PlaylistElements[i].Content = _Offset + i;
                    _PlaylistElements[i].Background.Visible = true;
                    _PlaylistElements[i].Cover.Visible = true;
                    _PlaylistElements[i].SelectSlide.Visible = true;
                    _PlaylistElements[i].Text1.Visible = true;
                    CPlaylistElementContent pec = _PlaylistElementContents[_Offset + i];
                    CSong song = CBase.Songs.GetSongByID(pec.SongID);
                    _PlaylistElements[i].Cover.Texture = song.CoverTextureSmall;
                    string t1 = CBase.Language.Translate(_Text1.Text).Replace("%a", song.Artist).Replace("%t", song.Title);
                    _PlaylistElements[i].Text1.Text = /*(Offset + i + 1) + ") " + */ t1; //TODO: Add text field for the number
                    _PlaylistElements[i].SelectSlide.Clear();
                    foreach (EGameMode gm in pec.Modes)
                        _PlaylistElements[i].SelectSlide.AddValue(Enum.GetName(typeof(EGameMode), gm), null, (int)gm);
                    _PlaylistElements[i].SelectSlide.SelectedTag = (int)pec.Mode;
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

        public object GetTheme()
        {
            _ReadSubThemeElements();
            return _Theme;
        }

        private void _UpdateRect()
        {
            //Check for highest x/y-coords
            _CompleteRect = _Theme.Rect;
            //ButtonPlaylistClose
            if (_ButtonPlaylistClose.Rect.X < _CompleteRect.X)
                _CompleteRect.X = _ButtonPlaylistClose.Rect.X;
            if (_ButtonPlaylistClose.Rect.Y < _CompleteRect.Y)
                _CompleteRect.Y = _ButtonPlaylistClose.Rect.Y;
            if (_ButtonPlaylistClose.Rect.W + _ButtonPlaylistClose.Rect.X > _CompleteRect.W + _CompleteRect.X)
                _CompleteRect.W = _ButtonPlaylistClose.Rect.W + _ButtonPlaylistClose.Rect.X - _CompleteRect.X;
            if (_ButtonPlaylistClose.Rect.Y + _ButtonPlaylistClose.Rect.H > _CompleteRect.Y + _CompleteRect.H)
                _CompleteRect.H = _ButtonPlaylistClose.Rect.H + _ButtonPlaylistClose.Rect.Y - _CompleteRect.Y;
            //ButtonPlaylistName
            if (_ButtonPlaylistName.Rect.X < _CompleteRect.X)
                _CompleteRect.X = _ButtonPlaylistName.Rect.X;
            if (_ButtonPlaylistName.Rect.Y < _CompleteRect.Y)
                _CompleteRect.Y = _ButtonPlaylistName.Rect.Y;
            if (_ButtonPlaylistName.Rect.W + _ButtonPlaylistName.Rect.X > _CompleteRect.W + _CompleteRect.X)
                _CompleteRect.W = _ButtonPlaylistName.Rect.W + _ButtonPlaylistName.Rect.X - _CompleteRect.X;
            if (_ButtonPlaylistName.Rect.Y + _ButtonPlaylistName.Rect.H > _CompleteRect.Y + _CompleteRect.H)
                _CompleteRect.H = _ButtonPlaylistName.Rect.H + _ButtonPlaylistName.Rect.Y - _CompleteRect.Y;
            //ButtonPlaylistSing
            if (_ButtonPlaylistSing.Rect.X < _CompleteRect.X)
                _CompleteRect.X = _ButtonPlaylistSing.Rect.X;
            if (_ButtonPlaylistSing.Rect.Y < _CompleteRect.Y)
                _CompleteRect.Y = _ButtonPlaylistSing.Rect.Y;
            if (_ButtonPlaylistSing.Rect.W + _ButtonPlaylistSing.Rect.X > _CompleteRect.W + _CompleteRect.X)
                _CompleteRect.W = _ButtonPlaylistSing.Rect.W + _ButtonPlaylistSing.Rect.X - _CompleteRect.X;
            if (_ButtonPlaylistSing.Rect.Y + _ButtonPlaylistSing.Rect.H > _CompleteRect.Y + _CompleteRect.H)
                _CompleteRect.H = _ButtonPlaylistSing.Rect.H + _ButtonPlaylistSing.Rect.Y - _CompleteRect.Y;
            //ButtonPlaylistSave
            if (_ButtonPlaylistSave.Rect.X < _CompleteRect.X)
                _CompleteRect.X = _ButtonPlaylistSave.Rect.X;
            if (_ButtonPlaylistSave.Rect.Y < _CompleteRect.Y)
                _CompleteRect.Y = _ButtonPlaylistSave.Rect.Y;
            if (_ButtonPlaylistSave.Rect.W + _ButtonPlaylistSave.Rect.X > _CompleteRect.W + _CompleteRect.X)
                _CompleteRect.W = _ButtonPlaylistSave.Rect.W + _ButtonPlaylistSave.Rect.X - _CompleteRect.X;
            if (_ButtonPlaylistSave.Rect.Y + _ButtonPlaylistSave.Rect.H > _CompleteRect.Y + _CompleteRect.H)
                _CompleteRect.H = _ButtonPlaylistSave.Rect.H + _ButtonPlaylistSave.Rect.Y - _CompleteRect.Y;
            //ButtonPlaylistDelete
            if (_ButtonPlaylistDelete.Rect.X < _CompleteRect.X)
                _CompleteRect.X = _ButtonPlaylistDelete.Rect.X;
            if (_ButtonPlaylistDelete.Rect.Y < _CompleteRect.Y)
                _CompleteRect.Y = _ButtonPlaylistDelete.Rect.Y;
            if (_ButtonPlaylistDelete.Rect.W + _ButtonPlaylistDelete.Rect.X > _CompleteRect.W + _CompleteRect.X)
                _CompleteRect.W = _ButtonPlaylistDelete.Rect.W + _ButtonPlaylistDelete.Rect.X - _CompleteRect.X;
            if (_ButtonPlaylistDelete.Rect.Y + _ButtonPlaylistDelete.Rect.H > _CompleteRect.Y + _CompleteRect.H)
                _CompleteRect.H = _ButtonPlaylistDelete.Rect.H + _ButtonPlaylistDelete.Rect.Y - _CompleteRect.Y;
        }

        #region ThemeEdit
        public void MoveElement(int stepX, int stepY)
        {
            SRectF rect = MaxRect;
            rect.X += stepX;
            rect.Y += stepY;
            MaxRect = rect;

            _Theme.Rect.X += stepX;
            _Theme.Rect.Y += stepY;
        }

        public void ResizeElement(int stepW, int stepH)
        {
            SRectF rect = MaxRect;
            rect.W += stepW;
            if (rect.W <= 0)
                rect.W = 1;

            rect.H += stepH;
            if (rect.H <= 0)
                rect.H = 1;
            MaxRect = rect;

            _Theme.Rect.W = Rect.W;
            _Theme.Rect.H = Rect.H;
        }
        #endregion ThemeEdit
    }
}