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
using System.Windows.Forms;
using VocaluxeLib.Menu;

namespace Vocaluxe.Base.Font
{
    class CGlyph
    {
        private STexture _Texture;
        private readonly SizeF _BoundingBox;
        private readonly Point _Offset;

        public float Width
        {
            get { return _BoundingBox.Width * _GetFactor(); }
        }

        public float Height
        {
            get { return _BoundingBox.Height * _GetFactor(); }
        }

        public CGlyph(char chr, float maxHigh)
        {
            float oldHeight = CFonts.Height;
            float outline = CFonts.Outline;
            float outlineSize = outline * maxHigh;
            string chrString = chr.ToString();

            CFonts.Height = maxHigh; // *factor;
            //float factor = _GetFactor(chr, flags);
            System.Drawing.Font fo = CFonts.GetFont();
            SizeF fullSize;
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                fullSize = g.MeasureString(chrString, fo);
                fullSize.Width += outlineSize;
                if (chr != ' ')
                {
                    SizeF boundingSize = g.MeasureString(chrString, fo, -1, new StringFormat(StringFormat.GenericTypographic));
                    boundingSize.Width += outlineSize;
                    _BoundingBox = boundingSize;
                }
                else
                {
                    _BoundingBox = fullSize;
                    fullSize.Width = 1;
                    fullSize.Height = 1;
                }
            }
            using (Bitmap bmp = new Bitmap((int)(fullSize.Width), (int)(fullSize.Height), PixelFormat.Format32bppArgb))
            {
                Graphics g = Graphics.FromImage(bmp);
                g.Clear(Color.Transparent);

                if (chr != ' ')
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                    PointF point = new PointF(outlineSize / 2, 0);

                    using (GraphicsPath path = new GraphicsPath())
                    {
                        path.AddString(chrString, fo.FontFamily, (int)fo.Style, CFonts.Height, point, new StringFormat());

                        using (Pen pen = new Pen(CFonts.OutlineColor.AsColor(), outlineSize))
                        {
                            pen.LineJoin = LineJoin.Round;
                            g.DrawPath(pen, path);
                            g.FillPath(Brushes.White, path);
                        }
                    }
                }
                _Texture = CDraw.AddTexture(bmp);

                if (false)
                {
                    if (outline > 0)
                        bmp.Save("font/" + chr + "o" + CFonts.Style + "2.png", ImageFormat.Png);
                    else
                        bmp.Save("font/" + chr + CFonts.Style + "2.png", ImageFormat.Png);
                }

                g.Dispose();
            }
            fo.Dispose();
            CFonts.Height = oldHeight;
        }

        private float _GetFactor()
        {
            return CFonts.Height / _BoundingBox.Height;
        }

        public void UnloadTexture()
        {
            CDraw.RemoveTexture(ref _Texture);
        }

        public void GetTextureAndRect(float x, float y, float z, out STexture texture, out SRectF rect)
        {
            texture = _Texture;
            rect = new SRectF(x, y, Width, Height, z);
        }

        private Point _TrimBmp(ref Bitmap bmp)
        {
            int minX = 0, minY = 0, maxX = bmp.Width - 1, maxY = bmp.Width - 1;
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
            //find from bottom
            for (int y = bmp.Height - 1; y >= minY && !found; y--)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    if (rgbValues[index] != 0)
                    {
                        if (x < minX)
                            minX = x;
                        else if (x > maxX)
                            maxX = x;
                        maxY = y;
                        found = true;
                        break;
                    }
                    index++;
                }
            }
            //find left
            for (int x = minX - 1; x >= 0; x--)
            {
                found = false;
                index = minY * bmp.Width + x;
                for (int y = minY; y <= maxY; y++)
                {
                    if (rgbValues[index] != 0)
                    {
                        found = true;
                        minX = x;
                        break;
                    }
                    index += bmp.Width;
                }
                if (!found)
                    break;
            }
            //find right
            for (int x = maxX + 1; x < bmp.Width; x++)
            {
                found = false;
                index = minY * bmp.Width + x;
                for (int y = minY; y <= maxY; y++)
                {
                    if (rgbValues[index] != 0)
                    {
                        found = true;
                        maxX = x;
                        break;
                    }
                    index += bmp.Width;
                }
                if (!found)
                    break;
            }
            bmp = bmp.Clone(new Rectangle(minX, minY, maxX - minX, maxY - minY), PixelFormat.Format32bppArgb);
            return new Point(minX, minY);
        }

        private float _GetStyleFactor(char chr, TextFormatFlags flags)
        {
            if (CFonts.Style == EStyle.Normal)
                return 1f;

            EStyle style = CFonts.Style;

            CFonts.Style = EStyle.Normal;
            float hStyle, hNormal;
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                System.Drawing.Font fo = CFonts.GetFont();
                Size sizeB = TextRenderer.MeasureText(g, chr.ToString(), fo, new Size(int.MaxValue, int.MaxValue), flags);
                //SizeF size = g.MeasureString(chr.ToString(), fo);
                hNormal = sizeB.Height;
            }
            CFonts.Style = style;
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                System.Drawing.Font fo = CFonts.GetFont();
                Size sizeB = TextRenderer.MeasureText(g, chr.ToString(), fo, new Size(int.MaxValue, int.MaxValue), flags);
                //size = g.MeasureString(chr.ToString(), fo);
                hStyle = sizeB.Height;
            }
            return hNormal / hStyle;
        }
    }
}