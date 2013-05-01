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
using System.Drawing.Text;
using VocaluxeLib.Menu;

namespace Vocaluxe.Base.Font
{
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

        public System.Drawing.Font GetFont()
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

            return new System.Drawing.Font(_Family, CFonts.Height, CFonts.GetFontStyle(), GraphicsUnit.Pixel);
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
}