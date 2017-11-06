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
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using Vocaluxe.Base.Fonts;
using VocaluxeLib;
using System.Linq;

namespace Vocaluxe.Base
{
    /// <summary>
    ///     Class that generates Covers if the right Cover is missing
    ///     Themeable via the xml of the Cover theme
    /// </summary>
    class CCoverGenerator
    {
        private readonly bool _Valid;
        private readonly SThemeCoverGenerator _Theme;
        private readonly string _Image;
        private readonly SColorF _TextColor;
        private readonly SColorF _BGColor;
        private const int _LineSpace = 5;
        private readonly int _MarginLeft;
        private readonly int _MarginRight;
        private readonly int _MarginTop;
        private readonly int _MarginBottom;

        /// <summary>
        ///     Split lines after special chars, descending priority
        /// </summary>
        private static readonly char[] _SplitCharPreferences = {':', '/', '-'};
        private static readonly char[] _SplitCharAfter = {':', '/', '-', ' ', ')', '.', '*', ','};
        private static readonly char[] _SplitCharBefore = {'('};

        public CCoverGenerator(SThemeCoverGenerator theme, string basePath)
        {
            _Theme = theme;
            _Valid = true;
            _Valid &= _Theme.Text.Color.Get(-1, out _TextColor);
            _Valid &= _Theme.BackgroundColor.Get(-1, out _BGColor);
            if (_Valid)
            {
                _Image = Path.Combine(basePath, _Theme.Image);
                _Valid = File.Exists(_Image);
            }
            _MarginLeft = theme.Text.Margin.Left.HasValue ? theme.Text.Margin.Left.Value : theme.Text.Margin.Default;
            _MarginRight = theme.Text.Margin.Right.HasValue ? theme.Text.Margin.Right.Value : theme.Text.Margin.Default;
            _MarginTop = theme.Text.Margin.Top.HasValue ? theme.Text.Margin.Top.Value : theme.Text.Margin.Default;
            _MarginBottom = theme.Text.Margin.Bottom.HasValue ? theme.Text.Margin.Bottom.Value : theme.Text.Margin.Default;
        }

        private class CTextElement
        {
            public readonly string Text;
            public float Width, Height;
            private readonly Graphics _Graphics;
            private readonly Font _Font;
            private float _WidthTrimmed = -1;
            public float WidthTrimmed
            {
                get
                {
                    if (_WidthTrimmed < 0f)
                    {
                        string text = Text.TrimEnd(null);
                        _WidthTrimmed = text.Length == Text.Length ? Width : _Graphics.MeasureString(text, _Font).Width;
                    }
                    return _WidthTrimmed;
                }
            }
            public int Line;

            public CTextElement(string text, Graphics g, Font font)
            {
                Text = text;
                SizeF dimensions = g.MeasureString(text, font);
                Width = dimensions.Width;
                Height = (dimensions.Height + font.Height) / 2;
                _Graphics = g;
                _Font = font;
            }

            public void AdjustSize(float factor)
            {
                Width *= factor;
                _WidthTrimmed *= factor;
                Height *= factor;
            }
        }

        private void _DrawBackground(Graphics g, Bitmap bmpBackground, String firstCoverPath)
        {
            g.Clear(_BGColor.AsColor());

            ImageAttributes ia = null;
            if (_Theme.ShowFirstCover && !String.IsNullOrEmpty(firstCoverPath) && File.Exists(firstCoverPath))
            {
                using (Bitmap bmp2 = new Bitmap(firstCoverPath))
                    g.DrawImage(bmp2, bmpBackground.GetRect(), 0, 0, bmp2.Width, bmp2.Height, GraphicsUnit.Pixel);
                ColorMatrix cm = new ColorMatrix {Matrix33 = _Theme.ImageAlpha};
                ia = new ImageAttributes();
                ia.SetColorMatrix(cm);
            }
            g.DrawImage(bmpBackground, bmpBackground.GetRect(), 0, 0, bmpBackground.Width, bmpBackground.Height, GraphicsUnit.Pixel, ia);
        }

        private void _DrawText(Graphics g, Size bmpSize, CFont font, List<CTextElement> elements)
        {
            Font fo = CFonts.GetSystemFont(font);

            float maxHeight = elements.Select(el => el.Height).Max();
            int lineCount = elements.Last().Line + 1;

            //Have to use size in em not pixels!
            float emSize = fo.Size * fo.FontFamily.GetCellAscent(fo.Style) / fo.FontFamily.GetEmHeight(fo.Style);
            float outlineSize = CFonts.GetOutlineSize(font) * font.Height;
            SColorF outlineColorF = CFonts.GetOutlineColor(font);
            outlineColorF.A = outlineColorF.A * _TextColor.A;

            using (var path = new GraphicsPath())
            using (var pen = new Pen(outlineColorF.AsColor(), outlineSize / 2))
            {
                pen.LineJoin = LineJoin.Round;
                pen.Alignment = PenAlignment.Outset;
                float top = (bmpSize.Height - _MarginBottom - _MarginTop - maxHeight * lineCount) / 2 + _MarginTop;
                int nextLineEl = 0;
                for (int i = 0; i < lineCount; i++)
                {
                    int firstEl = nextLineEl;
                    for (; nextLineEl < elements.Count; nextLineEl++)
                    {
                        if (elements[nextLineEl].Line > i)
                            break;
                    }

                    string line = elements.GetRange(firstEl, nextLineEl - firstEl).Aggregate("", (current, element) => current + element.Text);
                    float left;
                    if (lineCount == 1 || (i == 1 && lineCount == 3))
                    {
                        //Center Text if this is the only line or the middle line
                        float width = _GetWidth(elements, firstEl, nextLineEl - 1);
                        left = (bmpSize.Width - _MarginLeft - _MarginRight - width) / 2 + _MarginLeft;
                    }
                    else if (i == lineCount - 1)
                    {
                        //Place last line at right
                        float width = _GetWidth(elements, firstEl, nextLineEl - 1);
                        left = bmpSize.Width - width - _MarginRight;
                    }
                    else
                        left = _MarginLeft;
                    //g.DrawString(line, fo, new SolidBrush(_TextColor.AsColor()), left, top, StringFormat.GenericTypographic);
                    path.AddString(line, fo.FontFamily, (int)fo.Style, emSize, new PointF(left, top), StringFormat.GenericTypographic);
                    top += maxHeight + _LineSpace;
                }
                g.DrawPath(pen, path);
                g.FillPath(new SolidBrush(_TextColor.AsColor()), path);
            }
        }

        public Bitmap GetCover(string text, string firstCoverPath)
        {
            if (!_Valid)
                return null;
            text = CLanguage.Translate(_Theme.Text.Text.Replace("%TEXT%", text));
            using (Bitmap bmpImage = new Bitmap(_Image))
            {
                Bitmap bmp = new Bitmap(bmpImage.Width, bmpImage.Height, PixelFormat.Format32bppArgb);
                try
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.TextRenderingHint = TextRenderingHint.AntiAlias;

                        _DrawBackground(g, bmpImage, firstCoverPath);

                        if (text != "")
                        {
                            CFont font = new CFont(_Theme.Text.Font);
                            Font fo = CFonts.GetSystemFont(font);
                            IEnumerable<string> textParts = _SplitText(text);
                            List<CTextElement> elements = textParts.Select(line => new CTextElement(line, g, fo)).ToList();
                            float factor = _DistributeText(elements, bmp.Width, bmp.Height);
                            foreach (CTextElement element in elements)
                                element.AdjustSize(factor);
                            font.Height *= factor / (1f + CFonts.GetOutlineSize(font)); //Adjust for outline size
                            _DrawText(g, bmp.GetSize(), font, elements);
                        }
                    }
                    return bmp;
                }
                catch (Exception)
                {
                    bmp.Dispose();
                }
            }
            return null;
        }

        /// <summary>
        ///     Distributes the text among max. 3 lines maximizing the height of the line<br />
        ///     Sets the Line field in the elements
        /// </summary>
        /// <param name="elements">Text with set Width and Height</param>
        /// <param name="width">Image Width</param>
        /// <param name="height">Image Height</param>
        /// <returns>Factor for resizing the text (Maximizing this factor means maximizing the height)</returns>
        private float _DistributeText(List<CTextElement> elements, int width, int height)
        {
            int availableWidth = width - _MarginLeft - _MarginRight;
            int availableHeight = height - _MarginTop - _MarginBottom;

            float textHeight = elements.Select(el => el.Height).Max();

            //Try 1 line:
            float textWidth = _GetWidth(elements, 0);
            int maxHeight = Math.Min(availableHeight, (int)_Theme.Text.Font.Size);
            float factorH = maxHeight / textHeight;
            float factorW = availableWidth / textWidth;
            if (factorH <= factorW)
                return factorH; //Limited by Height
            float factor1 = factorW;

            //Try 2 lines
            if (elements.Count == 1)
            {
                //Only 1 element -> 1 line
                return factor1;
            }
            availableWidth -= _Theme.Text.Indent;
            maxHeight = Math.Min(availableHeight / 2 - _LineSpace, (int)_Theme.Text.Font.Size);
            factorH = maxHeight / textHeight;
            if (factorH <= factor1)
                return factor1; //Cannot get any bigger with more lines
            int splitEl = _GetSplitElement(elements, textWidth / 2);
            Debug.Assert(splitEl >= 0 && splitEl < elements.Count - 1);
            float width1 = _GetWidth(elements, 0, splitEl);
            float width2 = _GetWidth(elements, splitEl + 1);
            factorW = availableWidth / Math.Max(width1, width2);
            if (factorH <= factorW)
            {
                // Assertion: factorH>factor1 (check above) && factorH<=factorW --> Min(factorH,factorW)>factor 1
                // but for 2 lines it is limited by height
                // --> Use 2 lines
                _SetLine(elements, splitEl + 1, elements.Count - 1, 1);
                return factorH;
            }
            // factor2 < factorH && factorH>factor1
            float factor2 = factorW;

            //Try 3 lines
            maxHeight = Math.Min(availableHeight / 3 - 2 * _LineSpace, (int)_Theme.Text.Font.Size);
            factorH = maxHeight / textHeight;
            if (elements.Count == 2 || factorH <= Math.Max(factor1, factor2))
            {
                //Only 2 elements or cannot get any bigger
                if (factor2 <= factor1)
                    return factor1;
                _SetLine(elements, splitEl + 1, elements.Count - 1, 1);
                return factor2;
            }
            int splitEl21 = _GetSplitElement(elements, textWidth / 3, false);
            int splitEl22 = _GetSplitElement(elements, textWidth / 3, true, splitEl21 + 1);
            Debug.Assert(splitEl21 >= 0 && splitEl21 < splitEl22 && splitEl22 < elements.Count - 1);
            float width21 = _GetWidth(elements, 0, splitEl21);
            float width22 = _GetWidth(elements, splitEl21 + 1, splitEl22);
            float width23 = _GetWidth(elements, splitEl22 + 1);
            factorW = availableWidth / Math.Max(Math.Max(width21, width22), width23);
            float factor3 = Math.Min(factorH, factorW);
            if (factor3 > Math.Max(factor1, factor2))
            {
                _SetLine(elements, splitEl21 + 1, splitEl22, 1);
                _SetLine(elements, splitEl22 + 1, elements.Count - 1, 2);
                return factor3;
            }
            if (factor2 > factor1)
            {
                _SetLine(elements, splitEl + 1, elements.Count - 1, 1);
                return factor2;
            }
            return factor1;
        }

        /// <summary>
        ///     Returns the width of the elements with index from start to end of the list (inclusive) if they are put on 1 line
        /// </summary>
        /// <param name="list"></param>
        /// <param name="start"></param>
        /// <returns></returns>
        private static float _GetWidth(List<CTextElement> list, int start)
        {
            return _GetWidth(list, start, list.Count - 1);
        }

        /// <summary>
        ///     Returns the width of the elements with index from start to end (inclusive) if they are put on 1 line
        /// </summary>
        /// <param name="list"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private static float _GetWidth(List<CTextElement> list, int start, int end)
        {
            if (start < 0)
                start = 0;
            if (end >= list.Count)
                end = list.Count - 1;
            if (start > end)
                return 0f;
            float width = list.GetRange(start, end - start).Select(el => el.Width).Sum();
            return width + list[end].WidthTrimmed;
        }

        private static void _SetLine(List<CTextElement> list, int start, int end, int line)
        {
            for (int i = start; i <= end; i++)
                list[i].Line = line;
        }

        /// <summary>
        ///     Find the element after which to split the line
        /// </summary>
        /// <param name="elements">List with elements, width must be set</param>
        /// <param name="requestedWidth">The requested line width</param>
        /// <param name="singleSplit">Split on one point or two</param>
        /// <param name="startElement">First element on line</param>
        /// <returns>Index of element AFTER which to split</returns>
        private static int _GetSplitElement(List<CTextElement> elements, float requestedWidth, bool singleSplit = true, int startElement = 0)
        {
            //Assert we have enough elements
            Debug.Assert(singleSplit && elements.Count - startElement >= 2 || !singleSplit && elements.Count - startElement >= 3);
            float curWidth = 0f;
            int splitEl;
            for (splitEl = startElement; splitEl < elements.Count; splitEl++)
            {
                curWidth += elements[splitEl].Width;
                if (curWidth >= requestedWidth)
                    break;
            }

            // At this point we can either keep splitEl on this line for move it to the next line
            // ==> line with splitEl is to long, without it is to short. so find the best option
            // Check if line starts or ends with a long word, than put that on a single line
            if (splitEl == startElement)
                return splitEl;
            // These 2 conditions also cover if the text fits on 1 line
            if (!singleSplit && splitEl >= elements.Count - 2)
                return elements.Count - 3; // Make sure we have 2 elements left if in multi split mode!
            if (splitEl >= elements.Count - 1)
                return elements.Count - 2; // Make sure we have 1 element left!

            float diffWith = curWidth - requestedWidth;
            float diffWithout = requestedWidth - (curWidth - elements[splitEl].Width);
            if (!singleSplit)
            {
                // If we split in 3 lines, add the error of the next 2 lines to the current error for both cases
                int splitEl2 = _GetSplitElement(elements, requestedWidth, true, splitEl + 1);
                int splitEl3 = _GetSplitElement(elements, requestedWidth, true, splitEl);
                diffWith += Math.Abs(requestedWidth - _GetWidth(elements, splitEl + 1, splitEl2)) + Math.Abs(requestedWidth - _GetWidth(elements, splitEl2 + 1));
                diffWithout += Math.Abs(requestedWidth - _GetWidth(elements, splitEl, splitEl3)) + Math.Abs(requestedWidth - _GetWidth(elements, splitEl3 + 1));
            }
            float diff = diffWith - diffWithout;
            //Differences below this value are considered acceptable
            float equalDist = requestedWidth * 0.025f;
            if (diff > equalDist)
                return splitEl - 1; // width error with element is much higher than without -> put element on next line
            if (diff < -equalDist)
                return splitEl; // width error with element is much less than without -> keep element on line

            //The real split would be somewhere in the middle of the element so we can move it to either line
            string tmp = elements[splitEl].Text.TrimEnd(null);
            char lastCharWith = tmp[tmp.Length - 1];
            tmp = elements[splitEl - 1].Text.TrimEnd(null);
            char lastCharWithout = tmp[tmp.Length - 1];
            //Check if last chars are special chars
            if (Char.IsLetterOrDigit(lastCharWith))
            {
                if (Char.IsLetterOrDigit(lastCharWithout))
                    return startElement == 0 ? splitEl : splitEl - 1; //Both are alphanumeric, favor longer first lines but shorter middle lines
                return splitEl - 1; //Split after non-alphanumeric char
            }
            if (Char.IsLetterOrDigit(lastCharWithout))
                return splitEl; //Split after non-alphanumeric char

            int indexWith = Array.IndexOf(_SplitCharPreferences, lastCharWith);
            int indexWithout = Array.IndexOf(_SplitCharPreferences, lastCharWithout);
            if (indexWith <= indexWithout)
                return startElement == 0 ? splitEl : splitEl - 1; //favor longer first lines but shorter middle lines
            return splitEl - 1;
        }

        /// <summary>
        ///     Split the text in parts that can then be distributet among the lines
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static IEnumerable<string> _SplitText(string text)
        {
            Debug.Assert(!String.IsNullOrWhiteSpace(text));

            text = text.Trim().TrimMultipleWs();
            List<string> lines = new List<string>();
            if (text.Length == 1)
            {
                lines.Add(text);
                return lines;
            }
            //Split the text on non-letter chars
            int curStart = 0;
            for (int i = 1; i < text.Length - 1; i++)
            {
                if ((Array.IndexOf(_SplitCharAfter, text[i]) >= 0 && (Char.IsLetterOrDigit(text, i + 1) || Array.IndexOf(_SplitCharBefore, text[i + 1]) >= 0)) ||
                    (Char.IsLetterOrDigit(text, i) && Array.IndexOf(_SplitCharBefore, text[i + 1]) >= 0))
                {
                    lines.Add(text.Substring(curStart, i - curStart + 1));
                    curStart = ++i;
                }
            }
            //Add the rest
            lines.Add(text.Substring(curStart));

            //Check for initials (like J. R.R. Tolkien) and join them
            curStart = -1;
            string curText = "";
            for (int i = 0; i < lines.Count; i++)
            {
                string part = lines[i].Trim();
                if (part.Length == 2 && part[1] == '.')
                {
                    //Initials found, continue or start new
                    if (curStart < 0)
                        curStart = i;
                    curText += lines[i];
                }
                else if (curStart >= 0)
                {
                    //Initials end
                    if (curStart < i - 1)
                    {
                        //More than 1 part
                        lines[curStart] = curText;
                        lines.RemoveRange(curStart + 1, i - curStart - 1);
                        i = curStart;
                    }
                    curText = "";
                    curStart = -1;
                }
            }
            //Handle last part
            if (curStart >= 0)
            {
                //Initials end
                if (curStart < lines.Count - 1)
                {
                    //More than 1 part
                    lines[curStart] = curText;
                    lines.RemoveRange(curStart + 1, lines.Count - curStart - 1);
                }
            }

            // Remove single char lines
            for (int i = 0; i < lines.Count; i++)
            {
                string part = lines[i];
                if (part.Trim().Length == 1)
                {
                    lines.RemoveAt(i);
                    if (i == 0)
                        lines[0] = part + lines[0];
                    else
                    {
                        lines[i - 1] += part;
                        i--;
                    }
                }
            }
            return lines;
        }
    }
}