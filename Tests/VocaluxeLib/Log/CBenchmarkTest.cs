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
using System.IO;
using NUnit.Framework;
using VocaluxeLib.Log;

namespace Tests.VocaluxeLib.Log
{
    [TestFixture]
    public class CBenchmarkTest
    {
        private string _TestFolder;

        #region Setup methods

        [SetUp]
        public void SetUp()
        {
            _TestFolder = _GetTemporaryDirectory();
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(_TestFolder, true);
        }

        #endregion

        #region Tests

        [Test]
        public void BenchmarkTimeTest()
        {
            const string testMessage = "BenchmarkTime Test";
            string testFileName = Path.GetRandomFileName();
            string testFileSongName = Path.GetRandomFileName();
            string testFileMarkerName = Path.GetRandomFileName();
            const string versionTag = "Test Version (1.2.4)";

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) => { Assert.Fail("Benchmarks should not show the reporter."); },
                ELogLevel.Verbose);

            using (CBenchmark.Time(testMessage))
            {
                System.Threading.Thread.Sleep(1);
            }

            // Close logfile
            CLog.Close();

            // Check log
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileName)), "Mainlog file is missing.");
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileSongName)), "Songlog file is missing.");

            string mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            string songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            StringAssert.Contains($"[Information] Started \"{ testMessage }\"", mainLogContent, "Start entry wrong");
            StringAssert.Contains($"[Information] Finished \"{ testMessage }\" successfully in ", mainLogContent, "Finish entry wrong");
            StringAssert.AreEqualIgnoringCase("", songLogContent, "Benchmark should not create song log entries");
        }

        [Test]
        public void BenchmarkBeginTest()
        {
            const string testMessage = "BenchmarkBegin Test";
            string testFileName = Path.GetRandomFileName();
            string testFileSongName = Path.GetRandomFileName();
            string testFileMarkerName = Path.GetRandomFileName();
            const string versionTag = "Test Version (1.2.4)";

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) => { Assert.Fail("Benchmarks should not show the reporter."); },
                ELogLevel.Verbose);
            
            using (var token = CBenchmark.Begin(testMessage))
            {
                System.Threading.Thread.Sleep(1);
                token.End();
            }
            
            // Close logfile
            CLog.Close();

            // Check log
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileName)), "Mainlog file is missing.");
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileSongName)), "Songlog file is missing.");

            string mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            string songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            StringAssert.Contains($"[Information] Started \"{ testMessage }\"", mainLogContent, "Start entry wrong");
            StringAssert.Contains($"[Information] Finished \"{ testMessage }\" successfully in ", mainLogContent, "Finish entry wrong");
            StringAssert.AreEqualIgnoringCase("", songLogContent, "Benchmark should not create song log entries");
        }

        [Test]
        public void BenchmarkBeginWithoutEndTest()
        {
            const string testMessage = "BenchmarkBegin Test";
            string testFileName = Path.GetRandomFileName();
            string testFileSongName = Path.GetRandomFileName();
            string testFileMarkerName = Path.GetRandomFileName();
            const string versionTag = "Test Version (1.2.4)";

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) => { Assert.Fail("Benchmarks should not show the reporter."); },
                ELogLevel.Verbose);

            using (var token = CBenchmark.Begin(testMessage))
            {
                System.Threading.Thread.Sleep(1);
            }

            // Close logfile
            CLog.Close();

            // Check log
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileName)), "Mainlog file is missing.");
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileSongName)), "Songlog file is missing.");

            string mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            string songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            StringAssert.Contains($"[Information] Started \"{ testMessage }\"", mainLogContent, "Start entry wrong");
            StringAssert.Contains($"[Information] Failed \"{ testMessage }\" in ", mainLogContent, "Finish entry wrong");
            StringAssert.AreEqualIgnoringCase("", songLogContent, "Benchmark should not create song log entries");
        }

        #endregion

        #region Helper methods

        private string _GetTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        #endregion
    }
}
