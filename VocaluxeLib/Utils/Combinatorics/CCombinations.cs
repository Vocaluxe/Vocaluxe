using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace VocaluxeLib.Utils.Combinatorics
{
    /// <summary>
    ///     Calculates all combinations (without repetition) of a given input sequence <br />
    ///     Example: {A B C}, select 2 --> {A B}, {A C}, {B C}
    /// </summary>
    /// <typeparam name="T">Type of the input elements</typeparam>
    public class CCombinations<T> : IEnumerable<List<T>>
    {
        private readonly List<T> _Values;
        private readonly int _NumSelected;
        private readonly int _Count;

        /// <summary>
        ///     Construct combinations for the input sequence when selecting numSelected elements <br />
        ///     The values should not contain duplicates as this will result in duplicates in the output
        /// </summary>
        /// <param name="values">Input sequence</param>
        /// <param name="numSelected">Number of elements to select</param>
        public CCombinations(List<T> values, int numSelected)
        {
            _Values = values;
            _NumSelected = numSelected;
            // This also acts as a parameter check
            _Count = Count(_Values.Count, _NumSelected);
        }

        /// <summary>
        ///     Calculates how many combinations are possible when k out of n elements are selected
        /// </summary>
        /// <param name="n">Number of elements</param>
        /// <param name="k">Number of selected elements</param>
        /// <returns></returns>
        public static int Count(int n, int k)
        {
            if (n < 0 || k < 0)
                throw new ArgumentException("Parameters must not be negative");
            if (n < k)
                return 0;
            // Try to optimize because (n k) = (n n-k)
            if (n - k < k)
                k = n - k;
            if (k == 0) // or k == n but both are the same after the optimization above
                return 1;

            long result = n;
            for (long i = 1; i < k; i++)
            {
                checked
                {
                    result = result * (n - i) / (i + 1);
                }
            }
            return (int)result;
        }

        /// <summary>
        ///     Gets the number of combinations that are generated
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            return _Count;
        }

        /// <summary>
        ///     Gets all possible combinations as a list
        /// </summary>
        /// <returns></returns>
        public List<List<T>> GetAll()
        {
            List<List<T>> result = new List<List<T>>(Count());
            result.AddRange(this);
            return result;
        }

        public IEnumerator<List<T>> GetEnumerator()
        {
            return new CEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private class CEnumerator : IEnumerator<List<T>>
        {
            private readonly int _N;
            private readonly int _K;
            private readonly CCombinations<T> _Parent;
            private int[] _CurSet;

            public CEnumerator(CCombinations<T> parent)
            {
                _N = parent._Values.Count;
                _K = parent._NumSelected;
                _Parent = parent;
                Debug.Assert(_N >= 0 && _K >= 0);
            }

            public void Dispose() {}

            public bool MoveNext()
            {
                if (_N < _K)
                    return false;

                if (_CurSet == null)
                {
                    _CurSet = new int[_K];
                    for (int i = 0; i < _K; i++)
                        _CurSet[i] = i;
                    return true;
                }

                // Check if we reached the end
                if (_K == 0 || _CurSet[0] == _N - _K)
                    return false;
                // Search for element to increment
                int idx = _K - 1;
                while (idx > 0 && _CurSet[idx] == _N - _K + idx)
                    idx--;
                _CurSet[idx]++;
                // Increment all elements "right" of this to 1+their left neighbour
                for (int j = idx; j < _K - 1; j++)
                    _CurSet[j + 1] = _CurSet[j] + 1;
                return true;
            }

            public void Reset()
            {
                _CurSet = null;
            }

            public List<T> Current
            {
                get
                {
                    List<T> result = new List<T>(_K);
                    result.AddRange(_CurSet.Select(i => _Parent._Values[i]));
                    return result;
                }
            }
            object IEnumerator.Current
            {
                get { return Current; }
            }
        }
    }
}