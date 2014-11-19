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
using System.Xml.XPath;

namespace VocaluxeLib.Xml
{
    public class CXmlReader : CXmlReaderBase
    {
        private readonly XPathNavigator _Navigator;
        private readonly String _FilePath;

        public string FilePath
        {
            get { return _FilePath; }
        }

        //Private method. Use OpenFile factory method to get an instance
        private CXmlReader(string uri)
        {
            _FilePath = uri;
            var xmlDoc = new XPathDocument(uri);
            _Navigator = xmlDoc.CreateNavigator();
        }

        public XPathNavigator Navigator
        {
            get { return _Navigator; }
        }

        public static CXmlReader OpenFile(string fileName)
        {
            try
            {
                return new CXmlReader(fileName);
            }
            catch (Exception e)
            {
                CBase.Log.LogError("Can't open XML file: " + fileName + ": " + e.Message);
                return null;
            }
        }

        public override bool GetValue(string xPath, out string value, string defaultValue = "")
        {
            int resultCt = 0;
            string val = string.Empty;

            _Navigator.MoveToRoot();
            XPathNodeIterator iterator = _Navigator.Select(xPath);

            while (iterator.MoveNext())
            {
                val = iterator.Current.Value;
                resultCt++;
            }

            if (resultCt != 1)
            {
                value = defaultValue;
                return false;
            }
            value = val;
            return true;
        }

        public override bool GetAttribute(string xPath, string attribute, out string value)
        {
            _Navigator.MoveToRoot();
            XPathNodeIterator iterator = _Navigator.Select(xPath);
            if (iterator.MoveNext())
            {
                value = iterator.Current.GetAttribute(attribute, "");
                return true;
            }
            value = null;
            return false;
        }

        public List<string> GetNames(string xPath)
        {
            var values = new List<string>();

            _Navigator.MoveToRoot();
            XPathNodeIterator iterator = _Navigator.Select(xPath);
            while (iterator.MoveNext())
                values.Add(iterator.Current.LocalName);

            return values;
        }

        public IEnumerable<string> GetAttributes(string xPath, string attribute)
        {
            var values = new List<string>();

            _Navigator.MoveToRoot();
            XPathNodeIterator iterator = _Navigator.Select(xPath);
            while (iterator.MoveNext())
                values.Add(iterator.Current.GetAttribute(attribute, ""));

            return values;
        }

        public bool GetValues(string xPath, ref List<string> values)
        {
            _Navigator.MoveToRoot();
            XPathNodeIterator iterator = _Navigator.Select(xPath);
            while (iterator.MoveNext())
                values.Add(iterator.Current.Value);

            return true;
        }

        public bool ItemExists(string xPath)
        {
            _Navigator.MoveToRoot();

            return _Navigator.Select(xPath).MoveNext();
        }
    }
}