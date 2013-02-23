using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Xml;

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

    public class CText : IMenuElement
    {
        private SThemeText _Theme;
        private bool _ThemeLoaded;
        private int _PartyModeID;
        private int _TranslationID;

        public string GetThemeName()
        {
            return _Theme.Name;
        }

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
                    if (_MaxWidth > 0)
                        Bounds = new SRectF(-CBase.Settings.GetRenderW(), -CBase.Settings.GetRenderH(), _MaxWidth, 3f * CBase.Settings.GetRenderH(), 0f);
                    else
                        Bounds = new SRectF(-CBase.Settings.GetRenderW(), -CBase.Settings.GetRenderH(), 3f * CBase.Settings.GetRenderW(), 3f * CBase.Settings.GetRenderH(), 0f);

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
            set
            {
                _Text = CBase.Language.Translate(_Text, value);
                _PositionNeedsUpdate = true;
                _PartyModeID = value;
                _TranslationID = value;
            }
        }

        public int TranslationID
        {
            get { return _TranslationID; }
            set
            {
                _Text = CBase.Language.Translate(_Text, value);
                _PositionNeedsUpdate = true;
                _TranslationID = value;
            }
        }

        public bool Selected;
        public bool Visible = true;
        public bool EditMode = false;

        public float Alpha = 1f;

        public CText(int PartyModeID)
        {
            _Theme = new SThemeText();
            _ThemeLoaded = false;
            _ButtonText = false;
            _PartyModeID = PartyModeID;
            _TranslationID = _PartyModeID;

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

        public CText(CText text)
        {
            _Theme = new SThemeText();
            _ThemeLoaded = false;
            _ButtonText = false;
            _PartyModeID = text._PartyModeID;
            _TranslationID = text._TranslationID;

            X = text._X;
            Y = text._Y;
            Z = text._Z;
            Height = text._Height;
            MaxWidth = text._MaxWidth;
            Bounds = new SRectF(text._Bounds);
            Align = text._Align;
            HAlign = text._HAlign;
            Style = text._Style;
            Fon = text._Fon;

            Color = new SColorF(text.Color);
            SColor = new SColorF(text.SColor);
            Reflection = text.Reflection;
            ReflectionSpace = text.ReflectionSpace;
            ReflectionHeight = text.ReflectionHeight;

            Text = text._Text;
            Selected = text.Selected;
            Visible = text.Visible;
            Alpha = text.Alpha;

            EditMode = text.EditMode;
        }

        public CText(float x, float y, float z, EAlignment align, float h, float mw, float r, float g, float b, float a, EStyle style, string font, string text, float rspace, float rheight)
        {
            _Theme = new SThemeText();
            _ThemeLoaded = false;
            _ButtonText = false;
            _PartyModeID = -1;
            _TranslationID = -1;

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
                Bounds = new SRectF(-CBase.Settings.GetRenderW(), -CBase.Settings.GetRenderH(), MaxWidth, 3f * CBase.Settings.GetRenderH(), 0f);
            }
            else
            {
                Bounds = new SRectF(-CBase.Settings.GetRenderW(), -CBase.Settings.GetRenderH(), 3f * CBase.Settings.GetRenderW(), 3f * CBase.Settings.GetRenderH(), 0f);
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
            _PartyModeID = -1;
            _TranslationID = -1;

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
                Bounds = new SRectF(-CBase.Settings.GetRenderW(), -CBase.Settings.GetRenderH(), MaxWidth, 3f * CBase.Settings.GetRenderH(), 0f);
            }
            else
            {
                Bounds = new SRectF(-CBase.Settings.GetRenderW(), -CBase.Settings.GetRenderH(), 3f * CBase.Settings.GetRenderW(), 3f * CBase.Settings.GetRenderH(), 0f);
            }

            Reflection = false;
        }

        public CText(float x, float y, float z, float h, float mw, EAlignment align, EStyle style, string font, SColorF col, string text, float rspace, float rheight)
        {
            _Theme = new SThemeText();
            _ThemeLoaded = false;
            _ButtonText = false;
            _PartyModeID = -1;
            _TranslationID = -1;

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
                Bounds = new SRectF(-CBase.Settings.GetRenderW(), -CBase.Settings.GetRenderH(), MaxWidth, 3f * CBase.Settings.GetRenderH(), 0f);
            }
            else
            {
                Bounds = new SRectF(-CBase.Settings.GetRenderW(), -CBase.Settings.GetRenderH(), 3f * CBase.Settings.GetRenderW(), 3f * CBase.Settings.GetRenderH(), 0f);
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
            _PartyModeID = -1;
            _TranslationID = -1;

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
                Bounds = new SRectF(-CBase.Settings.GetRenderW(), -CBase.Settings.GetRenderH(), MaxWidth, 3f * CBase.Settings.GetRenderH(), 0f);
            }
            else
            {
                Bounds = new SRectF(-CBase.Settings.GetRenderW(), -CBase.Settings.GetRenderH(), 3f * CBase.Settings.GetRenderW(), 3f * CBase.Settings.GetRenderH(), 0f);
            }

            Reflection = false;
        }

        public bool LoadTheme(string XmlPath, string ElementName, CXMLReader xmlReader, int SkinIndex)
        {
            return LoadTheme(XmlPath, ElementName, xmlReader, SkinIndex, false);
        }

        public bool LoadTheme(string XmlPath, string ElementName, CXMLReader xmlReader, int SkinIndex, bool ButtonText)
        {
            string item = XmlPath + "/" + ElementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/X", ref _X);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Y", ref _Y);

            if (!ButtonText)
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Z", ref _Z);

            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/H", ref _Height);
            xmlReader.TryGetFloatValue(item + "/MaxW", ref _MaxWidth);

            if (xmlReader.GetValue(item + "/Color", ref _Theme.ColorName, String.Empty))
            {
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.ColorName, SkinIndex, ref Color);
            }
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/R", ref Color.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/G", ref Color.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/B", ref Color.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/A", ref Color.A);
            }

            if (xmlReader.GetValue(item + "/SColor", ref _Theme.SColorName, String.Empty))
            {
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.SColorName, SkinIndex, ref SColor);
            }
            else
            {
                if (xmlReader.TryGetFloatValue(item + "/SR", ref SColor.R))
                {
                    _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SG", ref SColor.G);
                    _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SB", ref SColor.B);
                    _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SA", ref SColor.A);
                }
            }

            _ThemeLoaded &= xmlReader.TryGetEnumValue<EAlignment>(item + "/Align", ref _Align);
            xmlReader.TryGetEnumValue<EHAlignment>(item + "/HAlign", ref _HAlign);
            _ThemeLoaded &= xmlReader.TryGetEnumValue<EStyle>(item + "/Style", ref _Style);
            _ThemeLoaded &= xmlReader.GetValue(item + "/Font", ref _Fon, "Normal");

            _ThemeLoaded &= xmlReader.GetValue(item + "/Text", ref _Theme.Text, String.Empty);

            if (xmlReader.ItemExists(item + "/Reflection") && !ButtonText)
            {
                Reflection = true;
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Reflection/Space", ref ReflectionSpace);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Reflection/Height", ref ReflectionHeight);
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
                    Bounds = new SRectF(-CBase.Settings.GetRenderW(), -CBase.Settings.GetRenderH(), MaxWidth, 3f * CBase.Settings.GetRenderH(), 0f);
                else
                    Bounds = new SRectF(-CBase.Settings.GetRenderW(), -CBase.Settings.GetRenderH(), 3f * CBase.Settings.GetRenderW(), 3f * CBase.Settings.GetRenderH(), 0f);

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

                writer.WriteComment("<Align>: Text align horizontal: " + CHelper.ListStrings(Enum.GetNames(typeof(EAlignment))));
                writer.WriteElementString("Align", Enum.GetName(typeof(EAlignment), Align));

                writer.WriteComment("<HAlign>: Text align vertical (on downsizing): " + CHelper.ListStrings(Enum.GetNames(typeof(EHAlignment))));
                writer.WriteElementString("HAlign", Enum.GetName(typeof(EHAlignment), HAlign));

                writer.WriteComment("<Style>: Text style: " + CHelper.ListStrings(Enum.GetNames(typeof(EStyle))));
                writer.WriteElementString("Style", Enum.GetName(typeof(EStyle), Style));

                writer.WriteComment("<Font>: Text font name");
                writer.WriteElementString("Font", Fon);

                writer.WriteComment("<Text>: Nothing or translation tag");
                if (CBase.Language.TranslationExists(_Theme.Text))
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
            if (!ForceDraw && !Visible && CBase.Settings.GetGameState() != EGameState.EditTheme)
                return;

            // Update Text
            Text = Text;

            if (_PositionNeedsUpdate)
                UpdateTextPosition();

            CBase.Fonts.SetFont(Fon);
            CBase.Fonts.SetStyle(Style);

            SColorF CurrentColor = new SColorF(Color);
            if (Selected)
                CurrentColor = new SColorF(SColor);

            SColorF color = new SColorF(CurrentColor.R, CurrentColor.G, CurrentColor.B, CurrentColor.A * Alpha);

            if (!EditMode)
                CBase.Fonts.DrawText(_Text, _DrawPosition.tH, _DrawPosition.X, _DrawPosition.Y, Z, color);
            else
                CBase.Fonts.DrawText(_Text + "|", _DrawPosition.tH, _DrawPosition.X, _DrawPosition.Y, Z, color);

            if (Reflection)
            {
                float sFactor = 0f;
                switch (HAlign)
                {
                    case EHAlignment.Top:
                        sFactor = (Height - _DrawPosition.tH) * 1.5f;
                        break;
                    case EHAlignment.Center:
                        sFactor = (Height - _DrawPosition.tH) * 1.0f;
                        break;
                    case EHAlignment.Bottom:
                        sFactor = (Height - _DrawPosition.tH) * 0.5f;
                        break;
                    default:
                        break;
                }
                if (!EditMode)
                    CBase.Fonts.DrawTextReflection(_Text, _DrawPosition.tH, _DrawPosition.X, _DrawPosition.Y, Z, color, ReflectionSpace + sFactor, ReflectionHeight);
                else
                    CBase.Fonts.DrawTextReflection(_Text + "|", _DrawPosition.tH, _DrawPosition.X, _DrawPosition.Y, Z, color, ReflectionSpace + sFactor, ReflectionHeight);
            }

            if (Selected && (CBase.Settings.GetGameState() == EGameState.EditTheme))
            {
                CBase.Drawing.DrawColor(
                    new SColorF(0.5f, 1f, 0.5f, 0.5f * CBase.Graphics.GetGlobalAlpha()),
                    new SRectF(_DrawPosition.X, _DrawPosition.Y, _DrawPosition.Width, _DrawPosition.Height, Z)
                    );
            }
        }

        public void Draw(float begin, float end)
        {
            RectangleF bounds = CBase.Fonts.GetTextBounds(this, this.Height);

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

            CBase.Fonts.SetFont(Fon);
            CBase.Fonts.SetStyle(Style);

            if (!EditMode)
                CBase.Fonts.DrawText(Text, Height, x, Y, Z, color, begin, end);
            else
                CBase.Fonts.DrawText(Text + "|", Height, x, Y, Z, color, begin, end);

            if (Reflection)
            {
                // TODO
            }

            if (Selected && (CBase.Settings.GetGameState() == EGameState.EditTheme))
            {
                CBase.Drawing.DrawColor(new SColorF(0.5f, 1f, 0.5f, 0.5f), new SRectF(x, Y, bounds.Width, bounds.Height, Z));
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

            CBase.Fonts.SetFont(Fon);
            CBase.Fonts.SetStyle(Style);

            RectangleF bounds = CBase.Fonts.GetTextBounds(this, this.Height);

            if (bounds.Width > Bounds.W && Bounds.W > 0f && bounds.Width > 0f)
            {
                float factor = Bounds.W / bounds.Width;
                float step = h * (1 - factor);
                h *= factor;
                switch (HAlign)
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
                    default:
                        break;
                }

                bounds = CBase.Fonts.GetTextBounds(this, h);
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

            CBase.Fonts.SetFont(Fon);
            CBase.Fonts.SetStyle(Style);
            
            if (!EditMode)
                CBase.Fonts.DrawText(_Text, h, x, y, Z, color);
            else
                CBase.Fonts.DrawText(_Text + "|", h, x, y, Z, color);

            if (reflection)
            {
                float space = (rectHeight - Y - bounds.Height) * 2f + reflectionSpace;
                float height = reflectionHeight - (rectHeight - Y) + bounds.Height;

                if (!EditMode)
                    CBase.Fonts.DrawTextReflection(_Text, h, x, y, Z, color, space, height);
                else
                    CBase.Fonts.DrawTextReflection(_Text + "|", h, x, y, Z, color, space, height);
            }

            if (Selected && (CBase.Settings.GetGameState() == EGameState.EditTheme))
            {
                CBase.Drawing.DrawColor(new SColorF(0.5f, 1f, 0.5f, 0.5f * CBase.Graphics.GetGlobalAlpha()), new SRectF(x, y, bounds.Width, bounds.Height, Z));
            }
        }

        public void UnloadTextures()
        {
        }

        public void LoadTextures()
        {
            if (_Theme.ColorName != String.Empty)
                Color = CBase.Theme.GetColor(_Theme.ColorName, _PartyModeID);

            if (_Theme.SColorName != String.Empty)
                SColor = CBase.Theme.GetColor(_Theme.SColorName, _PartyModeID);
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

            CBase.Fonts.SetFont(Fon);
            CBase.Fonts.SetStyle(Style);

            float h = Height;
            float y = Y;
            RectangleF bounds = CBase.Fonts.GetTextBounds(this, this.Height);

            if (bounds.Width > Bounds.W && Bounds.W > 0f && bounds.Width > 0f)
            {
                float factor = Bounds.W / bounds.Width;
                float step = h * (1 - factor);
                h *= factor ;
                switch (HAlign)
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
                    default:
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
