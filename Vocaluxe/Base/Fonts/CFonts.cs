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
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using VocaluxeLib;
using VocaluxeLib.Menu;
using VocaluxeLib.Xml;

namespace Vocaluxe.Base.Fonts
{
    static class CFonts
    {
        private static bool _IsInitialized;
        private static readonly List<SFontFamily> _FontFamilies = new List<SFontFamily>();
        private static readonly List<String> _LoggedMissingFonts = new List<string>();

        public static int PartyModeID { get; set; }

        public static bool Init()
        {
            if (_IsInitialized)
                return false;
            PartyModeID = -1;
            return _LoadDefaultFonts();
        }

        public static void Close()
        {
            foreach (SFontFamily font in _FontFamilies)
                font.Dispose();
            _FontFamilies.Clear();
            _IsInitialized = false;
        }

        #region DrawText
        /// <summary>
        ///     Draws a black text
        /// </summary>
        /// <param name="text">The text to be drawn</param>
        /// <param name="font">The texts font</param>
        /// <param name="x">The texts x-position</param>
        /// <param name="y">The texts y-position</param>
        /// <param name="z">The texts z-position</param>
        public static void DrawText(string text, CFont font, float x, float y, float z)
        {
            DrawText(text, font, x, y, z, new SColorF(0f, 0f, 0f, 1f));
        }

        /// <summary>
        ///     Draws a text
        /// </summary>
        /// <param name="text">The text to be drawn</param>
        /// <param name="font">The texts font</param>
        /// <param name="x">The texts x-position</param>
        /// <param name="y">The texts y-position</param>
        /// <param name="z">The texts z-position</param>
        /// <param name="color">The text color</param>
        public static void DrawText(string text, CFont font, float x, float y, float z, SColorF color, bool allMonitors = true)
        {
            if (font.Height <= 0f || text == "")
                return;

            CFontStyle fontStyle = _GetFontStyle(font);

            float dx = x;
            foreach (char chr in text)
            {
                fontStyle.DrawGlyph(chr, font.Height, dx, y, z, color, allMonitors);
                dx += fontStyle.GetWidth(chr, font.Height);
            }
        }

        public static void DrawTextReflection(string text, CFont font, float x, float y, float z, SColorF color, float rspace, float rheight)
        {
            if (font.Height <= 0f || text == "")
                return;

            CFontStyle fontStyle = _GetFontStyle(font);

            float dx = x;
            foreach (char chr in text)
            {
                fontStyle.DrawGlyphReflection(chr, font.Height, dx, y, z, color, rspace, rheight);
                dx += fontStyle.GetWidth(chr, font.Height);
            }
        }

        public static void DrawText(string text, CFont font, float x, float y, float z, SColorF color, float begin, float end)
        {
            if (font.Height <= 0f || text == "")
                return;

            float w = GetTextWidth(text, font);
            if (w <= 0f)
                return;

            float xStart = x + w * begin;
            float xEnd = x + w * end;
            float xCur = x;

            CFontStyle fontStyle = _GetFontStyle(font);

            foreach (char chr in text)
            {
                float w2 = fontStyle.GetWidth(chr, font.Height);
                float b = (xStart - xCur) / w2;

                if (b < 1f)
                {
                    if (b < 0f)
                        b = 0f;
                    float e = (xEnd - xCur) / w2;
                    if (e > 0f)
                    {
                        if (e > 1f)
                            e = 1f;
                        fontStyle.DrawGlyph(chr, font.Height, xCur, y, z, color, b, e);
                    }
                }
                xCur += w2;
            }
        }
        #endregion DrawText

        /// <summary>
        ///     Calculates the bounds for a CText object
        /// </summary>
        /// <param name="text">The CText object of which the bounds should be calculated for</param>
        /// <returns>RectangleF object containing the bounds</returns>
        public static RectangleF GetTextBounds(CText text)
        {
            return new RectangleF(text.X, text.Y, GetTextWidth(text.TranslatedText, text.CalculatedFont), GetTextHeight(text.TranslatedText, text.CalculatedFont));
        }

        public static Font GetSystemFont(CFont font)
        {
            return _GetFontStyle(font).GetSystemFont(font.Height);
        }

        public static float GetOutlineSize(CFont font)
        {
            return _FontFamilies[_GetFontIndex(font.Name)].Outline;
        }

        public static SColorF GetOutlineColor(CFont font)
        {
            return _FontFamilies[_GetFontIndex(font.Name)].OutlineColor;
        }

        private static CFontStyle _GetFontStyle(CFont font)
        {
            int index = _GetFontIndex(font.Name);

            switch (font.Style)
            {
                case EStyle.Normal:
                    return _FontFamilies[index].Normal;
                case EStyle.Italic:
                    return _FontFamilies[index].Italic;
                case EStyle.Bold:
                    return _FontFamilies[index].Bold;
                case EStyle.BoldItalic:
                    return _FontFamilies[index].BoldItalic;
            }
            throw new ArgumentException("Invalid Style: " + font.Style);
        }

        public static float GetTextWidth(string text, CFont font)
        {
            CFontStyle fontStyle = _GetFontStyle(font);
            return text.Sum(chr => fontStyle.GetWidth(chr, font.Height));
        }

        public static float GetTextHeight(string text, CFont font)
        {
            CFontStyle fontStyle = _GetFontStyle(font);
            return text == "" ? 0 : text.Select(chr => fontStyle.GetHeight(chr, font.Height)).Max();
        }

        private static int _GetFontIndex(string fontName)
        {
            if (fontName != "")
            {
                int index = _GetPartyFontIndex(fontName, PartyModeID);
                if (index < 0)
                    index = _GetThemeFontIndex(fontName, CConfig.Config.Theme.Theme);
                if (index >= 0)
                    return index;
                for (int i = 0; i < _FontFamilies.Count; i++)
                {
                    if (_FontFamilies[i].Name == fontName)
                        return i;
                }
                if (!_LoggedMissingFonts.Contains(fontName))
                {
                    _LoggedMissingFonts.Add(fontName);
                    CLog.LogError("Font \"" + fontName + "\" not found!");
                }
            }
            else
                CLog.LogError("Empty fontName requested", false, false, new Exception());

            if (_FontFamilies.Count == 0)
                CLog.LogError("No fonts found!", true, true);

            return 0;
        }

        private static int _GetThemeFontIndex(string fontName, string themeName)
        {
            Debug.Assert(fontName != "" && themeName != "");

            for (int i = 0; i < _FontFamilies.Count; i++)
            {
                if (_FontFamilies[i].Name == fontName && _FontFamilies[i].ThemeName == themeName)
                    return i;
            }

            return -1;
        }

        private static int _GetPartyFontIndex(string fontName, int partyModeID)
        {
            Debug.Assert(fontName != "");

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
            SDefaultFonts defaultFonts;
            try
            {
                var xml = new CXmlDeserializer();
                defaultFonts = xml.Deserialize<SDefaultFonts>(Path.Combine(CSettings.ProgramFolder, CSettings.FolderNameFonts, CSettings.FileNameFonts));
            }
            catch (Exception e)
            {
                CLog.LogError("Error loading default Fonts", false, false, e);
                return false;
            }

            return LoadThemeFonts(defaultFonts.Fonts, Path.Combine(CSettings.ProgramFolder, CSettings.FolderNameFonts), "", -1);
        }

        /// <summary>
        ///     Loads theme fonts
        /// </summary>
        public static bool LoadThemeFonts(IEnumerable<SFontFamily> fontFamilies, string fontFolder, string themeName, int partyModeId)
        {
            if (fontFamilies == null)
                return true;
            foreach (SFontFamily fontFamily in fontFamilies)
            {
                if (!_LoadFont(fontFamily, fontFolder, themeName, partyModeId))
                {
                    CLog.LogError("Error loading fonts for Theme " + themeName + "(" + partyModeId + ")");
                    return false;
                }
            }
            CLog.StartBenchmark("BuildGlyphs");
            _BuildGlyphs();
            CLog.StopBenchmark("BuildGlyphs");
            return true;
        }

        private static bool _LoadFont(SFontFamily fontFamily, string fontFolder, string themeName, int partyModeId)
        {
            fontFamily.ThemeName = themeName;
            fontFamily.PartyModeID = partyModeId;
            fontFamily.Normal = new CFontStyle(Path.Combine(fontFolder, fontFamily.Folder, fontFamily.FileNormal), EStyle.Normal, fontFamily.Outline, fontFamily.OutlineColor);
            fontFamily.Italic = new CFontStyle(Path.Combine(fontFolder, fontFamily.Folder, fontFamily.FileItalic), EStyle.Italic, fontFamily.Outline, fontFamily.OutlineColor);
            fontFamily.Bold = new CFontStyle(Path.Combine(fontFolder, fontFamily.Folder, fontFamily.FileBold), EStyle.Bold, fontFamily.Outline, fontFamily.OutlineColor);
            fontFamily.BoldItalic = new CFontStyle(Path.Combine(fontFolder, fontFamily.Folder, fontFamily.FileBoldItalic), EStyle.BoldItalic, fontFamily.Outline,
                                                   fontFamily.OutlineColor);
            _FontFamilies.Add(fontFamily);
            return true;
        }

        public static void UnloadThemeFonts(string themeName)
        {
            int index = 0;
            while (index < _FontFamilies.Count)
            {
                if (_FontFamilies[index].ThemeName == themeName)
                {
                    _FontFamilies[index].Dispose();
                    _FontFamilies.RemoveAt(index);
                }
                else
                    index++;
            }
        }

        public static void UnloadPartyModeFonts(int partyModeID)
        {
            Debug.Assert(partyModeID >= 0);
            int index = 0;
            while (index < _FontFamilies.Count)
            {
                if (_FontFamilies[index].PartyModeID == partyModeID)
                {
                    _FontFamilies[index].Dispose();
                    _FontFamilies.RemoveAt(index);
                }
                else
                    index++;
            }
        }

        private static void _BuildGlyphs()
        {
            const string text = " abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";

            foreach (SFontFamily fontFamily in _FontFamilies)
            {
                foreach (char chr in text)
                {
                    fontFamily.Normal.GetOrAddGlyph(chr, -1);
                    fontFamily.Bold.GetOrAddGlyph(chr, -1);
                    fontFamily.Italic.GetOrAddGlyph(chr, -1);
                    fontFamily.BoldItalic.GetOrAddGlyph(chr, -1);
                }
            }
        }
    }
}