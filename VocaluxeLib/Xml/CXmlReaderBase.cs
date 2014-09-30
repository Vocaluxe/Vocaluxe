using System;

namespace VocaluxeLib.Xml
{
    /// <summary>
    ///     Base class that provides basic methods for reading xml, while the actual XML implementation is still abstract to allow different implementations
    /// </summary>
    public abstract class CXmlReaderBase
    {
        public abstract bool GetValue(string xPath, out string value, string defaultValue = "");
        public abstract bool GetAttribute(string xPath, string attribute, out string value);

        public bool TryGetBoolValue(string xPath, out bool value, bool defaultValue = false)
        {
            value = defaultValue;
            string val;
            return GetValue(xPath, out val) && bool.TryParse(val, out value);
        }

        public bool TryGetIntValue(string xPath, ref int value, int? defaultValue = null)
        {
            string val;
            bool ok = GetValue(xPath, out val) && int.TryParse(val, out value);
            if (!ok && defaultValue.HasValue)
                value = defaultValue.Value;
            return ok;
        }

        public bool TryGetFloatValue(string xPath, ref float value)
        {
            string val;
            return GetValue(xPath, out val, value.ToString()) && CHelper.TryParse(val, out value);
        }

        public bool TryGetEnumValue<T>(string xPath, ref T value) where T : struct
        {
            string val;
            return GetValue(xPath, out val, Enum.GetName(typeof(T), value)) && CHelper.TryParse(val, out value, true);
        }

        public bool TryGetIntValueRange(string xPath, ref int value, int min = 0, int max = 100)
        {
            return TryGetIntValue(xPath, ref value) && value.IsInRange(min, max);
        }

        /// <summary>
        ///     Gets a normalized (0&lt;=x&lt;=1) float value
        /// </summary>
        /// <param name="xPath"></param>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns>True if exists and is normalized</returns>
        public bool TryGetNormalizedFloatValue(string xPath, ref float value, float defaultValue = 0)
        {
            bool ok = TryGetFloatValue(xPath, ref value) && value.IsInRange(0f, 1f);
            if (!ok)
                value = defaultValue;
            return ok;
        }
    }
}