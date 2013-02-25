using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;

namespace Vocaluxe.Menu
{
    public static class CHelper
    {

        public static int nCk(int n, int k)
        {
            if (k > n)
                return 0;

            if (k == 0 || k == n)
                return 1;

            if (k < 0 || n <= 0)
                return 0; //is not defined

            if (k * 2 > n)
                k = n - k;

            long result = n;
            long nl = n;
            long nk = k;

            for (long i = 1; i <= k - 1; i++)
            {
                result = result * (n - i) / (i + 1);
            }
            return (int)result;
        }

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
                int tmp = sr.Peek();
                //Check for ' ', ?, ?, \n, \r
                while (tmp != 32 && tmp != 19 && tmp != 16 && tmp != 13 && tmp != 10)
                {
                    chr = (char)sr.Read();
                    value += chr.ToString();
                    tmp = sr.Peek();
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



        public static List<string> ListFiles(string path, string cast)
        {
            return ListFiles(path, cast, false, false);
        }

        public static List<string> ListFiles(string path, string cast, bool recursive)
        {
            return ListFiles(path, cast, recursive, false);
        }

        public static List<string> ListFiles(string path, string cast, bool recursive, bool fullpath)
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
