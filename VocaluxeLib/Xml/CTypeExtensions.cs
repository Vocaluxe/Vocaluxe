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
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace VocaluxeLib.Xml
{
    // ReSharper disable InconsistentNaming
    /// <summary>
    ///     Attribute containing an alternative name in the xml files during deserialization
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class XmlAltNameAttribute : Attribute
    {
        public readonly string AltName;

        public XmlAltNameAttribute(string altName)
        {
            AltName = altName;
        }
    }

    /// <summary>
    ///     Attribute for float fields to enforce normalized (0&lt;=x&lt;=1) values
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class XmlNormalizedAttribute : XmlRangedAttribute
    {
        public XmlNormalizedAttribute() : base(0, 1) {}
    }

    /// <summary>
    ///     Attribute for int fields to enforce values in the given range
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class XmlRangedAttribute : Attribute
    {
        public readonly int Min;
        public readonly int Max;

        public XmlRangedAttribute(int min, int max)
        {
            Min = min;
            Max = max;
        }

        public bool IsValid(Type type, object value)
        {
            if (type == typeof(int))
                return ((int)value).IsInRange(Min, Max);
            if (type == typeof(float))
                return ((float)value).IsInRange(Min, Max);
            if (type == typeof(double))
                return ((double)value).IsInRange(Min, Max);
            Debug.Assert(false, "Ranged attribute has invalid type");
            return false;
        }

        public override string ToString()
        {
            return Min + "-" + Max;
        }
    }

    // ReSharper restore InconsistentNaming

    public static class CTypeExtensions
    {
        // Cache for fields to speed up access to common types
        private static readonly Dictionary<Type, List<SFieldInfo>> _CacheFields = new Dictionary<Type, List<SFieldInfo>>();
        private static readonly Dictionary<Type, String> _CacheTypeName = new Dictionary<Type, string>();

        /// <summary>
        ///     Gets the name of the subType (e.g. list/array element) if any
        /// </summary>
        /// <param name="type">Xml type name of the subType or null if no subType</param>
        /// <returns></returns>
        public static string GetSubTypeName(this Type type)
        {
            Type subType;
            if (type.IsGenericType)
                subType = type.GetGenericArguments().Last();
            else if (type.IsArray)
                subType = type.GetElementType();
            else
                return null;
            return subType.GetTypeName();
        }

        /// <summary>
        ///     Fills the struct with information about the field
        /// </summary>
        /// <param name="info">Struct to fill</param>
        /// <param name="field">Field descrived by the struct</param>
        private static void _FillInfo(ref SFieldInfo info, MemberInfo field)
        {
            XmlAttributeAttribute attribute = field.GetAttribute<XmlAttributeAttribute>();
            if (attribute != null)
            {
                info.IsAttribute = true;
                info.Name = attribute.AttributeName;
            }
            else
            {
                XmlElementAttribute element = field.GetAttribute<XmlElementAttribute>();
                if (element != null)
                    info.Name = element.ElementName;
                else
                {
                    XmlArrayAttribute array = field.GetAttribute<XmlArrayAttribute>();
                    if (array != null)
                    {
                        Debug.Assert(info.Type.IsList() || info.Type.IsArray, "Only lists and arrays can have the array attribute");
                        Debug.Assert(!info.IsAttribute, "Lists cannot be attributes");
                        info.Name = array.ElementName;
                        info.IsList = true;
                    }
                }
            }
            if (string.IsNullOrEmpty(info.Name))
                info.Name = field.Name;
            XmlAltNameAttribute altName = field.GetAttribute<XmlAltNameAttribute>();
            if (altName != null)
                info.AltName = altName.AltName;

            if (info.Type.IsGenericType)
                info.SubType = info.Type.GetGenericArguments().Last();
            else if (info.Type.IsArray)
                info.SubType = info.Type.GetElementType();
            if (info.Type.IsList() || info.Type.IsArray)
            {
                if (!info.IsList)
                {
                    Debug.Assert(!field.HasAttribute<XmlArrayAttribute>(), "A field cannot have an XmlElement- and XmlArray-Attribute");
                    if (info.Type.IsArray && info.SubType == typeof(byte))
                        info.IsByteArray = true;
                    else
                        info.IsEmbeddedList = true;
                }
                else
                {
                    XmlArrayItemAttribute arrayItem = field.GetAttribute<XmlArrayItemAttribute>();
                    if (arrayItem != null && !string.IsNullOrEmpty(arrayItem.ElementName))
                        info.ArrayItemName = arrayItem.ElementName;
                }
            }
            else if (info.Type.IsNullable())
                info.IsNullable = true;
            else if (info.Type.IsDictionary())
            {
                Debug.Assert(info.Type.GetGenericArguments()[0] == typeof(string), "Keys of dictionaries must be strings");
                info.IsDictionary = true;
            }

            if (field.HasAttribute<XmlNormalizedAttribute>())
                Debug.Assert(info.Type == typeof(float) || (info.IsNullable && info.SubType == typeof(float)), "Only floats can be normalized");
            info.Ranged = field.GetAttribute<XmlRangedAttribute>();
            Type tmpType = info.IsNullable ? info.SubType : info.Type;
            Debug.Assert(info.Ranged == null || tmpType == typeof(int) || tmpType == typeof(float) || tmpType == typeof(double),
                         "Only ints,floats and double can be ranged");

            DefaultValueAttribute defAttr = field.GetAttribute<DefaultValueAttribute>();
            if (defAttr != null)
            {
                Debug.Assert(!info.Type.IsList(), "Lists cannot have a default value");
                info.HasDefaultValue = true;
                info.DefaultValue = defAttr.Value;
            }
            else if (info.IsNullable)
            {
                info.HasDefaultValue = true;
                info.DefaultValue = null;
            }
        }

        /// <summary>
        ///     Returns a collection with infos about all fields and properties of the type that can be (xml-)serialized
        /// </summary>
        /// <param name="type">Type to get information about</param>
        /// <returns></returns>
        public static IEnumerable<SFieldInfo> GetFieldInfos(this Type type)
        {
            List<SFieldInfo> result;
            if (_CacheFields.TryGetValue(type, out result))
                return result;
            result = new List<SFieldInfo>();
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                if (field.HasAttribute<XmlIgnoreAttribute>() || field.Name.EndsWith("Specified"))
                    continue;
                SFieldInfo info = new SFieldInfo(field);
                _FillInfo(ref info, field);
                result.Add(info);
            }
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo property in properties)
            {
                if (property.HasAttribute<XmlIgnoreAttribute>() || property.Name.EndsWith("Specified") || !property.CanRead || !property.CanWrite)
                    continue;
                SFieldInfo info = new SFieldInfo(property);
                _FillInfo(ref info, property);
                result.Add(info);
            }
            _CacheFields.Add(type, result);
            return result;
        }

        /// <summary>
        ///     Returns a collection with infos about all fields and properties of the type that can be (xml-)serialized but limits it to those which are (xml)-nodes or attributes
        /// </summary>
        /// <param name="type">Type to get information about</param>
        /// <param name="attributes">True to return infos about attributes, false for nodes</param>
        /// <returns></returns>
        public static List<SFieldInfo> GetFields(this Type type, bool attributes)
        {
            return GetFieldInfos(type).Where(f => f.IsAttribute == attributes).ToList();
        }

        /// <summary>
        ///     Gets the (xml)-type name for the type (influenced by XmlTypeAttributes)
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetTypeName(this Type type)
        {
            string name;
            if (_CacheTypeName.TryGetValue(type, out name))
                return name;
            XmlTypeAttribute att = type.GetAttribute<XmlTypeAttribute>();
            if (att != null)
            {
                name = att.TypeName;
                if (string.IsNullOrEmpty(name))
                    name = type.Name;
            }
            else
                name = type.Name;
            _CacheTypeName.Add(type, name);
            return name;
        }

        /// <summary>
        ///     Returns true if the field has the attribute
        /// </summary>
        /// <typeparam name="T">Attribute type</typeparam>
        /// <param name="field"></param>
        /// <returns></returns>
        public static bool HasAttribute<T>(this ICustomAttributeProvider field)
        {
            return field.GetCustomAttributes(typeof(T), false).Length > 0;
        }

        /// <summary>
        ///     Gets the first attribute with the given type or null if none found
        /// </summary>
        /// <typeparam name="T">Attribute type</typeparam>
        /// <param name="field"></param>
        /// <returns></returns>
        public static T GetAttribute<T>(this ICustomAttributeProvider field) where T : class
        {
            object[] attributes = field.GetCustomAttributes(typeof(T), false);
            return attributes.Length == 0 ? null : (T)attributes[0];
        }

        public static bool IsNullable(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static bool IsDictionary(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
        }

        public static bool IsList(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
        }
    }
}