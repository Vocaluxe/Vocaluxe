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

using System.Text;

namespace VocaluxeLib
{
    static class CEncoding
    {
        public static Encoding GetEncoding(this string encodingName)
        {
            switch (encodingName)
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
                    return Encoding.Default;
            }
        }

        public static string GetEncodingName(this Encoding enc)
        {
            string result = "UTF8";

            if (enc.CodePage == 1250)
                result = "CP1250";

            if (enc.CodePage == 1252)
                result = "CP1252";

            return result;
        }
    }
}