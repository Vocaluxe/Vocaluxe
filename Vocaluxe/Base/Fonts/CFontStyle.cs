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
using System.Drawing.Text;
using VocaluxeLib;
using VocaluxeLib.Draw;

namespace Vocaluxe.Base.Fonts
{
    class CFontStyle : IDisposable
    {
        private readonly string _FilePath;
        private readonly EStyle _Style;
        public readonly float Outline; //0..1, 0=not outline 1=100% outline
        public readonly SColorF OutlineColor;
        private readonly float _MaxGlyphHeight;
        private readonly Dictionary<char, CGlyph> _Glyphs = new Dictionary<char, CGlyph>();
        private PrivateFontCollection _Fonts;
        private FontFamily _Family;

        public CFontStyle(string file, EStyle style, float outline, SColorF outlineColor)
        {
            _FilePath = file;
            _Style = style;
            Outline = outline;
            OutlineColor = outlineColor;

            switch (CConfig.Config.Graphics.TextureQuality)
            {
                case ETextureQuality.TR_CONFIG_TEXTURE_LOWEST:
                    _MaxGlyphHeight = 20f;
                    break;
                case ETextureQuality.TR_CONFIG_TEXTURE_LOW:
                    _MaxGlyphHeight = 40f;
                    break;
                case ETextureQuality.TR_CONFIG_TEXTURE_MEDIUM:
                    _MaxGlyphHeight = 60f;
                    break;
                case ETextureQuality.TR_CONFIG_TEXTURE_HIGH:
                    _MaxGlyphHeight = 80f;
                    break;
                case ETextureQuality.TR_CONFIG_TEXTURE_HIGHEST:
                    _MaxGlyphHeight = 100f;
                    break;
                default:
                    _MaxGlyphHeight = 60f;
                    break;
            }
        }

        private FontStyle _GetSystemFontStyle()
        {
            switch (_Style)
            {
                case EStyle.Normal:
                    return FontStyle.Regular;
                case EStyle.Italic:
                    return FontStyle.Italic;
                case EStyle.Bold:
                    return FontStyle.Bold;
                case EStyle.BoldItalic:
                    return FontStyle.Bold | FontStyle.Italic;
            }
            throw new ArgumentException("Invalid style: " + _Style);
        }

        public Font GetSystemFont(float height)
        {
            if (_Fonts == null)
            {
                _Fonts = new PrivateFontCollection();
                try
                {
                    _Fonts.AddFontFile(_FilePath);
                    _Family = _Fonts.Families[0];
                }
                catch (Exception e)
                {
                    CLog.LogError("Error opening font file " + _FilePath + ": " + e.Message);
                }
            }
            return new Font(_Family, height, _GetSystemFontStyle(), GraphicsUnit.Pixel);
        }

        public void DrawGlyph(char chr, float fontHeight, float x, float y, float z, SColorF color, bool allMonitors = true)
        {
            CTextureRef texture;
            SRectF rect;
            GetOrAddGlyph(chr, fontHeight).GetTextureAndRect(fontHeight, x, y, z, out texture, out rect);
            CDraw.DrawTexture(texture, rect, color, false, allMonitors);
        }

        public void DrawGlyph(char chr, float fontHeight, float x, float y, float z, SColorF color, float begin, float end)
        {
            CTextureRef texture;
            SRectF rect;
            GetOrAddGlyph(chr, fontHeight).GetTextureAndRect(fontHeight, x, y, z, out texture, out rect);
            CDraw.DrawTexture(texture, rect, color, begin, end);
        }

        public void DrawGlyphReflection(char chr, float fontHeight, float x, float y, float z, SColorF color, float rspace, float rheight)
        {
            CTextureRef texture;
            SRectF rect;
            GetOrAddGlyph(chr, fontHeight).GetTextureAndRect(fontHeight, x, y, z, out texture, out rect);
            CDraw.DrawTextureReflection(texture, rect, color, rect, rspace, rheight);
        }

        public float GetWidth(char chr, float height)
        {
            return GetOrAddGlyph(chr, height).GetWidth(height);
        }

        public float GetHeight(char chr, float height)
        {
            return GetOrAddGlyph(chr, height).GetHeight(height);
        }

        public CGlyph GetOrAddGlyph(char chr, float height)
        {
            CGlyph glyph;
            if (!_Glyphs.TryGetValue(chr, out glyph))
            {
                float maxHeight = (height < 0 || _MaxGlyphHeight + 50 >= height) ? _MaxGlyphHeight : (float)Math.Round(height / 50) * 50;
                glyph = new CGlyph(chr, this, maxHeight);
                _Glyphs.Add(chr, glyph);
            }
            if (glyph.MaxHeight + 50 < height)
            {
                glyph.UnloadTexture();
                glyph = new CGlyph(chr, this, (float)Math.Round(height / 50) * 50);
                _Glyphs[chr] = glyph;
            }
            return glyph;
        }

        private void _UnloadGlyphs()
        {
            foreach (CGlyph glyph in _Glyphs.Values)
                glyph.UnloadTexture();
            _Glyphs.Clear();
        }

        private bool _Disposed;

        public void Dispose()
        {
            if (!_Disposed)
            {
                if (_Fonts != null)
                {
                    _Fonts.Dispose();
                    _Fonts = null;
                }
                _UnloadGlyphs();
                _Disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}