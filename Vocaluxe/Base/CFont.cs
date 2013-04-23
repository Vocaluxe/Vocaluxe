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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using VocaluxeLib.Menu;

namespace Vocaluxe.Base
{
    class CGlyph
    {
        private readonly float _Sizeh = 50f;
        public float Sizeh
        {
            get { return _Sizeh; }
        }

        public STexture Texture;
        public readonly int Width;

        public CGlyph(char chr, float maxHigh)
        {
            _Sizeh = maxHigh;

            float outline = CFonts.Outline;
            const TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;

            float factor = _GetFactor(chr, flags);
            CFonts.Height = Sizeh * factor;
            Font fo;
            Size sizeB;
            SizeF size;
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                fo = CFonts.GetFont();
                sizeB = TextRenderer.MeasureText(g, chr.ToString(), fo, new Size(int.MaxValue, int.MaxValue), flags);

                size = g.MeasureString(chr.ToString(), fo);
            }

            using (Bitmap bmp = new Bitmap((int)(sizeB.Width * 2f), sizeB.Height))
            {
                Graphics g = Graphics.FromImage(bmp);
                g.Clear(Color.Transparent);

                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                CFonts.Height = Sizeh;
                fo = CFonts.GetFont();

                PointF point = new PointF(
                    outline * Math.Abs(sizeB.Width - size.Width) + (sizeB.Width - size.Width) / 2f + Sizeh / 5f,
                    (sizeB.Height - size.Height - (size.Height + Sizeh / 4f) * (1f - factor)) / 2f);

                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddString(
                        chr.ToString(),
                        fo.FontFamily,
                        (int)fo.Style,
                        Sizeh,
                        point,
                        new StringFormat());

                    using (Pen pen = new Pen(
                        Color.FromArgb(
                            (int)CFonts.OutlineColor.A * 255,
                            (int)CFonts.OutlineColor.R * 255,
                            (int)CFonts.OutlineColor.G * 255,
                            (int)CFonts.OutlineColor.B * 255),
                        Sizeh * outline))
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
                Width = (int)((1f + outline / 2f) * sizeB.Width * Texture.Width / factor / bmp.Width);
                g.Dispose();
            }
            fo.Dispose();
        }

        public void UnloadTexture()
        {
            CDraw.RemoveTexture(ref Texture);
        }

        private float _GetFactor(char chr, TextFormatFlags flags)
        {
            if (CFonts.Style == EStyle.Normal)
                return 1f;

            EStyle style = CFonts.Style;

            CFonts.Style = EStyle.Normal;
            CFonts.Height = Sizeh;
            float hStyle, hNormal;
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                Font fo = CFonts.GetFont();
                Size sizeB = TextRenderer.MeasureText(g, chr.ToString(), fo, new Size(int.MaxValue, int.MaxValue), flags);
                //SizeF size = g.MeasureString(chr.ToString(), fo);
                hNormal = sizeB.Height;
            }
            CFonts.Style = style;
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                Font fo = CFonts.GetFont();
                Size sizeB = TextRenderer.MeasureText(g, chr.ToString(), fo, new Size(int.MaxValue, int.MaxValue), flags);
                //size = g.MeasureString(chr.ToString(), fo);
                hStyle = sizeB.Height;
            }
            return hNormal / hStyle;
        }
    }

    class CFont : IDisposable
    {
        private readonly Dictionary<char, CGlyph> _Glyphs;
        private PrivateFontCollection _Fonts;
        private FontFamily _Family;
        private readonly float _Sizeh;

        public readonly string FilePath;

        public CFont(string file)
        {
            FilePath = file;

            _Glyphs = new Dictionary<char, CGlyph>();

            switch (CConfig.TextureQuality)
            {
                case ETextureQuality.TR_CONFIG_TEXTURE_LOWEST:
                    _Sizeh = 25f;
                    break;
                case ETextureQuality.TR_CONFIG_TEXTURE_LOW:
                    _Sizeh = 50f;
                    break;
                case ETextureQuality.TR_CONFIG_TEXTURE_MEDIUM:
                    _Sizeh = 100f;
                    break;
                case ETextureQuality.TR_CONFIG_TEXTURE_HIGH:
                    _Sizeh = 200f;
                    break;
                case ETextureQuality.TR_CONFIG_TEXTURE_HIGHEST:
                    _Sizeh = 400f;
                    break;
                default:
                    _Sizeh = 100f;
                    break;
            }
        }

        public Font GetFont()
        {
            if (_Fonts == null)
            {
                _Fonts = new PrivateFontCollection();
                try
                {
                    _Fonts.AddFontFile(FilePath);
                    _Family = _Fonts.Families[0];
                }
                catch (Exception e)
                {
                    CLog.LogError("Error opening font file " + FilePath + ": " + e.Message);
                }
            }

            return new Font(_Family, CFonts.Height, CFonts.GetFontStyle(), GraphicsUnit.Pixel);
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
            float factor = h / texture.Height;
            float width = texture.Width * factor;
            float d = glyph.Sizeh / 5f * factor;
            rect = new SRectF(x - d, y, width, h, z);
        }

        public float GetWidth(char chr)
        {
            AddGlyph(chr);

            CGlyph glyph = _Glyphs[chr];
            float factor = CFonts.Height / glyph.Texture.Height;
            return glyph.Width * factor;
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
            _Glyphs.Add(chr, new CGlyph(chr, _Sizeh));
            CFonts.Height = h;
        }

        public void UnloadAllGlyphs()
        {
            foreach (CGlyph glyph in _Glyphs.Values)
                glyph.UnloadTexture();
            _Glyphs.Clear();
        }

        public void Dispose()
        {
            if (_Fonts != null)
            {
                _Fonts.Dispose();
                _Fonts = null;
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

        public float Outline; //0..1, 0=not outline 1=100% outline
        public SColorF OutlineColor;

        public CFont Normal;
        public CFont Italic;
        public CFont Bold;
        public CFont BoldItalic;
    }

    static class CFonts
    {
        private static readonly XmlWriterSettings _Settings = new XmlWriterSettings();

        private static List<SFont> _Fonts;
        private static int _CurrentFont;
        private static float _Height = 1f;

        public static int PartyModeID { get; set; }

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
            set { _Height = value < 0f ? 0f : value; }
        }

        public static float Outline
        {
            get { return _Fonts[_CurrentFont].Outline; }
        }

        public static SColorF OutlineColor
        {
            get { return _Fonts[_CurrentFont].OutlineColor; }
        }

        public static void Init()
        {
            _Settings.Indent = true;
            _Settings.Encoding = Encoding.UTF8;
            _Settings.ConformanceLevel = ConformanceLevel.Document;

            _CurrentFont = 0;
            PartyModeID = -1;
            BuildFonts();
        }

        public static void BuildFonts()
        {
            _Fonts = new List<SFont>();
            _CurrentFont = 0;

            _LoadFontList();
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
        public static void DrawText(string text, int x, int y, int h)
        {
            DrawText(text, h, x, y, 0f, new SColorF(0f, 0f, 0f, 1f));
        }

        public static void DrawText(string text, float x, float y, float z)
        {
            DrawText(text, Height, x, y, z, new SColorF(0f, 0f, 0f, 1f));
        }

        public static void DrawText(string text, float h, float x, float y, float z, SColorF color)
        {
            if (h <= 0f)
                return;

            if (text == "")
                return;

            Height = h;
            CFont font = _GetCurrentFont();

            float dx = x;
            foreach (char chr in text)
            {
                font.DrawGlyph(chr, dx, y, Height, z, color);
                dx += font.GetWidth(chr);
            }
        }

        public static void DrawTextReflection(string text, float h, float x, float y, float z, SColorF color, float rspace, float rheight)
        {
            if (h <= 0f)
                return;

            if (text == "")
                return;

            Height = h;
            CFont font = _GetCurrentFont();

            float dx = x;
            foreach (char chr in text)
            {
                font.DrawGlyphReflection(chr, dx, y, Height, z, color, rspace, rheight);
                dx += font.GetWidth(chr);
            }
        }

        public static void DrawText(string text, float h, float x, float y, float z, SColorF color, float begin, float end)
        {
            if (h <= 0f)
                return;

            if (text == "")
                return;

            Height = h;

            float dx = x;
            float w = GetTextWidth(text);
            if (w <= 0f)
                return;

            float x1 = x + w * begin;
            float x2 = x + w * end;

            CFont font = _GetCurrentFont();

            foreach (char chr in text)
            {
                float w2 = font.GetWidth(chr);
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
                        font.DrawGlyph(chr, dx, y, Height, z, color, b, e);
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

        public static void SetFont(string fontName)
        {
            int index;

            if (PartyModeID != -1)
            {
                index = _GetFontIndexParty(PartyModeID, fontName);

                if (index >= 0 && index < _Fonts.Count)
                {
                    _CurrentFont = index;
                    return;
                }
            }

            index = _GetFontIndex(CConfig.Theme, fontName);

            if (index >= 0 && index < _Fonts.Count)
            {
                _CurrentFont = index;
                return;
            }

            for (int i = 0; i < _Fonts.Count; i++)
            {
                if (!_Fonts[i].IsThemeFont && _Fonts[i].Name == fontName)
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
            return new RectangleF(text.X, text.Y, GetTextWidth(CLanguage.Translate(text.Text, text.TranslationID)),
                                  GetTextHeight(CLanguage.Translate(text.Text, text.TranslationID)));
        }

        public static float GetTextWidth(string text)
        {
            CFont font = _GetCurrentFont();
            return text.Sum(chr => font.GetWidth(chr));
        }

        public static float GetTextHeight(string text)
        {
            //return TextRenderer.MeasureText(text, GetFont()).Height;
            CFont font = _GetCurrentFont();
            return text == "" ? 0 : text.Select(font.GetHeight).Max();
        }

        private static void _LoadFontFiles(CXMLReader xmlReader, string fontFolder, string themeName = "", int partyModeId = -1)
        {
            string value = string.Empty;
            int i = 1;
            while (xmlReader.GetValue("//root/Fonts/Font" + i + "/Folder", out value, value))
            {
                SFont sf = new SFont {Folder = value, IsThemeFont = themeName != "", ThemeName = themeName, PartyModeID = partyModeId};

                bool ok = true;

                string name;
                ok &= xmlReader.GetValue("//root/Fonts/Font" + i + "/Name", out name, value);
                sf.Name = name;

                ok &= xmlReader.GetValue("//root/Fonts/Font" + i + "/FileNormal", out value, value);
                sf.FileNormal = value;
                value = Path.Combine(fontFolder, Path.Combine(sf.Folder, value));
                CFont f = new CFont(value);
                sf.Normal = f;

                ok &= xmlReader.GetValue("//root/Fonts/Font" + i + "/FileItalic", out value, value);
                sf.FileItalic = value;
                value = Path.Combine(fontFolder, Path.Combine(sf.Folder, value));
                f = new CFont(value);
                sf.Italic = f;

                ok &= xmlReader.GetValue("//root/Fonts/Font" + i + "/FileBold", out value, value);
                sf.FileBold = value;
                value = Path.Combine(fontFolder, Path.Combine(sf.Folder, value));
                f = new CFont(value);
                sf.Bold = f;

                ok &= xmlReader.GetValue("//root/Fonts/Font" + i + "/FileBoldItalic", out value, value);
                sf.FileBoldItalic = value;
                value = Path.Combine(fontFolder, Path.Combine(sf.Folder, value));
                f = new CFont(value);
                sf.BoldItalic = f;

                sf.Outline = 0f;
                ok &= xmlReader.TryGetFloatValue("//root/Fonts/Font" + i + "/Outline", ref sf.Outline);

                sf.OutlineColor = new SColorF(0f, 0f, 0f, 1f);
                ok &= xmlReader.TryGetFloatValue("//root/Fonts/Font" + i + "/OutlineColorR", ref sf.OutlineColor.R);
                ok &= xmlReader.TryGetFloatValue("//root/Fonts/Font" + i + "/OutlineColorG", ref sf.OutlineColor.G);
                ok &= xmlReader.TryGetFloatValue("//root/Fonts/Font" + i + "/OutlineColorB", ref sf.OutlineColor.B);
                ok &= xmlReader.TryGetFloatValue("//root/Fonts/Font" + i + "/OutlineColorA", ref sf.OutlineColor.A);

                if (ok)
                    _Fonts.Add(sf);
                else
                {
                    string fontTypes;
                    if (partyModeId >= 0)
                        fontTypes = "theme fonts for party mode";
                    else if (themeName != "")
                        fontTypes = "theme fonts for theme \"" + themeName + "\"";
                    else
                        fontTypes = "basic fonts";
                    CLog.LogError("Error loading " + fontTypes + ": Error in Font" + i);
                }
                i++;
            }
        }

        /// <summary>
        ///     Load default fonts
        /// </summary>
        /// <returns></returns>
        private static bool _LoadFontList()
        {
            CXMLReader xmlReader = CXMLReader.OpenFile(Path.Combine(CSettings.FolderFonts, CSettings.FileFonts));
            if (xmlReader == null)
                return false;

            _Fonts.Clear();

            _LoadFontFiles(xmlReader, Path.Combine(Directory.GetCurrentDirectory(), CSettings.FolderFonts));
            return true;
        }

        /// <summary>
        ///     Loads theme fonts from skin file
        /// </summary>
        public static void LoadThemeFonts(string themeName, string fontFolder, CXMLReader xmlReader)
        {
            _LoadFontFiles(xmlReader, fontFolder, themeName);
            CLog.StartBenchmark(1, "BuildGlyphs");
            _BuildGlyphs();
            CLog.StopBenchmark(1, "BuildGlyphs");
        }

        /// <summary>
        ///     Loads party mode fonts from skin file
        /// </summary>
        public static void LoadPartyModeFonts(int partyModeID, string fontFolder, CXMLReader xmlReader)
        {
            _LoadFontFiles(xmlReader, fontFolder, "", partyModeID);
            CLog.StartBenchmark(1, "BuildGlyphs");
            _BuildGlyphs();
            CLog.StopBenchmark(1, "BuildGlyphs");
        }

        public static void SaveThemeFonts(string themeName, XmlWriter writer)
        {
            if (_Fonts.Count == 0)
                return;

            int index = 0;
            int fontNr = 1;
            bool setStart = false;
            while (index < _Fonts.Count)
            {
                if (_Fonts[index].IsThemeFont && _Fonts[index].ThemeName == themeName)
                {
                    if (!setStart)
                    {
                        writer.WriteStartElement("Fonts");
                        setStart = true;
                    }

                    writer.WriteStartElement("Font" + fontNr);

                    writer.WriteElementString("Name", _Fonts[index].Name);
                    writer.WriteElementString("Folder", _Fonts[index].Folder);

                    writer.WriteElementString("Outline", _Fonts[index].Outline.ToString("#0.00"));
                    writer.WriteElementString("OutlineColorR", _Fonts[index].OutlineColor.R.ToString("#0.00"));
                    writer.WriteElementString("OutlineColorG", _Fonts[index].OutlineColor.G.ToString("#0.00"));
                    writer.WriteElementString("OutlineColorB", _Fonts[index].OutlineColor.B.ToString("#0.00"));
                    writer.WriteElementString("OutlineColorA", _Fonts[index].OutlineColor.A.ToString("#0.00"));

                    writer.WriteElementString("FileNormal", _Fonts[index].FileNormal);
                    writer.WriteElementString("FileBold", _Fonts[index].FileBold);
                    writer.WriteElementString("FileItalic", _Fonts[index].FileItalic);
                    writer.WriteElementString("FileBoldItalic", _Fonts[index].FileBoldItalic);

                    writer.WriteEndElement();

                    fontNr++;
                }
                index++;
            }

            if (setStart)
                writer.WriteEndElement();
        }

        public static void UnloadThemeFonts(string themeName)
        {
            if (_Fonts.Count == 0)
                return;

            int index = 0;
            while (index < _Fonts.Count)
            {
                if (_Fonts[index].IsThemeFont && _Fonts[index].ThemeName == themeName)
                {
                    _Fonts[index].Normal.UnloadAllGlyphs();
                    _Fonts[index].Italic.UnloadAllGlyphs();
                    _Fonts[index].Bold.UnloadAllGlyphs();
                    _Fonts[index].BoldItalic.UnloadAllGlyphs();
                    _Fonts.RemoveAt(index);
                }
                else
                    index++;
            }
        }

        private static int _GetFontIndex(string themeName, string fontName)
        {
            if (themeName == "" || fontName == "")
                return -1;

            for (int i = 0; i < _Fonts.Count; i++)
            {
                if (_Fonts[i].IsThemeFont && _Fonts[i].Name == fontName && _Fonts[i].ThemeName == themeName)
                    return i;
            }

            return -1;
        }

        private static int _GetFontIndexParty(int partyModeID, string fontName)
        {
            if (partyModeID == -1 || fontName == "")
                return -1;

            for (int i = 0; i < _Fonts.Count; i++)
            {
                if (!_Fonts[i].IsThemeFont && _Fonts[i].Name == fontName && _Fonts[i].PartyModeID == partyModeID)
                    return i;
            }

            return -1;
        }

        private static void _BuildGlyphs()
        {
            const string text = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPGRSTUVWGXZ1234567890";

            for (int i = 0; i < _Fonts.Count; i++)
            {
                CurrentFont = i;

                foreach (char chr in text)
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