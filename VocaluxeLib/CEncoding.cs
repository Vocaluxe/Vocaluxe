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