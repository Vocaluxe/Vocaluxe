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
using VocaluxeLib.Draw;
using VocaluxeLib.Profile;
using VocaluxeLib.Xml;

namespace VocaluxeLib.Menu
{
    [XmlType("NameSelection")]
    public struct SThemeNameSelection
    {
        [XmlAttribute(AttributeName = "Name")] public string Name;
        public SRectF Rect;
        public string SkinEmptyTile;
        public SThemeColor ColorEmptyTile;
        public string SkinTileSelected;
        public SThemeNameSelectionTiles Tiles;
    }

    public struct SThemeNameSelectionTiles
    {
        public int W;
        public int H;
        public int NumW;
        public int NumH;
        public float SpaceW;
        public float SpaceH;
        public SThemeNameSelectionName Name;
    }

    public struct SThemeNameSelectionName
    {
        public float Space;
        [XmlElement("H")] public float Height;
        public string Font;
        public EStyle Style;
        public SThemeColor Color;
    }

    public class CNameSelection : CMenuElementBase, IMenuElement, IThemeable
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

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        public bool ThemeLoaded { get; private set; }

        private readonly List<CTile> _Tiles;

        private CTextureRef _TextureEmptyTile;
        private CTextureRef _TextureTileSelected;
        public CTextureRef TextureEmptyTile
        {
            get { return _TextureEmptyTile; }
        }

        private SColorF _ColorEmptyTile;
        private SColorF _ColorNameTile;

        public int Offset;
        private int _ActualSelection = -1;
        public int Selection = -1;

        private int _Player = -1;

        private readonly List<int> _VisibleProfiles;
        private readonly List<int> _UsedProfiles;

        private CStatic _PlayerSelector;

        private SRectF _Rect;
        public override SRectF Rect
        {
            get { return _Rect; }
        }

        public bool Selectable
        {
            get { return Visible; }
        }

        public CNameSelection(int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Theme = new SThemeNameSelection {Tiles = new SThemeNameSelectionTiles {Name = new SThemeNameSelectionName()}};

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

            ThemeLoaded = true;
        }

        public void Init()
        {
            _PrepareTiles();

            _PlayerSelector = new CStatic(_PartyModeID, _TextureTileSelected, new SColorF(), new SRectF(0, 0, _Theme.Tiles.W + 6, _Theme.Tiles.H + 6, Rect.Z - 0.5f))
                {
                    Visible = false
                };

            _UpdateVisibleProfiles();

            UpdateList(0);
        }

        public bool LoadTheme(string xmlPath, string elementName, CXmlReader xmlReader)
        {
            string item = xmlPath + "/" + elementName;
            ThemeLoaded = true;

            ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/X", ref _Theme.Rect.X);
            ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Y", ref _Theme.Rect.Y);
            ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Z", ref _Theme.Rect.Z);
            ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/W", ref _Theme.Rect.W);
            ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/H", ref _Theme.Rect.H);

            ThemeLoaded &= xmlReader.GetValue(item + "/SkinEmptyTile", out _Theme.SkinEmptyTile, String.Empty);

            if (xmlReader.GetValue(item + "/ColorEmptyTile", out _Theme.ColorEmptyTile.Name, String.Empty))
                ThemeLoaded &= _Theme.ColorEmptyTile.Get(_PartyModeID, out _ColorEmptyTile);
            else
            {
                ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/R", ref _ColorEmptyTile.R);
                ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/G", ref _ColorEmptyTile.G);
                ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/B", ref _ColorEmptyTile.B);
                ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/A", ref _ColorEmptyTile.A);
            }

            ThemeLoaded &= xmlReader.GetValue(item + "/SkinTileSelected", out _Theme.SkinTileSelected, String.Empty);

            ThemeLoaded &= xmlReader.TryGetIntValue(item + "/Tiles/W", ref _Theme.Tiles.W);
            ThemeLoaded &= xmlReader.TryGetIntValue(item + "/Tiles/H", ref _Theme.Tiles.H);
            ThemeLoaded &= xmlReader.TryGetIntValue(item + "/Tiles/NumW", ref _Theme.Tiles.NumW);
            ThemeLoaded &= xmlReader.TryGetIntValue(item + "/Tiles/NumH", ref _Theme.Tiles.NumH);
            ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Tiles/SpaceW", ref _Theme.Tiles.SpaceW);
            ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Tiles/SpaceH", ref _Theme.Tiles.SpaceH);
            ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Tiles/Name/Space", ref _Theme.Tiles.Name.Space);
            ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Tiles/Name/H", ref _Theme.Tiles.Name.Height);
            ThemeLoaded &= xmlReader.GetValue(item + "/Tiles/Name/Font", out _Theme.Tiles.Name.Font, "Normal");
            ThemeLoaded &= xmlReader.TryGetEnumValue(item + "/Tiles/Name/Style", ref _Theme.Tiles.Name.Style);
            if (xmlReader.GetValue(item + "/Tiles/Name/Color", out _Theme.Tiles.Name.Color.Name, String.Empty))
                ThemeLoaded &= _Theme.Tiles.Name.Color.Get(_PartyModeID, out _ColorNameTile);
            else
            {
                if (xmlReader.TryGetFloatValue(item + "/Tiles/Name/R", ref _ColorNameTile.R))
                {
                    ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Tiles/Name/G", ref _ColorNameTile.G);
                    ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Tiles/Name/B", ref _ColorNameTile.B);
                    ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Tiles/Name/A", ref _ColorNameTile.A);
                }
            }


            if (ThemeLoaded)
            {
                _Theme.Name = elementName;
                _Theme.Tiles.Name.Color.Color = _ColorNameTile;
                _Theme.ColorEmptyTile.Color = _ColorEmptyTile;
                LoadSkin();
            }
            return ThemeLoaded;
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
                        _PlayerSelector.X = tile.Avatar.Rect.X - 3;
                        _PlayerSelector.Y = tile.Avatar.Rect.Y - 3;
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
                _PlayerSelector.Color = CBase.Themes.GetPlayerColor(player);
            }
                //Normal activation
            else if (active)
            {
                Selection = 0;
                _ActualSelection = 0;
                _Player = player;
                _PlayerSelector.Color = CBase.Themes.GetPlayerColor(player);
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
                    _Tiles[i].Avatar.Color = _ColorEmptyTile;
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

        public void UnloadSkin() {}

        public void LoadSkin()
        {
            _TextureEmptyTile = CBase.Themes.GetSkinTexture(_Theme.SkinEmptyTile, _PartyModeID);
            _TextureTileSelected = CBase.Themes.GetSkinTexture(_Theme.SkinTileSelected, _PartyModeID);

            _Theme.ColorEmptyTile.Get(_PartyModeID, out _ColorEmptyTile);
            _Theme.Tiles.Name.Color.Get(_PartyModeID, out _ColorNameTile);

            MaxRect = _Theme.Rect;
        }

        public void ReloadSkin()
        {
            UnloadSkin();
            LoadSkin();

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
            if (_VisibleProfiles.Count == 0)
                return -1;

            int rand = CBase.Game.GetRandom(_VisibleProfiles.Count);
            return _VisibleProfiles[rand];
        }

        private void _PrepareTiles()
        {
            _Tiles.Clear();
            for (int i = 0; i < _Theme.Tiles.NumH; i++)
            {
                for (int j = 0; j < _Theme.Tiles.NumW; j++)
                {
                    var rect = new SRectF(MaxRect.X + j * (_Theme.Tiles.W + _Theme.Tiles.SpaceW), MaxRect.Y + i * (_Theme.Tiles.H + _Theme.Tiles.SpaceH),
                                          _Theme.Tiles.W, _Theme.Tiles.H, MaxRect.Z);
                    var tileStatic = new CStatic(_PartyModeID, _TextureEmptyTile, _ColorEmptyTile, rect) {Aspect = EAspect.Crop};
                    var tileText = new CText(rect.X + rect.W / 2, rect.Y + rect.H + _Theme.Tiles.Name.Space, rect.Z, _Theme.Tiles.Name.Height, rect.W, EAlignment.Center,
                                             _Theme.Tiles.Name.Style, _Theme.Tiles.Name.Font, _ColorNameTile, "");
                    _Tiles.Add(new CTile(tileStatic, tileText, -1));
                }
            }
            _Rect.X = MaxRect.X;
            _Rect.Y = MaxRect.Y;
            _Rect.Right = _Tiles[_Tiles.Count - 1].Avatar.Rect.Right;
            _Rect.Bottom = _Tiles[_Tiles.Count - 1].Name.Rect.Bottom;
            _Rect.Z = MaxRect.Z;
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

        public object GetTheme()
        {
            return _Theme;
        }

        #region ThemeEdit
        public void MoveElement(int stepX, int stepY)
        {
            X += stepX;
            Y += stepY;

            _Theme.Rect.X += stepX;
            _Theme.Rect.Y += stepY;
        }

        public void ResizeElement(int stepW, int stepH)
        {
            W += stepW;
            if (W <= 0)
                W = 1;

            H += stepH;
            if (H <= 0)
                H = 1;

            _Theme.Rect.W = W;
            _Theme.Rect.H = H;
        }
        #endregion ThemeEdit
    }
}