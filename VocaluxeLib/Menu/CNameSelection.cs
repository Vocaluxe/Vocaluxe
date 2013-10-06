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
using VocaluxeLib.Draw;
using VocaluxeLib.Profile;

namespace VocaluxeLib.Menu
{
    struct SThemeNameSelection
    {
        public string Name;
        public string TextureEmptyTileName;
        public string ColorEmptyTileName;
        public string TextureTileSelectedName;
        public float NameSpace;
        public float NameHeight;
        public string NameFont;
        public EStyle NameStyle;
        public SColorF NameColor;
        public string NameColorName;
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

        public bool Selected;
        public bool Visible = true;

        public SRectF Rect;
        private readonly List<CTile> _Tiles;

        private CTexture _TextureEmptyTile;
        private CTexture _TextureTileSelected;

        public SColorF ColorEmptyTile;

        private int _NumW;
        private int _NumH;

        private float _SpaceW;
        private float _SpaceH;

        private int _TileW;
        private int _TileH;

        public int Offset;
        private int _ActualSelection = -1;
        public int Selection = -1;

        private int _Player = -1;

        private readonly List<int> _VisibleProfiles;

        private CStatic _PlayerSelector;

        public CNameSelection(int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Theme = new SThemeNameSelection();

            _Tiles = new List<CTile>();
            _VisibleProfiles = new List<int>();
        }

        public void Init()
        {
            _PrepareTiles();

            _PlayerSelector = new CStatic(_PartyModeID)
                {
                    Texture = _TextureTileSelected,
                    Rect = new SRectF(0, 0, _TileW + 6, _TileH + 6, Rect.Z - 0.5f),
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

            if (xmlReader.GetValue(item + "/ColorEmptyTile", out _Theme.ColorEmptyTileName, String.Empty))
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.ColorEmptyTileName, skinIndex, out ColorEmptyTile);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/R", ref ColorEmptyTile.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/G", ref ColorEmptyTile.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/B", ref ColorEmptyTile.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/A", ref ColorEmptyTile.A);
            }

            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinTileSelected", out _Theme.TextureTileSelectedName, String.Empty);

            _ThemeLoaded &= xmlReader.TryGetIntValue(item + "/Tiles/W", ref _TileW);
            _ThemeLoaded &= xmlReader.TryGetIntValue(item + "/Tiles/H", ref _TileH);
            _ThemeLoaded &= xmlReader.TryGetIntValue(item + "/Tiles/NumW", ref _NumW);
            _ThemeLoaded &= xmlReader.TryGetIntValue(item + "/Tiles/NumH", ref _NumH);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Tiles/SpaceW", ref _SpaceW);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Tiles/SpaceH", ref _SpaceH);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Tiles/Name/Space", ref _Theme.NameSpace);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Tiles/Name/H", ref _Theme.NameHeight);
            _ThemeLoaded &= xmlReader.GetValue(item + "/Tiles/Name/Font", out _Theme.NameFont, "Normal");
            _ThemeLoaded &= xmlReader.TryGetEnumValue(item + "/Tiles/Name/Style", ref _Theme.NameStyle);
            if (xmlReader.GetValue(item + "/Tiles/Name/Color", out _Theme.NameColorName, String.Empty))
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.NameColorName, skinIndex, out _Theme.NameColor);
            else
            {
                if (xmlReader.TryGetFloatValue(item + "/Tiles/Name/R", ref _Theme.NameColor.R))
                {
                    _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Tiles/Name/G", ref _Theme.NameColor.G);
                    _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Tiles/Name/B", ref _Theme.NameColor.B);
                    _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Tiles/Name/A", ref _Theme.NameColor.A);
                }
            }


            if (_ThemeLoaded)
            {
                _Theme.Name = elementName;
                LoadTextures();
            }
            return _ThemeLoaded;
        }

        public bool SaveTheme(XmlWriter writer)
        {
            if (_ThemeLoaded)
            {
                writer.WriteStartElement(_Theme.Name);

                writer.WriteComment("<X>, <Y>, <Z>, <W>, <H>: NameSelection position, width and height");
                writer.WriteElementString("X", Rect.X.ToString("#0"));
                writer.WriteElementString("Y", Rect.Y.ToString("#0"));
                writer.WriteElementString("Z", Rect.Z.ToString("#0.00"));
                writer.WriteElementString("W", Rect.W.ToString("#0"));
                writer.WriteElementString("H", Rect.H.ToString("#0"));

                writer.WriteComment("<SkinEmptyTile>: Texture name");
                writer.WriteElementString("SkinEmptyTile", _Theme.TextureEmptyTileName);

                writer.WriteComment("<ColorEmptyTile>: Static color from ColorScheme (high priority)");
                writer.WriteComment("or <R>, <G>, <B>, <A> (lower priority)");
                if (!String.IsNullOrEmpty(_Theme.ColorEmptyTileName))
                    writer.WriteElementString("ColorEmptyTile", _Theme.ColorEmptyTileName);
                else
                {
                    writer.WriteElementString("R", ColorEmptyTile.R.ToString("#0.00"));
                    writer.WriteElementString("G", ColorEmptyTile.G.ToString("#0.00"));
                    writer.WriteElementString("B", ColorEmptyTile.B.ToString("#0.00"));
                    writer.WriteElementString("A", ColorEmptyTile.A.ToString("#0.00"));
                }

                writer.WriteComment("<SkinTileSelected>: Texture name");
                writer.WriteElementString("SkinTileSelected", _Theme.TextureTileSelectedName);

                writer.WriteComment("<Tiles>: Options for tiles");
                writer.WriteStartElement("Tiles");
                writer.WriteComment("<W>, <H>: Width and height of tile");
                writer.WriteElementString("W", _TileW.ToString());
                writer.WriteElementString("H", _TileH.ToString());
                writer.WriteComment("<NumW>, <NumH>: Number of tiles");
                writer.WriteElementString("NumW", _NumW.ToString());
                writer.WriteElementString("NumH", _NumH.ToString());
                writer.WriteComment("<SpaceW>, <SpaceH>: Space between tiles");
                writer.WriteElementString("SpaceW", _SpaceW.ToString("#0.00"));
                writer.WriteElementString("SpaceH", _SpaceH.ToString("#0.00"));
                writer.WriteComment("<Name>: Options for player-name");
                writer.WriteStartElement("Name");
                writer.WriteComment("<Space>: Space between name and tile");
                writer.WriteElementString("Space", _Theme.NameSpace.ToString("#0.0"));
                writer.WriteComment("<Font>: Text font name");
                writer.WriteElementString("Font", _Theme.NameFont);
                writer.WriteComment("<Color>: Text color from ColorScheme (high priority)");
                if (!String.IsNullOrEmpty(_Theme.NameColorName))
                    writer.WriteElementString("Color", _Theme.NameColorName);
                else
                {
                    writer.WriteElementString("R", _Theme.NameColor.R.ToString("#0.00"));
                    writer.WriteElementString("G", _Theme.NameColor.G.ToString("#0.00"));
                    writer.WriteElementString("B", _Theme.NameColor.B.ToString("#0.00"));
                    writer.WriteElementString("A", _Theme.NameColor.A.ToString("#0.00"));
                }
                writer.WriteComment("<Style>: Text style: " + CHelper.ListStrings(Enum.GetNames(typeof(EStyle))));
                writer.WriteElementString("Style", _Theme.NameStyle.ToString());
                writer.WriteComment("<H>: Text height");
                writer.WriteElementString("H", _Theme.NameHeight.ToString("#0"));
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteEndElement();
                return true;
            }
            return false;
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
                    if (_ActualSelection - _NumW > -1)
                        _ActualSelection -= _NumW;
                    else if (Offset > 0)
                    {
                        UpdateList(Offset - 1);
                        _ActualSelection += _Tiles.Count - _NumW;
                    }
                    break;

                case Keys.Down:
                    if (_ActualSelection + _NumW < _Tiles.Count)
                    {
                        if (_Tiles[_ActualSelection + _NumW].ProfileID != -1)
                            _ActualSelection += _NumW;
                    }
                    else
                    {
                        int offset = Offset;
                        UpdateList(Offset + 1);
                        if (offset != Offset)
                        {
                            _ActualSelection = _ActualSelection - _Tiles.Count + _NumW;
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
                    _Tiles[i].Name.Text = CBase.Profiles.GetPlayerName(_VisibleProfiles[i + offset * _Tiles.Count]); ;
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

            if (!String.IsNullOrEmpty(_Theme.ColorEmptyTileName))
                ColorEmptyTile = CBase.Theme.GetColor(_Theme.ColorEmptyTileName, _PartyModeID);

            if (!String.IsNullOrEmpty(_Theme.NameColorName))
                _Theme.NameColor = CBase.Theme.GetColor(_Theme.NameColorName, _PartyModeID);
        }

        public void ReloadTextures()
        {
            UnloadTextures();
            LoadTextures();

            _PrepareTiles();
        }

        private void _PrepareTiles()
        {
            _Tiles.Clear();
            for (int i = 0; i < _NumH; i++)
            {
                for (int j = 0; j < _NumW; j++)
                {
                    SRectF rect = new SRectF(Rect.X + j * (_TileW + _SpaceW), Rect.Y + i * (_TileH + _SpaceH), _TileW, _TileH, Rect.Z);
                    CStatic tileStatic = new CStatic(_PartyModeID, _TextureEmptyTile, ColorEmptyTile, rect) {Aspect = EAspect.Crop};
                    CText tileText = new CText(rect.X + rect.W / 2, rect.Y + rect.H + _Theme.NameSpace, rect.Z, _Theme.NameHeight, rect.W, EAlignment.Center, _Theme.NameStyle,
                                               _Theme.NameFont, _Theme.NameColor, "");
                    _Tiles.Add(new CTile(tileStatic, tileText, -1));
                }
            }
        }

        private void _UpdateVisibleProfiles()
        {
            _VisibleProfiles.Clear();
            CProfile[] profiles = CBase.Profiles.GetProfiles();

            for (int i = 0; i < profiles.Length; i++)
            {
                bool visible = profiles[i].Active == EOffOn.TR_CONFIG_ON;
                if (visible)
                {
                    //Show profile only if active
                    for (int p = 0; p < CBase.Game.GetNumPlayer(); p++)
                    {
                        //Don't show profile if is selected, but if selected and guest
                        if (CBase.Game.GetPlayers()[p].ProfileID == profiles[i].ID && profiles[i].GuestProfile == EOffOn.TR_CONFIG_OFF)
                            visible = false;
                    }
                }
                if (visible)
                    _VisibleProfiles.Add(profiles[i].ID);
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