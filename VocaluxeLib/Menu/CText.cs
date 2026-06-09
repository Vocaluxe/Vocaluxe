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
using System.Xml.Serialization;

namespace VocaluxeLib.Menu
{
    [XmlType("Text")]
    public struct SThemeText
    {
        [XmlAttribute(AttributeName = "Name")]
        public string Name;

        public float X;
        public float Y;
        public float Z;
        [XmlElement("H")]
        public float FontHeight;
        [XmlElement("MaxW")]
        public float MaxWidth;
        public SThemeColor Color;
        public SThemeColor SelColor; //for Buttons
        public EAlignment Align;
        public EHAlignment ResizeAlign;
        [XmlElement("Style")]
        public EStyle FontStyle;
        [XmlElement("Font")]
        public string FontFamily;
        public string Text;
        public SReflection? Reflection;
        public bool? AllMonitors;
    }

    public sealed class CText : CMenuElementBase, IMenuElement, IThemeable, IFontObserver
    {
        private SThemeText _Theme;
        private readonly int _PartyModeId = -1;
        private int _TranslationId = -1;

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        public bool ThemeLoaded { get; private set; }

        private bool _ButtonText;
        private bool _PositionNeedsUpdate = true;

        public override float X
        {
            set
            {
                if (Math.Abs(X - value) > 0.01)
                {
                    base.X = value;
                    _PositionNeedsUpdate = true;
                }
            }
        }

        public override float Y
        {
            set
            {
                if (Math.Abs(Y - value) > 0.01)
                {
                    base.Y = value;
                    _PositionNeedsUpdate = true;
                }
            }
        }

        public override float W
        {
            set
            {
                if (Math.Abs(W - value) > 0.01)
                {
                    base.W = value;
                    _PositionNeedsUpdate = true;
                }
            }
        }

        private EAlignment _Align = EAlignment.Left;
        public EAlignment Align
        {
            get { return _Align; }
            set
            {
                if (_Align != value)
                {
                    _Align = value;
                    _PositionNeedsUpdate = true;
                }
            }
        }

        private EHAlignment _ResizeAlign = EHAlignment.Center;
        public EHAlignment ResizeAlign
        {
            get { return _ResizeAlign; }
            set
            {
                if (_ResizeAlign != value)
                {
                    _ResizeAlign = value;
                    _PositionNeedsUpdate = true;
                }
            }
        }

        private CFont _Font = new CFont();
        public CFont Font
        {
            get { return _Font; }
            set
            {
                if (!_Font.Equals(value))
                {
                    _Font.RemoveObserver(this);
                    _Font = value;
                    _Font.AddObserver(this);
                    _PositionNeedsUpdate = true;
                }
            }
        }

        /// <summary>
        ///     Gets set in _UpdateTextPosition!
        /// </summary>
        private SRectF _Rect;
        public override SRectF Rect
        {
            get
            {
                _UpdateTextPosition();
                return _Rect;
            }
        }

        public bool Selectable
        {
            get { return false; }
        }

        public SColorF Color; //normal Color
        public SColorF SelColor; //selected Color for Buttons

        private float _ReflectionSpace;
        private float _ReflectionHeight;

        public bool AllMonitors = true;

        public string Text
        {
            get { return _Theme.Text; }
            set
            {
                var translation = CBase.Language.Translate(value, _TranslationId);
                if (_Theme.Text != value || translation != TranslatedText)
                {
                    _Theme.Text = value;
                    TranslatedText = translation;
                    if (_EditMode)
                    {
                        TranslatedText += "|";
                    }

                    _PositionNeedsUpdate = true;
                }
            }
        }
        public string TranslatedText { get; private set; } = String.Empty;

        public int TranslationId
        {
            get { return _TranslationId; }
            set
            {
                if (_TranslationId != value)
                {
                    TranslatedText = CBase.Language.Translate(TranslatedText, value);
                    _TranslationId = value;
                    _PositionNeedsUpdate = true;
                }
            }
        }

        private bool _EditMode;
        public bool EditMode
        {
            get { return _EditMode; }
            set
            {
                if (_EditMode != value)
                {
                    TranslatedText = value ? TranslatedText + "|" : TranslatedText.Substring(0, TranslatedText.Length - 1);
                    _EditMode = value;
                    _PositionNeedsUpdate = true;
                }
            }
        }

        public float Alpha = 1f;
        private CFont _CalculatedFont;

        public CFont CalculatedFont
        {
            get
            {
                if (_PositionNeedsUpdate)
                {
                    _UpdateTextPosition();
                }

                return _CalculatedFont;
            }
        }

        public CText(int partyModeId)
        {
            _PartyModeId = partyModeId;
            _TranslationId = _PartyModeId;
            _Font.AddObserver(this);
        }

        public CText(CText text)
        {
            _PartyModeId = text._PartyModeId;
            _TranslationId = text._TranslationId;

            MaxRect = text.MaxRect;
            _Rect = text._Rect;
            _PositionNeedsUpdate = false;
            _Align = text._Align;
            _ResizeAlign = text._ResizeAlign;
            Font = new CFont(text.Font); //Use setter to set observer

            Color = text.Color;
            SelColor = text.SelColor;
            _ReflectionSpace = text._ReflectionSpace;
            _ReflectionHeight = text._ReflectionHeight;
            AllMonitors = text.AllMonitors;

            Text = text.Text;
            Visible = text.Visible;
            Alpha = text.Alpha;

            _EditMode = text._EditMode;
        }

        public CText(float x, float y, float z, float h, float mw, EAlignment align, EStyle style, string fontFamily, SColorF col, string text, int partyModeId = -1,
            float rheight = 0,
            float rspace = 0) : this(partyModeId)
        {
            _Theme = new SThemeText { FontFamily = fontFamily, FontStyle = style, FontHeight = h, Text = text, Color = { A = col.A, B = col.B, G = col.G, R = col.R } };
            ThemeLoaded = false;
            _ButtonText = false;

            MaxRect = new SRectF(x, y, mw, h, z);
            Align = align;
            ResizeAlign = EHAlignment.Center;
            _Font.Name = fontFamily;
            _Font.Style = style;
            _Font.Height = h;

            Color = col;
            SelColor = col;

            Text = text;

            Selected = false;

            _ReflectionSpace = rspace;
            _ReflectionHeight = rheight;
        }

        public CText(SThemeText theme, int partyModeId, bool buttonText = false)
        {
            _PartyModeId = partyModeId;
            _TranslationId = partyModeId;
            _Theme = theme;

            _ButtonText = buttonText;

            ThemeLoaded = true;
        }

        public void Draw()
        {
            _Draw(false);
        }

        private void _Draw(bool force)
        {
            if (!force && !Visible && CBase.Settings.GetProgramState() != EProgramState.EditTheme)
            {
                return;
            }

            // Update Text
            Text = Text;

            var currentColor = Selected ? SelColor : Color;
            var color = new SColorF(currentColor.R, currentColor.G, currentColor.B, currentColor.A * Alpha);

            CBase.Fonts.DrawText(TranslatedText, CalculatedFont, Rect.X, Rect.Y, Z, color, AllMonitors);

            if (_ReflectionHeight > 0)
            {
                CBase.Fonts.DrawTextReflection(TranslatedText, CalculatedFont, Rect.X, Rect.Y, Z, color, _ReflectionSpace, _ReflectionHeight);
            }

            if (Selected && CBase.Settings.GetProgramState() == EProgramState.EditTheme)
            {
                CBase.Drawing.DrawRect(new SColorF(0.5f, 1f, 0.5f, 0.5f), new SRectF(Rect.X, Rect.Y, Rect.W, Rect.H, Z));
            }
        }

        public void Draw(float begin, float end)
        {
            var currentColor = Selected ? SelColor : Color;
            var color = new SColorF(currentColor.R, currentColor.G, currentColor.B, currentColor.A * Alpha);

            CBase.Fonts.DrawText(Text, CalculatedFont, Rect.X, Rect.Y, Z, color, begin, end);

            if (_ReflectionHeight > 0)
            {
                // TODO
            }

            if (Selected && CBase.Settings.GetProgramState() == EProgramState.EditTheme)
            {
                CBase.Drawing.DrawRect(new SColorF(0.5f, 1f, 0.5f, 0.5f), new SRectF(X, Y, Rect.W, Rect.H, Z));
            }
        }

        public void DrawRelative(float rx, float ry, float reflectionHeight = 0f, float reflectionSpace = 0f, float rectHeight = 0f)
        {
            var oldReflectionSpace = _ReflectionSpace;
            var oldReflectionHeight = _ReflectionHeight;
            if (reflectionHeight > 0)
            {
                _ReflectionSpace = (rectHeight - Rect.Y - Rect.H) * 2 + reflectionSpace;
                _ReflectionHeight = reflectionHeight - (rectHeight - Rect.Y) + Rect.H;
            }
            else
            {
                _ReflectionHeight = 0;
            }

            X += rx;
            Y += ry;
            _Draw(true);
            _ReflectionSpace = oldReflectionSpace;
            _ReflectionHeight = oldReflectionHeight;
            X -= rx;
            Y -= ry;
        }

        public void UnloadSkin() { }

        public void LoadSkin()
        {
            if (!ThemeLoaded)
            {
                return;
            }

            _Theme.Color.Get(_PartyModeId, out Color);
            _Theme.SelColor.Get(_PartyModeId, out SelColor);

            X = _Theme.X;
            Y = _Theme.Y;
            if (!_ButtonText)
            {
                Z = _Theme.Z;
            }

            W = _Theme.MaxWidth;
            H = _Theme.FontHeight;
            _Align = _Theme.Align;
            _ResizeAlign = _Theme.ResizeAlign;
            Font = new CFont(_Theme.FontFamily, _Theme.FontStyle, _Theme.FontHeight);

            if (_Theme.Reflection.HasValue)
            {
                _ReflectionSpace = _Theme.Reflection.Value.Space;
                _ReflectionHeight = _Theme.Reflection.Value.Height;
            }

            Text = _Theme.Text;
            Selected = false;
        }

        public void ReloadSkin()
        {
            UnloadSkin();
            LoadSkin();
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

            _Theme.X = X;
            _Theme.Y = Y;
        }

        public void ResizeElement(int stepW, int stepH)
        {
            _Theme.FontHeight += stepH;
            if (_Theme.FontHeight <= 0)
            {
                _Theme.FontHeight = 1;
            }

            _Font.Height = _Theme.FontHeight;
        }
        #endregion ThemeEdit

        private void _UpdateTextPosition()
        {
            if (!_PositionNeedsUpdate)
            {
                return;
            }

            _CalculatedFont = new CFont(Font);
            _Rect = MaxRect;
            _PositionNeedsUpdate = false;

            if (TranslatedText == "")
            {
                return;
            }

            var h = _CalculatedFont.Height;
            var y = Y;
            var bounds = CBase.Fonts.GetTextBounds(this);

            if (W > 0f && bounds.Width > W && bounds.Width > 0f)
            {
                var factor = W / bounds.Width;
                var step = h * (1 - factor);
                h *= factor;
                switch (ResizeAlign)
                {
                    case EHAlignment.Top:
                        y += step * 0.25f;
                        break;
                    case EHAlignment.Center:
                        y += step * 0.50f;
                        break;
                    case EHAlignment.Bottom:
                        y += step * 0.75f;
                        break;
                }

                _CalculatedFont.Height = h;
                bounds = CBase.Fonts.GetTextBounds(this);
            }

            var x = X;
            switch (Align)
            {
                case EAlignment.Center:
                    x = X - bounds.Width / 2;
                    break;
                case EAlignment.Right:
                    x = X - bounds.Width;
                    break;
            }

            _Rect = new SRectF(x, y, bounds.Width, bounds.Height, Z);
        }

        public void FontChanged()
        {
            _PositionNeedsUpdate = true;
        }
    }
}