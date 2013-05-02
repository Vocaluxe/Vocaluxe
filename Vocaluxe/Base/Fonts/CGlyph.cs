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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using VocaluxeLib.Menu;

namespace Vocaluxe.Base.Fonts
{
    class CGlyph
    {
        private STexture _Texture;
        private readonly SizeF _BoundingBox;
        private readonly RectangleF _DrawBounding;
        public readonly float MaxHeight;

        public float Width
        {
            get { return _BoundingBox.Width * _GetFactor(); }
        }

        public float Height
        {
            get { return _BoundingBox.Height * _GetFactor(); }
        }

        public CGlyph(char chr, float maxHeight)
        {
            MaxHeight = maxHeight;
            float oldHeight = CFonts.Height;
            float outline = CFonts.Outline;
            float outlineSize = outline * maxHeight;
            string chrString = chr.ToString();

            CFonts.Height = maxHeight; // *factor;
            //float factor = _GetFactor(chr, flags);
            Font fo = CFonts.GetFont();
            SizeF fullSize, boundingSize;
            Size bmpSize;
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                fullSize = g.MeasureString(chrString, fo);
                fullSize.Width += outlineSize;
                if (chr != ' ')
                {
                    //Gets exact height and width for drawing more than 1 char. But width is to small to draw char on bitmap as e.g. italic chars will get cropped
                    boundingSize = g.MeasureString(chrString, fo, -1, new StringFormat(StringFormat.GenericTypographic));
                    boundingSize.Width += outlineSize;
                    if (boundingSize.Height > 0)
                        boundingSize.Height += outlineSize;
                    else
                        boundingSize.Height = fullSize.Height;
                    bmpSize = new Size((int)fullSize.Width, (int)Math.Round(boundingSize.Height));
                }
                else
                {
                    boundingSize = fullSize;
                    bmpSize = new Size(1, 1);
                }
                _BoundingBox = boundingSize;
            }
            using (Bitmap bmp = new Bitmap(bmpSize.Width, bmpSize.Height, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);

                if (chr == ' ')
                {
                    _Texture = CDraw.AddTexture(bmp);
                    _DrawBounding = new RectangleF(0, 0, 1, 1);
                }
                else
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                    PointF point = new PointF(outlineSize / 2, outlineSize / 2);

                    using (GraphicsPath path = new GraphicsPath())
                    {
                        //Have to use size in em not pixels!
                        float emSize = fo.Size * fo.FontFamily.GetCellAscent(fo.Style) / fo.FontFamily.GetEmHeight(fo.Style);
                        path.AddString(chrString, fo.FontFamily, (int)fo.Style, emSize, point, new StringFormat());

                        using (Pen pen = new Pen(CFonts.OutlineColor.AsColor(), outlineSize))
                        {
                            pen.LineJoin = LineJoin.Round;
                            g.DrawPath(pen, path);
                            g.FillPath(Brushes.White, path);
                        }
                    }
                    _DrawBounding = _GetRealBounds(bmp);
                    using (Bitmap bmpCropped = bmp.Clone(_DrawBounding, PixelFormat.Format32bppArgb))
                    {
                        float dx = (fullSize.Width - boundingSize.Width - 1) / 2;
                        _DrawBounding.X -= dx;
                        _Texture = CDraw.AddTexture(bmpCropped);
                        /*_DrawBounding.X *= _Texture.Width / _DrawBounding.Width;
                        _DrawBounding.Y *= _Texture.Width / _DrawBounding.Width;
                        _DrawBounding.Width = _Texture.Width;
                        _DrawBounding.Height = _Texture.Height;*/
                        if (false)
                        {
                            if (outline > 0)
                                bmpCropped.Save("font/" + chr + "o" + CFonts.Style + "2.png", ImageFormat.Png);
                            else
                                bmpCropped.Save("font/" + chr + CFonts.Style + "2.png", ImageFormat.Png);
                        }
                    }
                }
            }
            CFonts.Height = oldHeight;
        }

        public void UnloadTexture()
        {
            CDraw.RemoveTexture(ref _Texture);
        }

        private float _GetFactor()
        {
            return CFonts.Height / _BoundingBox.Height;
        }

        public void GetTextureAndRect(float x, float y, float z, out STexture texture, out SRectF rect)
        {
            texture = _Texture;
            float factor = _GetFactor();
            x += _DrawBounding.X * factor;
            y += _DrawBounding.Y * factor;
            float h = _DrawBounding.Height * factor;
            float w = _DrawBounding.Width * factor;
            rect = new SRectF(x, y, w, h, z);
        }

        private static Rectangle _GetRealBounds(Bitmap bmp)
        {
            int minX = 0, maxX = bmp.Width - 1, minY = 0;
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int values = bmpData.Width * bmp.Height;
            Int32[] rgbValues = new Int32[values];
            Marshal.Copy(bmpData.Scan0, rgbValues, 0, values);
            int index = 0;
            bool found = false;
            //find from top
            for (int y = 0; y < bmp.Height && !found; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
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
            for (int x = 0; x < minX && !found; x++)
            {
                index = x + minY * bmp.Width;
                for (int y = minY; y < bmp.Height; y++)
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
            for (int x = bmp.Width - 1; x > maxX && !found; x--)
            {
                index = x + minY * bmp.Width;
                for (int y = minY; y < bmp.Height; y++)
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
            return new Rectangle(minX, minY, maxX - minX + 1, bmp.Height - minY);
        }
    }
}