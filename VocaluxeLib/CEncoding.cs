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
using System.Globalization;
using System.Text;

namespace VocaluxeLib
{
    public static class CEncoding
    {
        static CEncoding()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public static Encoding Locale => OperatingSystem.IsWindows() ? Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.ANSICodePage) : Encoding.UTF8;

        public static Encoding GetEncoding(this string encodingName)
        {
            switch (encodingName)
            {
                case "AUTO":
                    return Locale;
                case "CP1250":
                    return Encoding.GetEncoding(1250);
                case "CP1252":
                    return Encoding.GetEncoding(1252);
                case "LOCALE":
                    return Locale;
                case "UTF8":
                    return Encoding.UTF8;
                default:
                    return Locale;
            }
        }

        public static string GetEncodingName(this Encoding enc)
        {
            var result = "UTF8";

            if (enc.CodePage == 1250)
            {
                result = "CP1250";
            }

            if (enc.CodePage == 1252)
            {
                result = "CP1252";
            }

            return result;
        }
    }
}