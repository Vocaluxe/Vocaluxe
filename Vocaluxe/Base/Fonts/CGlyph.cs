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
using System.Drawing; // System.Drawing.Primitives only (SizeF/RectangleF/Rectangle) - cross-platform, no GDI+
using SkiaSharp;
using VocaluxeLib;
using VocaluxeLib.Draw;

namespace Vocaluxe.Base.Fonts
{
    /// <summary>
    ///     Rasterizes a single glyph to an OpenGL texture.
    ///     Ported from GDI+ (System.Drawing Font/Graphics/GraphicsPath) to SkiaSharp so it works
    ///     cross-platform. Layout metrics (SizeF/RectangleF) come from System.Drawing.Primitives,
    ///     which is platform-independent and does not touch GDI+.
    /// </summary>
    class CGlyph
    {
        private CTextureRef _Texture;
        private SizeF _BoundingBox;
        private RectangleF _DrawBounding;
        public readonly float MaxHeight;

        public CGlyph(char chr, CFontStyle fontStyle, float maxHeight)
        {
            MaxHeight = maxHeight;
            float outlineSize = fontStyle.Outline * maxHeight;
            string chrString = chr.ToString();

            SKTypeface typeface = fontStyle.GetTypeface();
            using (var font = new SKFont(typeface, maxHeight))
            {
                font.Edging = SKFontEdging.Antialias;
                font.Subpixel = true;
                font.Embolden = fontStyle.IsBold;
                if (fontStyle.IsItalic)
                    font.SkewX = -0.25f;

                SKFontMetrics metrics = font.Metrics;
                float fullHeight = metrics.Descent - metrics.Ascent;
                SKRect tightBounds;
                float advance = font.MeasureText(chrString, out tightBounds);

                if (chr == ' ')
                {
                    _BoundingBox = new SizeF(advance, fullHeight + outlineSize);
                    _Texture = CDraw.AddTexture(1, 1, new byte[4]);
                    _DrawBounding = new RectangleF(0, 0, 0, 0);
                    return;
                }

                // The bounding box height must be the full line height (ascent→descent), identical for
                // EVERY glyph. The draw scale factor is fontHeight / _BoundingBox.Height, so it has to be
                // the same for all glyphs - otherwise each glyph is scaled differently and sits at its own
                // height. Using the per-glyph tight ink height here made short glyphs (e.g. '-') get a huge
                // factor (giant hyphen) and every letter end up at a different size/baseline.
                _BoundingBox = new SizeF(advance + outlineSize / 2, fullHeight + outlineSize);
                float fullWidth = advance + outlineSize;

                // Render generously sized; the real ink area is cropped afterwards.
                int bmpW = Math.Max(1, (int)Math.Ceiling(fullWidth) + 4);
                int bmpH = Math.Max(1, (int)Math.Ceiling(fullHeight + outlineSize) + 4);

                byte[] bgra;
                using (var bmp = new SKBitmap(bmpW, bmpH, SKColorType.Bgra8888, SKAlphaType.Premul))
                {
                    using (var canvas = new SKCanvas(bmp))
                    {
                        canvas.Clear(SKColors.Transparent);

                        float baselineX = outlineSize / 2;
                        float baselineY = -metrics.Ascent + outlineSize / 2;

                        if (outlineSize > 0)
                        {
                            using (var outlinePaint = new SKPaint
                            {
                                IsAntialias = true,
                                Style = SKPaintStyle.Stroke,
                                StrokeWidth = outlineSize,
                                StrokeJoin = SKStrokeJoin.Round,
                                Color = _ToSkColor(fontStyle.OutlineColor)
                            })
                                canvas.DrawText(chrString, baselineX, baselineY, font, outlinePaint);
                        }

                        using (var fillPaint = new SKPaint {IsAntialias = true, Style = SKPaintStyle.Fill, Color = SKColors.White})
                            canvas.DrawText(chrString, baselineX, baselineY, font, fillPaint);
                    }

                    _DrawBounding = _GetRealBounds(bmp);
                    bgra = _CropBgra(bmp, _DrawBounding);
                }

                float dx = (fullWidth - _BoundingBox.Width - 1) / 2;
                _DrawBounding.X -= dx;
                _Texture = CDraw.AddTexture((int)_DrawBounding.Width, (int)_DrawBounding.Height, bgra);
            }
        }

        public void UnloadTexture()
        {
            CDraw.RemoveTexture(ref _Texture);
        }

        private float _GetFactor(float fontHeight)
        {
            return fontHeight / _BoundingBox.Height;
        }

        public float GetWidth(float fontHeight)
        {
            return _BoundingBox.Width * _GetFactor(fontHeight);
        }

        public float GetHeight(float fontHeight)
        {
            return _BoundingBox.Height * _GetFactor(fontHeight);
        }

        public void GetTextureAndRect(float fontHeight, float x, float y, float z, out CTextureRef texture, out SRectF rect)
        {
            texture = _Texture;
            float factor = _GetFactor(fontHeight);
            x += _DrawBounding.X * factor;
            y += _DrawBounding.Y * factor;
            float h = _DrawBounding.Height * factor;
            float w = _DrawBounding.Width * factor;
            rect = new SRectF(x, y, w, h, z);
        }

        private static SKColor _ToSkColor(SColorF c)
        {
            return new SKColor((byte)(c.R * 255), (byte)(c.G * 255), (byte)(c.B * 255), (byte)(c.A * 255));
        }

        /// <summary>
        ///     Finds the bounding rectangle of the non-transparent (inked) pixels of the bitmap.
        ///     Mirrors the former GDI+ LockBits-based scan, working on the SKBitmap's BGRA bytes.
        /// </summary>
        private static Rectangle _GetRealBounds(SKBitmap bmp)
        {
            int w = bmp.Width;
            int h = bmp.Height;
            byte[] data = bmp.Bytes;

            // Scan for the full inked bounding box (all four edges). The previous version returned a
            // height of (h - minY), i.e. down to the bottom of the bitmap instead of the bottom of the
            // ink, which left a variable amount of empty space below each glyph.
            int minX = w, minY = h, maxX = -1, maxY = -1;
            for (int y = 0; y < h; y++)
            {
                int rowBase = y * w;
                for (int x = 0; x < w; x++)
                {
                    if (data[(rowBase + x) * 4 + 3] != 0)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            if (maxX < minX || maxY < minY) // nothing inked
                return new Rectangle(0, 0, 1, 1);

            // Add some additional space. Textures need some extra pixels for resizing.
            const int d = 2;
            minX = Math.Max(0, minX - d);
            minY = Math.Max(0, minY - d);
            maxX = Math.Min(w - 1, maxX + d);
            maxY = Math.Min(h - 1, maxY + d);

            return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        /// <summary>
        ///     Extracts the given rectangle of the bitmap as a tightly packed BGRA byte array.
        /// </summary>
        private static byte[] _CropBgra(SKBitmap bmp, RectangleF bounds)
        {
            int w = bmp.Width;
            byte[] src = bmp.Bytes;

            int cx = Math.Max(0, (int)bounds.X);
            int cy = Math.Max(0, (int)bounds.Y);
            int cw = Math.Max(1, (int)bounds.Width);
            int ch = Math.Max(1, (int)bounds.Height);
            if (cx + cw > bmp.Width)
                cw = bmp.Width - cx;
            if (cy + ch > bmp.Height)
                ch = bmp.Height - cy;

            var dst = new byte[cw * ch * 4];
            for (int row = 0; row < ch; row++)
            {
                int srcOffset = ((cy + row) * w + cx) * 4;
                int dstOffset = row * cw * 4;
                Buffer.BlockCopy(src, srcOffset, dst, dstOffset, cw * 4);
            }
            return dst;
        }
    }
}
