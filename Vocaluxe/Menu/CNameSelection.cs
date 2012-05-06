using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.Lib.Draw;

namespace Vocaluxe.Menu
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

    class CNameSelection : IMenuElement
    {

        class CTile
        {
            public int PlayerNr;
            public CStatic Avatar;
            public CText Name;

            public CTile()
            {
            }

            public CTile(CStatic av, CText tex, int pl)
            {
                Avatar = av;
                Name = tex;
                PlayerNr = pl;
            }
        }

        private SThemeNameSelection _Theme;
        private bool _ThemeLoaded;

        public bool Selected = false;
        public bool Visible = true;

        public SRectF Rect;
        private List<CTile> _Tiles;

        private STexture _TextureEmptyTile;
        private STexture _TextureTileSelected;

        public SColorF ColorEmptyTile;

        private int _NumW;
        private int _NumH;

        private float _SpaceW;
        private float _SpaceH;

        private int _TileW;
        private int _TileH;

        public int _Offset = 0;
        private int _actualSelection = -1;
        public int Selection = -1;

        int _player = -1;

        private List<int> VisibleProfiles;

        private CStatic PlayerSelector;


        public CNameSelection()
        {
            _Theme = new SThemeNameSelection();

            _TextureEmptyTile = CTheme.GetSkinTexture(_Theme.TextureEmptyTileName);
            _TextureTileSelected = CTheme.GetSkinTexture(_Theme.TextureTileSelectedName);

            _Tiles = new List<CTile>();

            VisibleProfiles = new List<int>();
        }

        public void Init()
        {
            _Tiles.Clear();
            for (int i = 0; i < _NumH; i++)
            {
                for (int j = 0; j < _NumW; j++)
                {
                    SRectF rect = new SRectF(Rect.X + j * (_TileW + _SpaceW), Rect.Y + i * (_TileH + _SpaceH), _TileW, _TileH, Rect.Z);
                    CStatic tileStatic = new CStatic(_TextureEmptyTile, ColorEmptyTile, rect);
                    CText tileText = new CText(rect.X + rect.W / 2, rect.Y + rect.H + _Theme.NameSpace, rect.Z, _Theme.NameHeight, rect.W, EAlignment.Center, _Theme.NameStyle, _Theme.NameFont, _Theme.NameColor, "");
                    _Tiles.Add(new CTile(tileStatic, tileText, -1));
                }
            }

            PlayerSelector = new CStatic();
            PlayerSelector.Texture = _TextureTileSelected;
            PlayerSelector.Rect = new SRectF(0, 0, (_TileW + 2), (_TileH + 2), (Rect.Z - 0.5f));
            PlayerSelector.Visible = false;

            UpdateVisibleProfiles();

            UpdateList(0);
        }

        public bool LoadTheme(string XmlPath, string ElementName, XPathNavigator navigator, int SkinIndex)
        {
            string item = XmlPath + "/" + ElementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/X", navigator, ref Rect.X);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/Y", navigator, ref Rect.Y);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/Z", navigator, ref Rect.Z);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/W", navigator, ref Rect.W);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/H", navigator, ref Rect.H);

            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/SkinEmptyTile", navigator, ref _Theme.TextureEmptyTileName, String.Empty);

            if (CHelper.GetValueFromXML(item + "/ColorEmptyTile", navigator, ref _Theme.ColorEmptyTileName, String.Empty))
            {
                _ThemeLoaded &= CTheme.GetColor(_Theme.ColorEmptyTileName, SkinIndex, ref ColorEmptyTile);
            }
            else
            {
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/R", navigator, ref ColorEmptyTile.R);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/G", navigator, ref ColorEmptyTile.G);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/B", navigator, ref ColorEmptyTile.B);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/A", navigator, ref ColorEmptyTile.A);
            }

            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/SkinTileSelected", navigator, ref _Theme.TextureTileSelectedName, String.Empty);

            _ThemeLoaded &= CHelper.TryGetIntValueFromXML(item + "/Tiles/W", navigator, ref _TileW);
            _ThemeLoaded &= CHelper.TryGetIntValueFromXML(item + "/Tiles/H", navigator, ref _TileH);
            _ThemeLoaded &= CHelper.TryGetIntValueFromXML(item + "/Tiles/NumW", navigator, ref _NumW);
            _ThemeLoaded &= CHelper.TryGetIntValueFromXML(item + "/Tiles/NumH", navigator, ref _NumH);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/Tiles/SpaceW", navigator, ref _SpaceW);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/Tiles/SpaceH", navigator, ref _SpaceH);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/Tiles/Name/Space", navigator, ref _Theme.NameSpace);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/Tiles/Name/H", navigator, ref _Theme.NameHeight);
            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/Tiles/Name/Font", navigator, ref _Theme.NameFont, "Normal");
            _ThemeLoaded &= CHelper.TryGetEnumValueFromXML<EStyle>(item + "/Tiles/Name/Style", navigator, ref _Theme.NameStyle);
            if (CHelper.GetValueFromXML(item + "/Tiles/Name/Color", navigator, ref _Theme.NameColorName, String.Empty))
            {
                _ThemeLoaded &= CTheme.GetColor(_Theme.NameColorName, SkinIndex, ref _Theme.NameColor);
            }
            else
            {
                if (CHelper.TryGetFloatValueFromXML(item + "/Tiles/Name/R", navigator, ref _Theme.NameColor.R))
                {
                    _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/Tiles/Name/G", navigator, ref _Theme.NameColor.G);
                    _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/Tiles/Name/B", navigator, ref _Theme.NameColor.B);
                    _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/Tiles/Name/A", navigator, ref _Theme.NameColor.A);
                }
            }


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
                if (_Theme.ColorEmptyTileName != String.Empty)
                {
                    writer.WriteElementString("ColorEmptyTile", _Theme.ColorEmptyTileName);
                }
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
                if (_Theme.NameColorName != String.Empty)
                {
                    writer.WriteElementString("Color", _Theme.NameColorName);
                }
                else
                {
                    writer.WriteElementString("R", _Theme.NameColor.R.ToString("#0.00"));
                    writer.WriteElementString("G", _Theme.NameColor.G.ToString("#0.00"));
                    writer.WriteElementString("B", _Theme.NameColor.B.ToString("#0.00"));
                    writer.WriteElementString("A", _Theme.NameColor.A.ToString("#0.00"));
                }
                writer.WriteComment("<Style>: Text style: " + CConfig.ListStrings(Enum.GetNames(typeof(EStyle))));
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
            Draw(false);
        }

        public void ForceDraw()
        {
            Draw(true);
        }

        public void Draw(bool ForceDraw)
        {
            int i = 0;
            foreach (CTile tile in _Tiles)
            {
                tile.Avatar.Draw();
                tile.Name.Draw();

                if (PlayerSelector.Visible)
                {
                    //Update PlayerSelector-Coords
                    if (_player > -1 && _actualSelection == i)
                    {
                        PlayerSelector.Rect.X = tile.Avatar.Rect.X - 1;
                        PlayerSelector.Rect.Y = tile.Avatar.Rect.Y - 1;
                    }
                }

                i++;
            }
            if (PlayerSelector.Visible)
            {
                //Draw PlayerSelector
                PlayerSelector.Draw();
            }
        }

        public void HandleInput(KeyEvent kevent)
        {
            switch (kevent.Key)
            {
                case Keys.Right:
                    if (_actualSelection + 1 < _Tiles.Count)
                    {
                        if (_Tiles[_actualSelection + 1].PlayerNr != -1)
                        {
                            _actualSelection++;
                        }
                        
                    }
                    else
                    {
                        int offset = _Offset;
                        UpdateList(_Offset + 1);
                        if (offset != _Offset)
                        {
                            _actualSelection = 0;
                        }
                    }
                    break;

                case Keys.Left:
                    if (_actualSelection - 1 > -1)
                    {
                        _actualSelection--;
                    }
                    else if (_Offset > 0)
                    {
                        UpdateList(_Offset - 1);
                        _actualSelection = _Tiles.Count-1;
                    }
                    break;

                case Keys.Up:
                    if (_actualSelection - _NumW > -1)
                    {
                        _actualSelection -= _NumW;
                    }
                    else if (_Offset > 0)
                    {
                        UpdateList(_Offset - 1);
                        _actualSelection += _Tiles.Count- _NumW;
                    }
                    break;

                case Keys.Down:
                    if (_actualSelection + _NumW < _Tiles.Count)
                    {
                        if (_Tiles[_actualSelection + _NumW].PlayerNr != -1)
                        {
                            _actualSelection += _NumW;
                        }
                    }
                    else
                    {
                        int offset = _Offset;
                        UpdateList(_Offset + 1);
                        if (offset != _Offset)
                        {
                            _actualSelection = _actualSelection - _Tiles.Count + _NumW;
                            if (_Tiles[_actualSelection].PlayerNr == -1)
                            {
                                for (int i = (_Tiles.Count - 1); i >= 0; i--)
                                {
                                    if (_Tiles[i].PlayerNr != -1)
                                    {
                                        _actualSelection = i;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    break;
            }

            Selection = _Offset * _Tiles.Count + _actualSelection;
        }

        public void KeyboardSelection(bool active, int player)
        {
            //Overwrite player-selection; Same profile, but other player
            if (active && Selection != -1)
            {
                _player = player;
                PlayerSelector.Color = CTheme.GetPlayerColor(player);
            }
            //Normal activation
            else if (active)
            {
                Selection = 0;
                _actualSelection = 0;
                _player = player;
                PlayerSelector.Color = CTheme.GetPlayerColor(player);
                PlayerSelector.Visible = true;
            }
            //Deactivate
            else
            {
                Selection = -1;
                _actualSelection = -1;
                _player = -1;
                PlayerSelector.Visible = false;
            }
        }

        public void UpdateList()
        {
            UpdateVisibleProfiles();
            if (_Tiles.Count * (_Offset + 1) - VisibleProfiles.Count >= _Tiles.Count * _Offset)
            {
                UpdateList(_Offset - 1);
            }
            else
            {
                UpdateList(_Offset);
            }
        }

        public void UpdateList(int offset)
        {
            if (offset < 0)
                offset = 0;

            if ( _Tiles.Count * (offset + 1) - VisibleProfiles.Count < _Tiles.Count)
            {

                for (int i = 0; i < _Tiles.Count; i++)
                {
                    if ((i + offset * _Tiles.Count) < VisibleProfiles.Count)
                    {
                        _Tiles[i].Avatar.Texture = CProfiles.Profiles[VisibleProfiles[i + offset * _Tiles.Count]].Avatar.Texture;
                        _Tiles[i].Avatar.Color = new SColorF(1, 1, 1, 1);
                        _Tiles[i].Name.Text = CProfiles.Profiles[VisibleProfiles[i + offset * _Tiles.Count]].PlayerName;
                        _Tiles[i].PlayerNr = VisibleProfiles[i + offset * _Tiles.Count];
                    }
                    else
                    {
                        _Tiles[i].Avatar.Texture = _TextureEmptyTile;
                        _Tiles[i].Avatar.Color = ColorEmptyTile;
                        _Tiles[i].Name.Text = "";
                        _Tiles[i].PlayerNr = -1;
                    }
                }
                _Offset = offset;
            }
        }

        public bool isOverTile(MouseEvent mevent)
        {
            bool isOver = false;
            foreach (CTile tile in _Tiles)
            {
                if (CHelper.IsInBounds(tile.Avatar.Rect, mevent))
                {
                    isOver = true;
                }
            }
            return isOver;
        }

        public int TilePlayerNr(MouseEvent mevent)
        {
            foreach (CTile tile in _Tiles)
            {
                if (CHelper.IsInBounds(tile.Avatar.Rect, mevent))
                {
                    return tile.PlayerNr;
                }
            }

            return -1;
        }

        public CStatic TilePlayerAvatar(MouseEvent mevent)
        {
            foreach (CTile tile in _Tiles)
            {
                if (CHelper.IsInBounds(tile.Avatar.Rect, mevent))
                {
                    return tile.Avatar;
                }
            }

            return new CStatic();
        }

        public CStatic TilePlayerAvatar(int nr)
        {
            nr -= _Offset * _Tiles.Count;
            return _Tiles[nr].Avatar;
        }

        public void UnloadTextures()
        {
        }

        public void LoadTextures()
        {
        }

        public void ReloadTextures()
        {
            UnloadTextures();
            LoadTextures();
        }

        private void UpdateVisibleProfiles()
        {
            VisibleProfiles.Clear();
            for (int i = 0; i < CProfiles.Profiles.Length; i++)
            {
                bool visible = false;
                //Show profile only if active
                if (CProfiles.Profiles[i].Active == EOffOn.TR_CONFIG_ON)
                {
                    visible = true;
                }
                for (int p = 0; p < CGame.NumPlayer; p++)
                {
                    //Don't show profile if is selected, but if selected and guest
                    if (CGame.Player[p].ProfileID == i && CProfiles.Profiles[i].GuestProfile == EOffOn.TR_CONFIG_OFF)
                    {
                        visible = false;
                    }
                }
                if (visible)
                {
                    VisibleProfiles.Add(i);
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
