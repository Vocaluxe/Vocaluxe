using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;

using Vocaluxe.Lib.Draw;
using Vocaluxe.Menu;

namespace Vocaluxe.Base
{  
    class CGlyph
    {
        private float _SIZEh = 50f;
        public float SIZEh
        {
            get { return _SIZEh; }
        }

        public STexture Texture;
        public char Chr;
        public int width;
        
        public CGlyph(char chr, float MaxHigh)
        {
            _SIZEh = MaxHigh;
            
            float outline = CFonts.Outline;
            TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;

            float factor = GetFactor(chr, flags);
            CFonts.Height = SIZEh * factor;
            Bitmap bmp = new Bitmap(10, 10);
            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp);

            Font fo = CFonts.GetFont();
            Size sizeB = TextRenderer.MeasureText(g, chr.ToString(), fo, new Size(int.MaxValue, int.MaxValue), flags);

            SizeF size = g.MeasureString(chr.ToString(), fo);
            
            g.Dispose();
            bmp.Dispose();

            bmp = new Bitmap((int)(sizeB.Width * 2f), sizeB.Height);
            g = System.Drawing.Graphics.FromImage(bmp);
            g.Clear(System.Drawing.Color.Transparent);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            CFonts.Height = SIZEh;
            fo = CFonts.GetFont();

            PointF point = new PointF(
                    outline * Math.Abs(sizeB.Width - size.Width) + (sizeB.Width - size.Width) / 2f + SIZEh / 5f,
                    (sizeB.Height - size.Height - (size.Height + SIZEh/4f) * (1f - factor)) / 2f);

            GraphicsPath path = new GraphicsPath();
            path.AddString(
                chr.ToString(),
                fo.FontFamily,
                (int)fo.Style,
                SIZEh,
                point,
                new StringFormat());

            Pen pen = new Pen(
                Color.FromArgb(
                    (int)CFonts.OutlineColor.A * 255,
                    (int)CFonts.OutlineColor.R * 255,
                    (int)CFonts.OutlineColor.G * 255,
                    (int)CFonts.OutlineColor.B * 255),
                SIZEh * outline);

            pen.LineJoin = LineJoin.Round;
            g.DrawPath(pen, path);
            g.FillPath(Brushes.White, path);

            /*
            g.DrawString(
                chr.ToString(),
                fo,
                Brushes.White,
                point);
             * */

            Texture = CDraw.AddTexture(bmp);
            //bmp.Save("test.png", ImageFormat.Png);
            Chr = chr;
            width = (int)((1f + outline / 2f) * sizeB.Width * Texture.width/factor / bmp.Width);

            bmp.Dispose();
            g.Dispose();
            fo.Dispose();
        }

        public void UnloadTexture()
        {
            CDraw.RemoveTexture(ref Texture);
        }

        private float GetFactor(char chr, TextFormatFlags flags)
        {
            if (CFonts.Style == EStyle.Normal)
                return 1f;

            EStyle style = CFonts.Style;

            CFonts.Style = EStyle.Normal;
            CFonts.Height = SIZEh;
            Bitmap bmp = new Bitmap(10, 10);
            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp);

            Font fo = CFonts.GetFont();
            Size sizeB = TextRenderer.MeasureText(g, chr.ToString(), fo, new Size(int.MaxValue, int.MaxValue), flags);
            //SizeF size = g.MeasureString(chr.ToString(), fo);
            float h_normal = sizeB.Height;

            CFonts.Style = style;
            bmp = new Bitmap(10, 10);
            g = System.Drawing.Graphics.FromImage(bmp);

            fo = CFonts.GetFont();
            sizeB = TextRenderer.MeasureText(g, chr.ToString(), fo, new Size(int.MaxValue, int.MaxValue), flags);
            //size = g.MeasureString(chr.ToString(), fo);
            float h_style = sizeB.Height;
            g.Dispose();
            return h_normal / h_style;
        }
    }

    class CFont
    {
        private List<CGlyph> _Glyphs;
        private Hashtable _htGlyphs;
        private PrivateFontCollection fonts;
        private FontFamily family;
        private float SIZEh;
        
        public string FilePath;
        
        
        public Font GetFont()
        {
            if (fonts == null)
            {
                fonts = new PrivateFontCollection();
                try
                {
                    fonts.AddFontFile(FilePath);
                    family = fonts.Families[0];
                }
                catch (Exception e)
                {
                    CLog.LogError("Error opening font file " + FilePath + ": " + e.Message);
                }      
            }
            
            return new Font(family, CFonts.Height, CFonts.GetFontStyle(), GraphicsUnit.Pixel);
        }

        public CFont(string File)
        {
            FilePath = File;
       
            _Glyphs = new List<CGlyph>();
            _htGlyphs = new Hashtable();

            switch (CConfig.TextureQuality)
            {
                case ETextureQuality.TR_CONFIG_TEXTURE_LOWEST:
                    SIZEh = 25f;
                    break;
                case ETextureQuality.TR_CONFIG_TEXTURE_LOW:
                    SIZEh = 50f;
                    break;
                case ETextureQuality.TR_CONFIG_TEXTURE_MEDIUM:
                    SIZEh = 100f;
                    break;
                case ETextureQuality.TR_CONFIG_TEXTURE_HIGH:
                    SIZEh = 200f;
                    break;
                case ETextureQuality.TR_CONFIG_TEXTURE_HIGHEST:
                    SIZEh = 400f;
                    break;
                default:
                    SIZEh = 100f;
                    break;
            }
        }

        public void DrawGlyph(char chr, float x, float y, float h, float z, SColorF color)
        {
            AddGlyph(chr);

            CFonts.Height = h;
            CGlyph glyph = _Glyphs[(int)_htGlyphs[chr]];
            float factor = h / glyph.Texture.height;
            float d = glyph.SIZEh / 5f * factor;
            CDraw.DrawTexture(glyph.Texture, new SRectF(x - d, y, glyph.Texture.width * factor, h, z), color);
        }

        public void DrawGlyphReflection(char chr, float x, float y, float h, float z, SColorF color, float rspace, float rheight)
        {
            AddGlyph(chr);

            CFonts.Height = h;
            CGlyph glyph = _Glyphs[(int)_htGlyphs[chr]];
            float factor = h / glyph.Texture.height;
            float d = glyph.SIZEh / 5f * factor;
            SRectF rect = new SRectF(x - d, y, glyph.Texture.width * factor, h, z);
            CDraw.DrawTextureReflection(glyph.Texture, rect, color, rect, rspace, rheight);
        }

        public void DrawGlyph(char chr, float x, float y, float h, float z, SColorF color, float begin, float end)
        {
            AddGlyph(chr);

            CFonts.Height = h;
            CGlyph glyph = _Glyphs[(int)_htGlyphs[chr]];
            float factor = h / glyph.Texture.height;
            float width = glyph.Texture.width * factor;
            float d = glyph.SIZEh / 5f * factor;

            CDraw.DrawTexture(glyph.Texture, new SRectF(x - d, y, width, h, z), color, begin, end);
        }

        public float GetWidth(char chr)
        {
            AddGlyph(chr);

            CGlyph glyph = _Glyphs[(int)_htGlyphs[chr]];
            float factor = CFonts.Height / glyph.Texture.height;
            return glyph.width * factor;
        }

        public float GetHeight(char chr)
        {
            AddGlyph(chr);

            CGlyph glyph = _Glyphs[(int)_htGlyphs[chr]];
            float factor = CFonts.Height / glyph.Texture.height;
            return glyph.Texture.height * factor;
        }

        public void AddGlyph(char chr)
        {
            if (_htGlyphs.ContainsKey(chr))
                return;

            float h = CFonts.Height;
            _Glyphs.Add(new CGlyph(chr, SIZEh));
            _htGlyphs.Add(chr, _Glyphs.Count - 1);
            CFonts.Height = h;
        }

        public void UnloadAllGlyphs()
        {
            foreach (CGlyph glyph in _Glyphs)
            {
                glyph.UnloadTexture();
            }
            _Glyphs.Clear();
        }
    }

    struct SFont
    {
        public string Name;

        public bool IsThemeFont;
        public int PartyModeID;
        public string ThemeName;

        public string Folder;
        public string FileNormal;
        public string FileItalic;
        public string FileBold;
        public string FileBoldItalic;

        public float Outline;       //0..1, 0=not outline 1=100% outline
        public SColorF OutlineColor;

        public CFont Normal;
        public CFont Italic;
        public CFont Bold;
        public CFont BoldItalic;
    }

    static class CFonts
    {
        private static XmlWriterSettings _settings = new XmlWriterSettings();
                
        private static List<SFont> _Fonts;
        private static int _CurrentFont;
        private static float _Height = 1f;

        public static int PartyModeID
        { get; set; }

        public static EStyle Style = EStyle.Normal;

        public static int CurrentFont
        {
            get { return _CurrentFont; }
            set
            {
                if ((value >= 0) && (value < _Fonts.Count))
                    _CurrentFont = value;
            }
        }
        public static float Height
        {
            get { return _Height; }
            set
            {
                if (value < 0f)
                    _Height = 0f;
                else
                    _Height = value;
            }
        }

        public static float Outline
        {
            get { return _Fonts[_CurrentFont].Outline; }
            set
            {
                SFont font = _Fonts[_CurrentFont];
                font.Outline = value;
                if (font.Outline < 0f)
                    font.Outline = 0f;
                if (font.Outline > 1f)
                    font.Outline = 1f;
            }
        }

        public static SColorF OutlineColor
        {
            get { return _Fonts[_CurrentFont].OutlineColor; }
            set
            {
                SFont font = _Fonts[_CurrentFont];
                font.OutlineColor = value;
            }
        }

        public static void Init()
        {
            _settings.Indent = true;
            _settings.Encoding = System.Text.Encoding.UTF8;
            _settings.ConformanceLevel = ConformanceLevel.Document;

            _CurrentFont = 0;
            PartyModeID = -1;
            BuildFonts();
        }
        
        public static void BuildFonts()
        {
            _Fonts = new List<SFont>();
            _CurrentFont = 0;

            LoadFontList();
        }

        public static Font GetFont()
        {
            switch (Style)
            {
                case EStyle.Normal:
                    return _Fonts[_CurrentFont].Normal.GetFont();
                case EStyle.Italic:
                    return _Fonts[_CurrentFont].Italic.GetFont();
                case EStyle.Bold:
                    return _Fonts[_CurrentFont].Bold.GetFont();
                case EStyle.BoldItalic:
                    return _Fonts[_CurrentFont].BoldItalic.GetFont();
                default:
                    break;
            }
            // should never happen...
            return null;
        }

        #region DrawText
        public static void DrawText(string Text, int x, int y, int h)
        {
            DrawText(Text, h, x, y, 0f, new SColorF(0f, 0f, 0f, 1f));
        }

        public static void DrawText(string Text, float x, float y, float z)
        {
            DrawText(Text, Height, x, y, z, new SColorF(0f, 0f, 0f, 1f));
        }

        public static void DrawText(string Text, float h, float x, float y, float z, SColorF color)
        {
            if (h <= 0f)
                return;

            if (Text == String.Empty)
                return;

            Height = h;

            float dx = x;
            foreach (char chr in Text)
            {
                switch (Style)
                {
                    case EStyle.Normal:
                        _Fonts[_CurrentFont].Normal.DrawGlyph(chr, dx, y, Height, z, color);
                        dx += _Fonts[_CurrentFont].Normal.GetWidth(chr);
                        break;
                    case EStyle.Italic:
                        _Fonts[_CurrentFont].Italic.DrawGlyph(chr, dx, y, Height, z, color);
                        dx += _Fonts[_CurrentFont].Italic.GetWidth(chr);
                        break;
                    case EStyle.Bold:
                        _Fonts[_CurrentFont].Bold.DrawGlyph(chr, dx, y, Height, z, color);
                        dx += _Fonts[_CurrentFont].Bold.GetWidth(chr);
                        break;
                    case EStyle.BoldItalic:
                        _Fonts[_CurrentFont].BoldItalic.DrawGlyph(chr, dx, y, Height, z, color);
                        dx += _Fonts[_CurrentFont].BoldItalic.GetWidth(chr);
                        break;
                    default:
                        break;
                }
            }
        }

        public static void DrawTextReflection(string Text, float h, float x, float y, float z, SColorF color, float rspace, float rheight)
        {
            if (h <= 0f)
                return;

            if (Text == String.Empty)
                return;

            Height = h;

            float dx = x;
            foreach (char chr in Text)
            {
                switch (Style)
                {
                    case EStyle.Normal:
                        _Fonts[_CurrentFont].Normal.DrawGlyphReflection(chr, dx, y, Height, z, color, rspace, rheight);
                        dx += _Fonts[_CurrentFont].Normal.GetWidth(chr);
                        break;
                    case EStyle.Italic:
                        _Fonts[_CurrentFont].Italic.DrawGlyphReflection(chr, dx, y, Height, z, color, rspace, rheight);
                        dx += _Fonts[_CurrentFont].Italic.GetWidth(chr);
                        break;
                    case EStyle.Bold:
                        _Fonts[_CurrentFont].Bold.DrawGlyphReflection(chr, dx, y, Height, z, color, rspace, rheight);
                        dx += _Fonts[_CurrentFont].Bold.GetWidth(chr);
                        break;
                    case EStyle.BoldItalic:
                        _Fonts[_CurrentFont].BoldItalic.DrawGlyphReflection(chr, dx, y, Height, z, color, rspace, rheight);
                        dx += _Fonts[_CurrentFont].BoldItalic.GetWidth(chr);
                        break;
                    default:
                        break;
                }
            }
        }

        public static void DrawText(string Text, float h, float x, float y, float z, SColorF color, float begin, float end)
        {
            if (h <= 0f)
                return;

            if (Text == String.Empty)
                return;

            Height = h;

            float dx = x;
            float w = GetTextWidth(Text);
            if (w <= 0f)
                return;

            float x1 = x + w * begin;
            float x2 = x + w * end;

            foreach (char chr in Text)
            {
                float w2 = 0f;
                switch (Style)
                {
                    case EStyle.Normal:
                        w2 = _Fonts[_CurrentFont].Normal.GetWidth(chr);
                        break;
                    case EStyle.Italic:
                        w2 = _Fonts[_CurrentFont].Italic.GetWidth(chr);
                        break;
                    case EStyle.Bold:
                        w2 = _Fonts[_CurrentFont].Bold.GetWidth(chr);
                        break;
                    case EStyle.BoldItalic:
                        w2 = _Fonts[_CurrentFont].BoldItalic.GetWidth(chr);
                        break;
                    default:
                        break;
                }
                
                float b = (x1 - dx) / w2;
                if (b < 0f)
                    b = 0f;

                if (b < 1f)
                {
                    float e = (x2 - dx) / w2;
                    if (e > 1f)
                        e = 1f;

                    if (e > 0f)
                    {
                        switch (Style)
                        {
                            case EStyle.Normal:
                                _Fonts[_CurrentFont].Normal.DrawGlyph(chr, dx, y, Height, z, color, b, e);
                                break;
                            case EStyle.Italic:
                                _Fonts[_CurrentFont].Italic.DrawGlyph(chr, dx, y, Height, z, color, b, e);
                                break;
                            case EStyle.Bold:
                                _Fonts[_CurrentFont].Bold.DrawGlyph(chr, dx, y, Height, z, color, b, e);
                                break;
                            case EStyle.BoldItalic:
                                _Fonts[_CurrentFont].BoldItalic.DrawGlyph(chr, dx, y, Height, z, color, b, e);
                                break;
                            default:
                                break;
                        }
                    }
                }
                dx += w2;
            }
        }
        #endregion DrawText

        public static FontStyle GetFontStyle()
        {
            switch (Style)
            {
                case EStyle.Normal:
                    return FontStyle.Regular;
                case EStyle.Italic:
                    return FontStyle.Italic;
                case EStyle.Bold:
                    return FontStyle.Bold;
                case EStyle.BoldItalic:
                    return FontStyle.Bold | FontStyle.Italic;
                default:
                    return FontStyle.Regular;
            }
        }

	    public static void SetFont(string FontName)
	    {
            int Index = -1;

            if (PartyModeID != -1)
            {
                Index = GetFontIndexParty(PartyModeID, FontName);

                if (Index >= 0 && Index < _Fonts.Count)
                {
                    _CurrentFont = Index;
                    return;
                }
            }

            Index = GetFontIndex(CConfig.Theme, FontName);

            if (Index >= 0 && Index < _Fonts.Count)
            {
                _CurrentFont = Index;
                return;
            }

            for (int i = 0; i < _Fonts.Count; i++)
            {
                if (!_Fonts[i].IsThemeFont && _Fonts[i].Name == FontName)
                {
                    _CurrentFont = i;
                    return;
                }
            }
	    }

        public static RectangleF GetTextBounds(CText text)
        {
            return GetTextBounds(text, text.Height);
        }

        public static RectangleF GetTextBounds(CText text, float height)
        {
            Height = height;
            return new RectangleF(text.X, text.Y, GetTextWidth(CLanguage.Translate(text.Text, text.TranslationID)), GetTextHeight(CLanguage.Translate(text.Text, text.TranslationID)));
        }

        public static float GetTextWidth(string text)
        {
            float dx = 0;
            foreach (char chr in text)
            {
                switch (Style)
                {
                    case EStyle.Normal:
                        dx += _Fonts[_CurrentFont].Normal.GetWidth(chr);
                        break;
                    case EStyle.Italic:
                        dx += _Fonts[_CurrentFont].Italic.GetWidth(chr);
                        break;
                    case EStyle.Bold:
                        dx += _Fonts[_CurrentFont].Bold.GetWidth(chr);
                        break;
                    case EStyle.BoldItalic:
                        dx += _Fonts[_CurrentFont].BoldItalic.GetWidth(chr);
                        break;
                    default:
                        break;
                }
            }
            return dx;
        }

        public static float GetTextHeight(string text)
        {
            //return TextRenderer.MeasureText(text, GetFont()).Height;
            float h = 0f;
            foreach (char chr in text)
            {
                float hh = 0f;
                switch (Style)
                {
                    case EStyle.Normal:
                        hh = _Fonts[_CurrentFont].Normal.GetHeight(chr);
                        break;
                    case EStyle.Italic:
                        hh = _Fonts[_CurrentFont].Italic.GetHeight(chr);
                        break;
                    case EStyle.Bold:
                        hh = _Fonts[_CurrentFont].Bold.GetHeight(chr);
                        break;
                    case EStyle.BoldItalic:
                        hh = _Fonts[_CurrentFont].BoldItalic.GetHeight(chr);
                        break;
                    default:
                        break;
                }
            
                if (hh>h)
                    h = hh;
            }
            return h;
        }

        /// <summary>
        /// Load default fonts
        /// </summary>
        /// <returns></returns>
        private static bool LoadFontList()
        {
            CXMLReader xmlReader = CXMLReader.OpenFile(System.IO.Path.Combine(CSettings.sFolderFonts, CSettings.sFileFonts));
            if (xmlReader == null)
                return false;

            _Fonts.Clear();

            string value = string.Empty;
            int i = 1;
            while (xmlReader.GetValue("//root/Font" + i.ToString() + "/Folder", ref value, value))
            {
                string Folder = value;

                xmlReader.GetValue("//root/Font" + i.ToString() + "/FileNormal", ref value, value);
                value = Path.Combine(Directory.GetCurrentDirectory(),
                    Path.Combine(CSettings.sFolderFonts, Path.Combine(Folder, value)));
                CFont f = new CFont(value);
                SFont sf = new SFont();
                sf.Normal = f;

                string name = String.Empty;
                xmlReader.GetValue("//root/Font" + i.ToString() + "/Name", ref name, value);
                sf.Name = name;
                sf.IsThemeFont = false;
                sf.ThemeName = String.Empty;
                sf.PartyModeID = -1;
                    
                xmlReader.GetValue("//root/Font" + i.ToString() + "/FileItalic", ref value, value);
                value = Path.Combine(Directory.GetCurrentDirectory(),
                    Path.Combine(CSettings.sFolderFonts, Path.Combine(Folder, value)));
                f = new CFont(value);
                sf.Italic = f;

                xmlReader.GetValue("//root/Font" + i.ToString() + "/FileBold", ref value, value);
                value = Path.Combine(Directory.GetCurrentDirectory(),
                    Path.Combine(CSettings.sFolderFonts, Path.Combine(Folder, value)));
                f = new CFont(value);
                sf.Bold = f;

                xmlReader.GetValue("//root/Font" + i.ToString() + "/FileBoldItalic", ref value, value);
                value = Path.Combine(Directory.GetCurrentDirectory(),
                    Path.Combine(CSettings.sFolderFonts, Path.Combine(Folder, value)));
                f = new CFont(value);
                sf.BoldItalic = f;

                sf.Outline = 0f;
                xmlReader.TryGetFloatValue("//root/Font" + i.ToString() + "/Outline", ref sf.Outline);

                sf.OutlineColor = new SColorF(0f, 0f, 0f, 1f);
                xmlReader.TryGetFloatValue("//root/Font" + i.ToString() + "/OutlineColorR", ref sf.OutlineColor.R);
                xmlReader.TryGetFloatValue("//root/Font" + i.ToString() + "/OutlineColorG", ref sf.OutlineColor.G);
                xmlReader.TryGetFloatValue("//root/Font" + i.ToString() + "/OutlineColorB", ref sf.OutlineColor.B);
                xmlReader.TryGetFloatValue("//root/Font" + i.ToString() + "/OutlineColorA", ref sf.OutlineColor.A);

                _Fonts.Add(sf);
                i++;
            }
            return true;
        }

        /// <summary>
        /// Loads theme fonts from skin file
        /// </summary>
        public static void LoadThemeFonts(string ThemeName, string FontFolder, CXMLReader xmlReader)
        {
            string value = string.Empty;
            int i = 1;
            while (xmlReader.GetValue("//root/Fonts/Font" + i.ToString() + "/Folder", ref value, value))
            {
                SFont sf = new SFont();
                sf.Folder = value;

                sf.IsThemeFont = true;
                sf.ThemeName = ThemeName;
                sf.PartyModeID = -1;

                bool ok = true;

                ok &= xmlReader.GetValue("//root/Fonts/Font" + i.ToString() + "/FileNormal", ref value, value);
                sf.FileNormal = value;
                value = Path.Combine(FontFolder, Path.Combine(sf.Folder, value));
                CFont f = new CFont(value);
                sf.Normal = f;
                
                string name = String.Empty;
                ok &= xmlReader.GetValue("//root/Fonts/Font" + i.ToString() + "/Name", ref name, value);
                sf.Name = name;
                
                ok &= xmlReader.GetValue("//root/Fonts/Font" + i.ToString() + "/FileItalic", ref value, value);
                sf.FileItalic = value;
                value = Path.Combine(FontFolder, Path.Combine(sf.Folder, value));
                f = new CFont(value);
                sf.Italic = f;

                ok &= xmlReader.GetValue("//root/Fonts/Font" + i.ToString() + "/FileBold", ref value, value);
                sf.FileBold = value;
                value = Path.Combine(FontFolder, Path.Combine(sf.Folder, value));
                f = new CFont(value);
                sf.Bold = f;

                ok &= xmlReader.GetValue("//root/Fonts/Font" + i.ToString() + "/FileBoldItalic", ref value, value);
                sf.FileBoldItalic = value;
                value = Path.Combine(FontFolder, Path.Combine(sf.Folder, value));
                f = new CFont(value);
                sf.BoldItalic = f;

                sf.Outline = 0f;
                ok &= xmlReader.TryGetFloatValue("//root/Fonts/Font" + i.ToString() + "/Outline", ref sf.Outline);

                sf.OutlineColor = new SColorF(0f, 0f, 0f, 1f);
                ok &= xmlReader.TryGetFloatValue("//root/Fonts/Font" + i.ToString() + "/OutlineColorR", ref sf.OutlineColor.R);
                ok &= xmlReader.TryGetFloatValue("//root/Fonts/Font" + i.ToString() + "/OutlineColorG", ref sf.OutlineColor.G);
                ok &= xmlReader.TryGetFloatValue("//root/Fonts/Font" + i.ToString() + "/OutlineColorB", ref sf.OutlineColor.B);
                ok &= xmlReader.TryGetFloatValue("//root/Fonts/Font" + i.ToString() + "/OutlineColorA", ref sf.OutlineColor.A);

                if (ok)
                    _Fonts.Add(sf);
                else
                {
                    CLog.LogError("Error loading theme fonts for theme \"" + ThemeName + "\": Error in Font" + i.ToString());
                }
                i++;
            }

            CLog.StartBenchmark(1, "BuildGlyphs");
            BuildGlyphs();
            CLog.StopBenchmark(1, "BuildGlyphs");
        }

        public static void SaveThemeFonts(string ThemeName, XmlWriter writer)
        {
            if (_Fonts.Count == 0)
                return;

            int Index = 0;
            int FontNr = 1;
            bool SetStart = false;
            while (Index < _Fonts.Count)
            {
                if (_Fonts[Index].IsThemeFont && _Fonts[Index].ThemeName == ThemeName)
                {
                    if (!SetStart)
                    {
                        writer.WriteStartElement("Fonts");
                        SetStart = true;
                    }

                    writer.WriteStartElement("Font" + FontNr.ToString());

                    writer.WriteElementString("Name", _Fonts[Index].Name);
                    writer.WriteElementString("Folder", _Fonts[Index].Folder);

                    writer.WriteElementString("Outline", _Fonts[Index].Outline.ToString("#0.00"));
                    writer.WriteElementString("OutlineColorR", _Fonts[Index].OutlineColor.R.ToString("#0.00"));
                    writer.WriteElementString("OutlineColorG", _Fonts[Index].OutlineColor.G.ToString("#0.00"));
                    writer.WriteElementString("OutlineColorB", _Fonts[Index].OutlineColor.B.ToString("#0.00"));
                    writer.WriteElementString("OutlineColorA", _Fonts[Index].OutlineColor.A.ToString("#0.00"));

                    writer.WriteElementString("FileNormal", _Fonts[Index].FileNormal);
                    writer.WriteElementString("FileBold", _Fonts[Index].FileBold);
                    writer.WriteElementString("FileItalic", _Fonts[Index].FileItalic);
                    writer.WriteElementString("FileBoldItalic", _Fonts[Index].FileBoldItalic);

                    writer.WriteEndElement();

                    FontNr++;
                }
                Index++;
            }

            if (SetStart)
                writer.WriteEndElement();
        }

        public static void UnloadThemeFonts(string ThemeName)
        {
            if (_Fonts.Count == 0)
                return;

            int Index = 0;
            while (Index < _Fonts.Count)
            {
                if (_Fonts[Index].IsThemeFont && _Fonts[Index].ThemeName == ThemeName)
                {
                    _Fonts[Index].Normal.UnloadAllGlyphs();
                    _Fonts[Index].Italic.UnloadAllGlyphs();
                    _Fonts[Index].Bold.UnloadAllGlyphs();
                    _Fonts[Index].BoldItalic.UnloadAllGlyphs();
                    _Fonts.RemoveAt(Index);
                }
                else
                    Index++;
            }
        }

        /// <summary>
        /// Loads party mode fonts from skin file
        /// </summary>
        public static void LoadPartyModeFonts(int PartyModeID, string FontFolder, CXMLReader xmlReader)
        {
            string value = string.Empty;
            int i = 1;
            while (xmlReader.GetValue("//root/Fonts/Font" + i.ToString() + "/Folder", ref value, value))
            {
                SFont sf = new SFont();
                sf.Folder = value;

                sf.IsThemeFont = false;
                sf.ThemeName = String.Empty;
                sf.PartyModeID = PartyModeID;

                bool ok = true;

                ok &= xmlReader.GetValue("//root/Fonts/Font" + i.ToString() + "/FileNormal", ref value, value);
                sf.FileNormal = value;
                value = Path.Combine(FontFolder, Path.Combine(sf.Folder, value));
                CFont f = new CFont(value);
                sf.Normal = f;

                string name = String.Empty;
                ok &= xmlReader.GetValue("//root/Fonts/Font" + i.ToString() + "/Name", ref name, value);
                sf.Name = name;

                ok &= xmlReader.GetValue("//root/Fonts/Font" + i.ToString() + "/FileItalic", ref value, value);
                sf.FileItalic = value;
                value = Path.Combine(FontFolder, Path.Combine(sf.Folder, value));
                f = new CFont(value);
                sf.Italic = f;

                ok &= xmlReader.GetValue("//root/Fonts/Font" + i.ToString() + "/FileBold", ref value, value);
                sf.FileBold = value;
                value = Path.Combine(FontFolder, Path.Combine(sf.Folder, value));
                f = new CFont(value);
                sf.Bold = f;

                ok &= xmlReader.GetValue("//root/Fonts/Font" + i.ToString() + "/FileBoldItalic", ref value, value);
                sf.FileBoldItalic = value;
                value = Path.Combine(FontFolder, Path.Combine(sf.Folder, value));
                f = new CFont(value);
                sf.BoldItalic = f;

                sf.Outline = 0f;
                ok &= xmlReader.TryGetFloatValue("//root/Fonts/Font" + i.ToString() + "/Outline", ref sf.Outline);

                sf.OutlineColor = new SColorF(0f, 0f, 0f, 1f);
                ok &= xmlReader.TryGetFloatValue("//root/Fonts/Font" + i.ToString() + "/OutlineColorR", ref sf.OutlineColor.R);
                ok &= xmlReader.TryGetFloatValue("//root/Fonts/Font" + i.ToString() + "/OutlineColorG", ref sf.OutlineColor.G);
                ok &= xmlReader.TryGetFloatValue("//root/Fonts/Font" + i.ToString() + "/OutlineColorB", ref sf.OutlineColor.B);
                ok &= xmlReader.TryGetFloatValue("//root/Fonts/Font" + i.ToString() + "/OutlineColorA", ref sf.OutlineColor.A);

                if (ok)
                    _Fonts.Add(sf);
                else
                {
                    CLog.LogError("Error loading theme fonts for party mode from Folder \"" + FontFolder + "\": Error in Font" + i.ToString());
                }
                i++;
            }

            CLog.StartBenchmark(1, "BuildGlyphs");
            BuildGlyphs();
            CLog.StopBenchmark(1, "BuildGlyphs");
        }

        private static int GetFontIndex(string ThemeName, string FontName)
        {
            if (ThemeName == String.Empty || FontName == String.Empty)
                return -1;

            for (int i = 0; i < _Fonts.Count; i++)
            {
                if (_Fonts[i].IsThemeFont && _Fonts[i].Name == FontName && _Fonts[i].ThemeName == ThemeName)
                    return i;
            }

            return -1;
        }

        private static int GetFontIndexParty(int PartyModeID, string FontName)
        {
            if (PartyModeID == -1 || FontName == String.Empty)
                return -1;

            for (int i = 0; i < _Fonts.Count; i++)
            {
                if (!_Fonts[i].IsThemeFont && _Fonts[i].Name == FontName && _Fonts[i].PartyModeID == PartyModeID)
                    return i;
            }

            return -1;
        }

        private static void BuildGlyphs()
        {
            string Text = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPGRSTUVWGXZ1234567890";

            for (int i = 0; i < _Fonts.Count; i++)
            {
                CurrentFont = i;

                foreach (char chr in Text)
                {
                    Style = EStyle.Normal;
                    _Fonts[_CurrentFont].Normal.AddGlyph(chr);
                    Style = EStyle.Bold;
                    _Fonts[_CurrentFont].Bold.AddGlyph(chr);
                    Style = EStyle.Italic;
                    _Fonts[_CurrentFont].Italic.AddGlyph(chr);
                    Style = EStyle.BoldItalic;
                    _Fonts[_CurrentFont].BoldItalic.AddGlyph(chr);
                }
            }
            Style = EStyle.Normal;
            SetFont("Normal");
            
        }
    }
}
