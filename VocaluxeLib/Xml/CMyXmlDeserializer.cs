using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace VocaluxeLib.Xml
{
    // ReSharper disable InconsistentNaming
    [AttributeUsage(AttributeTargets.Field)]
    public class XmlNormalizedAttribute : Attribute {}

    // ReSharper restore InconsistentNaming

    public static class CMyXmlDeserializer
    {
        private struct SFieldInfo
        {
            public string Name;
            public FieldInfo Info;
            public bool IsList; //List with child elements (<List><El/><El/></List>)
            public bool IsEmbeddedList; //List w/o child elements(<List/><List/>)
            public bool IsNullable;
            public bool IsNormalized;
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

        private static bool _HasAttribute(this FieldInfo field, Type attributeType)
        {
            return field.GetCustomAttributes(attributeType, false).Length > 0;
        }

        private static List<SFieldInfo> _GetFields(Type type, bool attributes)
        {
            List<SFieldInfo> result = new List<SFieldInfo>();
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                if (field._HasAttribute(typeof(XmlIgnoreAttribute)))
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
                    {
                        attElement = field.GetCustomAttributes(typeof(XmlArrayAttribute), false);
                        if (attElement.Length > 0)
                            info.IsList = true;
                        else
                            info.IsEmbeddedList = true;
                    }
                    else if (field.FieldType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        info.IsNullable = true;
                    info.SubType = field.FieldType.GetGenericArguments()[0];
                }
                if (field._HasAttribute(typeof(XmlNormalizedAttribute)))
                {
                    Debug.Assert(field.FieldType == typeof(float) || (info.IsNullable && info.SubType == typeof(float)), "Only floats can be normalized");
                    info.IsNormalized = true;
                }
                result.Add(info);
            }
            return result;
        }

        private static string _GetTypeName(Type type)
        {
            object[] att = type.GetCustomAttributes(typeof(XmlTypeAttribute), false);
            if (att.Length > 0)
            {
                string name = ((XmlTypeAttribute)att[0]).TypeName;
                if (!string.IsNullOrEmpty(name))
                    return name;
            }
            return type.Name;
        }

        private static object _GetValue(XmlNode node, Type type)
        {
            if (type.IsEnum)
            {
                string stringValue = node.InnerText;
                try
                {
                    return Enum.Parse(type, stringValue);
                }
                catch (Exception)
                {
                    throw new XmlException("Invalid value in " + _FindXPath(node));
                }
            }
            if (type == typeof(string))
                return node.InnerText;
            if (type.IsPrimitive)
                return _GetPrimitiveValue(node, type);
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type subType = type.GetGenericArguments()[0];
                String subName = _GetTypeName(subType);
                List<object> subValues = new List<object>();
                foreach (XmlNode subNode in node.ChildNodes)
                {
                    if (subNode is XmlComment)
                        continue;
                    if (subName != subNode.Name)
                        throw new XmlException("Invalid list entry '" + subNode.Name + "' in " + _FindXPath(node) + "; Expected: " + subName);
                    object subValue = _GetValue(subNode, subType);
                    _ProcessChildNodes(subNode, ref subValue, true);
                    subValues.Add(subValue);
                }
                return _CreateList(type, subValues);
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                Type subType = type.GetGenericArguments()[0];
                return _GetValue(node, subType);
            }

            object value = Activator.CreateInstance(type);
            _ProcessChildNodes(node, ref value, true);
            _ProcessChildNodes(node, ref value, false);
            return value;
        }

        private static object _GetPrimitiveValue(XmlNode node, Type type)
        {
            object value;
            try
            {
                value = Convert.ChangeType(node.InnerText, type, CultureInfo.InvariantCulture);
            }
            catch (FormatException e)
            {
                throw new XmlException("Invalid format in " + _FindXPath(node) + ": '" + node.InnerText + "' (" + e.Message + ")");
            }
            catch (InvalidCastException e)
            {
                throw new XmlException(e.Message + " in " + _FindXPath(node) + ": '" + node.InnerText + "'");
            }
            return value;
        }

        private static bool _CheckAndSetDefaultValue(ref object result, SFieldInfo field)
        {
            if (field.IsEmbeddedList)
            {
                _AddList(result, field, new List<object>());
                return true;
            }
            object[] defAtt = field.Info.GetCustomAttributes(typeof(DefaultValueAttribute), false);
            if (defAtt.Length == 0)
                return field.IsNullable;
            field.Info.SetValue(result, ((DefaultValueAttribute)defAtt[0]).Value);
            return true;
        }

        private static object _CreateList(Type type, List<object> values)
        {
            object list = Activator.CreateInstance(type, new object[] {values.Count});
            if (values.Count > 0)
            {
                MethodInfo addMethod = type.GetMethod("Add");
                foreach (object value in values)
                    addMethod.Invoke(list, new object[] {value});
            }
            return list;
        }

        private static void _AddList(object result, SFieldInfo listField, List<object> list)
        {
            listField.Info.SetValue(result, _CreateList(listField.Info.FieldType, list));
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

            if (nodes != null)
            {
                List<object> subValues = new List<object>();
                SFieldInfo curListField = new SFieldInfo();
                string curListName = null;
                foreach (XmlNode node in nodes)
                {
                    if (node is XmlComment)
                        continue;

                    if (curListName != null)
                    {
                        if (node.Name != curListName)
                        {
                            // End an embedded List
                            _AddList(result, curListField, subValues);
                            curField++;
                            curListName = null;
                        }
                        else
                        {
                            // Continue an embedded List
                            object subValue = _GetValue(node, curListField.SubType);
                            subValues.Add(subValue);
                            continue;
                        }
                    }
                    SFieldInfo field = new SFieldInfo();
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

                    if (field.IsEmbeddedList)
                    {
                        // Start an embedded List
                        curListField = field;
                        curListName = field.Name;
                        subValues.Clear();
                        object subValue = _GetValue(node, curListField.SubType);
                        subValues.Add(subValue);
                    }
                    else
                    {
                        object value = _GetValue(node, field.Info.FieldType);
                        if (field.IsNormalized)
                        {
                            if (field.IsNullable && !((float?)value).Value.IsInRange(0, 1) ||
                                !field.IsNullable && !((float)value).IsInRange(0, 1))
                                throw new XmlException("Value in " + _FindXPath(node) + " is not normalized. (Value=" + value + ")");
                        }
                        field.Info.SetValue(result, value);
                        curField++;
                    }
                }
                if (curListName != null)
                {
                    // End an embedded List
                    _AddList(result, curListField, subValues);
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

        public static T Deserialize<T>(Stream stream) where T : new()
        {
            return Deserialize<T>(new XmlTextReader(stream)
                {
                    WhitespaceHandling = WhitespaceHandling.Significant,
                    Normalization = true,
                    XmlResolver = null
                });
        }

        public static T Deserialize<T>(TextReader textReader) where T : new()
        {
            return Deserialize<T>(new XmlTextReader(textReader)
                {
                    WhitespaceHandling = WhitespaceHandling.Significant,
                    Normalization = true,
                    XmlResolver = null
                });
        }
    }
}