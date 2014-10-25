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
        public string Name;
        public string AltName;
        public FieldInfo Info;
        public object DefaultValue;
        public bool HasDefaultValue;
        public bool IsAttribute;
        public bool IsList; //List with child elements (<List><El/><El/></List>)
        public bool IsEmbeddedList; //List w/o child elements(<List/><List/>)
        public bool IsNullable;
        public bool IsNormalized;
        public XmlRangedAttribute Ranged;
        public Type SubType;
        public string ArrayItemName;
    }

    public static class CTypeExtensions
    {
        private static readonly Dictionary<Type, List<SFieldInfo>> _CacheFields = new Dictionary<Type, List<SFieldInfo>>();
        private static readonly Dictionary<Type, String> _CacheTypeName = new Dictionary<Type, string>();

        public static IEnumerable<SFieldInfo> GetFieldInfos(this Type type)
        {
            List<SFieldInfo> result;
            if (_CacheFields.TryGetValue(type, out result))
                return result;
            result = new List<SFieldInfo>();
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                if (field.HasAttribute<XmlIgnoreAttribute>())
                    continue;
                XmlAttributeAttribute attribute = field.GetAttribute<XmlAttributeAttribute>();
                SFieldInfo info = new SFieldInfo {Info = field};
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
                            Debug.Assert(field.IsList() || field.FieldType.IsArray, "Only lists and arrays can have the array attribute");
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

                if (field.FieldType.IsGenericType)
                    info.SubType = field.FieldType.GetGenericArguments()[0];
                if (field.FieldType.IsArray)
                    info.SubType = field.FieldType.GetElementType();
                if (field.IsList() || field.FieldType.IsArray)
                {
                    if (!info.IsList)
                    {
                        Debug.Assert(!field.HasAttribute<XmlArrayAttribute>(), "A field cannot have an XmlElement- and XmlArray-Attribute");
                        info.IsEmbeddedList = true;
                    }
                    else
                    {
                        XmlArrayItemAttribute arrayItem = field.GetAttribute<XmlArrayItemAttribute>();
                        if (arrayItem != null && !string.IsNullOrEmpty(arrayItem.ElementName))
                            info.ArrayItemName = arrayItem.ElementName;
                    }
                }
                else if (field.FieldType.IsNullable())
                    info.IsNullable = true;

                if (field.HasAttribute<XmlNormalizedAttribute>())
                {
                    Debug.Assert(field.FieldType == typeof(float) || (info.IsNullable && info.SubType == typeof(float)), "Only floats can be normalized");
                    info.IsNormalized = true;
                }
                info.Ranged = field.GetAttribute<XmlRangedAttribute>();
                Debug.Assert(info.Ranged == null || field.FieldType == typeof(int), "Only ints can be ranged");

                DefaultValueAttribute defAttr = field.GetAttribute<DefaultValueAttribute>();
                if (defAttr != null)
                {
                    Debug.Assert(!field.IsList(), "Lists cannot have a default value");
                    info.HasDefaultValue = true;
                    info.DefaultValue = defAttr.Value;
                }
                else if (info.IsNullable)
                {
                    info.HasDefaultValue = true;
                    info.DefaultValue = null;
                }
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

        public static bool IsList(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
        }

        public static bool IsList(this FieldInfo field)
        {
            return field.FieldType.IsList();
        }
    }
}