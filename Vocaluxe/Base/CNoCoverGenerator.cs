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
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using Vocaluxe.Base.Fonts;
using VocaluxeLib;
using VocaluxeLib.Draw;
using System.Linq;

namespace Vocaluxe.Base
{
    /// <summary>
    ///     Class that generates Covers if the right Cover is missing
    ///     Themeable via the xml of the Cover theme
    /// </summary>
    class CNoCoverGenerator
    {
        private readonly bool _Valid;
        private readonly string _Text;
        private readonly SColorF _TextColor;
        private readonly string _TextColorName;
        private readonly float _TextMargin;
        private readonly float _TextIndent;
        private readonly int _TextSize;
        private readonly string _Font;
        private readonly EStyle _Style;
        private readonly string _Image;
        private readonly bool _ShowFirstCover;
        private readonly float _ImageAlpha;
        private const float _LineSpace = 5;

        public CNoCoverGenerator(CXMLReader xmlReader, string xPath, string basePath)
        {
            _Valid = xmlReader.ItemExists(xPath);
            if (!_Valid)
                return;
            _Valid &= xmlReader.GetValue(xPath + "/Text/Text", out _Text);
            _Valid &= xmlReader.GetValue(xPath + "/Text/Font", out _Font);
            _Valid &= xmlReader.TryGetEnumValue(xPath + "/Text/Style", ref _Style);
            _Valid &= xmlReader.TryGetIntValue(xPath + "/Text/Size", ref _TextSize);
            if (xmlReader.GetValue(xPath + "/Text/Color", out _TextColorName))
                _Valid &= CBase.Theme.GetColor(_TextColorName, CBase.Theme.GetSkinIndex(-1), out _TextColor);
            else
                _Valid &= xmlReader.TryGetColorFromRGBA(xPath + "/Text", ref _TextColor);
            _Valid &= xmlReader.TryGetFloatValue(xPath + "/Text/Margin", ref _TextMargin);
            if (!xmlReader.TryGetFloatValue(xPath + "/Text/Indent", ref _TextIndent))
                _TextIndent = 6 * _TextMargin;
            _Valid &= xmlReader.GetValue(xPath + "/Image", out _Image);
            string tmp;
            if (xmlReader.GetValue(xPath + "/ShowFirstCover", out tmp))
                Boolean.TryParse(tmp, out _ShowFirstCover);
            else
                _ShowFirstCover = false;
            if (!xmlReader.TryGetFloatValue(xPath + "/ImageAlpha", ref _ImageAlpha))
                _ImageAlpha = 0.5f;
            else
                _ImageAlpha = _ImageAlpha.Clamp(0f, 1f);
            if (_Valid)
            {
                // ReSharper disable AssignNullToNotNullAttribute
                _Image = Path.Combine(basePath, _Image);
                // ReSharper restore AssignNullToNotNullAttribute
                _Valid = File.Exists(_Image);
            }
        }

        public CTexture GetCover(string text, string firstCoverPath)
        {
            if (!_Valid)
                return null;
            text = CLanguage.Translate(_Text.Replace("%TEXT%", text));
            using (Bitmap bmp = new Bitmap(_Image))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.TextRenderingHint = TextRenderingHint.AntiAlias;

                if (_ShowFirstCover && !String.IsNullOrEmpty(firstCoverPath) && File.Exists(firstCoverPath))
                {
                    ColorMatrix cm = new ColorMatrix {Matrix33 = 1f - _ImageAlpha};
                    ImageAttributes ia = new ImageAttributes();
                    ia.SetColorMatrix(cm);
                    using (Bitmap bmp2 = new Bitmap(firstCoverPath))
                        g.DrawImage(bmp2, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, bmp2.Width, bmp2.Height, GraphicsUnit.Pixel, ia);
                }
                if (text != "")
                {
                    List<string> lines = _SplitText(text);
                    CFonts.SetFont(_Font);
                    CFonts.Style = _Style;
                    float allowedWidth = bmp.Width - 2 * _TextMargin;
                    if (lines.Count > 1)
                        allowedWidth -= _TextIndent;
                    float allowedHeight = bmp.Height - 2 * _TextMargin - _LineSpace * (lines.Count - 1);
                    CFonts.Height = allowedHeight / lines.Count;
                    Font fo = CFonts.GetFont();
                    List<SizeF> sizes = lines.Select(line => g.MeasureString(line, fo, -1, StringFormat.GenericTypographic)).ToList();
                    float maxWidth = sizes.Select(s => s.Width).Max();
                    float maxHeight = sizes.Select(s => s.Height).Max();
                    maxHeight = (maxHeight + CFonts.Height) / 2;
                    if (maxWidth > allowedWidth)
                    {
                        CFonts.Height *= allowedWidth / maxWidth;
                        maxHeight *= allowedWidth / maxWidth;
                    }
                    if (maxHeight > _TextSize)
                    {
                        CFonts.Height *= _TextSize / maxHeight;
                        maxHeight = _TextSize;
                    }
                    fo = CFonts.GetFont();

                    //Have to use size in em not pixels!
                    float emSize = fo.Size * fo.FontFamily.GetCellAscent(fo.Style) / fo.FontFamily.GetEmHeight(fo.Style);
                    float outlineSize = CFonts.Outline * CFonts.Height;
                    SColorF outlineColorF = CFonts.OutlineColor;
                    outlineColorF.A = outlineColorF.A * _TextColor.A;

                    using (var path = new GraphicsPath())
                    using (var pen = new Pen(outlineColorF.AsColor(), outlineSize / 2))
                    {
                        pen.LineJoin = LineJoin.Round;
                        pen.Alignment = PenAlignment.Outset;
                        float top = (allowedHeight - maxHeight * lines.Count) / 2;
                        for (int i = 0; i < lines.Count; i++)
                        {
                            string line = lines[i];
                            float left;
                            if (lines.Count == 1 || (i == 1 && lines.Count == 3))
                            {
                                //Center Text if this is the only line or the middle line
                                float width = g.MeasureString(line, fo, -1, StringFormat.GenericTypographic).Width;
                                left = (bmp.Width - width) / 2;
                            }
                            else if (i == lines.Count - 1)
                            {
                                //Place last line at right
                                float width = g.MeasureString(line, fo, -1, StringFormat.GenericTypographic).Width;
                                left = bmp.Width - width - _TextMargin;
                            }
                            else
                                left = _TextMargin;
                            //g.DrawString(line, fo, new SolidBrush(_TextColor.AsColor()), left, top, StringFormat.GenericTypographic);
                            path.AddString(line, fo.FontFamily, (int)fo.Style, emSize, new PointF(left, top), StringFormat.GenericTypographic);
                            top += maxHeight + _LineSpace;
                        }
                        g.DrawPath(pen, path);
                        // ReSharper disable ImpureMethodCallOnReadonlyValueField
                        g.FillPath(new SolidBrush(_TextColor.AsColor()), path);
                        // ReSharper restore ImpureMethodCallOnReadonlyValueField
                    }
                }
                return CDraw.AddTexture(bmp);
            }
        }

        private static List<string> _SplitText(string text)
        {
            text = text.Replace(".", ". ").Replace("-", "- ").Replace("  ", " ");
            string[] parts = text.Split(' ');
            List<string> lines = new List<string>();
            string line = "";
            bool appendingName = false;
            foreach (string part in parts)
            {
                if (part.Length == 1 && !Char.IsLetter(part, 0))
                {
                    //Add non-letter chars to the end of the line (e.g. in "19 - 20")
                    line = (line != "" ? " " : "") + part;
                    appendingName = false;
                }
                else if (part.Length == 1 || (part.Length == 2 && part[1] == '.'))
                {
                    //We have something like the initials (e.g. "J. R. R. Tolkien") - Keep them together
                    if (!appendingName && line != "")
                    {
                        //When we are not already appending those, add the last line and start a new one
                        lines.Add(line);
                        line = part;
                    }
                    line = (line != "" ? " " : "") + part;
                    appendingName = true;
                }
                else
                {
                    if (line != "")
                        lines.Add(line);
                    line = part;
                    appendingName = false;
                }
            }
            if (line != "")
                lines.Add(line);

            int i;
            //If we have more than 3 lines try to distribute them evenly
            if (lines.Count > 3)
            {
                int len = lines.Sum(el => el.Length) + lines.Count - 1;
                int avgLen = (int)Math.Ceiling(len / 3d);
                i = 1;
                while (i < lines.Count)
                {
                    string prev = lines[i - 1];
                    string cur = lines[i];
                    if (prev.Length + cur.Length <= avgLen)
                    {
                        lines[i - 1] += " " + cur;
                        lines.RemoveAt(i);
                    }
                    else
                        i++;
                }
            }

            //Further reduce the lines by taking the longest line as the max length
            i = 1;
            while (i < lines.Count)
            {
                string prev = lines[i - 1];
                string cur = lines[i];
                if (prev.Length + cur.Length <= lines.Max(el => el.Length))
                {
                    lines[i - 1] += " " + cur;
                    lines.RemoveAt(i);
                }
                else
                    i++;
            }

            //If we still have more than 3 lines be more agressive
            while (lines.Count > 3)
            {
                //Join the shortest line with shortest neighbour
                int minLen = lines.Min(el => el.Length);
                for (i = 0; i < lines.Count; i++)
                {
                    if (lines[i].Length != minLen)
                        continue;
                    int other;
                    if (i == 0)
                        other = 1;
                    else if (i == lines.Count - 1)
                        other = i - 1;
                    else if (lines[i - 1].Length < lines[i + 1].Length)
                        other = i - 1;
                    else
                        other = i + 1;
                    lines[i] += " " + lines[other];
                    lines.RemoveAt(other);
                    break;
                }
            }
            return lines;
        }
    }
}