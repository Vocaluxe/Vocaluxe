using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Vocaluxe.UI.AbstractElements;

namespace Vocaluxe.UI.Parser
{
    public static class CUiScreenParser
    {
        public static CUiScreen ParseOld(string path)
        {
            if(!File.Exists(path))
                throw new FileNotFoundException(path);

            using (XmlReader reader = XmlReader.Create(path, new XmlReaderSettings { IgnoreWhitespace = true }))
            {
                return _ParseElementOld(reader)[0].Item1 as CUiScreen ?? throw new ArgumentException("First node must be a screen element");
            }
        }

        private static List<(CUiElement uiElement, string bindingId)> _ParseElementOld(XmlReader reader)
        {
            List<(CUiElement, string)> elementList = new List<(CUiElement uiElement, string bindingId)>();

            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element)
                    continue;

                string elementName = reader.Name;

                var subtreeReader = reader.ReadSubtree();
                subtreeReader.Read();
                List<(CUiElement uiElement, string bindingId)> childElements = _ParseElementOld(subtreeReader);

                Dictionary<string, string> attributes = new Dictionary<string, string>();
                for (var attrIndex = 0; attrIndex < reader.AttributeCount; attrIndex++)
                {
                    reader.MoveToAttribute(attrIndex);
                    attributes.Add(reader.Name, reader.Value);
                }

                attributes.TryGetValue("bindingId", out string bindingId);
                elementList.Add((CUiElementFactory.CreateElement(elementName, attributes, childElements), bindingId));
            }

            reader.Close();
            return elementList;
        }

        public static CUiScreen Parse(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException(path);

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(path);

            return _ParseElement(xmlDoc.DocumentElement).uiElement as CUiScreen ?? throw new ArgumentException("First node must be a screen element");
        }

        private static (CUiElement uiElement, string bindingId) _ParseElement(XmlElement node)
        {
            (CUiElement, string) resultElement = (null,null);
            
            if (node.NodeType != XmlNodeType.Element)
                return resultElement;

            List<(CUiElement uiElement, string bindingId)> childElements = new List<(CUiElement uiElement, string bindingId)>();
            foreach (XmlElement childNode in node.ChildNodes)
            {
                childElements.Add(_ParseElement(childNode));
            }
      
            Dictionary<string, string> attributes = new Dictionary<string, string>();
            foreach (XmlAttribute attribute in node.Attributes)
            {
                attributes.Add(attribute.Name, attribute.Value);
            }

            attributes.TryGetValue("bindingId", out string bindingId);
            resultElement = (CUiElementFactory.CreateElement(node.Name, attributes, childElements), bindingId);
            
            return resultElement;
        }
    }
}
