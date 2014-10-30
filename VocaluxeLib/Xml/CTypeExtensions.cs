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
    [AttributeUsage(AttributeTargets.Field)]
    public class XmlAltNameAttribute : Attribute
    {
        public readonly string AltName;

        public XmlAltNameAttribute(string altName)
        {
            AltName = altName;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class XmlNormalizedAttribute : Attribute {}

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
    }

    // ReSharper restore InconsistentNaming

    public struct SFieldInfo
    {
        private readonly FieldInfo _Field;
        private readonly PropertyInfo _Property;

        public Type Type
        {
            get { return _Field != null ? _Field.FieldType : _Property.PropertyType; }
        }
        public string Name;
        public string AltName;
        public object DefaultValue;
        public bool HasDefaultValue;
        public bool IsAttribute;
        public bool IsList; //List with child elements (<List><El/><El/></List>)
        public bool IsEmbeddedList; //List w/o child elements(<List/><List/>)
        public bool IsDictionary; //List where the element names are the keys
        public bool IsByteArray; // Byte arrays w/o XmlArrayAttribute are serialized as Base64-Encoded strings
        public bool IsNullable;
        public bool IsNormalized;
        public XmlRangedAttribute Ranged;
        public Type SubType;
        public string ArrayItemName;

        public SFieldInfo(FieldInfo field)
            : this()
        {
            _Field = field;
        }

        public SFieldInfo(PropertyInfo property)
            : this()
        {
            _Property = property;
        }

        public void SetValue(object o, object value)
        {
            if (_Field != null)
                _Field.SetValue(o, value);
            else
                _Property.SetValue(o, value, new object[] {});
        }

        public object GetValue(object o)
        {
            return (_Field != null) ? _Field.GetValue(o) : _Property.GetValue(o, new object[] {});
        }
    }

    public static class CTypeExtensions
    {
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
            {
                Debug.Assert(info.Type == typeof(float) || (info.IsNullable && info.SubType == typeof(float)), "Only floats can be normalized");
                info.IsNormalized = true;
            }
            info.Ranged = field.GetAttribute<XmlRangedAttribute>();
            Debug.Assert(info.Ranged == null || info.Type == typeof(int) || info.Type == typeof(float) || info.Type == typeof(double),
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

        public static List<SFieldInfo> GetFields(this Type type, bool attributes)
        {
            return GetFieldInfos(type).Where(f => f.IsAttribute == attributes).ToList();
        }

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

        public static bool HasAttribute<T>(this ICustomAttributeProvider field)
        {
            return field.GetCustomAttributes(typeof(T), false).Length > 0;
        }

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