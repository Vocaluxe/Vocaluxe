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

//Uncomment to make the font write the glyphs to file
//#define FONT_DEBUG_OUTPUT

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using SkiaSharp;
using VocaluxeLib;
using VocaluxeLib.Draw;

namespace Vocaluxe.Base.Fonts
{
    class CGlyph
    {
        private CTextureRef _Texture;
        private readonly SizeF _BoundingBox;
        private readonly RectangleF _DrawBounding;
        public readonly float MaxHeight;

        public CGlyph(char chr, CFontStyle fontStyle, float maxHeight)
        {
            MaxHeight = maxHeight;
            var outlineSize = fontStyle.Outline * maxHeight;
            var chrString = chr.ToString();

            var typeface = fontStyle.GetTypeface();
            SizeF fullSize;
            Size bmpSize;
            float ascent;
            using (var fo = new SKFont(typeface, maxHeight))
            {
                ascent = -fo.Metrics.Ascent;
                var lineSpacing = fo.Spacing;
                fullSize = new SizeF(fo.MeasureText(chrString) + maxHeight / 3, lineSpacing);
                if (chr != ' ')
                {
                    _BoundingBox = new SizeF(fo.MeasureText(chrString), lineSpacing);

                    // ReSharper disable CompareOfFloatsByEqualityOperator
                    if (_BoundingBox.Height == 0)
                        // ReSharper restore CompareOfFloatsByEqualityOperator
                    {
                        _BoundingBox.Height = fullSize.Height;
                    }

                    _BoundingBox.Width += outlineSize / 2;
                    _BoundingBox.Height += outlineSize;
                    fullSize.Width += outlineSize;
                    bmpSize = new Size((int)fullSize.Width, (int)Math.Round(_BoundingBox.Height));
                }
                else
                {
                    fullSize.Width = maxHeight / 3;
                    _BoundingBox = fullSize;
                    _BoundingBox.Height += outlineSize;
                    bmpSize = new Size(1, 1);
                }
            }

            using (var bmp = new SKBitmap(bmpSize.Width, bmpSize.Height, SKColorType.Bgra8888, SKAlphaType.Premul))
            using (var g = new SKCanvas(bmp))
            {
                g.Clear(SKColors.Transparent);

                if (chr == ' ')
                {
                    _Texture = CDraw.AddTexture(bmp);
                    _DrawBounding = new RectangleF(0, 0, 0, 0);
                }
                else
                {
                    //Have to use size in em not pixels!
                    using (var drawFont = new SKFont(typeface, ascent) { Edging = SKFontEdging.Antialias, Subpixel = true })
                    {
                        var point = new SKPoint(outlineSize / 2 + ascent / 6, outlineSize / 4 - drawFont.Metrics.Ascent);

                        using (var path = drawFont.GetTextPath(chrString, point))
                        using (var pen = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = outlineSize, StrokeJoin = SKStrokeJoin.Round, IsAntialias = true, Color = _ToSKColor(fontStyle.OutlineColor) })
                        using (var brush = new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true, Color = SKColors.White })
                        {
                            g.DrawPath(path, pen);
                            g.DrawPath(path, brush);
                        }
                    }

                    g.Flush();
                    _DrawBounding = _GetRealBounds(bmp);
                    using (var bmpCropped = new SKBitmap())
                    {
                        var bounds = Rectangle.Round(_DrawBounding);
                        bmp.ExtractSubset(bmpCropped, new SKRectI(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom));
                        var dx = (fullSize.Width - _BoundingBox.Width - 1) / 2;
                        _DrawBounding.X -= dx;
                        _Texture = CDraw.AddTexture(bmpCropped);
                        /*_DrawBounding.X *= _Texture.Width / _DrawBounding.Width;
                        _DrawBounding.Y *= _Texture.Width / _DrawBounding.Width;
                        _DrawBounding.Width = _Texture.Width;
                        _DrawBounding.Height = _Texture.Height;*/
#if FONT_DEBUG_OUTPUT
                        if (Char.IsLetterOrDigit(chr))
                        {
                            if (outline > 0)
                                bmpCropped.Save("font_" + chr + "o" + CFonts.Style + "2.png", ImageFormat.Png);
                            else
                                bmpCropped.Save("font_" + chr + CFonts.Style + "2.png", ImageFormat.Png);
                        }
#endif
                    }
                }
            }
        }

        private static SKColor _ToSKColor(SColorF color)
        {
            var c = color.AsColor();
            return new SKColor(c.R, c.G, c.B, c.A);
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
            var factor = _GetFactor(fontHeight);
            x += _DrawBounding.X * factor;
            y += _DrawBounding.Y * factor;
            var h = _DrawBounding.Height * factor;
            var w = _DrawBounding.Width * factor;
            rect = new SRectF(x, y, w, h, z);
        }

        private static Rectangle _GetRealBounds(SKBitmap bmp)
        {
            int minX = 0, maxX = bmp.Width - 1, minY = 0;
            var values = bmp.Width * bmp.Height;
            var rgbValues = new Int32[values];
            Marshal.Copy(bmp.GetPixels(), rgbValues, 0, values);
            var index = 0;
            var found = false;
            //find from top
            for (var y = 0; y < bmp.Height && !found; y++)
            {
                for (var x = 0; x < bmp.Width; x++)
                {
                    if (rgbValues[index] != 0)
                    {
                        minX = x;
                        maxX = x;
                        minY = y;
                        found = true;
                        break;
                    }

                    index++;
                }
            }

            found = false;
            //find left
            for (var x = 0; x < minX && !found; x++)
            {
                index = x + minY * bmp.Width;
                for (var y = minY; y < bmp.Height; y++)
                {
                    if (rgbValues[index] != 0)
                    {
                        found = true;
                        minX = x;
                        break;
                    }

                    index += bmp.Width;
                }
            }

            found = false;
            //find right
            for (var x = bmp.Width - 1; x > maxX && !found; x--)
            {
                index = x + minY * bmp.Width;
                for (var y = minY; y < bmp.Height; y++)
                {
                    if (rgbValues[index] != 0)
                    {
                        found = true;
                        maxX = x;
                        break;
                    }

                    index += bmp.Width;
                }
            }

            //Add some additional space. Textures need some extra pixel for resizing.
            const int d = 4;
            minX = minX - d;
            if (minX < 0)
            {
                minX = 0;
            }

            minY = minY - d;
            if (minY < 0)
            {
                minY = 0;
            }

            maxX = maxX + d;
            if (maxX > bmp.Width)
            {
                maxX = bmp.Width;
            }

            return new Rectangle(minX, minY, maxX - minX, bmp.Height - minY);
        }
    }
}