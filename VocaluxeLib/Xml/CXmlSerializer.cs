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
using System.Diagnostics;
using System.Globalization;
using System.IO;
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

        /// <summary>
        ///     Creates a new serializer
        /// </summary>
        /// <param name="writeDefaults">When true, also nodes with default values will be written</param>
        /// <param name="getCommentCallback">Callback that is called for each node and should return a string that will be used as a comment</param>
        public CXmlSerializer(bool writeDefaults = false, GetCommentDelegate getCommentCallback = null)
        {
            _WriteDefaults = writeDefaults;
            _GetCommentCallback = getCommentCallback;
        }

        /// <summary>
        ///     Uniform settings for writing XML files. ALWAYS use this!
        /// </summary>
        private readonly XmlWriterSettings _XmlSettings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.UTF8,
                ConformanceLevel = ConformanceLevel.Document
            };

        /// <summary>
        ///     Writes a value as a node or attribute
        /// </summary>
        /// <param name="writer">XmlWriter to use</param>
        /// <param name="name">Name of the node/attribute</param>
        /// <param name="type">Type of the value</param>
        /// <param name="value">Object to write</param>
        /// <param name="isAttribute">When true, value will be written as an attribute to the currently open node, otherwhise it will create a new node</param>
        /// <param name="arrayItemName">Name of the sub nodes for array types(Array,List,Dictionary), can be set by XmlArrayItemAttribute and defaults to the type name</param>
        /// <param name="nameAttribute">If set, an attribute 'name' with this value will be written</param>
        private void _WriteValue(XmlWriter writer, string name, Type type, object value, bool isAttribute, string arrayItemName = null, string nameAttribute = null)
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
            else if (type == typeof(Guid))
            {
                writer.WriteElementString(name, value.ToString());
            }
            else if (type.IsNullable())
                _WriteValue(writer, name, type.GetGenericArguments()[0], value, isAttribute, arrayItemName, nameAttribute);
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
                    _WriteValue(writer, subName, subType, subValue, false);
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
                    _WriteValue(writer, subName, subType, entry.Value, false, null, subNameAttribute);
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
                    _WriteFields(writer, value);
                writer.WriteEndElement();
            }
        }

        /// <summary>
        ///     Writes the fields of the given object, does not write the node tags itself
        /// </summary>
        /// <param name="writer">XmlWriter to use</param>
        /// <param name="o">Object to process</param>
        private void _WriteFields(XmlWriter writer, object o)
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
                    bool empty = true;
                    foreach (object subValue in values)
                    {
                        _WriteValue(writer, field.Name, field.SubType, subValue, field.IsAttribute);
                        empty = false;
                    }
                    if (empty && _WriteDefaults)
                        writer.WriteElementString(field.Name, "");
                }
                else if (field.IsByteArray)
                    writer.WriteElementString(field.Name, Convert.ToBase64String((byte[])value));
                else
                    _WriteValue(writer, field.Name, field.Type, value, field.IsAttribute, field.ArrayItemName);
            }
        }

        /// <summary>
        ///     Serializes the given object using the writer
        /// </summary>
        /// <param name="writer">XmlWriter to use</param>
        /// <param name="o">Object to serialize</param>
        /// <param name="rootNodeName">(optional) Name of the root node (overwrites default value specified by XmlRoot/XmlTypeAttributes which defaults to "root")</param>
        private void _Serialize(XmlWriter writer, object o, string rootNodeName)
        {
            if (string.IsNullOrEmpty(rootNodeName))
            {
                XmlRootAttribute root = o.GetType().GetAttribute<XmlRootAttribute>();
                if (root != null && !string.IsNullOrEmpty(root.ElementName))
                    rootNodeName = root.ElementName;
                else
                {
                    XmlTypeAttribute typeAtt = o.GetType().GetAttribute<XmlTypeAttribute>();
                    if (typeAtt != null && !string.IsNullOrEmpty(typeAtt.TypeName))
                        rootNodeName = typeAtt.TypeName;
                    else
                        rootNodeName = "root";
                }
            }
            try
            {
                writer.WriteStartDocument();
                _WriteValue(writer, rootNodeName, o.GetType(), o, false, o.GetType().GetSubTypeName());
                writer.WriteEndDocument();
            }
            finally
            {
                writer.Flush();
            }
        }

        /// <summary>
        ///     Serializes the given object to the file
        /// </summary>
        /// <param name="filePath">Full path to file</param>
        /// <param name="o">Object to serialize</param>
        /// <param name="rootNodeName">Name of the root node (overwrites default value specified by XmlRoot/XmlTypeAttributes which defaults to "root")</param>
        public void Serialize(string filePath, object o, string rootNodeName = null)
        {
            using (XmlWriter writer = XmlWriter.Create(filePath, _XmlSettings))
                _Serialize(writer, o, rootNodeName);
        }

        /// <summary>
        ///     Serializes the given object and returns the resulting xml as a string
        /// </summary>
        /// <param name="o">Object to serialize</param>
        /// <param name="rootNodeName">Name of the root node (overwrites default value specified by XmlRoot/XmlTypeAttributes which defaults to "root")</param>
        /// <returns>Serialized object</returns>
        public string Serialize(object o, string rootNodeName = null)
        {
            MemoryStream result = new MemoryStream();
            using (XmlWriter writer = XmlWriter.Create(result, _XmlSettings))
                _Serialize(writer, o, rootNodeName);
            result.Position = 0;
            return new StreamReader(result).ReadToEnd();
        }
    }
}