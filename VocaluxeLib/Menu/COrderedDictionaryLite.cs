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

namespace VocaluxeLib.Menu
{
    public class COrderedDictionaryLite<T> : IEnumerable<T>
    {
        private readonly List<T> _Items;
        private readonly Dictionary<String, int> _HtIndex;
        private readonly String _ParentName;

        public COrderedDictionaryLite(object parent)
        {
            _Items = new List<T>();
            _HtIndex = new Dictionary<String, int>();
            _ParentName = parent.GetType().Name;
            if (_ParentName[0] == 'C' && Char.IsUpper(_ParentName[1]))
                _ParentName = _ParentName.Remove(0, 1);
        }

        public int Count
        {
            get { return _Items.Count; }
        }

        public T this[int index]
        {
            get { return _Items[index]; }
            set { _Items[index] = value; }
        }

        public T this[string key]
        {
            get
            {
                try
                {
                    return _Items[_HtIndex[key]];
                }
                catch (Exception)
                {
                    CBase.Log.LogError("Can't find " + typeof(T).Name.Substring(1) + " Element \"" + key + "\" in " + _ParentName);
                    throw;
                }
            }
            set
            {
                if (!_HtIndex.ContainsKey(key))
                {
                    _HtIndex.Add(key, _Items.Count);
                    _Items.Add(value);
                }
                else
                    _Items[_HtIndex[key]] = value;
            }
        }

        public int Add(T item, String key = null)
        {
            if (key != null)
                _HtIndex.Add(key, _Items.Count);
            _Items.Add(item);
            return _Items.Count - 1;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Clear()
        {
            _HtIndex.Clear();
            _Items.Clear();
        }
    }
}