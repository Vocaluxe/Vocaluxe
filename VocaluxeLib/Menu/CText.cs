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
using System.Drawing;
using System.Xml.Serialization;
using VocaluxeLib.Xml;

namespace VocaluxeLib.Menu
{
    [XmlType("Text")]
    public struct SThemeText
    {
        [XmlAttribute(AttributeName = "Name")] public string Name;

        public float X;
        public float Y;
        public float Z;
        [XmlElement("H")] public float FontHeight;
        [XmlElement("MaxW")] public float MaxWidth;
        public SThemeColor Color;
        public SThemeColor SColor; //for Buttons
        public EAlignment Align;
        public EHAlignment ResizeAlign;
        [XmlElement("Style")] public EStyle FontStyle;
        [XmlElement("Font")] public string FontFamily;
        public string Text;
        public SReflection Reflection;
    }

    public class CText : IMenuElement, IFontObserver
    {
        private SThemeText _Theme;
        private bool _ThemeLoaded;
        private readonly int _PartyModeID = -1;
        private int _TranslationID = -1;

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        public bool ThemeLoaded
        {
            get { return _ThemeLoaded; }
        }

        private bool _ButtonText;
        private bool _PositionNeedsUpdate = true;

        private float _X;
        public float X
        {
            //left
            get { return _X; }
            set
            {
                if (Math.Abs(_X - value) > 0.01)
                {
                    _X = value;
                    _PositionNeedsUpdate = true;
                }
            }
        }

        private float _Y;
        public float Y
        {
            //higher
            get { return _Y; }
            set
            {
                if (Math.Abs(_Y - value) > 0.01)
                {
                    _Y = value;
                    _PositionNeedsUpdate = true;
                }
            }
        }

        private float _Z;
        public float Z
        {
            get { return _Z; }
            set { _Z = value; }
        }

        private float _MaxWidth;
        public float MaxWidth
        {
            get { return _MaxWidth; }
            set
            {
                if (Math.Abs(_MaxWidth - value) > 0.01)
                {
                    _MaxWidth = value;
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
        ///     Do NOT read/write this anywhere but in _UpdateTextPosition!
        /// </summary>
        private SRectF _Rect;
        public SRectF Rect
        {
            get
            {
                if (_PositionNeedsUpdate)
                    _UpdateTextPosition();
                return _Rect;
            }
        }

        public SColorF Color; //normal Color
        public SColorF SelColor; //selected Color for Buttons

        public float ReflectionSpace;
        public float ReflectionHeight;

        private string _Text = String.Empty;
        public string Text
        {
            get { return _Theme.Text; }
            set
            {
                string translation = CBase.Language.Translate(value, _TranslationID);
                if (_Theme.Text != value || translation != _Text)
                {
                    _Theme.Text = value;
                    _Text = translation;
                    if (_EditMode)
                        _Text += "|";
                    _PositionNeedsUpdate = true;
                }
            }
        }
        public string TranslatedText
        {
            get { return _Text; }
        }

        public int PartyModeID
        {
            get { return _PartyModeID; }
        }

        public int TranslationID
        {
            get { return _TranslationID; }
            set
            {
                if (_TranslationID != value)
                {
                    _Text = CBase.Language.Translate(_Text, value);
                    _TranslationID = value;
                    _PositionNeedsUpdate = true;
                }
            }
        }

        public bool Selected;
        public bool Visible = true;
        private bool _EditMode;
        public bool EditMode
        {
            get { return _EditMode; }
            set
            {
                if (_EditMode != value)
                {
                    _Text = value ? _Text + "|" : _Text.Substring(0, _Text.Length - 1);
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
                    _UpdateTextPosition();
                return _CalculatedFont;
            }
        }

        public CText(int partyModeID)
        {
            _PartyModeID = partyModeID;
            _TranslationID = _PartyModeID;
            _Font.AddObserver(this);
        }

        public CText(CText text)
        {
            _PartyModeID = text._PartyModeID;
            _TranslationID = text._TranslationID;

            _X = text._X;
            _Y = text._Y;
            _Z = text._Z;
            _MaxWidth = text._MaxWidth;
            _Align = text._Align;
            _ResizeAlign = text._ResizeAlign;
            Font = new CFont(text.Font); //Use setter to set observer

            Color = new SColorF(text.Color);
            SelColor = new SColorF(text.SelColor);
            ReflectionSpace = text.ReflectionSpace;
            ReflectionHeight = text.ReflectionHeight;

            Text = text.Text;
            Selected = text.Selected;
            Visible = text.Visible;
            Alpha = text.Alpha;

            _EditMode = text._EditMode;
        }

        public CText(float x, float y, float z, float h, float mw, EAlignment align, EStyle style, string fontFamily, SColorF col, string text, int partyModeID = -1,
                     float rheight = 0,
                     float rspace = 0) : this(partyModeID)
        {
            _Theme = new SThemeText {FontFamily = fontFamily, FontStyle = style, FontHeight = h, Text = text};
            _ThemeLoaded = false;
            _ButtonText = false;

            X = x;
            Y = y;
            Z = z;
            MaxWidth = mw;
            Align = align;
            ResizeAlign = EHAlignment.Center;
            _Font.Name = fontFamily;
            _Font.Style = style;
            _Font.Height = h;

            Color = col;
            SelColor = new SColorF(col);

            Text = text;

            Selected = false;

            ReflectionSpace = rspace;
            ReflectionHeight = rheight;
        }

        public CText(SThemeText theme, int partyModeID, bool buttonText = false)
        {
            _PartyModeID = partyModeID;
            _TranslationID = partyModeID;
            _Theme = theme;

            _ButtonText = buttonText;

            LoadTextures();
        }

        public bool LoadTheme(string xmlPath, string elementName, CXMLReader xmlReader)
        {
            return LoadTheme(xmlPath, elementName, xmlReader, false);
        }

        public bool LoadTheme(string xmlPath, string elementName, CXMLReader xmlReader, bool buttonText)
        {
            string item = xmlPath + "/" + elementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/X", ref _X);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Y", ref _Y);

            if (!buttonText)
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Z", ref _Z);

            xmlReader.TryGetFloatValue(item + "/MaxW", ref _MaxWidth);

            if (xmlReader.GetValue(item + "/Color", out _Theme.Color.Name, String.Empty))
                _ThemeLoaded &= _Theme.Color.Get(_PartyModeID, out Color);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/R", ref Color.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/G", ref Color.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/B", ref Color.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/A", ref Color.A);
            }

            if (xmlReader.GetValue(item + "/SColor", out _Theme.SColor.Name, String.Empty))
                _ThemeLoaded &= _Theme.SColor.Get(_PartyModeID, out SelColor);
            else
            {
                if (xmlReader.TryGetFloatValue(item + "/SR", ref SelColor.R))
                {
                    _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SG", ref SelColor.G);
                    _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SB", ref SelColor.B);
                    _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SA", ref SelColor.A);
                }
            }

            _ThemeLoaded &= xmlReader.TryGetEnumValue(item + "/Align", ref _Align);
            xmlReader.TryGetEnumValue(item + "/ResizeAlign", ref _ResizeAlign);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/H", ref _Theme.FontHeight);
            _ThemeLoaded &= xmlReader.TryGetEnumValue(item + "/Style", ref _Theme.FontStyle);
            _ThemeLoaded &= xmlReader.GetValue(item + "/Font", out _Theme.FontFamily);

            _ThemeLoaded &= xmlReader.GetValue(item + "/Text", out _Theme.Text, String.Empty);

            if (xmlReader.ItemExists(item + "/Reflection") && !buttonText)
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Reflection/Space", ref ReflectionSpace);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Reflection/Height", ref ReflectionHeight);
                _Theme.Reflection = new SReflection(true, ReflectionHeight, ReflectionSpace);
            }
            else
                _Theme.Reflection = new SReflection(false, 0f, 0f);

            // Set values
            _Theme.Name = elementName;
            _Theme.Align = _Align;
            _Theme.Color.Color = Color;
            _Theme.ResizeAlign = _ResizeAlign;
            _Theme.SColor.Color = SelColor;
            _Theme.MaxWidth = _MaxWidth;
            _Theme.X = _X;
            _Theme.Y = _Y;
            _Theme.Z = _Z;

            _ButtonText = buttonText;
            _PositionNeedsUpdate = true;

            if (_ThemeLoaded)
                LoadTextures();
            return _ThemeLoaded;
        }

        public void Draw(bool forceDraw = false)
        {
            if (!forceDraw && !Visible && CBase.Settings.GetProgramState() != EProgramState.EditTheme)
                return;

            // Update Text
            Text = Text;

            SColorF currentColor = (Selected) ? SelColor : Color;
            var color = new SColorF(currentColor.R, currentColor.G, currentColor.B, currentColor.A * Alpha);

            CBase.Fonts.DrawText(_Text, CalculatedFont, Rect.X, Rect.Y, Z, color);

            if (ReflectionHeight > 0)
                CBase.Fonts.DrawTextReflection(_Text, CalculatedFont, Rect.X, Rect.Y, Z, color, ReflectionSpace, ReflectionHeight);

            if (Selected && (CBase.Settings.GetProgramState() == EProgramState.EditTheme))
                CBase.Drawing.DrawRect(new SColorF(0.5f, 1f, 0.5f, 0.5f), new SRectF(Rect.X, Rect.Y, Rect.W, Rect.H, Z));
        }

        public void Draw(float begin, float end)
        {
            SColorF currentColor = (Selected) ? SelColor : Color;
            var color = new SColorF(currentColor.R, currentColor.G, currentColor.B, currentColor.A * Alpha);

            CBase.Fonts.DrawText(Text, CalculatedFont, Rect.X, Rect.Y, Z, color, begin, end);

            if (ReflectionHeight > 0)
            {
                // TODO
            }

            if (Selected && (CBase.Settings.GetProgramState() == EProgramState.EditTheme))
                CBase.Drawing.DrawRect(new SColorF(0.5f, 1f, 0.5f, 0.5f), new SRectF(X, Y, Rect.W, Rect.H, Z));
        }

        public void DrawRelative(float rx, float ry, float reflectionHeight = 0f, float reflectionSpace = 0f, float rectHeight = 0f)
        {
            float oldReflectionSpace = ReflectionSpace;
            float oldReflectionHeight = ReflectionHeight;
            if (reflectionHeight > 0)
            {
                ReflectionSpace = (rectHeight - Rect.Y - Rect.H) * 2 + reflectionSpace;
                ReflectionHeight = reflectionHeight - (rectHeight - Rect.Y) + Rect.H;
            }
            else
                ReflectionHeight = 0;
            X += rx;
            Y += ry;
            Draw(true);
            ReflectionSpace = oldReflectionSpace;
            ReflectionHeight = oldReflectionHeight;
            X -= rx;
            Y -= ry;
        }

        public void UnloadTextures() {}

        public void LoadTextures()
        {
            _Theme.Color.Get(_PartyModeID, out Color);
            _Theme.SColor.Get(_PartyModeID, out SelColor);

            _X = _Theme.X;
            _Y = _Theme.Y;
            _Z = _Theme.Z;
            _MaxWidth = _Theme.MaxWidth;
            _Align = _Theme.Align;
            _ResizeAlign = _Theme.ResizeAlign;
            Font = new CFont(_Theme.FontFamily, _Theme.FontStyle, _Theme.FontHeight);

            if (_Theme.Reflection.Enabled)
            {
                ReflectionSpace = _Theme.Reflection.Space;
                ReflectionHeight = _Theme.Reflection.Height;
            }

            Text = _Theme.Text;
            Selected = false;

            _ThemeLoaded = true;
        }

        public void ReloadTextures()
        {
            UnloadTextures();
            LoadTextures();
        }

        public SThemeText GetTheme()
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
                _Theme.FontHeight = 1;
            _Font.Height = _Theme.FontHeight;
        }
        #endregion ThemeEdit

        private void _UpdateTextPosition()
        {
            _CalculatedFont = new CFont(Font);
            _PositionNeedsUpdate = false;

            if (_Text == "")
                return;

            float h = _CalculatedFont.Height;
            float y = Y;
            RectangleF bounds = CBase.Fonts.GetTextBounds(this);

            if (MaxWidth > 0f && bounds.Width > MaxWidth && bounds.Width > 0f)
            {
                float factor = MaxWidth / bounds.Width;
                float step = h * (1 - factor);
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

            float x = X;
            switch (Align)
            {
                case EAlignment.Center:
                    x = X - bounds.Width / 2;
                    break;
                case EAlignment.Right:
                    x = X - bounds.Width;
                    break;
            }

            _Rect.X = x;
            _Rect.Y = y;
            _Rect.W = bounds.Width;
            _Rect.H = bounds.Height;
        }

        public void FontChanged()
        {
            _PositionNeedsUpdate = true;
        }
    }
}