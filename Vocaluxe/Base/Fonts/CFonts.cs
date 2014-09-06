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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml;
using VocaluxeLib;
using VocaluxeLib.Menu;

namespace Vocaluxe.Base.Fonts
{
    struct SFont
    {
        public string Name;
        public EStyle Style;
        public float Height;
    }

    /// <summary>
    ///     Struct used for describing a font family (a type of text with 4 different styles)
    /// </summary>
    struct SFontFamily
    {
        public string Name;

        public int PartyModeID;
        public string ThemeName;
        public string Folder;

        public string FileNormal;
        public string FileItalic;
        public string FileBold;
        public string FileBoldItalic;

        public float Outline; //0..1, 0=not outline 1=100% outline
        public SColorF OutlineColor;

        public CFontStyle Normal;
        public CFontStyle Italic;
        public CFontStyle Bold;
        public CFontStyle BoldItalic;
    }

    static class CFonts
    {
        private static bool _IsInitialized;
        private static readonly List<SFontFamily> _FontFamilies = new List<SFontFamily>();
        private static int _CurrentFont;
        private static float _Height = 1f;

        public static int PartyModeID { get; set; }

        public static EStyle Style = EStyle.Normal;

        public static float Height
        {
            get { return _Height; }
            set { _Height = value < 0f ? 0f : value; }
        }

        public static bool Init()
        {
            if (_IsInitialized)
                return false;
            _CurrentFont = 0;
            PartyModeID = -1;
            return _LoadDefaultFonts();
        }

        public static void Close()
        {
            foreach (SFontFamily font in _FontFamilies)
            {
                font.Normal.Dispose();
                font.Bold.Dispose();
                font.Italic.Dispose();
                font.BoldItalic.Dispose();
            }
            _FontFamilies.Clear();
            _IsInitialized = false;
        }

        private static CFontStyle _GetCurrentFont()
        {
            switch (Style)
            {
                case EStyle.Normal:
                    return _FontFamilies[_CurrentFont].Normal;
                case EStyle.Italic:
                    return _FontFamilies[_CurrentFont].Italic;
                case EStyle.Bold:
                    return _FontFamilies[_CurrentFont].Bold;
                case EStyle.BoldItalic:
                    return _FontFamilies[_CurrentFont].BoldItalic;
            }
            throw new ArgumentException("Invalid Style: " + Style);
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

        /// <summary>
        ///     Draws a text string
        /// </summary>
        /// <param name="text">The text to be drawn</param>
        /// <param name="x">The text's x-position</param>
        /// <param name="y">The text's y-position</param>
        /// <param name="h">The text's height</param>
        /// <param name="z">The text's z-position</param>
        /// <param name="color">The text color</param>
        public static void DrawText(string text, float h, float x, float y, float z, SColorF color)
        {
            if (h <= 0f)
                return;

            if (text == "")
                return;

            Height = h;
            CFontStyle font = _GetCurrentFont();

            float dx = x;
            foreach (char chr in text)
            {
                font.DrawGlyph(chr, h, dx, y, z, color);
                dx += font.GetWidth(chr, h);
            }
        }

        public static void DrawTextReflection(string text, float h, float x, float y, float z, SColorF color, float rspace, float rheight)
        {
            if (h <= 0f)
                return;

            if (text == "")
                return;

            Height = h;
            CFontStyle font = _GetCurrentFont();

            float dx = x;
            foreach (char chr in text)
            {
                font.DrawGlyphReflection(chr, h, dx, y, z, color, rspace, rheight);
                dx += font.GetWidth(chr, h);
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

            CFontStyle font = _GetCurrentFont();

            foreach (char chr in text)
            {
                float w2 = font.GetWidth(chr, h);
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
                        font.DrawGlyph(chr, h, dx, y, z, color, b, e);
                    }
                }
                dx += w2;
            }
        }
        #endregion DrawText

        public static void SetFont(string fontName)
        {
            int index = _GetPartyFontIndex(fontName, PartyModeID);
            if (index < 0)
                index = _GetThemeFontIndex(fontName, CConfig.Theme);
            if (index < 0)
                index = _GetFontIndex(fontName);
            if (index >= 0)
                _CurrentFont = index;
        }

        /// <summary>
        ///     Calculates the bounds for a CText object
        /// </summary>
        /// <param name="text">The CText object of which the bounds should be calculated for</param>
        /// <returns>RectangleF object containing the bounds</returns>
        public static RectangleF GetTextBounds(CText text)
        {
            return GetTextBounds(text, text.Height);
        }

        /// <summary>
        ///     Calculates the bounds for a CText object
        /// </summary>
        /// <param name="text">The CText object of which the bounds should be calculated for</param>
        /// <param name="height">The height of the CText object</param>
        /// <returns>RectangleF object containing the bounds</returns>
        public static RectangleF GetTextBounds(CText text, float height)
        {
            float oldHeight = Height;
            int oldFont = _CurrentFont;
            EStyle oldStyle = Style;
            Height = height;
            SetFont(text.Font);
            Style = text.Style;
            var result = new RectangleF(text.X, text.Y, GetTextWidth(CLanguage.Translate(text.Text, text.TranslationID)),
                                        GetTextHeight(CLanguage.Translate(text.Text, text.TranslationID)));
            //restore old values
            Height = oldHeight;
            _CurrentFont = oldFont;
            Style = oldStyle;
            return result;
        }

        public static float GetTextWidth(string text)
        {
            CFontStyle font = _GetCurrentFont();
            return text.Sum(chr => font.GetWidth(chr, Height));
        }

        public static float GetTextHeight(string text)
        {
            CFontStyle font = _GetCurrentFont();
            return text == "" ? 0 : text.Select(chr => font.GetHeight(chr, Height)).Max();
        }

        private static int _GetFontIndex(string fontName)
        {
            for (int i = 0; i < _FontFamilies.Count; i++)
            {
                if (_FontFamilies[i].Name == fontName)
                    return i;
            }

            return -1;
        }

        private static int _GetThemeFontIndex(string fontName, string themeName)
        {
            if (themeName == "" || fontName == "")
                return -1;

            for (int i = 0; i < _FontFamilies.Count; i++)
            {
                if (_FontFamilies[i].Name == fontName && _FontFamilies[i].ThemeName == themeName)
                    return i;
            }

            return -1;
        }

        private static int _GetPartyFontIndex(string fontName, int partyModeID)
        {
            if (partyModeID == -1 || fontName == "")
                return -1;

            for (int i = 0; i < _FontFamilies.Count; i++)
            {
                if (_FontFamilies[i].PartyModeID == partyModeID && _FontFamilies[i].Name == fontName)
                    return i;
            }

            return -1;
        }

        /// <summary>
        ///     Load default fonts
        /// </summary>
        /// <returns></returns>
        private static bool _LoadDefaultFonts()
        {
            CXMLReader xmlReader = CXMLReader.OpenFile(Path.Combine(CSettings.ProgramFolder, CSettings.FolderNameFonts, CSettings.FileNameFonts));
            if (xmlReader == null)
                return false;

            return _LoadFontFile(xmlReader, Path.Combine(CSettings.ProgramFolder, CSettings.FolderNameFonts));
        }

        /// <summary>
        ///     Loads theme fonts from skin file
        /// </summary>
        public static bool LoadThemeFonts(string themeName, string fontFolder, CXMLReader xmlReader)
        {
            bool ok = _LoadFontFile(xmlReader, fontFolder, themeName);
            CLog.StartBenchmark("BuildGlyphs");
            _BuildGlyphs();
            CLog.StopBenchmark("BuildGlyphs");
            return ok;
        }

        /// <summary>
        ///     Loads party mode fonts from skin file
        /// </summary>
        public static bool LoadPartyModeFonts(int partyModeID, string fontFolder, CXMLReader xmlReader)
        {
            bool ok = _LoadFontFile(xmlReader, fontFolder, "", partyModeID);
            CLog.StartBenchmark("BuildGlyphs");
            _BuildGlyphs();
            CLog.StopBenchmark("BuildGlyphs");
            return ok;
        }

        private static bool _LoadFontFile(CXMLReader xmlReader, string fontFolder, string themeName = "", int partyModeId = -1)
        {
            string value;
            int i = 1;
            while (xmlReader.GetValue("//root/Fonts/Font" + i + "/Folder", out value))
            {
                var sf = new SFontFamily {Folder = value, ThemeName = themeName, PartyModeID = partyModeId};

                bool ok = true;

                ok &= xmlReader.GetValue("//root/Fonts/Font" + i + "/Name", out sf.Name);
                ok &= xmlReader.GetValue("//root/Fonts/Font" + i + "/FileNormal", out sf.FileNormal);
                ok &= xmlReader.GetValue("//root/Fonts/Font" + i + "/FileItalic", out sf.FileItalic);
                ok &= xmlReader.GetValue("//root/Fonts/Font" + i + "/FileBold", out sf.FileBold);
                ok &= xmlReader.GetValue("//root/Fonts/Font" + i + "/FileBoldItalic", out sf.FileBoldItalic);
                ok &= xmlReader.TryGetNormalizedFloatValue("//root/Fonts/Font" + i + "/Outline", ref sf.Outline);
                ok &= xmlReader.TryGetColorFromRGBA("//root/Fonts/Font" + i + "/OutlineColor", ref sf.OutlineColor);

                if (ok)
                {
                    sf.Normal = new CFontStyle(Path.Combine(fontFolder, sf.Folder, sf.FileNormal), EStyle.Normal, sf.Outline, sf.OutlineColor);
                    sf.Italic = new CFontStyle(Path.Combine(fontFolder, sf.Folder, sf.FileItalic), EStyle.Italic, sf.Outline, sf.OutlineColor);
                    sf.Bold = new CFontStyle(Path.Combine(fontFolder, sf.Folder, sf.FileBold), EStyle.Bold, sf.Outline, sf.OutlineColor);
                    sf.BoldItalic = new CFontStyle(Path.Combine(fontFolder, sf.Folder, sf.FileBoldItalic), EStyle.BoldItalic, sf.Outline, sf.OutlineColor);
                    _FontFamilies.Add(sf);
                }
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
                    return false;
                }
                i++;
            }
            return true;
        }

        public static void SaveThemeFonts(string themeName, XmlWriter writer)
        {
            if (_FontFamilies.Count == 0)
                return;

            writer.WriteStartElement("Fonts");
            int fontNr = 1;
            foreach (SFontFamily font in _FontFamilies)
            {
                if (font.ThemeName == themeName)
                {
                    writer.WriteStartElement("Font" + fontNr);

                    writer.WriteElementString("Name", font.Name);
                    writer.WriteElementString("Folder", font.Folder);

                    writer.WriteElementString("Outline", font.Outline.ToString("#0.00"));
                    writer.WriteStartElement("OutlineColor");
                    writer.WriteElementString("R", font.OutlineColor.R.ToString("#0.00"));
                    writer.WriteElementString("G", font.OutlineColor.G.ToString("#0.00"));
                    writer.WriteElementString("B", font.OutlineColor.B.ToString("#0.00"));
                    writer.WriteElementString("A", font.OutlineColor.A.ToString("#0.00"));
                    writer.WriteEndElement();

                    writer.WriteElementString("FileNormal", font.FileNormal);
                    writer.WriteElementString("FileBold", font.FileBold);
                    writer.WriteElementString("FileItalic", font.FileItalic);
                    writer.WriteElementString("FileBoldItalic", font.FileBoldItalic);

                    writer.WriteEndElement();

                    fontNr++;
                }
            }

            writer.WriteEndElement();
        }

        public static void UnloadThemeFonts(string themeName)
        {
            int index = 0;
            while (index < _FontFamilies.Count)
            {
                if (_FontFamilies[index].ThemeName == themeName)
                {
                    _FontFamilies[index].Normal.Dispose();
                    _FontFamilies[index].Italic.Dispose();
                    _FontFamilies[index].Bold.Dispose();
                    _FontFamilies[index].BoldItalic.Dispose();
                    _FontFamilies.RemoveAt(index);
                }
                else
                    index++;
            }
        }

        private static void _BuildGlyphs()
        {
            const string text = " abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";

            for (int i = 0; i < _FontFamilies.Count; i++)
            {
                _CurrentFont = i;

                foreach (char chr in text)
                {
                    Style = EStyle.Normal;
                    _FontFamilies[_CurrentFont].Normal.GetOrAddGlyph(chr, Height);
                    Style = EStyle.Bold;
                    _FontFamilies[_CurrentFont].Bold.GetOrAddGlyph(chr, Height);
                    Style = EStyle.Italic;
                    _FontFamilies[_CurrentFont].Italic.GetOrAddGlyph(chr, Height);
                    Style = EStyle.BoldItalic;
                    _FontFamilies[_CurrentFont].BoldItalic.GetOrAddGlyph(chr, Height);
                }
            }
            Style = EStyle.Normal;
            SetFont("Normal");
        }
    }
}