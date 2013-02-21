using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.XPath;

namespace Vocaluxe.Menu
{
    public class CXMLReader
    {
        private XPathNavigator _Navigator;

        public CXMLReader(string uri)
        {
            XPathDocument xPathDoc = new XPathDocument(uri);
            _Navigator = xPathDoc.CreateNavigator();
        }

        public XPathNavigator Navigator
        {
            get { return _Navigator; }
        }

        public static CXMLReader OpenFile(string sUri)
        {
            try
            {
                return new CXMLReader(sUri);
            }
            catch (Exception)
            {
                //TODO: How to log from here?
                return null;
            }
        }
        
        public bool TryGetEnumValue<T>(string Cast, ref T value)
            where T : struct
        {
            string val = String.Empty;
            if (GetValue(Cast, ref val, Enum.GetName(typeof(T), value)))
            {
                CHelper.TryParse<T>(val, out value, true);
                return true;
            }
            return false;
        }

        public bool TryGetIntValue(string Cast, ref int value)
        {
            string val = String.Empty;
            if (GetValue(Cast, ref val, value.ToString()))
            {
                return int.TryParse(val, out value);
            }
            return false;
        }

        public bool TryGetIntValueRange(string Cast,ref int value, int min = 0, int max = 100)
        {
            bool result = TryGetIntValue(Cast, ref value);
            if (result)
            {
                if (value < min)
                    value = min;
                else if (value > max)
                    value = max;
            }
            return result;
        }

        public bool TryGetFloatValue(string Cast, ref float value)
        {
            string val = String.Empty;
            if (GetValue(Cast, ref val, value.ToString()))
            {
                return CHelper.TryParse(val, out value);
            }
            return false;
        }

        public bool GetValue(string Cast, ref string Value, string DefaultValue)
        {
            XPathNodeIterator iterator;
            int results = 0;
            string val = string.Empty;

            try
            {
                _Navigator.MoveToFirstChild();
                iterator = _Navigator.Select(Cast);

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

        public List<string> GetValues(string Cast)
        {
            List<string> values = new List<string>();

            try
            {
                _Navigator.MoveToRoot();
                _Navigator.MoveToFirstChild();
                _Navigator.MoveToFirstChild();

                while (_Navigator.Name != Cast)
                    _Navigator.MoveToNext();

                _Navigator.MoveToFirstChild();

                values.Add(_Navigator.LocalName);
                while (_Navigator.MoveToNext())
                    values.Add(_Navigator.LocalName);

            }
            catch (Exception)
            {

            }

            return values;
        }

        public List<string> GetAttributes(string Cast, string attribute)
        {
            List<string> values = new List<string>();

            try
            {
                _Navigator.MoveToRoot();
                _Navigator.MoveToFirstChild();

                while (_Navigator.Name != Cast)
                    _Navigator.MoveToNext();

                _Navigator.MoveToFirstChild();

                values.Add(_Navigator.LocalName);
                while (_Navigator.MoveToNext())
                    values.Add(_Navigator.GetAttribute(attribute, ""));

            }
            catch (Exception)
            {

            }

            return values;
        }

        public bool GetInnerValues(string Cast, ref List<string> Values)
        {
            try
            {
                _Navigator.MoveToRoot();
                _Navigator.MoveToFirstChild();
                _Navigator.MoveToFirstChild();

                while (_Navigator.Name != Cast)
                    _Navigator.MoveToNext();

                _Navigator.MoveToFirstChild();

                Values.Add(_Navigator.Value);
                while (_Navigator.MoveToNext())
                    Values.Add(_Navigator.Value);

            }
            catch
            {
                return false;
            }

            return true;
        }

        public bool ItemExists(string Cast)
        {
            XPathNodeIterator iterator;
            int results = 0;

            try
            {
                _Navigator.MoveToFirstChild();
                iterator = _Navigator.Select(Cast);

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
    }
}
