using System;
using System.Collections;
using System.Collections.Generic;
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
    public delegate string GetCommentDelegate(string name);

    public class CXmlSerializer
    {
        private readonly GetCommentDelegate _GetCommentCallback;
        private readonly bool _WriteDefaults;

        public CXmlSerializer(bool writeDefaults = false, GetCommentDelegate getCommentCallback = null)
        {
            _WriteDefaults = writeDefaults;
            _GetCommentCallback = getCommentCallback;
        }

        /// <summary>
        ///     Uniform settings for writing XML files. ALWAYS use this!
        /// </summary>
        private readonly XmlWriterSettings _XMLSettings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.UTF8,
                ConformanceLevel = ConformanceLevel.Document
            };

        #region Debug Helpers
        private string _GetXPath(XmlNode node)
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

        private string _FindElementIndex(XmlNode element)
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

        #region Deserialization
        private object _GetValue(XmlNode node, Type type, string arrayItemName = null)
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
                    throw new XmlException("Invalid value in " + _GetXPath(node));
                }
            }
            if (type == typeof(string))
                return node.InnerText;
            if (type.IsPrimitive)
                return _GetPrimitiveValue(node, type);
            if (type.IsNullable())
            {
                if (!node.HasChildNodes)
                    return null;
                Type subType = type.GetGenericArguments()[0];
                return _GetValue(node, subType);
            }
            if (type.IsList() || type.IsArray)
            {
                Type subType = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
                String subName = arrayItemName ?? subType.GetTypeName();
                List<object> subValues = new List<object>();
                foreach (XmlNode subNode in node.ChildNodes)
                {
                    if (subNode is XmlComment)
                        continue;
                    if (subName != subNode.Name && !subNode.Name.StartsWith(subName))
                        throw new XmlException("Invalid list entry '" + subNode.Name + "' in " + _GetXPath(node) + "; Expected: " + subName);
                    object subValue = _GetValue(subNode, subType);
                    subValues.Add(subValue);
                }
                return _CreateList(type, subValues);
            }
            if (type.IsDictionary())
            {
                Type subType = type.GetGenericArguments()[1];
                object dict = Activator.CreateInstance(type);
                MethodInfo add = type.GetMethod("Add");
                foreach (XmlNode subNode in node.ChildNodes)
                {
                    if (subNode is XmlComment)
                        continue;
                    object subValue = _GetValue(subNode, subType);
                    add.Invoke(dict, new object[] {subNode.Name, subValue});
                }
                return dict;
            }

            object value;
            try
            {
                value = Activator.CreateInstance(type);
            }
            catch (Exception)
            {
                throw new XmlException("Could not create instance of " + _GetXPath(node) + "(Type=" + type.Name + ")");
            }
            _ReadChildNodes(node, ref value, true);
            _ReadChildNodes(node, ref value, false);
            return value;
        }

        private object _GetPrimitiveValue(XmlNode node, Type type)
        {
            object value;
            string nodeVal = node.InnerText;
            try
            {
                int p = nodeVal.IndexOf(',');
                if (p > 0 && p >= nodeVal.Length - 3)
                {
                    CBase.Log.LogError("German number format converted to english in " + _GetXPath(node));
                    char[] tmp = nodeVal.ToCharArray();
                    tmp[p] = '.';
                    nodeVal = new string(tmp);
                }
                value = Convert.ChangeType(nodeVal, type, CultureInfo.InvariantCulture);
            }
            catch (FormatException e)
            {
                throw new XmlException("Invalid format in " + _GetXPath(node) + ": '" + nodeVal + "' (" + e.Message + ")");
            }
            catch (InvalidCastException e)
            {
                throw new XmlException(e.Message + " in " + _GetXPath(node) + ": '" + nodeVal + "'");
            }
            return value;
        }

        private static bool _CheckAndSetDefaultValue(object result, SFieldInfo field)
        {
            if (field.IsEmbeddedList)
            {
                _AddList(result, field, new List<object>());
                return true;
            }
            if (!field.HasDefaultValue)
                return false;
            field.Info.SetValue(result, field.DefaultValue);
            return true;
        }

        private static object _CreateList(Type type, ICollection values)
        {
            if (type.IsArray)
            {
                Array array = Array.CreateInstance(type.GetElementType(), values.Count);
                int i = 0;
                foreach (object value in values)
                    array.SetValue(value, i++);
                return array;
            }
            object list = Activator.CreateInstance(type, new object[] {values.Count});
            if (values.Count > 0)
            {
                MethodInfo addMethod = type.GetMethod("Add");
                foreach (object value in values)
                    addMethod.Invoke(list, new object[] {value});
            }
            return list;
        }

        private static void _AddList(object result, SFieldInfo listField, ICollection list)
        {
            listField.Info.SetValue(result, _CreateList(listField.Info.FieldType, list));
        }

        private void _ReadChildNodes(XmlNode parent, ref object result, bool attributes)
        {
            IEnumerable nodes;
            if (attributes)
                nodes = parent.Attributes;
            else
                nodes = parent.ChildNodes;

            List<SFieldInfo> fields = result.GetType().GetFields(attributes);
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
                        if (node.Name != curListName && !node.Name.StartsWith(curListField.Name))
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
                        if (field.Name == node.Name || field.AltName == node.Name || (field.IsEmbeddedList && node.Name.StartsWith(field.Name)))
                            break;
                        if (!_CheckAndSetDefaultValue(result, field))
                            throw new XmlException("Unexpected element: " + _GetXPath(node) + "; Expected: " + field.Name);
                    }
                    if (curField >= fields.Count)
                        throw new XmlException("Unexpected element: " + _GetXPath(node));

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
                        object value = _GetValue(node, field.Info.FieldType, field.ArrayItemName);
                        if (field.IsNormalized)
                        {
                            if (field.IsNullable && !((float?)value).Value.IsInRange(0, 1) ||
                                !field.IsNullable && !((float)value).IsInRange(0, 1))
                                throw new XmlException("Value in " + _GetXPath(node) + " is not normalized. (Value=" + value + ")");
                        }
                        if (field.Ranged != null && value != null)
                        {
                            bool ok = false;
                            if (field.Info.FieldType == typeof(int))
                                ok = ((int)value).IsInRange(field.Ranged.Min, field.Ranged.Max);
                            else if (field.Info.FieldType == typeof(float))
                                ok = ((float)value).IsInRange(field.Ranged.Min, field.Ranged.Max);
                            else if (field.Info.FieldType == typeof(double))
                                ok = ((double)value).IsInRange(field.Ranged.Min, field.Ranged.Max);
                            else
                                Debug.Assert(false, "Ranged attribute has invalid type");
                            if (!ok)
                            {
                                throw new XmlException("Value in " + _GetXPath(node) + " is not in the range " + field.Ranged.Min + "-" + field.Ranged.Max + ". (Value=" + value +
                                                       ")");
                            }
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
                if (!_CheckAndSetDefaultValue(result, field))
                    throw new XmlException("element: " + field.Name + " is missing in " + _GetXPath(parent));
            }
        }
        #endregion Deserialization

        #region Serialization
        private void _WriteNode(XmlWriter writer, string name, Type type, object value, bool isAttribute, string arrayItemName = null)
        {
            if (!isAttribute && _GetCommentCallback != null)
            {
                string comment = _GetCommentCallback(name);
                if (!string.IsNullOrEmpty(comment))
                    writer.WriteComment(comment);
            }
            if (type.IsEnum || type == typeof(string) || type.IsPrimitive)
            {
                string strVal;
                try
                {
                    strVal = (string)Convert.ChangeType(value, typeof(string), CultureInfo.InvariantCulture);
                }
                catch (Exception e)
                {
                    throw new XmlException("Cannot convert value of type " + type.Name + " to string in node " + name + " (" + e.Message + ")");
                }
                if (isAttribute)
                    writer.WriteAttributeString(name, strVal);
                else
                    writer.WriteElementString(name, strVal);
            }
            else if (type.IsNullable())
                _WriteNode(writer, name, type.GetGenericArguments()[0], value, isAttribute, arrayItemName);
            else if (type.IsList() || type.IsArray)
            {
                Debug.Assert(!isAttribute, "Lists cannot be attributes");
                writer.WriteStartElement(name);
                Type subType = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
                String subName = arrayItemName ?? subType.GetTypeName();
                IEnumerable list = (IEnumerable)value;
                foreach (object subValue in list)
                    _WriteNode(writer, subName, subType, subValue, false);
                writer.WriteEndElement();
            }
            else if (type.IsDictionary())
            {
                Debug.Assert(!isAttribute, "Dictionaries cannot be attributes");
                writer.WriteStartElement(name);
                Type subType = type.GetGenericArguments()[1];
                IDictionary dict = (IDictionary)value;
                foreach (DictionaryEntry entry in dict)
                    _WriteNode(writer, (string)entry.Key, subType, entry.Value, false);
                writer.WriteEndElement();
            }
            else
            {
                Debug.Assert(!isAttribute, "Complex types cannot be attributes");
                writer.WriteStartElement(name);
                if (value != null)
                    _WriteChildNodes(writer, value);
                writer.WriteEndElement();
            }
        }

        private void _WriteChildNodes(XmlWriter writer, object o)
        {
            IEnumerable<SFieldInfo> fields = o.GetType().GetFieldInfos();
            foreach (SFieldInfo field in fields)
            {
                object value = field.Info.GetValue(o);
                if (!_WriteDefaults && field.HasDefaultValue && Equals(value, field.DefaultValue))
                    continue;
                if (field.IsEmbeddedList)
                {
                    IEnumerable values = (IEnumerable)value;
                    foreach (object subValue in values)
                        _WriteNode(writer, field.Name, field.SubType, subValue, field.IsAttribute);
                }
                else
                    _WriteNode(writer, field.Name, field.Info.FieldType, value, field.IsAttribute, field.ArrayItemName);
            }
        }
        #endregion

        private T _Deserialize<T>(XmlReader reader) where T : new()
        {
            object result = new T();
            if (reader.IsEmptyElement)
                return (T)result;

            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(reader);
            if (xDoc.DocumentElement == null)
                throw new XmlException("No root element found!");
            _ReadChildNodes(xDoc.DocumentElement, ref result, false);
            return (T)result;
        }

        public T DeserializeString<T>(string xml) where T : new()
        {
            var reader = new XmlTextReader(new StringReader(xml))
                {
                    WhitespaceHandling = WhitespaceHandling.Significant,
                    Normalization = true,
                    XmlResolver = null
                };
            try
            {
                return _Deserialize<T>(reader);
            }
            finally
            {
                reader.Close();
            }
        }

        public T Deserialize<T>(string filePath) where T : new()
        {
            var reader = new XmlTextReader(filePath)
                {
                    WhitespaceHandling = WhitespaceHandling.Significant,
                    Normalization = true,
                    XmlResolver = null
                };
            try
            {
                return _Deserialize<T>(reader);
            }
            finally
            {
                reader.Close();
            }
        }

        private void _Serialize(object o, XmlWriter writer)
        {
            XmlRootAttribute root = o.GetType().GetAttribute<XmlRootAttribute>();
            string name;
            if (root != null && !string.IsNullOrEmpty(root.ElementName))
                name = root.ElementName;
            else
            {
                XmlTypeAttribute typeAtt = o.GetType().GetAttribute<XmlTypeAttribute>();
                if (typeAtt != null && !string.IsNullOrEmpty(typeAtt.TypeName))
                    name = typeAtt.TypeName;
                else
                    name = "root";
            }
            writer.WriteStartDocument();
            writer.WriteStartElement(name);
            _WriteChildNodes(writer, o);
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();
        }

        public void Serialize(string filePath, object o)
        {
            using (XmlWriter writer = XmlWriter.Create(filePath, _XMLSettings))
                _Serialize(o, writer);
        }

        public string Serialize(object o)
        {
            MemoryStream result = new MemoryStream();
            using (XmlWriter writer = XmlWriter.Create(result, _XMLSettings))
                _Serialize(o, writer);
            result.Position = 0;
            return new StreamReader(result).ReadToEnd();
        }
    }
}