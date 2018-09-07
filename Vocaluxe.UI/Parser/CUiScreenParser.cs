using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Vocaluxe.UI.AbstractElements;

namespace Vocaluxe.UI.Parser
{
    public static class CUiScreenParser
    {
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
            Dictionary<string, string> attributes = new Dictionary<string, string>();
            foreach (XmlNode childNode in node.ChildNodes)
            {
                if(!(childNode is XmlElement))
                {
                    attributes.Add("value", childNode.Value);
                    continue;
                }
                childElements.Add(_ParseElement((XmlElement)childNode));
            }
      
            
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
