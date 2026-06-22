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
using System.IO;
using System.Linq;
using SkiaSharp;
using Vocaluxe.Base.Fonts;
using VocaluxeLib;

namespace Vocaluxe.Base
{
    /// <summary>
    ///     Generates covers for songs that have none, themeable via the cover-theme xml.
    ///     Rendering ported from GDI+ to SkiaSharp; the line-distribution maths are unchanged.
    ///     Returns the result as a straight-alpha BGRA byte array (see <see cref="GetCover" />).
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

        private static SKColor _ToSkColor(SColorF c)
        {
            return new SKColor((byte)(c.R * 255), (byte)(c.G * 255), (byte)(c.B * 255), (byte)(c.A * 255));
        }

        private class CTextElement
        {
            public readonly string Text;
            public float Width, Height;
            private readonly SKFont _Font;
            private float _WidthTrimmed = -1;
            public float WidthTrimmed
            {
                get
                {
                    if (_WidthTrimmed < 0f)
                    {
                        string text = Text.TrimEnd(null);
                        _WidthTrimmed = text.Length == Text.Length ? Width : _Font.MeasureText(text);
                    }
                    return _WidthTrimmed;
                }
            }
            public int Line;

            public CTextElement(string text, SKFont font)
            {
                Text = text;
                Width = font.MeasureText(text);
                SKFontMetrics m = font.Metrics;
                Height = m.Descent - m.Ascent;
                _Font = font;
            }

            public void AdjustSize(float factor)
            {
                Width *= factor;
                _WidthTrimmed *= factor;
                Height *= factor;
            }
        }

        private void _DrawBackground(SKCanvas canvas, int width, int height, SKBitmap bmpBackground, string firstCoverPath)
        {
            canvas.Clear(_ToSkColor(_BGColor));

            var dstRect = new SKRect(0, 0, width, height);
            byte bgAlpha = 255;
            if (_Theme.ShowFirstCover && !String.IsNullOrEmpty(firstCoverPath) && File.Exists(firstCoverPath))
            {
                using (SKBitmap first = SKBitmap.Decode(firstCoverPath))
                {
                    if (first != null)
                        canvas.DrawBitmap(first, dstRect);
                }
                bgAlpha = (byte)(_Theme.ImageAlpha * 255);
            }

            using (var paint = new SKPaint {Color = new SKColor(255, 255, 255, bgAlpha)})
                canvas.DrawBitmap(bmpBackground, dstRect, paint);
        }

        private void _DrawText(SKCanvas canvas, int width, int height, CFont font, List<CTextElement> elements)
        {
            SKTypeface typeface = CFonts.GetTypeface(font);
            using (var skFont = new SKFont(typeface, font.Height))
            {
                float maxHeight = elements.Select(el => el.Height).Max();
                int lineCount = elements.Last().Line + 1;

                float outlineSize = CFonts.GetOutlineSize(font) * font.Height;
                SColorF outlineColorF = CFonts.GetOutlineColor(font);
                outlineColorF.A = outlineColorF.A * _TextColor.A;

                using (var fillPaint = new SKPaint {IsAntialias = true, Style = SKPaintStyle.Fill, Color = _ToSkColor(_TextColor)})
                using (var outlinePaint = new SKPaint
                {
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = outlineSize / 2,
                    StrokeJoin = SKStrokeJoin.Round,
                    Color = _ToSkColor(outlineColorF)
                })
                {
                    SKFontMetrics metrics = skFont.Metrics;
                    float top = (height - _MarginBottom - _MarginTop - maxHeight * lineCount) / 2 + _MarginTop;
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
                            float lineWidth = _GetWidth(elements, firstEl, nextLineEl - 1);
                            left = (width - _MarginLeft - _MarginRight - lineWidth) / 2 + _MarginLeft;
                        }
                        else if (i == lineCount - 1)
                        {
                            float lineWidth = _GetWidth(elements, firstEl, nextLineEl - 1);
                            left = width - lineWidth - _MarginRight;
                        }
                        else
                            left = _MarginLeft;

                        float baseline = top - metrics.Ascent;
                        if (outlineSize > 0)
                            canvas.DrawText(line, left, baseline, skFont, outlinePaint);
                        canvas.DrawText(line, left, baseline, skFont, fillPaint);
                        top += maxHeight + _LineSpace;
                    }
                }
            }
        }

        /// <summary>
        ///     Renders the cover and returns it as a tightly packed, straight-alpha BGRA byte array.
        ///     Returns null (and width/height 0) if the generator is not valid.
        /// </summary>
        public byte[] GetCover(string text, string firstCoverPath, out int width, out int height)
        {
            width = 0;
            height = 0;
            if (!_Valid)
                return null;
            text = CLanguage.Translate(_Theme.Text.Text.Replace("%TEXT%", text));

            try
            {
                using (SKBitmap bmpImage = SKBitmap.Decode(_Image))
                {
                    if (bmpImage == null)
                        return null;
                    int w = bmpImage.Width;
                    int h = bmpImage.Height;
                    var info = new SKImageInfo(w, h, SKColorType.Bgra8888, SKAlphaType.Unpremul);
                    using (var bmp = new SKBitmap(info))
                    using (var canvas = new SKCanvas(bmp))
                    {
                        _DrawBackground(canvas, w, h, bmpImage, firstCoverPath);

                        if (text != "")
                        {
                            CFont font = new CFont(_Theme.Text.Font);
                            SKTypeface typeface = CFonts.GetTypeface(font);
                            using (var measureFont = new SKFont(typeface, font.Height))
                            {
                                IEnumerable<string> textParts = _SplitText(text);
                                List<CTextElement> elements = textParts.Select(line => new CTextElement(line, measureFont)).ToList();
                                float factor = _DistributeText(elements, w, h);
                                foreach (CTextElement element in elements)
                                    element.AdjustSize(factor);
                                font.Height *= factor / (1f + CFonts.GetOutlineSize(font)); //Adjust for outline size
                                _DrawText(canvas, w, h, font, elements);
                            }
                        }

                        canvas.Flush();
                        width = w;
                        height = h;
                        return bmp.Bytes;
                    }
                }
            }
            catch (Exception)
            {
                width = 0;
                height = 0;
                return null;
            }
        }

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
                return factor1;
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
                _SetLine(elements, splitEl + 1, elements.Count - 1, 1);
                return factorH;
            }
            float factor2 = factorW;

            //Try 3 lines
            maxHeight = Math.Min(availableHeight / 3 - 2 * _LineSpace, (int)_Theme.Text.Font.Size);
            factorH = maxHeight / textHeight;
            if (elements.Count == 2 || factorH <= Math.Max(factor1, factor2))
            {
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

        private static float _GetWidth(List<CTextElement> list, int start)
        {
            return _GetWidth(list, start, list.Count - 1);
        }

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

        private static int _GetSplitElement(List<CTextElement> elements, float requestedWidth, bool singleSplit = true, int startElement = 0)
        {
            Debug.Assert(singleSplit && elements.Count - startElement >= 2 || !singleSplit && elements.Count - startElement >= 3);
            float curWidth = 0f;
            int splitEl;
            for (splitEl = startElement; splitEl < elements.Count; splitEl++)
            {
                curWidth += elements[splitEl].Width;
                if (curWidth >= requestedWidth)
                    break;
            }

            if (splitEl == startElement)
                return splitEl;
            if (!singleSplit && splitEl >= elements.Count - 2)
                return elements.Count - 3;
            if (splitEl >= elements.Count - 1)
                return elements.Count - 2;

            float diffWith = curWidth - requestedWidth;
            float diffWithout = requestedWidth - (curWidth - elements[splitEl].Width);
            if (!singleSplit)
            {
                int splitEl2 = _GetSplitElement(elements, requestedWidth, true, splitEl + 1);
                int splitEl3 = _GetSplitElement(elements, requestedWidth, true, splitEl);
                diffWith += Math.Abs(requestedWidth - _GetWidth(elements, splitEl + 1, splitEl2)) + Math.Abs(requestedWidth - _GetWidth(elements, splitEl2 + 1));
                diffWithout += Math.Abs(requestedWidth - _GetWidth(elements, splitEl, splitEl3)) + Math.Abs(requestedWidth - _GetWidth(elements, splitEl3 + 1));
            }
            float diff = diffWith - diffWithout;
            float equalDist = requestedWidth * 0.025f;
            if (diff > equalDist)
                return splitEl - 1;
            if (diff < -equalDist)
                return splitEl;

            string tmp = elements[splitEl].Text.TrimEnd(null);
            char lastCharWith = tmp[tmp.Length - 1];
            tmp = elements[splitEl - 1].Text.TrimEnd(null);
            char lastCharWithout = tmp[tmp.Length - 1];
            if (Char.IsLetterOrDigit(lastCharWith))
            {
                if (Char.IsLetterOrDigit(lastCharWithout))
                    return startElement == 0 ? splitEl : splitEl - 1;
                return splitEl - 1;
            }
            if (Char.IsLetterOrDigit(lastCharWithout))
                return splitEl;

            int indexWith = Array.IndexOf(_SplitCharPreferences, lastCharWith);
            int indexWithout = Array.IndexOf(_SplitCharPreferences, lastCharWithout);
            if (indexWith <= indexWithout)
                return startElement == 0 ? splitEl : splitEl - 1;
            return splitEl - 1;
        }

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
            lines.Add(text.Substring(curStart));

            curStart = -1;
            string curText = "";
            for (int i = 0; i < lines.Count; i++)
            {
                string part = lines[i].Trim();
                if (part.Length == 2 && part[1] == '.')
                {
                    if (curStart < 0)
                        curStart = i;
                    curText += lines[i];
                }
                else if (curStart >= 0)
                {
                    if (curStart < i - 1)
                    {
                        lines[curStart] = curText;
                        lines.RemoveRange(curStart + 1, i - curStart - 1);
                        i = curStart;
                    }
                    curText = "";
                    curStart = -1;
                }
            }
            if (curStart >= 0)
            {
                if (curStart < lines.Count - 1)
                {
                    lines[curStart] = curText;
                    lines.RemoveRange(curStart + 1, lines.Count - curStart - 1);
                }
            }

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
