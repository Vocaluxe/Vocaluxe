using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace Vocaluxe.Menu
{
    public class CHelper
    {
        /// <summary>
        /// Concat strings into one string with ", " as separator.
        /// </summary>
        public static string ListStrings(string[] str)
        {
            string _Result = string.Empty;

            for (int i = 0; i < str.Length; i++)
            {
                _Result += str[i];
                if (i < str.Length - 1)
                    _Result += ", ";
            }

            return _Result;
        }

        public static int TryReadInt(StreamReader sr)
        {
            char chr = '0';
            string value = String.Empty;

            try
            {
                chr = (char)sr.Peek();
                while ((chr.CompareTo(' ') != 0) && ((int)chr != 19) && ((int)chr != 16) && ((int)chr != 13))
                {
                    chr = (char)sr.Read();
                    value += chr.ToString();
                    chr = (char)sr.Peek();
                }
            }
            catch (Exception)
            {
                return 0;
            }
            int result = 0;
            int.TryParse(value, out result);
            return result;
        }

        public static void SetRect(RectangleF Bounds, ref RectangleF Rect, float RectAspect, EAspect Aspect)
        {
            float rW = Bounds.Right - Bounds.Left;
            float rH = Bounds.Bottom - Bounds.Top;
            float rA = rW / rH;

            float ScaledWidth = rW;
            float ScaledHeight = rH;

            switch (Aspect)
            {
                case EAspect.Crop:
                    if (rA >= RectAspect)
                    {
                        ScaledWidth = rW;
                        ScaledHeight = rH * rA / RectAspect;
                    }
                    else
                    {
                        ScaledHeight = rH;
                        ScaledWidth = rW * RectAspect / rA;
                    }
                    break;
                case EAspect.LetterBox:
                    if (RectAspect >= 1)
                    {
                        ScaledWidth = rW;
                        ScaledHeight = rH * rA / RectAspect;
                    }
                    else
                    {
                        ScaledHeight = rH;
                        ScaledWidth = rW * RectAspect / rA;
                    }
                    break;
                default:
                    ScaledWidth = rW;
                    ScaledHeight = rH;
                    break;
            }

            float Left = (rW - ScaledWidth) / 2 + Bounds.Left;
            float Rigth = Left + ScaledWidth;

            float Upper = (rH - ScaledHeight) / 2 + Bounds.Top;
            float Lower = Upper + ScaledHeight;

            Rect = new RectangleF(Left, Upper, Rigth - Left, Lower - Upper);
        }

        public static bool TryGetEnumValueFromXML<T>(string Cast, XPathNavigator Navigator, ref T value)
            where T : struct
        {
            string val = String.Empty;
            if (GetValueFromXML(Cast, Navigator, ref val, Enum.GetName(typeof(T), value)))
            {
                TryParse<T>(val, out value, true);
                return true;
            }
            return false;
        }

        public static bool TryGetIntValueFromXML(string Cast, XPathNavigator Navigator, ref int value)
        {
            string val = String.Empty;
            if (GetValueFromXML(Cast, Navigator, ref val, value.ToString()))
            {
                int res = 0;
                if (int.TryParse(val, out res))
                {
                    value = res;
                    return true;
                }
            }
            return false;
        }

        public static bool TryGetFloatValueFromXML(string Cast, XPathNavigator Navigator, ref float value)
        {
            string val = String.Empty;
            if (GetValueFromXML(Cast, Navigator, ref val, value.ToString()))
            {
                float res = 0;
                if (TryParse(val, out res))
                {
                    value = res;
                    return true;
                }
            }
            return false;
        }

        public static bool GetValueFromXML(string Cast, XPathNavigator Navigator, ref string Value, string DefaultValue)
        {
            XPathNodeIterator iterator;
            int results = 0;
            string val = string.Empty;

            try
            {
                Navigator.MoveToFirstChild();
                iterator = Navigator.Select(Cast);

                while (iterator.MoveNext())
                {
                    val = iterator.Current.Value;
                    results++;
                }
            }
            catch (Exception)
            {
                results = 0;
            }

            if ((results == 0) || (results > 1))
            {
                Value = DefaultValue;
                return false;
            }
            else
            {
                Value = val;
                return true;
            }

        }

        public static List<string> GetValuesFromXML(string Cast, XPathNavigator Navigator)
        {
            List<string> values = new List<string>();

            try
            {
                Navigator.MoveToRoot();
                Navigator.MoveToFirstChild();
                Navigator.MoveToFirstChild();

                while (Navigator.Name != Cast)
                    Navigator.MoveToNext();

                Navigator.MoveToFirstChild();

                values.Add(Navigator.LocalName);
                while (Navigator.MoveToNext())
                    values.Add(Navigator.LocalName);

            }
            catch (Exception)
            {

            }

            return values;
        }

        public static bool ItemExistsInXML(string Cast, XPathNavigator Navigator)
        {
            XPathNodeIterator iterator;
            int results = 0;

            try
            {
                Navigator.MoveToFirstChild();
                iterator = Navigator.Select(Cast);

                while (iterator.MoveNext())
                    results++;
            }
            catch (Exception)
            {
                results = 0;
            }

            if (results == 0)
                return false;

            return true;
        }

        public List<string> ListFiles(string path, string cast)
        {
            return ListFiles(path, cast, false, false);
        }

        public List<string> ListFiles(string path, string cast, bool recursive)
        {
            return ListFiles(path, cast, recursive, false);
        }

        public List<string> ListFiles(string path, string cast, bool recursive, bool fullpath)
        {
            List<string> files = new List<string>();
            DirectoryInfo dir = new DirectoryInfo(path);

            try
            {

                foreach (FileInfo file in dir.GetFiles(cast))
                {
                    if (!fullpath)
                        files.Add(file.Name);
                    else
                        //files.Add(Path.Combine(file.DirectoryName, file.Name));
                        files.Add(file.FullName);
                }

                if (recursive)
                {
                    foreach (DirectoryInfo di in dir.GetDirectories())
                    {
                        files.AddRange(ListFiles(di.FullName, cast, recursive, fullpath));
                    }
                }
            }
            catch (Exception)
            {

            }

            return files;
        }

        public static bool TryParse<T>(string value, out T result)
            where T : struct
        {
            return TryParse<T>(value, out result, false);
        }

        public static bool TryParse<T>(string value, out T result, bool ignoreCase)
           where T : struct
        {
            result = default(T);
            try
            {
                result = (T)Enum.Parse(typeof(T), value, ignoreCase);
                return true;
            }
            catch { }

            return false;
        }

        public static bool TryParse(string value, out float result)
        {
            value = value.Replace(',', '.');
            return float.TryParse(value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo, out result);
        }

        public static bool IsInBounds(SRectF bounds, MouseEvent MouseEvent)
        {
            return IsInBounds(bounds, MouseEvent.X, MouseEvent.Y);
        }

        public static bool IsInBounds(SRectF bounds, int x, int y)
        {
            return ((bounds.X <= x) && (bounds.X + bounds.W >= x) && (bounds.Y <= y) && (bounds.Y + bounds.H >= y));
        }
    }

    static class CEncoding
    {
        public static Encoding GetEncoding(string EncodingName)
        {
            switch (EncodingName)
            {
                case "AUTO":
                    return Encoding.Default;
                case "CP1250":
                    return Encoding.GetEncoding(1250);
                case "CP1252":
                    return Encoding.GetEncoding(1252);
                case "LOCALE":
                    return Encoding.Default;
                case "UTF8":
                    return Encoding.UTF8;
                default:
                    return Encoding.UTF8;
            }
        }

        public static string GetEncodingName(Encoding Enc)
        {
            string Result = "UTF8";

            if (Enc.CodePage == 1250)
                Result = "CP1250";

            if (Enc.CodePage == 1252)
                Result = "CP1252";

            return Result;
        }
    }
}
