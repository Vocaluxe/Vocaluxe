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
using System.Linq;
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    public abstract class CScreenOptionsBase : CMenu
    {
        private static readonly string[] _NavNames = { "Game", "Sound", "Record", "Video", "Lyrics", "Theme", "Graphics", "Server", "SongFolders", "Credits" };
        private static readonly EScreen[] _NavScreens =
        {
            EScreen.OptionsGame, EScreen.OptionsSound, EScreen.OptionsRecord, EScreen.OptionsVideo, EScreen.OptionsLyrics, EScreen.OptionsTheme, EScreen.OptionsGraphics,
            EScreen.OptionsServer, EScreen.OptionsSongFolders, EScreen.Credits
        };
        private const int _NumOptionScreens = 9;

        private const string _StaticNavSelected = "StaticNavSelected";
        private const string _StaticNavIconSelected = "StaticNavIconSelected";
        private const string _TextNavSelected = "TextNavSelected";

        private readonly Dictionary<string, SColorF> _NavColors = new Dictionary<string, SColorF>();
        private bool _NavFocused;
        private int _NavCursor;
        private IMenuElement _ContentSelection;
        private bool _PendingNavFocus;
        private bool _PendingContentFocus;
        private static bool _SwitchingOptions;

        protected abstract EScreen _OptionsScreen { get; }

        protected abstract void _SaveOptions();

        private int _CurrentNavIndex
        {
            get { return Array.IndexOf(_NavScreens, _OptionsScreen); }
        }

        private bool _HasNavigation
        {
            get { return _Statics.ContainsKey(_StaticNavSelected) && _Statics.ContainsKey("StaticNav" + _NavNames[0]); }
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            _NavColors.Clear();
            foreach (var name in _NavNames)
            {
                if (_Statics.ContainsKey("StaticNavIcon" + name))
                {
                    _NavColors["StaticNavIcon" + name] = _Statics["StaticNavIcon" + name].Color;
                }

                if (_Texts.ContainsKey("TextNav" + name))
                {
                    _NavColors["TextNav" + name] = _Texts["TextNav" + name].Color;
                }
            }

            if (_Statics.ContainsKey(_StaticNavIconSelected))
            {
                _Statics[_StaticNavIconSelected].Visible = false;
            }

            if (_Texts.ContainsKey(_TextNavSelected))
            {
                _Texts[_TextNavSelected].Visible = false;
            }

            _NavFocused = false;
            _UpdateNavHighlight();
        }

        public override void OnShow()
        {
            base.OnShow();

            _NavFocused = false;
            _ContentSelection = null;
            _PendingNavFocus = !_SwitchingOptions;
            _PendingContentFocus = _SwitchingOptions;
            _SwitchingOptions = false;
            _UpdateNavHighlight();
        }

        public override void Draw()
        {
            if (_PendingNavFocus)
            {
                _PendingNavFocus = false;
                if (_HasNavigation)
                {
                    _SetNavFocus(true);
                    _ContentSelection = _GetFirstContentElement();
                }
            }
            else if (_PendingContentFocus)
            {
                _PendingContentFocus = false;
                var first = _GetFirstContentElement();
                if (first != null)
                {
                    _SelectElement(first);
                }
            }

            base.Draw();
        }

        protected bool _HandleNavigationInput(SKeyEvent keyEvent)
        {
            if (!_HasNavigation)
            {
                return false;
            }

            if (_NavFocused)
            {
                if (keyEvent.KeyPressed)
                {
                    return true;
                }

                switch (keyEvent.Key)
                {
                    case Keys.Escape:
                    case Keys.Back:
                        return false;

                    case Keys.Up:
                        _NavCursor = (_NavCursor - 1 + _NavNames.Length) % _NavNames.Length;
                        _UpdateNavHighlight();
                        break;

                    case Keys.Down:
                        _NavCursor = (_NavCursor + 1) % _NavNames.Length;
                        _UpdateNavHighlight();
                        break;

                    case Keys.Enter:
                    case Keys.Right:
                        if (_NavCursor == _CurrentNavIndex)
                        {
                            _SetNavFocus(false);
                        }
                        else
                        {
                            _SwitchTo(_NavCursor);
                        }

                        break;
                }

                return true;
            }

            if (keyEvent.Key == Keys.Escape || keyEvent.Key == Keys.Back)
            {
                if (!keyEvent.KeyPressed)
                {
                    _SetNavFocus(true);
                }

                return true;
            }

            if (keyEvent.Key != Keys.Tab)
            {
                return false;
            }

            if (!keyEvent.KeyPressed)
            {
                var step = keyEvent.Mod == EModifier.Shift ? -1 : 1;
                _SwitchTo((_CurrentNavIndex + step + _NumOptionScreens) % _NumOptionScreens);
            }

            return true;
        }

        protected bool _HandleNavigationMouse(SMouseEvent mouseEvent)
        {
            for (var i = 0; i < _NavNames.Length; i++)
            {
                var row = "StaticNav" + _NavNames[i];
                if (!_Statics.ContainsKey(row) || !CHelper.IsInBounds(_Statics[row].Rect, mouseEvent))
                {
                    continue;
                }

                if (!mouseEvent.LB)
                {
                    return false;
                }

                _SwitchTo(i);
                return true;
            }

            if (_NavFocused)
            {
                _SetNavFocus(false);
            }

            return false;
        }

        private IMenuElement _GetFirstContentElement()
        {
            return _SelectSlides.Cast<IMenuElement>().Concat(_Buttons).Where(element => element.Selectable).OrderBy(element => element.Rect.Y).ThenBy(element => element.Rect.X)
                                .FirstOrDefault();
        }

        private void _SetNavFocus(bool focused)
        {
            if (focused)
            {
                _ContentSelection = _SelectSlides.FirstOrDefault(slide => slide.Selected) ?? (IMenuElement)_Buttons.FirstOrDefault(button => button.Selected);
                if (_ContentSelection != null)
                {
                    _ContentSelection.Selected = false;
                }

                _NavCursor = _CurrentNavIndex;
            }
            else if (_ContentSelection != null)
            {
                _SelectElement(_ContentSelection);
                _ContentSelection = null;
            }

            _NavFocused = focused;
            _UpdateNavHighlight();
        }

        private void _UpdateNavHighlight()
        {
            if (!_HasNavigation)
            {
                return;
            }

            var highlighted = _NavFocused ? _NavCursor : _CurrentNavIndex;
            var selected = _Statics[_StaticNavSelected];
            for (var i = 0; i < _NavNames.Length; i++)
            {
                var row = "StaticNav" + _NavNames[i];
                var icon = "StaticNavIcon" + _NavNames[i];
                var text = "TextNav" + _NavNames[i];
                var isHighlighted = i == highlighted;

                if (_Statics.ContainsKey(row))
                {
                    _Statics[row].Visible = !isHighlighted;
                    if (isHighlighted)
                    {
                        var rowRect = _Statics[row].Rect;
                        selected.MaxRect = new SRectF(rowRect.X, rowRect.Y, rowRect.W, rowRect.H, selected.Rect.Z);
                    }
                }

                if (_Statics.ContainsKey(icon) && _NavColors.ContainsKey(icon))
                {
                    _Statics[icon].Color = isHighlighted && _Statics.ContainsKey(_StaticNavIconSelected) ? _Statics[_StaticNavIconSelected].Color : _NavColors[icon];
                }

                if (_Texts.ContainsKey(text) && _NavColors.ContainsKey(text))
                {
                    _Texts[text].Color = isHighlighted && _Texts.ContainsKey(_TextNavSelected) ? _Texts[_TextNavSelected].Color : _NavColors[text];
                }
            }
        }

        private void _SwitchTo(int index)
        {
            if (_NavScreens[index] == _OptionsScreen)
            {
                return;
            }

            _SaveOptions();
            _SwitchingOptions = true;
            CGraphics.FadeTo(_NavScreens[index]);
        }
    }
}
