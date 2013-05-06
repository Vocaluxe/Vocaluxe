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
using System.Drawing.Text;
using System.Windows.Forms;
using VocaluxeLib;
using VocaluxeLib.Menu;

namespace Vocaluxe.Base.Font
{
    class CGlyph
    {
        public float Sizeh { get; private set; }

        public STexture Texture;
        public readonly int Width;

        public CGlyph(char chr, float maxHigh)
        {
            Sizeh = maxHigh;

            float outline = CFonts.Outline;
            const TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;

            float factor = _GetFactor(chr, flags);
            CFonts.Height = Sizeh * factor;
            System.Drawing.Font fo;
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
                //bmp.Save("font/" + chr + CFonts.Style + ".png", ImageFormat.Png);
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