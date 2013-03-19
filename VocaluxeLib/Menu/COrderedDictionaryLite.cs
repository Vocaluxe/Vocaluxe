using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Vocaluxe.Menu
{
    public class COrderedDictionaryLite<T> : IEnumerable<T>
    {
        private List<T> _Items;
        private Dictionary<String, int> _htIndex;
        private CMenu _Parent;

        public COrderedDictionaryLite(CMenu Parent)
        {
            _Items = new List<T>();
            _htIndex = new Dictionary<String, int>();
            _Parent = Parent;
        }

        public COrderedDictionaryLite(COrderedDictionaryLite<T> Dict)
        {
            _Items = new List<T>(Dict._Items);
            _htIndex = new Dictionary<String, int>(Dict._htIndex);
            _Parent = Dict._Parent;
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
                    return _Items[_htIndex[key]];
                }
                catch (Exception)
                {
                    CBase.Log.LogError("Can't find " + typeof(T).Name.Substring(1) + " Element \"" + key + "\" in Screen " + _Parent.ThemeName);
                    throw;
                }
            }
            set
            {
                if (!_htIndex.ContainsKey(key))
                {
                    _htIndex.Add(key, _Items.Count);
                    _Items.Add(value);
                }
                else
                    _Items[_htIndex[key]] = value;
            }
        }

        public int Add(T item, String key = null)
        {
            if (key != null)
                _htIndex.Add(key, _Items.Count);
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
    }
}
