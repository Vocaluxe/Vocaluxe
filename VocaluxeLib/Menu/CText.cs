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
using System.Drawing;
using System.Xml;

namespace VocaluxeLib.Menu
{
    struct SThemeText
    {
        public string Name;

        public string Text;
        public string ColorName;
        public string SelColorName; //for Buttons
    }

    public class CText : IMenuElement
    {
        private SThemeText _Theme;
        private bool _ThemeLoaded;
        private readonly int _PartyModeID = -1;
        private int _TranslationID = -1;

        public string GetThemeName()
        {
            return _Theme.Name;
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

        private float _Height;
        public float Height
        {
            get { return _Height; }
            set
            {
                if (Math.Abs(_Height - value) > 0.01)
                {
                    _Height = value;
                    _PositionNeedsUpdate = true;
                }
            }
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

        private EStyle _Style = EStyle.Normal;
        public EStyle Style
        {
            get { return _Style; }
            set
            {
                if (_Style != value)
                {
                    _Style = value;
                    _PositionNeedsUpdate = true;
                }
            }
        }

        private string _Font = String.Empty;
        public string Font
        {
            get { return _Font; }
            set
            {
                if (_Font != value)
                {
                    _Font = value;
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
                    _PositionNeedsUpdate = true;
                }
            }
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

        public CText(int partyModeID)
        {
            _PartyModeID = partyModeID;
            _TranslationID = _PartyModeID;
            _Font = "Normal";
        }

        public CText(CText text)
        {
            _PartyModeID = text._PartyModeID;
            _TranslationID = text._TranslationID;

            _X = text._X;
            _Y = text._Y;
            _Z = text._Z;
            _Height = text._Height;
            _MaxWidth = text._MaxWidth;
            _Align = text._Align;
            _ResizeAlign = text._ResizeAlign;
            _Style = text._Style;
            _Font = text._Font;

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

        public CText(float x, float y, float z, float h, float mw, EAlignment align, EStyle style, string font, SColorF col, string text, int partyModeID = -1, float rheight = 0,
                     float rspace = 0)
        {
            _Theme = new SThemeText();
            _ThemeLoaded = false;
            _ButtonText = false;
            _PartyModeID = partyModeID;
            _TranslationID = _PartyModeID;

            X = x;
            Y = y;
            Z = z;
            Height = h;
            MaxWidth = mw;
            Align = align;
            ResizeAlign = EHAlignment.Center;
            Style = style;
            Font = font;

            Color = col;
            SelColor = new SColorF(col);

            Text = text;

            Selected = false;

            ReflectionSpace = rspace;
            ReflectionHeight = rheight;
        }

        public bool LoadTheme(string xmlPath, string elementName, CXMLReader xmlReader, int skinIndex)
        {
            return LoadTheme(xmlPath, elementName, xmlReader, skinIndex, false);
        }

        public bool LoadTheme(string xmlPath, string elementName, CXMLReader xmlReader, int skinIndex, bool buttonText)
        {
            string item = xmlPath + "/" + elementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/X", ref _X);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Y", ref _Y);

            if (!buttonText)
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Z", ref _Z);

            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/H", ref _Height);
            xmlReader.TryGetFloatValue(item + "/MaxW", ref _MaxWidth);

            if (xmlReader.GetValue(item + "/Color", out _Theme.ColorName, String.Empty))
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.ColorName, skinIndex, out Color);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/R", ref Color.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/G", ref Color.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/B", ref Color.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/A", ref Color.A);
            }

            if (xmlReader.GetValue(item + "/SColor", out _Theme.SelColorName, String.Empty))
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.SelColorName, skinIndex, out SelColor);
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
            _ThemeLoaded &= xmlReader.TryGetEnumValue(item + "/Style", ref _Style);
            _ThemeLoaded &= xmlReader.GetValue(item + "/Font", out _Font, "Normal");

            _ThemeLoaded &= xmlReader.GetValue(item + "/Text", out _Theme.Text, String.Empty);

            if (xmlReader.ItemExists(item + "/Reflection") && !buttonText)
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Reflection/Space", ref ReflectionSpace);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Reflection/Height", ref ReflectionHeight);
            }

            // Set values
            _Theme.Name = elementName;
            _ButtonText = buttonText;
            _PositionNeedsUpdate = true;

            if (_ThemeLoaded)
                LoadTextures();
            return _ThemeLoaded;
        }

        public bool SaveTheme(XmlWriter writer)
        {
            if (_ThemeLoaded || _ButtonText)
            {
                writer.WriteStartElement(_Theme.Name);

                writer.WriteComment("<X>, <Y>: Text position");
                writer.WriteElementString("X", X.ToString("#0"));
                writer.WriteElementString("Y", Y.ToString("#0"));

                if (!_ButtonText)
                {
                    writer.WriteComment("<Z>: Text position");
                    writer.WriteElementString("Z", Z.ToString("#0.00"));
                }

                writer.WriteComment("<H>: Text height");
                writer.WriteElementString("H", Height.ToString("#0"));

                writer.WriteComment("<MaxW>: Maximum text width (if exists)");
                if (MaxWidth > 0)
                    writer.WriteElementString("MaxW", MaxWidth.ToString("#0"));

                writer.WriteComment("<Color>: Text color from ColorScheme (high priority)");
                writer.WriteComment("or <R>, <G>, <B>, <A> (lower priority)");
                if (!String.IsNullOrEmpty(_Theme.ColorName))
                    writer.WriteElementString("Color", _Theme.ColorName);
                else
                {
                    writer.WriteElementString("R", Color.R.ToString("#0.00"));
                    writer.WriteElementString("G", Color.G.ToString("#0.00"));
                    writer.WriteElementString("B", Color.B.ToString("#0.00"));
                    writer.WriteElementString("A", Color.A.ToString("#0.00"));
                }

                writer.WriteComment("<SColor>: Selected Text color from ColorScheme (high priority)");
                writer.WriteComment("or <SR>, <SG>, <SB>, <SA> (lower priority)");
                if (!String.IsNullOrEmpty(_Theme.SelColorName))
                    writer.WriteElementString("SColor", _Theme.SelColorName);
                else
                {
                    writer.WriteElementString("SR", SelColor.R.ToString("#0.00"));
                    writer.WriteElementString("SG", SelColor.G.ToString("#0.00"));
                    writer.WriteElementString("SB", SelColor.B.ToString("#0.00"));
                    writer.WriteElementString("SA", SelColor.A.ToString("#0.00"));
                }

                writer.WriteComment("<Align>: Text align horizontal: " + CHelper.ListStrings(Enum.GetNames(typeof(EAlignment))));
                writer.WriteElementString("Align", Enum.GetName(typeof(EAlignment), Align));

                writer.WriteComment("<ResizeAlign>: Text align vertical (on downsizing): " + CHelper.ListStrings(Enum.GetNames(typeof(EHAlignment))));
                writer.WriteElementString("HAlign", Enum.GetName(typeof(EHAlignment), ResizeAlign));

                writer.WriteComment("<Style>: Text style: " + CHelper.ListStrings(Enum.GetNames(typeof(EStyle))));
                writer.WriteElementString("Style", Enum.GetName(typeof(EStyle), Style));

                writer.WriteComment("<Font>: Text font name");
                writer.WriteElementString("Font", Font);

                writer.WriteComment("<Text>: Nothing or translation tag");
                writer.WriteElementString("Text", CBase.Language.TranslationExists(_Theme.Text) ? _Theme.Text : string.Empty);

                if (!_ButtonText)
                {
                    writer.WriteComment("<Reflection> If exists:");
                    writer.WriteComment("   <Space>: Reflection Space");
                    writer.WriteComment("   <Height>: Reflection Height");
                }

                if (ReflectionHeight > 0 && !_ButtonText)
                {
                    writer.WriteStartElement("Reflection");
                    writer.WriteElementString("Space", ReflectionSpace.ToString("#0"));
                    writer.WriteElementString("Height", ReflectionHeight.ToString("#0"));
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();

                return true;
            }
            return false;
        }

        public void Draw(bool forceDraw = false)
        {
            if (!forceDraw && !Visible && CBase.Settings.GetGameState() != EGameState.EditTheme)
                return;

            // Update Text
            Text = Text;

            CBase.Fonts.SetFont(Font);
            CBase.Fonts.SetStyle(Style);

            SColorF currentColor = (Selected) ? SelColor : Color;
            SColorF color = new SColorF(currentColor.R, currentColor.G, currentColor.B, currentColor.A * Alpha);

            CBase.Fonts.DrawText(_Text, Rect.H, Rect.X, Rect.Y, Z, color);

            if (ReflectionHeight > 0)
                CBase.Fonts.DrawTextReflection(_Text, Rect.H, Rect.X, Rect.Y, Z, color, ReflectionSpace, ReflectionHeight);

            if (Selected && (CBase.Settings.GetGameState() == EGameState.EditTheme))
                CBase.Drawing.DrawColor(new SColorF(0.5f, 1f, 0.5f, 0.5f), new SRectF(Rect.X, Rect.Y, Rect.W, Rect.H, Z));
        }

        public void Draw(float begin, float end)
        {
            CBase.Fonts.SetFont(Font);
            CBase.Fonts.SetStyle(Style);

            SColorF currentColor = (Selected) ? SelColor : Color;
            SColorF color = new SColorF(currentColor.R, currentColor.G, currentColor.B, currentColor.A * Alpha);

            CBase.Fonts.DrawText(Text, Rect.H, Rect.X, Rect.Y, Z, color, begin, end);

            if (ReflectionHeight > 0)
            {
                // TODO
            }

            if (Selected && (CBase.Settings.GetGameState() == EGameState.EditTheme))
                CBase.Drawing.DrawColor(new SColorF(0.5f, 1f, 0.5f, 0.5f), new SRectF(X, Y, Rect.W, Rect.H, Z));
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
            if (!String.IsNullOrEmpty(_Theme.ColorName))
                Color = CBase.Theme.GetColor(_Theme.ColorName, _PartyModeID);

            if (!String.IsNullOrEmpty(_Theme.SelColorName))
                SelColor = CBase.Theme.GetColor(_Theme.SelColorName, _PartyModeID);
        }

        public void ReloadTextures()
        {
            UnloadTextures();
            LoadTextures();
        }

        #region ThemeEdit
        public void MoveElement(int stepX, int stepY)
        {
            X += stepX;
            Y += stepY;
        }

        public void ResizeElement(int stepW, int stepH)
        {
            Height += stepH;
            if (Height <= 0)
                Height = 1;
        }
        #endregion ThemeEdit

        private void _UpdateTextPosition()
        {
            if (_Text == "")
                return;

            CBase.Fonts.SetFont(Font);
            CBase.Fonts.SetStyle(Style);

            float h = Height;
            float y = Y;
            RectangleF bounds = CBase.Fonts.GetTextBounds(this);

            if (bounds.Width > MaxWidth && MaxWidth > 0f && bounds.Width > 0f)
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

                bounds = CBase.Fonts.GetTextBounds(this, h);
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

            _PositionNeedsUpdate = false;
        }
    }
}