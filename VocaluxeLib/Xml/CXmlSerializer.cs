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