using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VocaluxeTests
{
    static class CTestHelpers
    {
        public static void AssertFail<T>(Action test) where T : Exception
        {
            try
            {
                test.Invoke();
                Assert.Fail("Exception " + typeof(T).Name + " not thrown!");
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(T));
            }
        }
    }
}