using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Xml;
using System.Xml.XPath;

using Vocaluxe.Base;
using Vocaluxe.Lib.Draw;

namespace Vocaluxe.Menu
{
    struct SThemeText
    {
        public string Name;

        public string Text;
        public string ColorName;
        public string SColorName; //for Buttons
    }

    struct STextPosition
    {
        public float X;
        public float Y;
        public float tH;
        public float Width;
        public float Height;

        public STextPosition(int dummy)
        {
            X = 0f;
            Y = 0f;
            tH = 0f;
            Width = 0f;
            Height = 0f;
        }
    }

    class CText : IMenuElement
    {
        private SThemeText _Theme;
        private bool _ThemeLoaded;
        private bool _ButtonText;
        private bool _PositionNeedsUpdate = true;

        private STextPosition _DrawPosition = new STextPosition(0);

        private float _X = 0f;
        public float X //left
        {
            get { return _X; }
            set
            {
                if (_X != value)
                {
                    _X = value;
                    _PositionNeedsUpdate = true;
                }
            }
        }

        private float _Y = 0f;
        public float Y //higher
        {
            get { return _Y; }
            set
            {
                if (_Y != value)
                {
                    _Y = value;
                    _PositionNeedsUpdate = true;
                }
            }
        }

        private float _Z = 0f;
        public float Z
        {
            get { return _Z; }
            set
            {
                if (_Z != value)
                {
                    _Z = value;
                    _PositionNeedsUpdate = true;
                }
            }
        }

        private float _Height = 0f;
        public float Height
        {
            get { return _Height; }
            set
            {
                if (_Height != value)
                {
                    _Height = value;
                    _PositionNeedsUpdate = true;
                }
            }
        }

        private float _MaxWidth = 0f;
        public float MaxWidth
        {
            get { return _MaxWidth; }
            set
            {
                if (_MaxWidth != value)
                {
                    _MaxWidth = value;
                    _PositionNeedsUpdate = true;
                }
            }
        }

        private SRectF _Bounds = new SRectF();
        public SRectF Bounds
        {
            get { return _Bounds; }
            set
            {
                if (_Bounds.X != value.X || _Bounds.Y != value.Y || _Bounds.W != value.W || _Bounds.H != value.H || _Bounds.Z != value.Z)
                {
                    _Bounds = value;
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

        private EHAlignment _HAlign = EHAlignment.Center;
        public EHAlignment HAlign
        {
            get { return _HAlign; }
            set
            {
                if (_HAlign != value)
                {
                    _HAlign = value;
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

        private string _Fon = String.Empty;
        public string Fon
        {
            get { return _Fon; }
            set
            {
                if (_Fon != value)
                {
                    _Fon = value;
                    _PositionNeedsUpdate = true;
                }
            }
        }
       
        public SColorF Color;  //normal Color
        public SColorF SColor;  //selected Color for Buttons

        public bool Reflection;
        public float ReflectionSpace;
        public float ReflectionHeight;

        private string _Text = String.Empty;
        public string Text
        {
            get { return _Theme.Text; }
            set
            {
                string translation = CLanguage.Translate(value);
                if (_Theme.Text != value || translation != _Text)
                {
                    _Theme.Text = value;
                    _Text = translation;
                    _PositionNeedsUpdate = true; 
                }
            }
        }

        public bool Selected;
        public bool Visible = true;

        public float Alpha = 1f;

        public CText()
        {
            _Theme = new SThemeText();
            _ThemeLoaded = false;
            _ButtonText = false;

            X = 0f;
            Y = 0f;
            Z = 0f;
            Height = 1f;
            MaxWidth = 0f;
            Bounds = new SRectF();
            Align = EAlignment.Left;
            HAlign = EHAlignment.Center;
            Style = EStyle.Normal;
            Fon = "Normal";

            Color = new SColorF();
            SColor = new SColorF();
            Reflection = false;
            ReflectionSpace = 0f;
            ReflectionHeight = 0f;

            Text = String.Empty;
            Selected = false;
            Visible = true;
            Alpha = 1f;
        }

        public CText(float x, float y, float z, EAlignment align, float h, float mw, float r, float g, float b, float a, EStyle style, string font, string text, float rspace, float rheight)
        {
            _Theme = new SThemeText();
            _ThemeLoaded = false;
            _ButtonText = false;

            X = x;
            Y = y;
            Z = z;
            Height = h;
            MaxWidth = mw;
            Align = align;
            HAlign = EHAlignment.Center;
            Style = style;
            Fon = font;

            Color = new SColorF(r, g, b, a);
            SColor = new SColorF(r, g, b, a);
            
            Text = text;

            Selected = false;

            if (MaxWidth > 0)
            {
                Bounds = new SRectF(-CSettings.iRenderW, -CSettings.iRenderH, MaxWidth, 3f * CSettings.iRenderH, 0f);
            }
            else
            {
                Bounds = new SRectF(-CSettings.iRenderW, -CSettings.iRenderH, 3f * CSettings.iRenderW, 3f * CSettings.iRenderH, 0f);
            }

            Reflection = true;
            ReflectionSpace = rspace;
            ReflectionHeight = rheight;
        }

        public CText(float x, float y, float z, EAlignment align, float h, float mw, float r, float g, float b, float a, EStyle style, string font, string text)
        {
            _Theme = new SThemeText();
            _ThemeLoaded = false;
            _ButtonText = false;

            X = x;
            Y = y;
            Z = z;
            Height = h;
            MaxWidth = mw;
            Align = align;
            HAlign = EHAlignment.Center;
            Style = style;
            Fon = font;

            Color = new SColorF(r, g, b, a);
            SColor = new SColorF(r, g, b, a);

            Text = text;

            Selected = false;

            if (MaxWidth > 0)
            {
                Bounds = new SRectF(-CSettings.iRenderW, -CSettings.iRenderH, MaxWidth, 3f * CSettings.iRenderH, 0f);
            }
            else
            {
                Bounds = new SRectF(-CSettings.iRenderW, -CSettings.iRenderH, 3f * CSettings.iRenderW, 3f * CSettings.iRenderH, 0f);
            }

            Reflection = false;
        }

        public CText(float x, float y, float z, float h, float mw, EAlignment align, EStyle style, string font, SColorF col, string text, float rspace, float rheight)
        {
            _Theme = new SThemeText();
            _ThemeLoaded = false;
            _ButtonText = false;

            X = x;
            Y = y;
            Z = z;
            Height = h;
            MaxWidth = mw;
            Align = align;
            HAlign = EHAlignment.Center;
            Style = style;
            Fon = font;

            Color = col;
            SColor = new SColorF(col);
            
            Text = text;

            Selected = false;

            if (MaxWidth > 0)
            {
                Bounds = new SRectF(-CSettings.iRenderW, -CSettings.iRenderH, MaxWidth, 3f * CSettings.iRenderH, 0f);
            }
            else
            {
                Bounds = new SRectF(-CSettings.iRenderW, -CSettings.iRenderH, 3f * CSettings.iRenderW, 3f * CSettings.iRenderH, 0f);
            }

            Reflection = true;
            ReflectionSpace = rspace;
            ReflectionHeight = rheight;
        }

        public CText(float x, float y, float z, float h, float mw, EAlignment align, EStyle style, string font, SColorF col, string text)
        {
            _Theme = new SThemeText();
            _ThemeLoaded = false;
            _ButtonText = false;

            X = x;
            Y = y;
            Z = z;
            Height = h;
            MaxWidth = mw;
            Align = align;
            HAlign = EHAlignment.Center;
            Style = style;
            Fon = font;

            Color = col;
            SColor = new SColorF(col);

            Text = text;

            Selected = false;

            if (MaxWidth > 0)
            {
                Bounds = new SRectF(-CSettings.iRenderW, -CSettings.iRenderH, MaxWidth, 3f * CSettings.iRenderH, 0f);
            }
            else
            {
                Bounds = new SRectF(-CSettings.iRenderW, -CSettings.iRenderH, 3f * CSettings.iRenderW, 3f * CSettings.iRenderH, 0f);
            }

            Reflection = false;
        }

        public bool LoadTheme(string XmlPath, string ElementName, XPathNavigator navigator, int SkinIndex)
        {
            return LoadTheme(XmlPath, ElementName, navigator, SkinIndex, false);
        }

        public bool LoadTheme(string XmlPath, string ElementName, XPathNavigator navigator, int SkinIndex, bool ButtonText)
        {
            string item = XmlPath + "/" + ElementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/X", navigator, ref _X);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/Y", navigator, ref _Y);

            if (!ButtonText)
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/Z", navigator, ref _Z);

            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/H", navigator, ref _Height);
            CHelper.TryGetFloatValueFromXML(item + "/MaxW", navigator, ref _MaxWidth);

            if (CHelper.GetValueFromXML(item + "/Color", navigator, ref _Theme.ColorName, String.Empty))
            {
                _ThemeLoaded &= CTheme.GetColor(_Theme.ColorName, SkinIndex, ref Color);
            }
            else
            {
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/R", navigator, ref Color.R);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/G", navigator, ref Color.G);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/B", navigator, ref Color.B);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/A", navigator, ref Color.A);
            }

            if (CHelper.GetValueFromXML(item + "/SColor", navigator, ref _Theme.SColorName, String.Empty))
            {
                _ThemeLoaded &= CTheme.GetColor(_Theme.SColorName, SkinIndex, ref SColor);
            }
            else
            {
                if (CHelper.TryGetFloatValueFromXML(item + "/SR", navigator, ref SColor.R))
                {
                    _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SG", navigator, ref SColor.G);
                    _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SB", navigator, ref SColor.B);
                    _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SA", navigator, ref SColor.A);
                }
            }

            _ThemeLoaded &= CHelper.TryGetEnumValueFromXML<EAlignment>(item + "/Align", navigator, ref _Align);
            CHelper.TryGetEnumValueFromXML<EHAlignment>(item + "/HAlign", navigator, ref _HAlign);
            _ThemeLoaded &= CHelper.TryGetEnumValueFromXML<EStyle>(item + "/Style", navigator, ref _Style);
            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/Font", navigator, ref _Fon, "Normal");

            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/Text", navigator, ref _Theme.Text, String.Empty);

            if (CHelper.ItemExistsInXML(item + "/Reflection", navigator) && !ButtonText)
            {
                Reflection = true;
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/Reflection/Space", navigator, ref ReflectionSpace);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/Reflection/Height", navigator, ref ReflectionHeight);
            }
            else
                Reflection = false;

            // Set values
            X = _X;
            Y = _Y;
            Z = _Z;
            Height = _Height;
            MaxWidth = _MaxWidth;
            Text = _Theme.Text;
            Fon = _Fon;
            Align = _Align;
            HAlign = _HAlign;
            Style = _Style;

            if (_ThemeLoaded)
            {
                _Theme.Name = ElementName;
                LoadTextures();

                if (MaxWidth > 0)
                    Bounds = new SRectF(-CSettings.iRenderW, -CSettings.iRenderH, MaxWidth, 3f * CSettings.iRenderH, 0f);
                else
                    Bounds = new SRectF(-CSettings.iRenderW, -CSettings.iRenderH, 3f * CSettings.iRenderW, 3f * CSettings.iRenderH, 0f);

                _ButtonText = ButtonText;
            }
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
                if (_Theme.ColorName != String.Empty)
                {
                    writer.WriteElementString("Color", _Theme.ColorName);
                }
                else
                {
                    writer.WriteElementString("R", Color.R.ToString("#0.00"));
                    writer.WriteElementString("G", Color.G.ToString("#0.00"));
                    writer.WriteElementString("B", Color.B.ToString("#0.00"));
                    writer.WriteElementString("A", Color.A.ToString("#0.00"));
                }

                writer.WriteComment("<SColor>: Selected Text color from ColorScheme (high priority)");
                writer.WriteComment("or <SR>, <SG>, <SB>, <SA> (lower priority)");
                if (_Theme.SColorName != String.Empty)
                {
                    writer.WriteElementString("SColor", _Theme.SColorName);
                }
                else
                {
                    writer.WriteElementString("SR", SColor.R.ToString("#0.00"));
                    writer.WriteElementString("SG", SColor.G.ToString("#0.00"));
                    writer.WriteElementString("SB", SColor.B.ToString("#0.00"));
                    writer.WriteElementString("SA", SColor.A.ToString("#0.00"));
                }

                writer.WriteComment("<Align>: Text align horizontal: " + CConfig.ListStrings(Enum.GetNames(typeof(EAlignment))));
                writer.WriteElementString("Align", Enum.GetName(typeof(EAlignment), Align));

                writer.WriteComment("<HAlign>: Text align vertical (on downsizing): " + CConfig.ListStrings(Enum.GetNames(typeof(EHAlignment))));
                writer.WriteElementString("HAlign", Enum.GetName(typeof(EHAlignment), HAlign));

                writer.WriteComment("<Style>: Text style: " + CConfig.ListStrings(Enum.GetNames(typeof(EStyle))));
                writer.WriteElementString("Style", Enum.GetName(typeof(EStyle), Style));

                writer.WriteComment("<Font>: Text font name");
                writer.WriteElementString("Font", Fon);

                writer.WriteComment("<Text>: Text or translation tag");
                if (CLanguage.TranslationExists(_Theme.Text))
                    writer.WriteElementString("Text", _Theme.Text);
                else
                    writer.WriteElementString("Text", string.Empty);

                if (!_ButtonText)
                {
                    writer.WriteComment("<Reflection> If exists:");
                    writer.WriteComment("   <Space>: Reflection Space");
                    writer.WriteComment("   <Height>: Reflection Height");
                }

                if (Reflection && !_ButtonText)
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
            if (!ForceDraw && !Visible && CSettings.GameState != EGameState.EditTheme)
                return;

            // Update Text
            Text = Text;

            if (_PositionNeedsUpdate)
                UpdateTextPosition();

            CFonts.SetFont(Fon);
            CFonts.Style = Style;

            SColorF CurrentColor = new SColorF(Color);
            if (Selected)
                CurrentColor = new SColorF(SColor);

            SColorF color = new SColorF(CurrentColor.R, CurrentColor.G, CurrentColor.B, CurrentColor.A * Alpha);


            CFonts.DrawText(_Text, _DrawPosition.tH, _DrawPosition.X, _DrawPosition.Y, Z, color);

            if (Reflection)
            {
                CFonts.DrawTextReflection(_Text, _DrawPosition.tH, _DrawPosition.X, _DrawPosition.Y, Z, color, ReflectionSpace, ReflectionHeight);
            }

            if (Selected && (CSettings.GameState == EGameState.EditTheme))
            {
                CDraw.DrawColor(
                    new SColorF(0.5f, 1f, 0.5f, 0.5f * CGraphics.GlobalAlpha),
                    new SRectF(_DrawPosition.X, _DrawPosition.Y, _DrawPosition.Width, _DrawPosition.Height, Z)
                    );
            }
        }

        public void Draw(float begin, float end)
        {
            RectangleF bounds = CDraw.GetTextBounds(this);

            float x = X;
            switch (Align)
            {
                case EAlignment.Center:
                    x = X - bounds.Width / 2;
                    break;
                case EAlignment.Right:
                    x = X - bounds.Width;
                    break;
                default:
                    break;
            }

            SColorF CurrentColor = new SColorF(Color);
            if (Selected)
                CurrentColor = new SColorF(SColor);

            SColorF color = new SColorF(CurrentColor.R, CurrentColor.G, CurrentColor.B, CurrentColor.A * Alpha);

            CFonts.SetFont(Fon);
            CFonts.Style = Style;
            CFonts.DrawText(Text, Height, x, Y, Z, color, begin, end);

            if (Reflection)
            {
                // TODO
            }

            if (Selected && (CSettings.GameState == EGameState.EditTheme))
            {
                CDraw.DrawColor(new SColorF(0.5f, 1f, 0.5f, 0.5f), new SRectF(x, Y, bounds.Width, bounds.Height, Z));
            }
        }

        public void DrawRelative(float x, float y)
        {
            DrawRelative(x, y, false, 0f, 0f, 0f);
        }

        public void DrawRelative(float x, float y, float reflectionSpace, float reflectionHeigth, float rectHeight)
        {
            DrawRelative(x, y, true, reflectionSpace, reflectionHeigth, rectHeight);
        }

        public void DrawRelative(float rx, float ry, bool reflection, float reflectionSpace, float reflectionHeight, float rectHeight)
        {
            // Update Text
            Text = Text;

            float h = Height;

            float x = X + rx;
            float y = Y + ry;

            RectangleF bounds = CDraw.GetTextBounds(this);

            while (bounds.Width > Bounds.W)
            {
                h -= 0.2f;

                switch (HAlign)
                {
                    case EHAlignment.Top:
                        //nothing to do
                        break;
                    case EHAlignment.Center:
                        y += 0.1f;
                        break;
                    case EHAlignment.Buttom:
                        y += 0.2f;
                        break;
                    default:
                        break;
                }
                
                bounds = CDraw.GetTextBounds(this, h);
            }

            switch (Align)
            {
                case EAlignment.Center:
                    x = x - bounds.Width / 2;
                    break;
                case EAlignment.Right:
                    x = x - bounds.Width;
                    break;
                default:
                    break;
            }
                       

            SColorF CurrentColor = new SColorF(Color);
            if (Selected)
                CurrentColor = new SColorF(SColor);

            SColorF color = new SColorF(CurrentColor.R, CurrentColor.G, CurrentColor.B, CurrentColor.A * Alpha);

            CFonts.SetFont(Fon);
            CFonts.Style = Style;
            CFonts.DrawText(_Text, h, x, y, Z, color);

            if (reflection)
            {
                float space = (rectHeight - Y - bounds.Height) * 2f + reflectionSpace;
                float height = reflectionHeight - (rectHeight - Y) + bounds.Height;
                CFonts.DrawTextReflection(_Text, h, x, y, Z, color, space, height);
            }

            if (Selected && (CSettings.GameState == EGameState.EditTheme))
            {
                CDraw.DrawColor(new SColorF(0.5f, 1f, 0.5f, 0.5f * CGraphics.GlobalAlpha), new SRectF(x, y, bounds.Width, bounds.Height, Z));
            }
        }

        public void UnloadTextures()
        {
        }

        public void LoadTextures()
        {
            if (_Theme.ColorName != String.Empty)
                Color = CTheme.GetColor(_Theme.ColorName);

            if (_Theme.SColorName != String.Empty)
                SColor = CTheme.GetColor(_Theme.SColorName);
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

        private void UpdateTextPosition()
        {
            if (_Text == String.Empty)
                return;

            CFonts.SetFont(Fon);
            CFonts.Style = Style;

            float h = Height;
            float y = Y;
            RectangleF bounds = CFonts.GetTextBounds(this);

            while (bounds.Width > Bounds.W)
            {
                h -= 0.2f;

                switch (HAlign)
                {
                    case EHAlignment.Top:
                        //nothing to do
                        break;
                    case EHAlignment.Center:
                        y += 0.1f;
                        break;
                    case EHAlignment.Buttom:
                        y += 0.2f;
                        break;
                    default:
                        break;
                }

                bounds = CFonts.GetTextBounds(this, h);
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
                default:
                    break;
            }

            _DrawPosition.X = x;
            _DrawPosition.Y = y;
            _DrawPosition.tH = h;
            _DrawPosition.Width = bounds.Width;
            _DrawPosition.Height = bounds.Height;

            _PositionNeedsUpdate = false;
        }
    }
}
