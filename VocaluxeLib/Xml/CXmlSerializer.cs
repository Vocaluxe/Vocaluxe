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
        private object _GetValue(XmlNode node, Type type, string arrayItemName = null, object result = null)
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
                return _GetValue(node, subType, arrayItemName, result);
            }
            if (type.IsList() || type.IsArray)
            {
                Type subType = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
                String subName = arrayItemName ?? subType.GetTypeName();
                subName = subName.ToLowerInvariant();
                List<object> subValues = new List<object>();
                foreach (XmlNode subNode in node.ChildNodes)
                {
                    if (subNode is XmlComment)
                        continue;
                    if (subName == subNode.Name.ToLowerInvariant() && !subNode.Name.ToLowerInvariant().StartsWith(subName))
                        throw new XmlException("Invalid list entry '" + subNode.Name + "' in " + _GetXPath(node) + "; Expected: " + subName);
                    object subValue = _GetValue(subNode, subType);
                    subValues.Add(subValue);
                }
                if (result == null)
                    return _CreateList(type, subValues);
                _FillList(result, type, subValues);
                return result;
            }
            if (type.IsDictionary())
            {
                Type subType = type.GetGenericArguments()[1];
                object dict = result ?? Activator.CreateInstance(type);
                MethodInfo add = type.GetMethod("Add");
                if (arrayItemName != null)
                    arrayItemName = arrayItemName.ToLowerInvariant();
                foreach (XmlNode subNode in node.ChildNodes)
                {
                    if (subNode is XmlComment)
                        continue;
                    object subValue = _GetValue(subNode, subType);
                    string subName;
                    if (arrayItemName != null)
                    {
                        if (arrayItemName != subNode.Name.ToLowerInvariant() && !subNode.Name.ToLowerInvariant().StartsWith(arrayItemName))
                            throw new XmlException("Invalid dictionary entry '" + subNode.Name + "' in " + _GetXPath(node) + "; Expected: " + arrayItemName);
                        if (subNode.Attributes == null)
                            throw new XmlException("'name' attribute is missing in " + _GetXPath(subNode));
                        XmlNode nameAtt = subNode.Attributes.GetNamedItem("name");
                        if (nameAtt == null)
                            throw new XmlException("'name' attribute is missing in " + _GetXPath(subNode));
                        subName = nameAtt.Value;
                    }
                    else
                        subName = subNode.Name;
                    add.Invoke(dict, new object[] {subName, subValue});
                }
                return dict;
            }

            object value;
            try
            {
                value = result ?? Activator.CreateInstance(type);
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
            field.SetValue(result, field.DefaultValue);
            return true;
        }

        private static void _FillList(object list, Type type, ICollection values)
        {
            if (values.Count <= 0)
                return;
            if (type.IsArray)
            {
                Array array = (Array)list;
                int i = 0;
                foreach (object value in values)
                    array.SetValue(value, i++);
            }
            else
            {
                MethodInfo addMethod = type.GetMethod("Add");
                foreach (object value in values)
                    addMethod.Invoke(list, new object[] {value});
            }
        }

        private static object _CreateList(Type type, ICollection values)
        {
            object list = type.IsArray ? Array.CreateInstance(type.GetElementType(), values.Count) : Activator.CreateInstance(type, new object[] {values.Count});
            _FillList(list, type, values);
            return list;
        }

        private static void _AddList(object result, SFieldInfo listField, ICollection list)
        {
            listField.SetValue(result, _CreateList(listField.Type, list));
        }

        private void _ReadChildNodes(XmlNode parent, ref object result, bool attributes)
        {
            IEnumerable nodes;
            if (attributes)
                nodes = parent.Attributes;
            else
                nodes = parent.ChildNodes;

            List<SFieldInfo> fields = result.GetType().GetFields(attributes);

            if (nodes != null)
            {
                //Dictionary of all embedded lists to allow interleaved/mixed elements
                Dictionary<string, Tuple<SFieldInfo, List<object>>> embLists = new Dictionary<string, Tuple<SFieldInfo, List<object>>>();
                foreach (XmlNode node in nodes)
                {
                    if (node is XmlComment)
                        continue;

                    SFieldInfo field = new SFieldInfo();
                    int curField;
                    for (curField = 0; curField < fields.Count; curField++)
                    {
                        field = fields[curField];
                        if (field.Name == node.Name || field.AltName == node.Name || (field.IsEmbeddedList && node.Name.StartsWith(field.Name)))
                            break;
                    }
                    if (curField >= fields.Count)
                    {
                        string msg = "Unexpected element: " + _GetXPath(node);
                        if (fields.Count > 0)
                            msg += " Expected: " + string.Join(", ", fields.Select(f => f.Name));
                        throw new XmlException(msg);
                    }

                    if (field.IsByteArray)
                    {
                        field.SetValue(result, Convert.FromBase64String(node.InnerText));
                        fields.RemoveAt(curField);
                    }
                    else if (field.IsEmbeddedList)
                    {
                        Tuple<SFieldInfo, List<object>> entry;
                        if (!embLists.TryGetValue(field.Name, out entry))
                        {
                            entry = new Tuple<SFieldInfo, List<object>>(field, new List<object>());
                            embLists.Add(field.Name, entry);
                        }
                        object subValue = _GetValue(node, field.SubType);
                        entry.Item2.Add(subValue);
                    }
                    else
                    {
                        object value = _GetValue(node, field.Type, field.ArrayItemName);
                        if (field.IsNormalized)
                        {
                            if (field.IsNullable && !((float?)value).Value.IsInRange(0, 1) ||
                                !field.IsNullable && !((float)value).IsInRange(0, 1))
                                throw new XmlException("Value in " + _GetXPath(node) + " is not normalized. (Value=" + value + ")");
                        }
                        if (field.Ranged != null && value != null)
                        {
                            bool ok = false;
                            if (field.Type == typeof(int))
                                ok = ((int)value).IsInRange(field.Ranged.Min, field.Ranged.Max);
                            else if (field.Type == typeof(float))
                                ok = ((float)value).IsInRange(field.Ranged.Min, field.Ranged.Max);
                            else if (field.Type == typeof(double))
                                ok = ((double)value).IsInRange(field.Ranged.Min, field.Ranged.Max);
                            else
                                Debug.Assert(false, "Ranged attribute has invalid type");
                            if (!ok)
                            {
                                throw new XmlException("Value in " + _GetXPath(node) + " is not in the range " + field.Ranged.Min + "-" + field.Ranged.Max + ". (Value=" + value +
                                                       ")");
                            }
                        }
                        field.SetValue(result, value);
                        fields.RemoveAt(curField);
                    }
                }
                //Add embedded lists
                foreach (Tuple<SFieldInfo, List<object>> entry in embLists.Values)
                {
                    _AddList(result, entry.Item1, entry.Item2);
                    fields.Remove(entry.Item1);
                }
            }
            foreach (SFieldInfo field in fields)
            {
                if (!_CheckAndSetDefaultValue(result, field))
                    throw new XmlException("element: " + field.Name + " is missing in " + _GetXPath(parent));
            }
        }
        #endregion Deserialization

        #region Serialization
        private void _WriteNode(XmlWriter writer, string name, Type type, object value, bool isAttribute, string arrayItemName = null, string nameAttribute = null)
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
                {
                    writer.WriteStartElement(name);
                    if (nameAttribute != null)
                        writer.WriteAttributeString("name", nameAttribute);
                    if (!string.IsNullOrEmpty(strVal))
                        writer.WriteValue(strVal);
                    writer.WriteEndElement();
                }
            }
            else if (type.IsNullable())
                _WriteNode(writer, name, type.GetGenericArguments()[0], value, isAttribute, arrayItemName, nameAttribute);
            else if (type.IsList() || type.IsArray)
            {
                Debug.Assert(!isAttribute, "Lists cannot be attributes");
                writer.WriteStartElement(name);
                if (nameAttribute != null)
                    writer.WriteAttributeString("name", nameAttribute);
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
                if (nameAttribute != null)
                    writer.WriteAttributeString("name", nameAttribute);
                Type subType = type.GetGenericArguments()[1];
                IDictionary dict = (IDictionary)value;
                foreach (DictionaryEntry entry in dict)
                {
                    string subName;
                    string subNameAttribute;
                    if (arrayItemName == null)
                    {
                        subName = (string)entry.Key;
                        subNameAttribute = null;
                    }
                    else
                    {
                        subName = arrayItemName;
                        subNameAttribute = (string)entry.Key;
                    }
                    _WriteNode(writer, subName, subType, entry.Value, false, null, subNameAttribute);
                }
                writer.WriteEndElement();
            }
            else
            {
                Debug.Assert(!isAttribute, "Complex types cannot be attributes");
                writer.WriteStartElement(name);
                if (nameAttribute != null)
                    writer.WriteAttributeString("name", nameAttribute);
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
                object value = field.GetValue(o);
                if (!_WriteDefaults && field.HasDefaultValue && Equals(value, field.DefaultValue))
                    continue;
                if (field.IsEmbeddedList)
                {
                    IEnumerable values = (IEnumerable)value;
                    foreach (object subValue in values)
                        _WriteNode(writer, field.Name, field.SubType, subValue, field.IsAttribute);
                }
                else if (field.IsByteArray)
                    writer.WriteElementString(field.Name, Convert.ToBase64String((byte[])value));
                else
                    _WriteNode(writer, field.Name, field.Type, value, field.IsAttribute, field.ArrayItemName);
            }
        }
        #endregion

        private T _Deserialize<T>(XmlReader reader, object result) where T : new()
        {
            if (reader.IsEmptyElement)
                return (T)result;

            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(reader);
            if (xDoc.DocumentElement == null)
                throw new XmlException("No root element found!");
            try
            {
                result = _GetValue(xDoc.DocumentElement, typeof(T), typeof(T).GetSubTypeName(), result);
            }
            catch (TargetInvocationException e)
            {
                throw e.InnerException ?? e;
            }
            return (T)result;
        }

        public T DeserializeString<T>(string xml) where T : new()
        {
            return DeserializeString(xml, new T());
        }

        public T DeserializeString<T>(string xml, T o) where T : new()
        {
            var reader = new XmlTextReader(new StringReader(xml))
                {
                    WhitespaceHandling = WhitespaceHandling.Significant,
                    Normalization = true,
                    XmlResolver = null
                };
            try
            {
                return _Deserialize<T>(reader, o);
            }
            finally
            {
                reader.Close();
            }
        }

        public T Deserialize<T>(string filePath) where T : new()
        {
            return Deserialize(filePath, new T());
        }

        public T Deserialize<T>(string filePath, T o) where T : new()
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException(filePath);
            var reader = new XmlTextReader(filePath)
                {
                    WhitespaceHandling = WhitespaceHandling.Significant,
                    Normalization = true,
                    XmlResolver = null
                };
            try
            {
                return _Deserialize<T>(reader, o);
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
            try
            {
                writer.WriteStartDocument();
                _WriteNode(writer, name, o.GetType(), o, false, o.GetType().GetSubTypeName());
                writer.WriteEndDocument();
            }
            finally
            {
                writer.Flush();
            }
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