using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VocaluxeLib.Utils.Combinatorics;

namespace VocaluxeTests
{
    [TestClass]
    public class CCombinatoricsTest
    {
        [TestMethod]
        public void TestCounts()
        {
            Assert.AreEqual(4, CCombinations<int>.Count(4, 3));
            Assert.AreEqual(6, CCombinations<int>.Count(4, 2));
            Assert.AreEqual(66, CCombinations<int>.Count(12, 2));
            // Corner cases
            for (int i = 1; i < 12; i++)
            {
                Assert.AreEqual(1, CCombinations<int>.Count(i, 0));
                Assert.AreEqual(1, CCombinations<int>.Count(i, i));
                Assert.AreEqual(0, CCombinations<int>.Count(i, i + 1));
            }
            // Check identity (n k) = (n n-k)
            for (int i = 0; i <= 12; i++)
                Assert.AreEqual(CCombinations<int>.Count(12, i), CCombinations<int>.Count(12, 12 - i));
            CTestHelpers.AssertFail<ArgumentException>(() => CCombinations<int>.Count(-1, -1));
            CTestHelpers.AssertFail<ArgumentException>(() => CCombinations<int>.Count(-1, 1));
            CTestHelpers.AssertFail<ArgumentException>(() => CCombinations<int>.Count(1, -1));
        }

        // ReSharper disable UnusedParameter.Local
        private static void _CheckResults<T>(CCombinations<T> combs, List<T> input)
            // ReSharper restore UnusedParameter.Local
        {
            List<List<T>> results = new List<List<T>>();
            foreach (List<T> comb in combs)
            {
                foreach (List<T> other in results)
                    Assert.IsFalse(other.SequenceEqual(comb), "Result must be unique");
                for (int i = 0; i < comb.Count - 1; i++)
                {
                    Assert.IsTrue(comb.IndexOf(comb[i], i + 1) < 0, "Repetition not allowed");
                    Assert.IsTrue(input.Contains(comb[i]), "Output must be in input sequence");
                }
                results.Add(comb);
            }
            Assert.AreEqual(combs.Count(), results.Count);
            List<List<T>> results2 = combs.GetAll();
            Assert.AreEqual(results.Count, results2.Count);
            for (int i = 0; i < results2.Count; i++)
                Assert.IsTrue(results[i].SequenceEqual(results2[i]));
        }

        [TestMethod]
        public void TestResults()
        {
            Random rand = new Random();
            for (int n = 0; n < 8; n++)
            {
                for (int k = 0; k <= n; k++)
                {
                    List<int> collection = new List<int>(n);
                    int r = rand.Next(1000);
                    for (int i = 0; i < n; i++)
                        collection.Add(i + r);
                    CCombinations<int> combs = new CCombinations<int>(collection, k);
                    Assert.AreEqual(CCombinations<int>.Count(n, k), combs.Count());
                    _CheckResults(combs, collection);
                }
            }
        }
    }
}