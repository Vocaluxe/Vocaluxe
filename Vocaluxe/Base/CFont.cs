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
            Font fo;
            Size sizeB;
            SizeF size;
            using(Graphics g=Graphics.FromHwnd(IntPtr.Zero)){
                fo = CFonts.GetFont();
                sizeB = TextRenderer.MeasureText(g, chr.ToString(), fo, new Size(int.MaxValue, int.MaxValue), flags);

                size = g.MeasureString(chr.ToString(), fo);
            }

            using (Bitmap bmp = new Bitmap((int)(sizeB.Width * 2f), sizeB.Height))
            {
                Graphics g = System.Drawing.Graphics.FromImage(bmp);
                g.Clear(System.Drawing.Color.Transparent);

                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                CFonts.Height = SIZEh;
                fo = CFonts.GetFont();

                PointF point = new PointF(
                        outline * Math.Abs(sizeB.Width - size.Width) + (sizeB.Width - size.Width) / 2f + SIZEh / 5f,
                        (sizeB.Height - size.Height - (size.Height + SIZEh / 4f) * (1f - factor)) / 2f);

                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddString(
                        chr.ToString(),
                        fo.FontFamily,
                        (int)fo.Style,
                        SIZEh,
                        point,
                        new StringFormat());

                    using (Pen pen = new Pen(
                        Color.FromArgb(
                            (int)CFonts.OutlineColor.A * 255,
                            (int)CFonts.OutlineColor.R * 255,
                            (int)CFonts.OutlineColor.G * 255,
                            (int)CFonts.OutlineColor.B * 255),
                        SIZEh * outline))
                    {

                        pen.LineJoin = LineJoin.Round;
                        g.DrawPath(pen, path);
                        g.FillPath(Brushes.White, path);
                    }
                }

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
                width = (int)((1f + outline / 2f) * sizeB.Width * Texture.width / factor / bmp.Width);
                g.Dispose();
            }          
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
            float h_style, h_normal;
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                Font fo = CFonts.GetFont();
                Size sizeB = TextRenderer.MeasureText(g, chr.ToString(), fo, new Size(int.MaxValue, int.MaxValue), flags);
                //SizeF size = g.MeasureString(chr.ToString(), fo);
                h_normal = sizeB.Height;
            }
            CFonts.Style = style;
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                Font fo = CFonts.GetFont();
                Size sizeB = TextRenderer.MeasureText(g, chr.ToString(), fo, new Size(int.MaxValue, int.MaxValue), flags);
                //size = g.MeasureString(chr.ToString(), fo);
                h_style = sizeB.Height;
            }
            return h_normal / h_style;
        }
    }

    class CFont:IDisposable
    {
        private Dictionary<char, CGlyph> _Glyphs;
        private PrivateFontCollection fonts = null;
        private FontFamily family;
        private float SIZEh;
        
        public string FilePath;
               
        public CFont(string File)
        {
            FilePath = File;

            _Glyphs = new Dictionary<char, CGlyph>();

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

        public void DrawGlyph(char chr, float x, float y, float h, float z, SColorF color)
        {
            STexture texture;
            SRectF rect;
            _GetGlyphTextureAndRect(chr, x, y, z, h, out texture, out rect);
            CDraw.DrawTexture(texture, rect, color);
        }

        public void DrawGlyph(char chr, float x, float y, float h, float z, SColorF color, float begin, float end)
        {
            STexture texture;
            SRectF rect;
            _GetGlyphTextureAndRect(chr, x, y, z, h, out texture, out rect);
            CDraw.DrawTexture(texture, rect, color, begin, end);
        }

        public void DrawGlyphReflection(char chr, float x, float y, float h, float z, SColorF color, float rspace, float rheight)
        {
            STexture texture;
            SRectF rect;
            _GetGlyphTextureAndRect(chr, x, y, z, h, out texture, out rect);
            CDraw.DrawTextureReflection(texture, rect, color, rect, rspace, rheight);
        }

        private void _GetGlyphTextureAndRect(char chr, float x, float y, float z, float h, out STexture texture, out SRectF rect)
        {
            AddGlyph(chr);

            CFonts.Height = h;
            CGlyph glyph = _Glyphs[chr];
            texture = glyph.Texture;
            float factor = h / texture.height;
            float width = texture.width * factor;
            float d = glyph.SIZEh / 5f * factor;
            rect = new SRectF(x - d, y, width, h, z);
        }

        public float GetWidth(char chr)
        {
            AddGlyph(chr);

            CGlyph glyph = _Glyphs[chr];
            float factor = CFonts.Height / glyph.Texture.height;
            return glyph.width * factor;
        }

        public float GetHeight(char chr)
        {
            return CFonts.Height;
            /*WTF???
            AddGlyph(chr);

            CGlyph glyph = _Glyphs[chr];
            float factor = CFonts.Height / glyph.Texture.height;
            return glyph.Texture.height * factor;*/
        }

        public void AddGlyph(char chr)
        {
            if (_Glyphs.ContainsKey(chr))
                return;

            float h = CFonts.Height;
            _Glyphs.Add(chr, new CGlyph(chr, SIZEh));
            CFonts.Height = h;
        }

        public void UnloadAllGlyphs()
        {
            foreach (CGlyph glyph in _Glyphs.Values)
            {
                glyph.UnloadTexture();
            }
            _Glyphs.Clear();
        }

        public void Dispose()
        {
            if (fonts != null)
            {
                fonts.Dispose();
                fonts = null;
                UnloadAllGlyphs();
            }
            GC.SuppressFinalize(this);
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
            return _GetCurrentFont().GetFont();
        }

        private static CFont _GetCurrentFont()
        {
            switch (Style)
            {
                case EStyle.Normal:
                    return _Fonts[_CurrentFont].Normal;
                case EStyle.Italic:
                    return _Fonts[_CurrentFont].Italic;
                case EStyle.Bold:
                    return _Fonts[_CurrentFont].Bold;
                case EStyle.BoldItalic:
                    return _Fonts[_CurrentFont].BoldItalic;
            }
            //Just in case...
            return _Fonts[_CurrentFont].Normal;
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

            if (Text.Length == 0)
                return;

            Height = h;
            CFont Font = _GetCurrentFont();

            float dx = x;
            foreach (char chr in Text)
            {
                Font.DrawGlyph(chr, dx, y, Height, z, color);
                dx += Font.GetWidth(chr);
            }
        }

        public static void DrawTextReflection(string Text, float h, float x, float y, float z, SColorF color, float rspace, float rheight)
        {
            if (h <= 0f)
                return;

            if (Text.Length == 0)
                return;

            Height = h;
            CFont Font = _GetCurrentFont();

            float dx = x;
            foreach (char chr in Text)
            {
                Font.DrawGlyphReflection(chr, dx, y, Height, z, color, rspace, rheight);
                dx += Font.GetWidth(chr);
            }
        }

        public static void DrawText(string Text, float h, float x, float y, float z, SColorF color, float begin, float end)
        {
            if (h <= 0f)
                return;

            if (Text.Length == 0)
                return;

            Height = h;

            float dx = x;
            float w = GetTextWidth(Text);
            if (w <= 0f)
                return;

            float x1 = x + w * begin;
            float x2 = x + w * end;

            CFont Font = _GetCurrentFont();

            foreach (char chr in Text)
            {
                float w2 = Font.GetWidth(chr);
                float b = (x1 - dx) / w2;

                if (b < 1f)
                {
                    if (b < 0f)
                        b = 0f;
                    float e = (x2 - dx) / w2;
                    if (e > 0f)
                    {
                        if (e > 1f)
                            e = 1f;
                        Font.DrawGlyph(chr, dx, y, Height, z, color, b, e);
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
            CFont Font = _GetCurrentFont();
            foreach (char chr in text)
            {
                dx += Font.GetWidth(chr);
            }
            return dx;
        }

        public static float GetTextHeight(string text)
        {
            //return TextRenderer.MeasureText(text, GetFont()).Height;
            float h = 0f;
            CFont Font = _GetCurrentFont();
            foreach (char chr in text)
            {
                float hh = Font.GetHeight(chr);
                if (hh>h)
                    h = hh;
            }
            return h;
        }

        private static void _LoadFontFiles(CXMLReader xmlReader, string FontFolder, string ThemeName="", int PartyModeId=-1){
            string value = string.Empty;
            int i = 1;
            while (xmlReader.GetValue("//root/Fonts/Font" + i.ToString() + "/Folder", ref value, value))
            {
                SFont sf = new SFont();
                sf.Folder = value;
                sf.IsThemeFont = ThemeName.Length>0;
                sf.ThemeName = ThemeName;
                sf.PartyModeID = PartyModeId;

                bool ok = true;

                string name = String.Empty;
                ok &= xmlReader.GetValue("//root/Fonts/Font" + i.ToString() + "/Name", ref name, value);
                sf.Name = name;

                ok &= xmlReader.GetValue("//root/Fonts/Font" + i.ToString() + "/FileNormal", ref value, value);
                sf.FileNormal = value;
                value = Path.Combine(FontFolder, Path.Combine(sf.Folder, value));
                CFont f = new CFont(value);
                sf.Normal = f;
                
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
                    string fontTypes;
                    if (PartyModeId >= 0)
                        fontTypes = "theme fonts for party mode";
                    else if (ThemeName.Length > 0)
                        fontTypes = "theme fonts for theme \"" + ThemeName + "\"";
                    else
                        fontTypes = "basic fonts";
                    CLog.LogError("Error loading "+fontTypes+": Error in Font" + i.ToString());
                }
                i++;
            }
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

            _LoadFontFiles(xmlReader, Path.Combine(Directory.GetCurrentDirectory(), CSettings.sFolderFonts));
            return true;
        }

        /// <summary>
        /// Loads theme fonts from skin file
        /// </summary>
        public static void LoadThemeFonts(string ThemeName, string FontFolder, CXMLReader xmlReader)
        {
            _LoadFontFiles(xmlReader, FontFolder, ThemeName);
            CLog.StartBenchmark(1, "BuildGlyphs");
            BuildGlyphs();
            CLog.StopBenchmark(1, "BuildGlyphs");
        }

        /// <summary>
        /// Loads party mode fonts from skin file
        /// </summary>
        public static void LoadPartyModeFonts(int PartyModeID, string FontFolder, CXMLReader xmlReader)
        {
            _LoadFontFiles(xmlReader, FontFolder, "", PartyModeID);
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

        private static int GetFontIndex(string ThemeName, string FontName)
        {
            if (ThemeName.Length == 0 || FontName.Length == 0)
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
            if (PartyModeID == -1 || FontName.Length == 0)
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
