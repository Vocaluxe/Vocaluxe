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
using System.Xml;
using System.Xml.Serialization;
using VocaluxeLib.Draw;
using VocaluxeLib.Profile;

namespace VocaluxeLib.Menu
{
    [XmlType("NameSelection")]
    public struct SThemeNameSelection
    {
        [XmlAttributeAttribute(AttributeName = "Name")]
        public string Name;
        [XmlElement("Rect")]
        public SRectF Rect;
        [XmlElement("SkinEmptyTile")]
        public string TextureEmptyTileName;
        [XmlElement("ColorEmptyTile")]
        public SThemeColor ColorEmptyTile;
        [XmlElement("SkinTileSelected")]
        public string TextureTileSelectedName;
        [XmlElement("Tiles")]
        public SThemeNameSelectionTiles Tiles;
    }

    public struct SThemeNameSelectionTiles
    {
        [XmlElement("W")]
        public int W;
        [XmlElement("H")]
        public int H;
        [XmlElement("NumW")]
        public int NumW;
        [XmlElement("NumH")]
        public int NumH;
        [XmlElement("SpaceW")]
        public float SpaceW;
        [XmlElement("SpaceH")]
        public float SpaceH;
        [XmlElement("Name")]
        public SThemeNameSelectionName Name;
    }

    public struct SThemeNameSelectionName
    {
        [XmlElement("Space")]
        public float Space;
        [XmlElement("H")]
        public float Height;
        [XmlElement("Font")]
        public string Font;
        [XmlElement("Style")]
        public EStyle Style;
        [XmlElement("Color")]
        public SThemeColor Color;
    }

    public class CNameSelection : IMenuElement
    {
        private class CTile
        {
            public int ProfileID;
            public readonly CStatic Avatar;
            public readonly CText Name;

            public CTile(CStatic av, CText tex, int pID)
            {
                Avatar = av;
                Name = tex;
                ProfileID = pID;
            }
        }

        private readonly int _PartyModeID;
        private SThemeNameSelection _Theme;
        private bool _ThemeLoaded;

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        public bool ThemeLoaded
        {
            get { return _ThemeLoaded; }
        }

        public bool Selected;
        public bool Visible = true;

        public SRectF Rect;
        private readonly List<CTile> _Tiles;

        private CTextureRef _TextureEmptyTile;
        private CTextureRef _TextureTileSelected;
        public CTextureRef TextureEmptyTile
        {
            get { return _TextureEmptyTile; }
        }

        public SColorF ColorEmptyTile;


        public int Offset;
        private int _ActualSelection = -1;
        public int Selection = -1;

        private int _Player = -1;

        private readonly List<int> _VisibleProfiles;
        private readonly List<int> _UsedProfiles;

        private CStatic _PlayerSelector;

        public CNameSelection(int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Theme = new SThemeNameSelection();
            _Theme.Tiles = new SThemeNameSelectionTiles();
            _Theme.Tiles.Name = new SThemeNameSelectionName();

            _Tiles = new List<CTile>();
            _VisibleProfiles = new List<int>();
            _UsedProfiles = new List<int>();
        }

        public CNameSelection(SThemeNameSelection theme, int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Theme = theme;

            _Tiles = new List<CTile>();
            _VisibleProfiles = new List<int>();
            _UsedProfiles = new List<int>();

            LoadTextures();
        }

        public void Init()
        {
            _PrepareTiles();

            _PlayerSelector = new CStatic(_PartyModeID)
                {
                    Texture = _TextureTileSelected,
                    Rect = new SRectF(0, 0, _Theme.Tiles.W + 6, _Theme.Tiles.H + 6, Rect.Z - 0.5f),
                    Visible = false
                };

            _UpdateVisibleProfiles();

            UpdateList(0);
        }

        public bool LoadTheme(string xmlPath, string elementName, CXMLReader xmlReader, int skinIndex)
        {
            string item = xmlPath + "/" + elementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/X", ref Rect.X);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Y", ref Rect.Y);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Z", ref Rect.Z);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/W", ref Rect.W);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/H", ref Rect.H);

            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinEmptyTile", out _Theme.TextureEmptyTileName, String.Empty);

            if (xmlReader.GetValue(item + "/ColorEmptyTile", out _Theme.ColorEmptyTile.Name, String.Empty))
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.ColorEmptyTile.Name, skinIndex, out ColorEmptyTile);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/R", ref ColorEmptyTile.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/G", ref ColorEmptyTile.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/B", ref ColorEmptyTile.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/A", ref ColorEmptyTile.A);
            }

            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinTileSelected", out _Theme.TextureTileSelectedName, String.Empty);

            _ThemeLoaded &= xmlReader.TryGetIntValue(item + "/Tiles/W", ref _Theme.Tiles.W);
            _ThemeLoaded &= xmlReader.TryGetIntValue(item + "/Tiles/H", ref _Theme.Tiles.H);
            _ThemeLoaded &= xmlReader.TryGetIntValue(item + "/Tiles/NumW", ref _Theme.Tiles.NumW);
            _ThemeLoaded &= xmlReader.TryGetIntValue(item + "/Tiles/NumH", ref _Theme.Tiles.NumH);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Tiles/SpaceW", ref _Theme.Tiles.SpaceW);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Tiles/SpaceH", ref _Theme.Tiles.SpaceH);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Tiles/Name/Space", ref _Theme.Tiles.Name.Space);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Tiles/Name/H", ref _Theme.Tiles.Name.Height);
            _ThemeLoaded &= xmlReader.GetValue(item + "/Tiles/Name/Font", out _Theme.Tiles.Name.Font, "Normal");
            _ThemeLoaded &= xmlReader.TryGetEnumValue(item + "/Tiles/Name/Style", ref _Theme.Tiles.Name.Style);
            if (xmlReader.GetValue(item + "/Tiles/Name/Color", out _Theme.Tiles.Name.Color.Name, String.Empty))
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.Tiles.Name.Color.Name, skinIndex, out _Theme.Tiles.Name.Color.Color);
            else
            {
                if (xmlReader.TryGetFloatValue(item + "/Tiles/Name/R", ref _Theme.Tiles.Name.Color.Color.R))
                {
                    _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Tiles/Name/G", ref _Theme.Tiles.Name.Color.Color.G);
                    _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Tiles/Name/B", ref _Theme.Tiles.Name.Color.Color.B);
                    _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Tiles/Name/A", ref _Theme.Tiles.Name.Color.Color.A);
                }
            }


            if (_ThemeLoaded)
            {
                _Theme.Name = elementName;

                _Theme.Rect = new SRectF(Rect);
                _Theme.ColorEmptyTile.Color = new SColorF(ColorEmptyTile);

                LoadTextures();
            }
            return _ThemeLoaded;
        }

        public void Draw()
        {
            int i = 0;
            foreach (CTile tile in _Tiles)
            {
                tile.Avatar.Draw();
                tile.Name.Draw();

                if (_PlayerSelector.Visible)
                {
                    //Update PlayerSelector-Coords
                    if (_Player > -1 && _ActualSelection == i)
                    {
                        _PlayerSelector.Rect.X = tile.Avatar.Rect.X - 3;
                        _PlayerSelector.Rect.Y = tile.Avatar.Rect.Y - 3;
                    }
                }

                i++;
            }
            if (_PlayerSelector.Visible)
            {
                //Draw PlayerSelector
                _PlayerSelector.Draw();
            }
        }

        public void HandleInput(SKeyEvent kevent)
        {
            switch (kevent.Key)
            {
                case Keys.Right:
                    if (_ActualSelection + 1 < _Tiles.Count)
                    {
                        if (_Tiles[_ActualSelection + 1].ProfileID != -1)
                            _ActualSelection++;
                    }
                    else
                    {
                        int offset = Offset;
                        UpdateList(Offset + 1);
                        if (offset != Offset)
                            _ActualSelection = 0;
                    }
                    break;

                case Keys.Left:
                    if (_ActualSelection - 1 > -1)
                        _ActualSelection--;
                    else if (Offset > 0)
                    {
                        UpdateList(Offset - 1);
                        _ActualSelection = _Tiles.Count - 1;
                    }
                    break;

                case Keys.Up:
                    if (_ActualSelection - _Theme.Tiles.NumW > -1)
                        _ActualSelection -= _Theme.Tiles.NumW;
                    else if (Offset > 0)
                    {
                        UpdateList(Offset - 1);
                        _ActualSelection += _Tiles.Count - _Theme.Tiles.NumW;
                    }
                    break;

                case Keys.Down:
                    if (_ActualSelection + _Theme.Tiles.NumW < _Tiles.Count)
                    {
                        if (_Tiles[_ActualSelection + _Theme.Tiles.NumW].ProfileID != -1)
                            _ActualSelection += _Theme.Tiles.NumW;
                    }
                    else
                    {
                        int offset = Offset;
                        UpdateList(Offset + 1);
                        if (offset != Offset)
                        {
                            _ActualSelection = _ActualSelection - _Tiles.Count + _Theme.Tiles.NumW;
                            if (_Tiles[_ActualSelection].ProfileID == -1)
                            {
                                for (int i = _Tiles.Count - 1; i >= 0; i--)
                                {
                                    if (_Tiles[i].ProfileID != -1)
                                    {
                                        _ActualSelection = i;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    break;
            }

            if (Offset * _Tiles.Count + _ActualSelection < _VisibleProfiles.Count)
                Selection = _VisibleProfiles[Offset * _Tiles.Count + _ActualSelection];
            else
                Selection = -1;
        }

        public void HandleMouse(SMouseEvent mevent)
        {
            for (int i = 0; i < _Tiles.Count; i++)
            {
                if (CHelper.IsInBounds(_Tiles[i].Avatar.Rect, mevent))
                {
                    _ActualSelection = i;

                    if (Offset * _Tiles.Count + _ActualSelection < _VisibleProfiles.Count)
                        Selection = _VisibleProfiles[Offset * _Tiles.Count + _ActualSelection];
                    else
                        Selection = -1;
                }
            }
        }

        public void FastSelection(bool active, int player)
        {
            //Overwrite player-selection; Same profile, but other player
            if (active && Selection != -1)
            {
                _Player = player;
                _PlayerSelector.Color = CBase.Theme.GetPlayerColor(player);
            }
                //Normal activation
            else if (active)
            {
                Selection = 0;
                _ActualSelection = 0;
                _Player = player;
                _PlayerSelector.Color = CBase.Theme.GetPlayerColor(player);
                _PlayerSelector.Visible = true;
            }
                //Deactivate
            else
            {
                Selection = -1;
                _ActualSelection = -1;
                _Player = -1;
                _PlayerSelector.Visible = false;
            }
        }

        public void UpdateList()
        {
            _UpdateVisibleProfiles();
            if (_Tiles.Count * (Offset + 1) - _VisibleProfiles.Count >= _Tiles.Count * Offset)
                UpdateList(Offset - 1);
            else
                UpdateList(Offset);
        }

        public void UpdateList(int offset)
        {
            if (offset < 0)
                offset = 0;

            for (int i = 0; i < _Tiles.Count; i++)
            {
                if ((i + offset * _Tiles.Count) < _VisibleProfiles.Count)
                {
                    _Tiles[i].Avatar.Texture = CBase.Profiles.GetAvatar(_VisibleProfiles[i + offset * _Tiles.Count]);
                    _Tiles[i].Avatar.Color = new SColorF(1, 1, 1, 1);
                    _Tiles[i].Name.Text = CBase.Profiles.GetPlayerName(_VisibleProfiles[i + offset * _Tiles.Count]);
                    _Tiles[i].ProfileID = _VisibleProfiles[i + offset * _Tiles.Count];
                }
                else
                {
                    _Tiles[i].Avatar.Texture = _TextureEmptyTile;
                    _Tiles[i].Avatar.Color = ColorEmptyTile;
                    _Tiles[i].Name.Text = "";
                    _Tiles[i].ProfileID = -1;
                }
            }
            Offset = offset;
        }

        public bool IsOverTile(SMouseEvent mevent)
        {
            bool isOver = false;
            foreach (CTile tile in _Tiles)
            {
                if (CHelper.IsInBounds(tile.Avatar.Rect, mevent))
                    isOver = true;
            }
            return isOver;
        }

        public int TilePlayerNr(SMouseEvent mevent)
        {
            foreach (CTile tile in _Tiles)
            {
                if (CHelper.IsInBounds(tile.Avatar.Rect, mevent))
                    return tile.ProfileID;
            }

            return -1;
        }

        public CStatic TilePlayerAvatar(SMouseEvent mevent)
        {
            foreach (CTile tile in _Tiles)
            {
                if (CHelper.IsInBounds(tile.Avatar.Rect, mevent))
                    return tile.Avatar;
            }

            return new CStatic(_PartyModeID);
        }

        public void UnloadTextures() {}

        public void LoadTextures()
        {
            _TextureEmptyTile = CBase.Theme.GetSkinTexture(_Theme.TextureEmptyTileName, _PartyModeID);
            _TextureTileSelected = CBase.Theme.GetSkinTexture(_Theme.TextureTileSelectedName, _PartyModeID);

            if (!String.IsNullOrEmpty(_Theme.ColorEmptyTile.Name))
                ColorEmptyTile = CBase.Theme.GetColor(_Theme.ColorEmptyTile.Name, _PartyModeID);
            else
                ColorEmptyTile = _Theme.ColorEmptyTile.Color;

            if (!String.IsNullOrEmpty(_Theme.Tiles.Name.Color.Name))
                _Theme.Tiles.Name.Color.Color = CBase.Theme.GetColor(_Theme.Tiles.Name.Color.Name, _PartyModeID);

            Rect = _Theme.Rect;
        }

        public void ReloadTextures()
        {
            UnloadTextures();
            LoadTextures();

            _PrepareTiles();
        }

        public void UseProfile(int id)
        {
            if (id > -1)
            {
                if (!_UsedProfiles.Contains(id) && CBase.Profiles.IsProfileIDValid(id) && !CBase.Profiles.IsGuest(id))
                {
                    _UsedProfiles.Add(id);
                    UpdateList();
                }
            }
        }

        public void RemoveUsedProfile(int id)
        {
            if (id > -1)
            {
                _UsedProfiles.Remove(id);
                UpdateList();
            }
        }

        public int GetRandomUnusedProfile()
        {
            int id = -1;

            if (_VisibleProfiles.Count == 0)
                return id;

            if (_VisibleProfiles.Count == _UsedProfiles.Count)
                return id;

            while (id == -1)
            {
                int rand = CBase.Game.GetRandom(_VisibleProfiles.Count);
                if (_UsedProfiles.Contains(_VisibleProfiles[rand]))
                    continue;
                id = _VisibleProfiles[rand];
            }

            return id;
        }

        private void _PrepareTiles()
        {
            _Tiles.Clear();
            for (int i = 0; i < _Theme.Tiles.NumH; i++)
            {
                for (int j = 0; j < _Theme.Tiles.NumW; j++)
                {
                    var rect = new SRectF(Rect.X + j * (_Theme.Tiles.W + _Theme.Tiles.SpaceW), Rect.Y + i * (_Theme.Tiles.H + _Theme.Tiles.SpaceH), _Theme.Tiles.W, _Theme.Tiles.H, Rect.Z);
                    var tileStatic = new CStatic(_PartyModeID, _TextureEmptyTile, ColorEmptyTile, rect) {Aspect = EAspect.Crop};
                    var tileText = new CText(rect.X + rect.W / 2, rect.Y + rect.H + _Theme.Tiles.Name.Space, rect.Z, _Theme.Tiles.Name.Height, rect.W, EAlignment.Center, _Theme.Tiles.Name.Style,
                                             _Theme.Tiles.Name.Font, _Theme.Tiles.Name.Color.Color, "");
                    _Tiles.Add(new CTile(tileStatic, tileText, -1));
                }
            }
        }

        private void _UpdateVisibleProfiles()
        {
            _VisibleProfiles.Clear();
            CProfile[] profiles = CBase.Profiles.GetProfiles();

            foreach (CProfile profile in profiles)
            {
                bool visible = profile.Active == EOffOn.TR_CONFIG_ON;
                if (visible)
                {
                    if (_UsedProfiles.Contains(profile.ID) && ((int)profile.UserRole) >= ((int)EUserRole.TR_USERROLE_NORMAL))
                        visible = false;
                }
                if (visible)
                    _VisibleProfiles.Add(profile.ID);
            }
        }

        public SThemeNameSelection GetTheme()
        {
            return _Theme;
        }

        #region ThemeEdit
        public void MoveElement(int stepX, int stepY)
        {
            Rect.X += stepX;
            Rect.Y += stepY;

            _Theme.Rect.X += stepX;
            _Theme.Rect.Y += stepY;
        }

        public void ResizeElement(int stepW, int stepH)
        {
            Rect.W += stepW;
            if (Rect.W <= 0)
                Rect.W = 1;

            Rect.H += stepH;
            if (Rect.H <= 0)
                Rect.H = 1;

            _Theme.Rect.W = Rect.W;
            _Theme.Rect.H = Rect.H;
        }
        #endregion ThemeEdit
    }
}