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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using VocaluxeLib.Log;

namespace VocaluxeLib.Xml
{
    public class CXmlException : Exception
    {
        #region Debug Helpers
        /// <summary>
        ///     Returns the xPath from root to the node
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
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

        /// <summary>
        ///     Returns an index as a string or empty string if the node name is sufficient to identify the node
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
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

        public readonly XmlNode Node;
        public readonly bool IsError;

        public CXmlException(string msg, XmlNode node, bool isError = true)
            : base(msg)
        {
            IsError = isError;
            Node = node;
        }

        public override string Message
        {
            get { return ToString(); }
        }

        public override String ToString()
        {
            string xPath = Node == null ? "" : _GetXPath(Node);
            string type = IsError ? "Error" : "Warning";
            return type + ": " + base.Message.Replace("%n", xPath);
        }
    }

    public class CXmlMissingElementException : CXmlException
    {
        public readonly SFieldInfo Field;

        public CXmlMissingElementException(XmlNode parent, SFieldInfo field, bool isError = true) : base("Element: " + field.Name + " is missing in %n", parent, isError)
        {
            Field = field;
        }
    }

    public class CXmlInvalidValueException : CXmlException
    {
        private readonly string _Value;

        public CXmlInvalidValueException(string msg, XmlNode node, string value, bool isError = true) : base(msg, node, isError)
        {
            _Value = value;
        }

        public override String ToString()
        {
            return base.ToString().Replace("%v", _Value);
        }
    }

    public interface IXmlErrorHandler
    {
        /// <summary>
        ///     Gets called for every error that occurs during xml reading.
        ///     Recommended way of handling is throwing that error and log it<br />
        ///     However if the function returns without throwing a fallback solution is applied to continue reading where possible e.g. by using default values or ignoring the element
        /// </summary>
        /// <param name="e">Error that would be thrown</param>
        void HandleError(CXmlException e);
    }

    /// <summary>
    ///     Class for simple construction of error handlers by using a delegate
    /// </summary>
    public class CXmlErrorHandler : IXmlErrorHandler
    {
        public delegate void HandleErrorDelegate(CXmlException e);

        private readonly HandleErrorDelegate _HandleError;

        public CXmlErrorHandler(HandleErrorDelegate handleError)
        {
            _HandleError = handleError;
        }

        public void HandleError(CXmlException e)
        {
            _HandleError(e);
        }
    }

    public class CXmlDeserializer
    {
        public class CXmlDefaultErrorHandler : IXmlErrorHandler
        {
            public virtual void HandleError(CXmlException e)
            {
                if (e.IsError)
                    throw e;
                CLog.Error(e.ToString());
            }
        }

        private readonly IXmlErrorHandler _ErrorHandler;

        /// <summary>
        ///     Creates a new XmlDeserializer
        /// </summary>
        /// <param name="errorHandler">Class to be used for error callbacks, if not given a default handler will be used which throws errors and logs warnings</param>
        public CXmlDeserializer(IXmlErrorHandler errorHandler = null)
        {
            _ErrorHandler = errorHandler ?? new CXmlDefaultErrorHandler();
        }

        /// <summary>
        ///     Returns the value if the given node
        /// </summary>
        /// <param name="node">Node to process</param>
        /// <param name="type">Type of the object at that node</param>
        /// <param name="subName">Name of the sub nodes for array types(Array,List,Dictionary), can be set by XmlArrayItemAttribute and defaults to the type name</param>
        /// <param name="value">If given, no new class is created and value is used instead, also used as default value</param>
        /// <returns></returns>
        private object _GetValue(XmlNode node, Type type, string subName = null, object value = null)
        {
            if (type.IsEnum)
            {
                if (node == null)
                    return null;
                string stringValue = node.InnerText;
                try
                {
                    return Enum.Parse(type, stringValue);
                }
                catch (Exception)
                {
                    _ErrorHandler.HandleError(new CXmlInvalidValueException("Invalid value '%v' in %n", node, stringValue));
                    return value;
                }
            }
            if (type == typeof(Guid))
                return Guid.Parse(node.InnerText);
            if (type == typeof(string))
                return node == null ? null : node.InnerText;
            if (type.IsPrimitive)
                return _GetPrimitiveValue(node, type) ?? value;
            if (type.IsNullable())
            {
                if (node == null || !node.HasChildNodes)
                    return null;
                Type subType = type.GetGenericArguments()[0];
                return _GetValue(node, subType, subName, value);
            }
            if (type.IsList() || type.IsArray)
            {
                Type subType = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
                if (subName == null)
                    subName = subType.GetTypeName();
                subName = subName.ToLowerInvariant();
                List<object> subValues = new List<object>();
                if (node != null)
                {
                    foreach (XmlNode subNode in node.ChildNodes)
                    {
                        if (subNode is XmlComment)
                            continue;
                        if (subName == subNode.Name.ToLowerInvariant() && !subNode.Name.ToLowerInvariant().StartsWith(subName))
                            _ErrorHandler.HandleError(new CXmlException("Invalid list entry '" + subNode.Name + "' in %n; Expected: " + subName, node));
                        object subValue = _GetValue(subNode, subType);
                        if (subValue != null)
                            subValues.Add(subValue);
                    }
                }
                if (value == null)
                    return _CreateList(type, subValues);
                _FillList(value, type, subValues);
                return value;
            }
            if (type.IsDictionary())
            {
                Type subType = type.GetGenericArguments()[1];
                object dict = value ?? Activator.CreateInstance(type);
                MethodInfo add = type.GetMethod("Add");
                if (subName != null)
                    subName = subName.ToLowerInvariant();
                if (node != null)
                {
                    foreach (XmlNode subNode in node.ChildNodes)
                    {
                        if (subNode is XmlComment)
                            continue;
                        string key;
                        if (subName != null)
                        {
                            if (subName != subNode.Name.ToLowerInvariant() && !subNode.Name.ToLowerInvariant().StartsWith(subName))
                                _ErrorHandler.HandleError(new CXmlException("Invalid dictionary entry '" + subNode.Name + "' in %n; Expected: " + subName, node));
                            XmlNode nameAtt = (subNode.Attributes == null) ? null : subNode.Attributes.GetNamedItem("name");
                            if (nameAtt == null)
                            {
                                _ErrorHandler.HandleError(new CXmlException("'name' attribute is missing in %n", subNode));
                                continue;
                            }
                            key = nameAtt.Value;
                        }
                        else
                            key = subNode.Name;
                        object subValue = _GetValue(subNode, subType);
                        if (subValue != null)
                            add.Invoke(dict, new object[] {key, subValue});
                    }
                }
                return dict;
            }

            if (value == null)
            {
                try
                {
                    value = Activator.CreateInstance(type);
                }
                catch (Exception)
                {
                    _ErrorHandler.HandleError(new CXmlException("Could not create instance of %n(Type=" + type.Name + ")", node));
                    return null;
                }
            }
            _ReadChildNodes(node, value, true);
            _ReadChildNodes(node, value, false);
            return value;
        }

        /// <summary>
        ///     Gets the node's value assuming it is a primitive (int,float,...)
        /// </summary>
        /// <param name="node"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private object _GetPrimitiveValue(XmlNode node, Type type)
        {
            if (node == null)
                return null;
            object value;
            string nodeVal = node.InnerText;
            try
            {
                int p = nodeVal.IndexOf(',');
                if (p > 0 && p >= nodeVal.Length - 3)
                {
                    _ErrorHandler.HandleError(new CXmlInvalidValueException("German number format converted to English in %n", node, nodeVal, false));
                    char[] tmp = nodeVal.ToCharArray();
                    tmp[p] = '.';
                    nodeVal = new string(tmp);
                }
                value = Convert.ChangeType(nodeVal, type, CultureInfo.InvariantCulture);
            }
            catch (FormatException e)
            {
                _ErrorHandler.HandleError(new CXmlInvalidValueException("Invalid format in %n: '%v' (" + e.Message + ")", node, nodeVal));
                return null;
            }
            catch (InvalidCastException e)
            {
                _ErrorHandler.HandleError(new CXmlInvalidValueException(e.Message + " in %n: '%v'", node, nodeVal));
                return null;
            }
            return value;
        }

        /// <summary>
        ///     Sets the field to its default value if it has one (specified as DefaultValueAttribute or an empty collection if it is an embedded one)
        /// </summary>
        /// <param name="result"></param>
        /// <param name="field"></param>
        /// <returns>True if default value was set</returns>
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

        /// <summary>
        ///     Fills the specified collection (array or list) with the values
        /// </summary>
        /// <param name="list">List to fill</param>
        /// <param name="type">Type of the list</param>
        /// <param name="values">Collection of values</param>
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

        /// <summary>
        ///     Creates a collection (array or list) and fills it with the values
        /// </summary>
        /// <param name="type">Type of the collection to be created</param>
        /// <param name="values">Collection of values</param>
        /// <returns>Newly created collection</returns>
        private static object _CreateList(Type type, ICollection values)
        {
            object list = type.IsArray ? Array.CreateInstance(type.GetElementType(), values.Count) : Activator.CreateInstance(type, new object[] {values.Count});
            _FillList(list, type, values);
            return list;
        }

        /// <summary>
        ///     Creates a collection (array or list) and puts it into the specified field
        /// </summary>
        /// <param name="o">Object which listField belongs to</param>
        /// <param name="listField">Field that will contain the list</param>
        /// <param name="values">Collection of values that are used to fill the list</param>
        private static void _AddList(object o, SFieldInfo listField, ICollection values)
        {
            listField.SetValue(o, _CreateList(listField.Type, values));
        }

        /// <summary>
        ///     Reads childnodes of the given node (either nodes or attributes)
        /// </summary>
        /// <param name="parent">Node to process</param>
        /// <param name="o">Object to put the values in</param>
        /// <param name="attributes">True for processing attributes, false for nodes</param>
        private void _ReadChildNodes(XmlNode parent, object o, bool attributes)
        {
            IEnumerable nodes;
            if (parent == null)
                nodes = null;
            else if (attributes)
                nodes = parent.Attributes;
            else
                nodes = parent.ChildNodes;

            List<SFieldInfo> fields = o.GetType().GetFields(attributes);

            if (nodes != null)
            {
                //Dictionary of all embedded lists to allow interleaved/mixed elements
                Dictionary<string, Tuple<SFieldInfo, List<object>>> embLists = new Dictionary<string, Tuple<SFieldInfo, List<object>>>();
                foreach (XmlNode node in nodes)
                {
                    if (node is XmlComment || node.LocalName == "xsd" || node.LocalName == "xsi")
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
                        string msg = "Unexpected element: %n";
                        if (fields.Count > 0)
                            msg += " Expected: " + string.Join(", ", fields.Select(f => f.Name));
                        _ErrorHandler.HandleError(new CXmlException(msg, node));
                        continue;
                    }

                    if (field.IsByteArray)
                    {
                        fields.RemoveAt(curField); //Do this also on error
                        byte[] value;
                        try
                        {
                            value = Convert.FromBase64String(node.InnerText);
                        }
                        catch (Exception)
                        {
                            _ErrorHandler.HandleError(new CXmlInvalidValueException("Invalid value in %n: %v", node, node.InnerText));
                            if (!_CheckAndSetDefaultValue(o, field))
                                _ErrorHandler.HandleError(new CXmlException("No default value for %n", node));
                            continue;
                        }
                        field.SetValue(o, value);
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
                        if (subValue != null)
                            entry.Item2.Add(subValue);
                    }
                    else
                    {
                        fields.RemoveAt(curField); //Do this also on error
                        object value = _GetValue(node, field.Type, field.ArrayItemName);
                        if (value == null)
                        {
                            if (_CheckAndSetDefaultValue(o, field))
                                continue;
                            if (!field.IsNullable)
                            {
                                _ErrorHandler.HandleError(new CXmlException("No default value for unset field at %n", node));
                                continue;
                            }
                        }
                        if (field.Ranged != null && value != null && !field.Ranged.IsValid(field.IsNullable ? field.SubType : field.Type, value))
                        {
                            _ErrorHandler.HandleError(new CXmlInvalidValueException("Value in %n is not in the range " + field.Ranged +
                                                                                    ". (Value=%v)", node, value.ToString()));
                        }

                        field.SetValue(o, value);
                    }
                }
                //Add embedded lists
                foreach (Tuple<SFieldInfo, List<object>> entry in embLists.Values)
                {
                    _AddList(o, entry.Item1, entry.Item2);
                    fields.Remove(entry.Item1);
                }
            }
            foreach (SFieldInfo field in fields)
            {
                if (!_CheckAndSetDefaultValue(o, field))
                {
                    if (parent != null)
                        _ErrorHandler.HandleError(new CXmlMissingElementException(parent, field));
                    object value = _GetValue(null, field.Type, field.ArrayItemName, field.GetValue(o));
                    if (value != null)
                        field.SetValue(o, value);
                }
            }
        }

        /// <summary>
        ///     Deserializes the content of read into the given object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <param name="o"></param>
        /// <returns>Deserialized object</returns>
        private T _Deserialize<T>(XmlReader reader, object o) where T : new()
        {
            if (reader.IsEmptyElement)
            {
                _ErrorHandler.HandleError(new CXmlException("No content in xml!", null));
                return (T)o;
            }

            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(reader);
            if (xDoc.DocumentElement == null)
            {
                _ErrorHandler.HandleError(new CXmlException("Root element not found!", null));
                return (T)o;
            }
            try
            {
                o = _GetValue(xDoc.DocumentElement, o.GetType(), o.GetType().GetSubTypeName(), o);
            }
            catch (TargetInvocationException e)
            {
                throw e.InnerException ?? e;
            }
            return (T)o;
        }

        /// <summary>
        ///     Deserializes the given xml string and returns the resulting object
        /// </summary>
        /// <typeparam name="T">Type of the object contained in the xml</typeparam>
        /// <param name="xml">xml string</param>
        /// <returns>New object of type T</returns>
        public T DeserializeString<T>(string xml) where T : new()
        {
            return DeserializeString(xml, new T());
        }

        /// <summary>
        ///     Deserializes the given xml string and returns the resulting object<br />
        ///     Does not create a new object but reuses the given one overwriting its values
        /// </summary>
        /// <typeparam name="T">Type of the object contained in the xml</typeparam>
        /// <param name="xml">xml string</param>
        /// <param name="o">Existing object</param>
        /// <returns>New object of type T</returns>
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

        /// <summary>
        ///     Deserializes the given file and returns the resulting object
        /// </summary>
        /// <typeparam name="T">Type of the object contained in the xml</typeparam>
        /// <param name="filePath">Full path to the file</param>
        /// <returns>New object of type T</returns>
        public T Deserialize<T>(string filePath) where T : new()
        {
            return Deserialize(filePath, new T());
        }

        /// <summary>
        ///     Deserializes the given file and returns the resulting object<br />
        ///     Does not create a new object but reuses the given one overwriting its values
        /// </summary>
        /// <typeparam name="T">Type of the object contained in the xml</typeparam>
        /// <param name="filePath">Full path to the file</param>
        /// <param name="o">Existing object</param>
        /// <returns>New object of type T</returns>
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
    }
}