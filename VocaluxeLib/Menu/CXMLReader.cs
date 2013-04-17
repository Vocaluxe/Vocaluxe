using System;
using System.Collections.Generic;
using System.Xml.XPath;

namespace VocaluxeLib.Menu
{
    public class CXMLReader
    {
        private readonly XPathNavigator _Navigator;
        private readonly String _FileName;

        public string FileName
        {
            get { return _FileName; }
        }

        //Private method. Use OpenFile factory method to get an instance
        private CXMLReader(string uri)
        {
            _FileName = uri;
            XPathDocument xmlDoc = new XPathDocument(uri);
            _Navigator = xmlDoc.CreateNavigator();
        }

        public XPathNavigator Navigator
        {
            get { return _Navigator; }
        }

        public static CXMLReader OpenFile(string fileName)
        {
            try
            {
                return new CXMLReader(fileName);
            }
            catch (Exception e)
            {
                CBase.Log.LogError("Can't open XML file: " + fileName + ": " + e.Message);
                return null;
            }
        }

        public bool TryGetEnumValue<T>(string cast, ref T value)
            where T : struct
        {
            string val = String.Empty;
            if (GetValue(cast, ref val, Enum.GetName(typeof(T), value)))
            {
                CHelper.TryParse(val, out value, true);
                return true;
            }
            return false;
        }

        public bool TryGetIntValue(string cast, ref int value)
        {
            string val = String.Empty;
            if (GetValue(cast, ref val, value.ToString()))
                return int.TryParse(val, out value);
            return false;
        }

        public bool TryGetIntValueRange(string cast, ref int value, int min = 0, int max = 100)
        {
            bool result = TryGetIntValue(cast, ref value);
            if (result)
            {
                if (value < min)
                    value = min;
                else if (value > max)
                    value = max;
            }
            return result;
        }

        public bool TryGetFloatValue(string cast, ref float value)
        {
            string val = String.Empty;
            if (GetValue(cast, ref val, value.ToString()))
                return CHelper.TryParse(val, out value);
            return false;
        }

        public bool GetValue(string cast, ref string value, string defaultValue)
        {
            XPathNodeIterator iterator;
            int results = 0;
            string val = string.Empty;

            _Navigator.MoveToFirstChild();
            iterator = _Navigator.Select(cast);

            while (iterator.MoveNext())
            {
                val = iterator.Current.Value;
                results++;
            }

            if ((results == 0) || (results > 1))
            {
                value = defaultValue;
                return false;
            }
            else
            {
                value = val;
                return true;
            }
        }

        public List<string> GetValues(string cast)
        {
            List<string> values = new List<string>();

            _Navigator.MoveToRoot();
            _Navigator.MoveToFirstChild();
            _Navigator.MoveToFirstChild();

            while (_Navigator.Name != cast)
                _Navigator.MoveToNext();

            _Navigator.MoveToFirstChild();

            values.Add(_Navigator.LocalName);
            while (_Navigator.MoveToNext())
                values.Add(_Navigator.LocalName);

            return values;
        }

        public List<string> GetAttributes(string cast, string attribute)
        {
            List<string> values = new List<string>();

            _Navigator.MoveToRoot();
            _Navigator.MoveToFirstChild();

            while (_Navigator.Name != cast)
                _Navigator.MoveToNext();

            _Navigator.MoveToFirstChild();

            values.Add(_Navigator.LocalName);
            while (_Navigator.MoveToNext())
                values.Add(_Navigator.GetAttribute(attribute, ""));

            return values;
        }

        public bool GetInnerValues(string cast, ref List<string> values)
        {
            _Navigator.MoveToRoot();
            _Navigator.MoveToFirstChild();
            _Navigator.MoveToFirstChild();

            while (_Navigator.Name != cast)
                _Navigator.MoveToNext();

            _Navigator.MoveToFirstChild();

            values.Add(_Navigator.Value);
            while (_Navigator.MoveToNext())
                values.Add(_Navigator.Value);

            return true;
        }

        public bool ItemExists(string cast)
        {
            XPathNodeIterator iterator;
            int results = 0;

            _Navigator.MoveToFirstChild();
            iterator = _Navigator.Select(cast);

            while (iterator.MoveNext())
                results++;

            if (results == 0)
                return false;

            return true;
        }
    }
}