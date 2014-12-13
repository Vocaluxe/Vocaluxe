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
using System.Reflection;

namespace VocaluxeLib.Xml
{
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
        /// <summary>
        ///     List with child elements (
        ///     <List>
        ///         <El /><El />
        ///     </List>
        ///     )
        /// </summary>
        public bool IsList;
        /// <summary>
        ///     List w/o child elements(<List /><List />)
        /// </summary>
        public bool IsEmbeddedList;
        /// <summary>
        ///     List where the element names are the keys
        /// </summary>
        public bool IsDictionary;
        /// <summary>
        ///     Byte arrays w/o XmlArrayAttribute are serialized as Base64-Encoded strings
        /// </summary>
        public bool IsByteArray;
        public bool IsNullable;
        public XmlRangedAttribute Ranged;
        /// <summary>
        ///     Type of subnodes (array/list elements or dictionary values)
        /// </summary>
        public Type SubType;
        /// <summary>
        ///     Name of the subnodes
        /// </summary>
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
}