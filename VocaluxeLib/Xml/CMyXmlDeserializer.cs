using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace VocaluxeLib.Xml
{
    public static class CMyXmlDeserializer
    {
        private struct SFieldInfo
        {
            public string Name;
            public FieldInfo Info;
            public bool IsList;
            public bool IsNullable;
            public Type SubType;
        }

        #region Debug Helpers
        private static string _FindXPath(XmlNode node)
        {
            StringBuilder builder = new StringBuilder();
            while (node != null)
            {
                switch (node.NodeType)
                {
                    case XmlNodeType.Attribute:
                        builder.Insert(0, "/@" + node.Name);
                        node = ((XmlAttribute)node).OwnerElement;
                        break;
                    case XmlNodeType.Element:
                        string index = _FindElementIndex(node);
                        builder.Insert(0, "/" + node.Name + index);
                        node = node.ParentNode;
                        break;
                    case XmlNodeType.Document:
                        return builder.ToString();
                    default:
                        throw new ArgumentException("Only elements and attributes are supported");
                }
            }
            throw new ArgumentException("Node was not in a document");
        }

        private static string _FindElementIndex(XmlNode element)
        {
            XmlNode parentNode = element.ParentNode;
            if (parentNode is XmlDocument)
                return "";
            XmlElement parent = (XmlElement)parentNode;
            int index = 1;
            if (parent != null)
            {
                XmlNode[] siblings = parent.ChildNodes.Cast<XmlNode>().Where(candidate => candidate is XmlElement && candidate.Name == element.Name).ToArray();
                foreach (XmlNode candidate in siblings)
                {
                    if (candidate == element)
                        return siblings.Length > 1 ? "[" + index + "]" : "";
                    index++;
                }
            }
            throw new ArgumentException("Couldn't find element within parent");
        }
        #endregion Debug Helpers

        private static List<SFieldInfo> _GetFields(Type type, bool attributes)
        {
            List<SFieldInfo> result = new List<SFieldInfo>();
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                if (field.GetCustomAttributes(typeof(XmlIgnoreAttribute), false).Length > 0)
                    continue;
                object[] attElement = field.GetCustomAttributes(typeof(XmlAttributeAttribute), false);
                if (attElement.Length > 0 != attributes)
                    continue;
                SFieldInfo info = new SFieldInfo {Info = field};
                if (attElement.Length > 0)
                    info.Name = ((XmlAttributeAttribute)attElement[0]).AttributeName;
                else
                {
                    attElement = field.GetCustomAttributes(typeof(XmlElementAttribute), false);
                    if (attElement.Length > 0)
                        info.Name = ((XmlElementAttribute)attElement[0]).ElementName;
                }
                if (string.IsNullOrEmpty(info.Name))
                    info.Name = field.Name;
                if (field.FieldType.IsGenericType)
                {
                    if (field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                        info.IsList = true;
                    else if (field.FieldType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        info.IsNullable = true;
                    info.SubType = field.FieldType.GetGenericArguments()[0];
                }
                result.Add(info);
            }
            return result;
        }

        public static T Deserialize<T>(Stream stream) where T : new()
        {
            return Deserialize<T>(new XmlTextReader(stream)
                {
                    WhitespaceHandling = WhitespaceHandling.Significant,
                    Normalization = true,
                    XmlResolver = null
                });
        }

        private static object _GetValue(XmlNode node, Type type)
        {
            object value;
            if (type.IsEnum)
            {
                string stringValue = node.InnerText;
                try
                {
                    value = Enum.Parse(type, stringValue);
                }
                catch (Exception)
                {
                    throw new XmlException("Invalid value in " + _FindXPath(node));
                }
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type subType = type.GetGenericArguments()[0];
                value = Activator.CreateInstance(type);
                MethodInfo addMethod = value.GetType().GetMethod("Add");
                List<object> subValues = new List<object>(node.ChildNodes.Count);
                foreach (XmlNode subNode in node.ChildNodes)
                {
                    if (subNode is XmlComment)
                        continue;
                    object subValue = _GetValue(subNode, subType);
                    _ProcessChildNodes(subNode, ref subValue, true);
                    subValues.Add(subValue);
                }
                addMethod.Invoke(value, subValues.ToArray());
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                Type subType = type.GetGenericArguments()[0];
                string stringValue = node.InnerText;
                value = Convert.ChangeType(stringValue, subType);
            }
            else if (type == typeof(string))
                value = node.InnerText;
            else if (!type.IsPrimitive)
            {
                value = Activator.CreateInstance(type);
                _ProcessChildNodes(node, ref value, true);
                _ProcessChildNodes(node, ref value, false);
            }
            else
            {
                string stringValue = node.InnerText;
                try
                {
                    value = Convert.ChangeType(stringValue, type, CultureInfo.InvariantCulture);
                }
                catch (FormatException)
                {
                    throw new XmlException("Invalid format in " + _FindXPath(node));
                }
            }
            return value;
        }

        private static bool _CheckAndSetDefaultValue(ref object result, SFieldInfo field)
        {
            object[] defAtt = field.Info.GetCustomAttributes(typeof(DefaultValueAttribute), false);
            if (defAtt.Length == 0)
                return field.IsNullable;
            field.Info.SetValue(result, ((DefaultValueAttribute)defAtt[0]).Value);
            return true;
        }

        private static void _ProcessChildNodes(XmlNode parent, ref object result, bool attributes)
        {
            IEnumerable nodes;
            if (attributes)
                nodes = parent.Attributes;
            else
                nodes = parent.ChildNodes;

            List<SFieldInfo> fields = _GetFields(result.GetType(), attributes);
            int curField = 0;

            List<object> subValues = new List<object>();
            SFieldInfo curListField = new SFieldInfo();
            string curListName = null;
            if (nodes != null)
            {
                foreach (XmlNode node in nodes)
                {
                    if (node is XmlComment)
                        continue;

                    if (curListName != null)
                    {
                        if (node.Name != curListName)
                        {
                            // End an embedded List
                            object list = Activator.CreateInstance(curListField.Info.FieldType);
                            MethodInfo addMethod = curListField.Info.FieldType.GetMethod("Add");
                            addMethod.Invoke(list, subValues.ToArray());
                            curListField.Info.SetValue(result, list);
                            curField++;
                            curListName = null;
                        }
                    }
                    SFieldInfo field = new SFieldInfo();
                    if (curListName == null)
                    {
                        for (; curField < fields.Count; curField++)
                        {
                            field = fields[curField];
                            if (field.Name == node.Name)
                                break;
                            if (!_CheckAndSetDefaultValue(ref result, field))
                                throw new XmlException("Unexpected element: " + _FindXPath(node) + "; Expected: " + field.Name);
                        }
                        if (curField >= fields.Count)
                            throw new XmlException("Unexpected element: " + _FindXPath(node));

                        if (field.IsList)
                        {
                            // Start an embedded List
                            curListField = field;
                            curListName = field.Name;
                            subValues.Clear();
                        }
                    }
                    if (curListName == null)
                    {
                        object value = _GetValue(node, field.Info.FieldType);
                        field.Info.SetValue(result, value);
                        curField++;
                    }
                    else
                    {
                        // Continue an embedded List
                        object value = _GetValue(node, field.SubType);
                        subValues.Add(value);
                    }
                }
                if (curListName != null)
                {
                    // End an embedded List
                    object list = Activator.CreateInstance(curListField.Info.FieldType);
                    MethodInfo addMethod = curListField.Info.FieldType.GetMethod("Add");
                    addMethod.Invoke(list, subValues.ToArray());
                    curListField.Info.SetValue(result, list);
                    curField++;
                }
            }
            for (; curField < fields.Count; curField++)
            {
                SFieldInfo field = fields[curField];
                if (!_CheckAndSetDefaultValue(ref result, field))
                    throw new XmlException("element: " + field.Name + " is missing in " + _FindXPath(parent));
            }
        }

        public static T Deserialize<T>(XmlReader reader) where T : new()
        {
            object result = new T();
            if (reader.IsEmptyElement)
                return (T)result;

            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(reader);
            if (xDoc.DocumentElement == null)
                throw new XmlException("No root element found!");
            _ProcessChildNodes(xDoc.DocumentElement, ref result, false);
            return (T)result;
        }
    }
}