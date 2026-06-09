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
using System.Linq;
using NUnit.Framework;
using VocaluxeLib.Utils.Combinatorics;

namespace Tests.VocaluxeLib.Utils.Combinatorics
{
    [TestFixture]
    public class CCombinationsTest
    {
        #region Tests
        [Test]
        public void TestCount()
        {
            Assert.That(CCombinations<int>.Count(4, 3), Is.EqualTo(4));
            Assert.That(CCombinations<int>.Count(4, 2), Is.EqualTo(6));
            Assert.That(CCombinations<int>.Count(12, 2), Is.EqualTo(66));
            // Corner cases
            for (var i = 1; i < 12; i++)
            {
                Assert.That(CCombinations<int>.Count(i, 0), Is.EqualTo(1));
                Assert.That(CCombinations<int>.Count(i, i), Is.EqualTo(1));
                Assert.That(CCombinations<int>.Count(i, i + 1), Is.EqualTo(0));
            }

            // Check identity (n k) = (n n-k)
            for (var i = 0; i <= 12; i++)
            {
                Assert.That(CCombinations<int>.Count(12, 12 - i), Is.EqualTo(CCombinations<int>.Count(12, i)));
            }

            Assert.Throws<ArgumentException>(new Action(() => CCombinations<int>.Count(-1, -1)));
            Assert.Throws<ArgumentException>(new Action(() => CCombinations<int>.Count(-1, 1)));
            Assert.Throws<ArgumentException>(new Action(() => CCombinations<int>.Count(1, -1)));
        }

        [Test]
        public void TestResults([Range(0, 8, 1)] int n)
        {
            var rand = new Random();

            for (var k = 0; k <= n; k++)
            {
                var collection = new List<int>(n);
                var r = rand.Next(1000);
                for (var i = 0; i < n; i++)
                {
                    collection.Add(i + r);
                }

                var combs = new CCombinations<int>(collection, k);
                Assert.That(combs.Count(), Is.EqualTo(CCombinations<int>.Count(n, k)));
                _CheckResults(combs, collection);
            }
        }
        #endregion

        #region Helper methods
        // ReSharper disable UnusedParameter.Local
        private static void _CheckResults<T>(CCombinations<T> combs, List<T> input)
            // ReSharper restore UnusedParameter.Local
        {
            var results = new List<List<T>>();
            foreach (var comb in combs)
            {
                foreach (var other in results)
                {
                    Assert.That(other.SequenceEqual(comb), Is.False, "Result must be unique");
                }

                for (var i = 0; i < comb.Count - 1; i++)
                {
                    Assert.That(comb.IndexOf(comb[i], i + 1), Is.LessThan(0),"Repetition not allowed");
                    Assert.That(input.Contains(comb[i]), Is.True, "Output must be in input sequence");
                }

                results.Add(comb);
            }

            Assert.That(results, Has.Count.EqualTo(combs.Count()));
            var results2 = combs.GetAll();
            Assert.That(results, Has.Count.EqualTo(results2.Count));
            for (var i = 0; i < results2.Count; i++)
            {
                Assert.That(results[i], Is.EquivalentTo(results2[i]));
            }
        }
        #endregion
    }
}