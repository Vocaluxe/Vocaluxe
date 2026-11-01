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
using Vocaluxe.Base;
using Vocaluxe.Base.ThemeSystem;
using VocaluxeLib;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    public class CScreenParty : CMenu
    {
        private class CModeTile
        {
            public CStatic Box;
            public CStatic Icon;
            public CText Name;
        }

        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 2; }
        }

        private const string _TextDetailName = "TextDetailName";
        private const string _TextDescription = "TextDescription";
        private const string _TextTargetAudience = "TextTargetAudience";
        private const string _TextNumTeams = "TextNumTeams";
        private const string _TextNumPlayers = "TextNumPlayers";
        private const string _TextNumMics = "TextNumMics";
        private const string _TextAuthor = "TextAuthor";
        private const string _TextError = "TextError";
        private const string _TextTileName = "TextTileName";
        private const string _TextTileNameSelected = "TextTileNameSelected";
        private const string _TextPage = "TextPage";
        private const string _StaticTile = "StaticTile";
        private const string _StaticTileSelected = "StaticTileSelected";
        private const string _StaticTileIcon = "StaticTileIcon";
        private const string _StaticTileIconSelected = "StaticTileIconSelected";
        private const string _ButtonStart = "ButtonStart";
        private const string _ButtonExit = "ButtonExit";
        private const string _ButtonPageUp = "ButtonPageUp";
        private const string _ButtonPageDown = "ButtonPageDown";

        private const string _SkinPartyModeIcon = "PartyModeIcon";
        private const int _NumColumns = 3;
        private const int _NumRows = 2;
        private const int _TilesPerPage = _NumColumns * _NumRows;
        private const float _TileSpaceX = 40f;
        private const float _TileSpaceY = 30f;
        private const float _UnavailableAlpha = 0.4f;

        private List<SPartyModeInfos> _PartyModeInfos;
        private readonly List<CModeTile> _Tiles = new List<CModeTile>();
        private int _Selected;
        private bool _OnButtons;

        public override void Init()
        {
            base.Init();

            _ThemeTexts = new string[]
            {
                _TextDetailName, _TextDescription, _TextTargetAudience, _TextNumTeams, _TextNumPlayers, _TextNumMics, _TextAuthor, _TextError, _TextTileName,
                _TextTileNameSelected, _TextPage
            };
            _ThemeStatics = new string[] { _StaticTile, _StaticTileSelected, _StaticTileIcon, _StaticTileIconSelected };
            _ThemeButtons = new string[] { _ButtonStart, _ButtonExit, _ButtonPageUp, _ButtonPageDown };
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            _Tiles.Clear();
            var box = _Statics[_StaticTile].Rect;
            var icon = _Statics[_StaticTileIcon].Rect;
            for (var i = 0; i < _TilesPerPage; i++)
            {
                var x = box.X + (i % _NumColumns) * (box.W + _TileSpaceX);
                var y = box.Y + (i / _NumColumns) * (box.H + _TileSpaceY);
                var tile = new CModeTile
                {
                    Box = GetNewStatic(_Statics[_StaticTile]),
                    Icon = GetNewStatic(_Statics[_StaticTileIcon]),
                    Name = GetNewText(_Texts[_TextTileName])
                };
                tile.Box.MaxRect = new SRectF(x, y, box.W, box.H, box.Z);
                tile.Icon.MaxRect = new SRectF(x + icon.X - box.X, y + icon.Y - box.Y, icon.W, icon.H, icon.Z);
                tile.Name.X = x + _Texts[_TextTileName].X - box.X;
                tile.Name.Y = y + _Texts[_TextTileName].Y - box.Y;
                _AddStatic(tile.Box);
                _AddStatic(tile.Icon);
                _AddText(tile.Name);
                _Tiles.Add(tile);
            }

            _Statics[_StaticTile].Visible = false;
            _Statics[_StaticTileSelected].Visible = false;
            _Statics[_StaticTileIcon].Visible = false;
            _Statics[_StaticTileIconSelected].Visible = false;
            _Texts[_TextTileName].Visible = false;
            _Texts[_TextTileNameSelected].Visible = false;
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            if (keyEvent.Key != Keys.Left && keyEvent.Key != Keys.Right && keyEvent.Key != Keys.Up && keyEvent.Key != Keys.Down && keyEvent.Key != Keys.Tab)
            {
                base.HandleInput(keyEvent);
            }

            if (keyEvent.KeyPressed)
            {
                return true;
            }

            switch (keyEvent.Key)
            {
                case Keys.Back:
                case Keys.Escape:
                    CGraphics.FadeTo(EScreen.Main);
                    break;

                case Keys.Enter:
                    if (_OnButtons && _Buttons[_ButtonExit].Selected)
                    {
                        CGraphics.FadeTo(EScreen.Main);
                    }
                    else
                    {
                        _StartPartyMode();
                    }

                    break;

                case Keys.Left:
                case Keys.Right:
                    if (_OnButtons)
                    {
                        _SwitchButton();
                    }
                    else
                    {
                        _Select(_Selected + (keyEvent.Key == Keys.Left ? -1 : 1));
                    }

                    break;

                case Keys.Up:
                    if (_OnButtons)
                    {
                        _FocusTiles();
                    }
                    else
                    {
                        _Select(_Selected - _NumColumns);
                    }

                    break;

                case Keys.Down:
                    if (!_OnButtons)
                    {
                        if (_Selected + _NumColumns < _PartyModeInfos.Count)
                        {
                            _Select(_Selected + _NumColumns);
                        }
                        else
                        {
                            _FocusButtons();
                        }
                    }

                    break;

                case Keys.Tab:
                    if (_OnButtons)
                    {
                        _SwitchButton();
                    }
                    else
                    {
                        _FocusButtons();
                    }

                    break;
            }

            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);
            if (!_OnButtons && (_Buttons[_ButtonStart].Selected || _Buttons[_ButtonExit].Selected))
            {
                _OnButtons = true;
                _UpdateTiles();
            }

            var first = _GetPage() * _TilesPerPage;
            for (var i = 0; i < _Tiles.Count && first + i < _PartyModeInfos.Count; i++)
            {
                if (!CHelper.IsInBounds(_Tiles[i].Box.Rect, mouseEvent))
                {
                    continue;
                }

                if (_OnButtons || first + i != _Selected)
                {
                    _FocusTiles();
                    _Select(first + i);
                }

                if (mouseEvent.LB)
                {
                    _StartPartyMode();
                }

                return true;
            }

            if (mouseEvent.LB)
            {
                if (_IsMouseOverCurSelection(mouseEvent))
                {
                    if (_Buttons[_ButtonStart].Selected)
                    {
                        _StartPartyMode();
                    }

                    if (_Buttons[_ButtonExit].Selected)
                    {
                        CGraphics.FadeTo(EScreen.Main);
                    }

                    if (_Buttons[_ButtonPageUp].Selected)
                    {
                        _Select((_GetPage() - 1) * _TilesPerPage);
                    }

                    if (_Buttons[_ButtonPageDown].Selected)
                    {
                        _Select((_GetPage() + 1) * _TilesPerPage);
                    }
                }
            }

            if (mouseEvent.RB)
            {
                CGraphics.FadeTo(EScreen.Main);
            }

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();
            _PartyModeInfos = CParty.GetPartyModeInfos();
            _PartyModeInfos.Sort((a, b) => string.Compare(CLanguage.Translate(a.Name, a.ExtInfo.Id), CLanguage.Translate(b.Name, b.ExtInfo.Id), StringComparison.CurrentCultureIgnoreCase));
            _Select(Math.Min(_Selected, Math.Max(0, _PartyModeInfos.Count - 1)));
            _FocusTiles();
        }

        public override bool UpdateGame()
        {
            return true;
        }

        private void _FocusTiles()
        {
            _OnButtons = false;
            _Buttons[_ButtonStart].Selected = false;
            _Buttons[_ButtonExit].Selected = false;
            _UpdateTiles();
        }

        private void _FocusButtons()
        {
            _OnButtons = true;
            _SelectElement(_Buttons[_ButtonStart].Visible ? _Buttons[_ButtonStart] : _Buttons[_ButtonExit]);
            _UpdateTiles();
        }

        private void _SwitchButton()
        {
            if (_Buttons[_ButtonStart].Selected || !_Buttons[_ButtonStart].Visible)
            {
                _SelectElement(_Buttons[_ButtonExit]);
            }
            else
            {
                _SelectElement(_Buttons[_ButtonStart]);
            }
        }

        private int _GetPage()
        {
            return _Selected / _TilesPerPage;
        }

        private int _GetNumPages()
        {
            return Math.Max(1, (_PartyModeInfos.Count + _TilesPerPage - 1) / _TilesPerPage);
        }

        private static bool _IsPlayable(SPartyModeInfos info)
        {
            return CConfig.GetMaxNumMics() >= info.ExtInfo.MinMics;
        }

        private void _Select(int index)
        {
            if (_PartyModeInfos == null || _PartyModeInfos.Count == 0)
            {
                _Selected = 0;
                return;
            }

            _Selected = Math.Max(0, Math.Min(_PartyModeInfos.Count - 1, index));
            _UpdateTiles();
            _UpdateSelection();
        }

        private void _UpdateTiles()
        {
            var page = _GetPage();
            var first = page * _TilesPerPage;
            for (var i = 0; i < _Tiles.Count; i++)
            {
                var tile = _Tiles[i];
                var index = first + i;
                var visible = index < _PartyModeInfos.Count;
                tile.Box.Visible = visible;
                tile.Icon.Visible = visible;
                tile.Name.Visible = visible;
                if (!visible)
                {
                    continue;
                }

                var info = _PartyModeInfos[index];
                var selected = index == _Selected && !_OnButtons;
                var alpha = _IsPlayable(info) ? 1f : _UnavailableAlpha;

                tile.Icon.Texture = CThemes.GetSkinTexture(_SkinPartyModeIcon, info.ExtInfo.Id);
                tile.Name.TranslationId = info.ExtInfo.Id;
                tile.Name.Text = info.Name;

                var iconColor = selected ? _Statics[_StaticTileIconSelected].Color : _Statics[_StaticTileIcon].Color;
                var nameColor = selected ? _Texts[_TextTileNameSelected].Color : _Texts[_TextTileName].Color;
                var boxTheme = selected ? _Statics[_StaticTileSelected] : _Statics[_StaticTile];
                var boxColor = boxTheme.Color;
                tile.Box.Texture = boxTheme.Texture;
                tile.Icon.Color = new SColorF(iconColor.R, iconColor.G, iconColor.B, iconColor.A * alpha);
                tile.Name.Color = new SColorF(nameColor.R, nameColor.G, nameColor.B, nameColor.A * alpha);
                tile.Box.Color = new SColorF(boxColor.R, boxColor.G, boxColor.B, boxColor.A * alpha);
            }

            var numPages = _GetNumPages();
            _Buttons[_ButtonPageUp].Visible = page > 0;
            _Buttons[_ButtonPageDown].Visible = page < numPages - 1;
            _Texts[_TextPage].Visible = numPages > 1;
            _Texts[_TextPage].Text = (page + 1) + " / " + numPages;
        }

        private void _UpdateSelection()
        {
            var infos = _PartyModeInfos[_Selected];
            var extInfo = infos.ExtInfo;

            _Texts[_TextDetailName].TranslationId = extInfo.Id;
            _Texts[_TextDetailName].Text = infos.Name;

            //Description
            _Texts[_TextDescription].TranslationId = extInfo.Id;
            _Texts[_TextDescription].Text = infos.Description;

            //TargetAudience
            _Texts[_TextTargetAudience].TranslationId = extInfo.Id;
            _Texts[_TextTargetAudience].Text = infos.TargetAudience;

            //NumTeams
            if (extInfo.MaxTeams == 1)
            {
                _Texts[_TextNumTeams].Text = "TR_SCREENPARTY_NOTEAMS";
            }
            else if (extInfo.MaxTeams == extInfo.MinTeams)
            {
                _Texts[_TextNumTeams].Text = extInfo.MaxTeams.ToString();
            }
            else
            {
                _Texts[_TextNumTeams].Text = extInfo.MinTeams + " - " + extInfo.MaxTeams;
            }

            //NumPlayers
            if (extInfo.MaxPlayers == extInfo.MinPlayers)
            {
                _Texts[_TextNumPlayers].Text = extInfo.MaxPlayers.ToString();
            }
            else
            {
                _Texts[_TextNumPlayers].Text = extInfo.MinPlayers + " - " + extInfo.MaxPlayers;
            }

            _Texts[_TextNumMics].Text = extInfo.MinMics.ToString();

            //Author
            _Texts[_TextAuthor].Text = CLanguage.Translate(infos.Author, extInfo.Id) + "  ·  " + infos.VersionMajor + "." + infos.VersionMinor;

            if (!_IsPlayable(infos))
            {
                _Buttons[_ButtonStart].Visible = false;
                _Texts[_TextError].Text = CConfig.GetMaxNumMics() == 0
                    ? "TR_SCREENPARTY_ERROR_MICS" : CLanguage.Translate("TR_SCREENPARTY_ERROR_MIC_NUM").Replace("%d", extInfo.MinMics.ToString());
                _Texts[_TextError].Visible = true;
            }
            else
            {
                _Buttons[_ButtonStart].Visible = true;
                _Texts[_TextError].Visible = false;
            }
        }

        private void _StartPartyMode()
        {
            if (_PartyModeInfos.Count == 0 || _Selected >= _PartyModeInfos.Count || !_IsPlayable(_PartyModeInfos[_Selected]))
            {
                return;
            }

            CParty.SetPartyMode(_PartyModeInfos[_Selected].ExtInfo.Id);
            CGraphics.FadeTo(CParty.GetStartScreen());
        }
    }
}
