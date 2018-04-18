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
    public class CLogTest
    {
        private const string _TestMessage = "This is a test";
        private const string _TestMessageWithData = "This is a test with data: {Data1}";
        private const string _FirstParam = "Test";
        private const string _SecondParam = "Foo";
        private const string _TestMessageWithResolvedData = "This is a test with data: \"Test\"";
        private const string _TestExceptionMessage = "TestExceptionMessage";
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
        
        #region Verbose tests

        [Test]
        public void VerboseTestWithDataWithException([Values(true, false)] bool show)
        {
            string testFileName = Path.GetRandomFileName();
            string testFileSongName = Path.GetRandomFileName();
            string testFileMarkerName = Path.GetRandomFileName();
            string versionTag = "Test Version (1.2.4)";
            bool messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
            {
                messageShown = true;
                Assert.IsTrue(show);
                Assert.IsFalse(crash);
                Assert.IsTrue(cont);
                Assert.AreEqual(versionTag, tag);

                StringAssert.Contains("[Information] Starting to log", log, "Main log start entry wrong");
                StringAssert.Contains("Version = " + versionTag, log, "Main log version tag entry wrong");
                StringAssert.Contains("[Verbose] " + _TestMessageWithResolvedData, log, "Main log entry wrong");

                StringAssert.Contains(_TestMessageWithData, error, "Error message wrong");
            },
            ELogLevel.Verbose);

            // Add log entry
            CLog.Verbose(new Exception(_TestExceptionMessage), _TestMessageWithData, CLog.Params(_FirstParam, _SecondParam), show);
            CLog.CSongLog.Verbose(_TestMessageWithData, CLog.Params(_FirstParam, _SecondParam));

            // Close logfile
            CLog.Close();

            // Check log
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileName)), "Mainlog file is missing.");
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileSongName)), "Songlog file is missing.");

            string mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            string songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            StringAssert.Contains("[Information] Starting to log", mainLogContent, "Main log start entry wrong");
            StringAssert.Contains("Version = " + versionTag, mainLogContent, "Main log version tag entry wrong");
            StringAssert.Contains("[Verbose] " +_TestMessageWithResolvedData, mainLogContent, "Main log entry wrong");
            StringAssert.Contains(_TestExceptionMessage, mainLogContent, "Exception is missing");
            StringAssert.Contains(_SecondParam, mainLogContent, "Second data field is missing");
            StringAssert.Contains(_TestMessageWithResolvedData, songLogContent, "Song log entry wrong");
            if(!show)
            {
                Assert.IsFalse(messageShown);
            }
        }

        [Test]
        public void VerboseTestWithDataWithoutException([Values(true, false)] bool show)
        {
            string testFileName = Path.GetRandomFileName();
            string testFileSongName = Path.GetRandomFileName();
            string testFileMarkerName = Path.GetRandomFileName();
            string versionTag = "Test Version (1.2.4)";
            bool messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
            {
                messageShown = true;
                Assert.IsTrue(show);
                Assert.IsFalse(crash);
                Assert.IsTrue(cont);
                Assert.AreEqual(versionTag, tag);

                StringAssert.Contains("[Information] Starting to log", log, "Main log start entry wrong");
                StringAssert.Contains("Version = " + versionTag, log, "Main log version tag entry wrong");
                StringAssert.Contains("[Verbose] " + _TestMessageWithResolvedData, log, "Main log entry wrong");

                StringAssert.Contains(_TestMessageWithData, error, "Error message wrong");
            },
            ELogLevel.Verbose);

            // Add log entry
            CLog.Verbose(_TestMessageWithData, CLog.Params(_FirstParam, _SecondParam), show);
            CLog.CSongLog.Verbose(_TestMessageWithData, CLog.Params(_FirstParam, _SecondParam));

            // Close logfile
            CLog.Close();

            // Check log
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileName)), "Mainlog file is missing.");
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileSongName)), "Songlog file is missing.");

            string mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            string songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            StringAssert.Contains("[Information] Starting to log", mainLogContent, "Main log start entry wrong");
            StringAssert.Contains("Version = " + versionTag, mainLogContent, "Main log version tag entry wrong");
            StringAssert.Contains("[Verbose] " +_TestMessageWithResolvedData, mainLogContent, "Main log entry wrong");
            StringAssert.Contains(_SecondParam, mainLogContent, "Second data field is missing");
            StringAssert.Contains(_TestMessageWithResolvedData, songLogContent, "Song log entry wrong");
            if(!show)
            {
                Assert.IsFalse(messageShown);
            }
        }

        [Test]
        public void VerboseTestWithoutDataWithException([Values(true, false)] bool show)
        {
            string testFileName = Path.GetRandomFileName();
            string testFileSongName = Path.GetRandomFileName();
            string testFileMarkerName = Path.GetRandomFileName();
            string versionTag = "Test Version (1.2.4)";
            bool messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
            {
                messageShown = true;
                Assert.IsTrue(show);
                Assert.IsFalse(crash);
                Assert.IsTrue(cont);
                Assert.AreEqual(versionTag, tag);

                StringAssert.Contains("[Information] Starting to log", log, "Main log start entry wrong");
                StringAssert.Contains("Version = " + versionTag, log, "Main log version tag entry wrong");
                StringAssert.Contains("[Verbose] " + _TestMessage, log, "Main log entry wrong");

                StringAssert.Contains(_TestMessage, error, "Error message wrong");
            },
            ELogLevel.Verbose);

            // Add log entry
            CLog.Verbose(new Exception(_TestExceptionMessage), _TestMessage, show);
            CLog.CSongLog.Verbose(_TestMessage);

            // Close logfile
            CLog.Close();

            // Check log
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileName)), "Mainlog file is missing.");
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileSongName)), "Songlog file is missing.");

            string mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            string songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            StringAssert.Contains("[Information] Starting to log", mainLogContent, "Main log start entry wrong");
            StringAssert.Contains("Version = " + versionTag, mainLogContent, "Main log version tag entry wrong");
            StringAssert.Contains("[Verbose] " +_TestMessage, mainLogContent, "Main log entry wrong");
            StringAssert.Contains(_TestExceptionMessage, mainLogContent, "Exception is missing");
            StringAssert.Contains(_TestMessage, songLogContent, "Song log entry wrong");
            if(!show)
            {
                Assert.IsFalse(messageShown);
            }
        }

        [Test]
        public void VerboseTestWithoutDataWithoutException([Values(true, false)] bool show)
        {
            string testFileName = Path.GetRandomFileName();
            string testFileSongName = Path.GetRandomFileName();
            string testFileMarkerName = Path.GetRandomFileName();
            string versionTag = "Test Version (1.2.4)";
            bool messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
            {
                messageShown = true;
                Assert.IsTrue(show);
                Assert.IsFalse(crash);
                Assert.IsTrue(cont);
                Assert.AreEqual(versionTag, tag);

                StringAssert.Contains("[Information] Starting to log", log, "Main log start entry wrong");
                StringAssert.Contains("Version = " + versionTag, log, "Main log version tag entry wrong");
                StringAssert.Contains("[Verbose] " + _TestMessage, log, "Main log entry wrong");

                StringAssert.Contains(_TestMessage, error, "Error message wrong");
            },
            ELogLevel.Verbose);

            // Add log entry
            CLog.Verbose(_TestMessage, show);
            CLog.CSongLog.Verbose(_TestMessage);

            // Close logfile
            CLog.Close();

            // Check log
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileName)), "Mainlog file is missing.");
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileSongName)), "Songlog file is missing.");

            string mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            string songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            StringAssert.Contains("[Information] Starting to log", mainLogContent, "Main log start entry wrong");
            StringAssert.Contains("Version = " + versionTag, mainLogContent, "Main log version tag entry wrong");
            StringAssert.Contains("[Verbose] " +_TestMessage, mainLogContent, "Main log entry wrong");
            StringAssert.Contains(_TestMessage, songLogContent, "Song log entry wrong");
            if(!show)
            {
                Assert.IsFalse(messageShown);
            }
        }

        #endregion
        #region Debug tests

        [Test]
        public void DebugTestWithDataWithException([Values(true, false)] bool show)
        {
            string testFileName = Path.GetRandomFileName();
            string testFileSongName = Path.GetRandomFileName();
            string testFileMarkerName = Path.GetRandomFileName();
            string versionTag = "Test Version (1.2.4)";
            bool messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
            {
                messageShown = true;
                Assert.IsTrue(show);
                Assert.IsFalse(crash);
                Assert.IsTrue(cont);
                Assert.AreEqual(versionTag, tag);

                StringAssert.Contains("[Information] Starting to log", log, "Main log start entry wrong");
                StringAssert.Contains("Version = " + versionTag, log, "Main log version tag entry wrong");
                StringAssert.Contains("[Debug] " + _TestMessageWithResolvedData, log, "Main log entry wrong");

                StringAssert.Contains(_TestMessageWithData, error, "Error message wrong");
            },
            ELogLevel.Verbose);

            // Add log entry
            CLog.Debug(new Exception(_TestExceptionMessage), _TestMessageWithData, CLog.Params(_FirstParam, _SecondParam), show);
            CLog.CSongLog.Debug(_TestMessageWithData, CLog.Params(_FirstParam, _SecondParam));

            // Close logfile
            CLog.Close();

            // Check log
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileName)), "Mainlog file is missing.");
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileSongName)), "Songlog file is missing.");

            string mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            string songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            StringAssert.Contains("[Information] Starting to log", mainLogContent, "Main log start entry wrong");
            StringAssert.Contains("Version = " + versionTag, mainLogContent, "Main log version tag entry wrong");
            StringAssert.Contains("[Debug] " +_TestMessageWithResolvedData, mainLogContent, "Main log entry wrong");
            StringAssert.Contains(_TestExceptionMessage, mainLogContent, "Exception is missing");
            StringAssert.Contains(_SecondParam, mainLogContent, "Second data field is missing");
            StringAssert.Contains(_TestMessageWithResolvedData, songLogContent, "Song log entry wrong");
            if(!show)
            {
                Assert.IsFalse(messageShown);
            }
        }

        [Test]
        public void DebugTestWithDataWithoutException([Values(true, false)] bool show)
        {
            string testFileName = Path.GetRandomFileName();
            string testFileSongName = Path.GetRandomFileName();
            string testFileMarkerName = Path.GetRandomFileName();
            string versionTag = "Test Version (1.2.4)";
            bool messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
            {
                messageShown = true;
                Assert.IsTrue(show);
                Assert.IsFalse(crash);
                Assert.IsTrue(cont);
                Assert.AreEqual(versionTag, tag);

                StringAssert.Contains("[Information] Starting to log", log, "Main log start entry wrong");
                StringAssert.Contains("Version = " + versionTag, log, "Main log version tag entry wrong");
                StringAssert.Contains("[Debug] " + _TestMessageWithResolvedData, log, "Main log entry wrong");

                StringAssert.Contains(_TestMessageWithData, error, "Error message wrong");
            },
            ELogLevel.Verbose);

            // Add log entry
            CLog.Debug(_TestMessageWithData, CLog.Params(_FirstParam, _SecondParam), show);
            CLog.CSongLog.Debug(_TestMessageWithData, CLog.Params(_FirstParam, _SecondParam));

            // Close logfile
            CLog.Close();

            // Check log
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileName)), "Mainlog file is missing.");
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileSongName)), "Songlog file is missing.");

            string mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            string songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            StringAssert.Contains("[Information] Starting to log", mainLogContent, "Main log start entry wrong");
            StringAssert.Contains("Version = " + versionTag, mainLogContent, "Main log version tag entry wrong");
            StringAssert.Contains("[Debug] " +_TestMessageWithResolvedData, mainLogContent, "Main log entry wrong");
            StringAssert.Contains(_SecondParam, mainLogContent, "Second data field is missing");
            StringAssert.Contains(_TestMessageWithResolvedData, songLogContent, "Song log entry wrong");
            if(!show)
            {
                Assert.IsFalse(messageShown);
            }
        }

        [Test]
        public void DebugTestWithoutDataWithException([Values(true, false)] bool show)
        {
            string testFileName = Path.GetRandomFileName();
            string testFileSongName = Path.GetRandomFileName();
            string testFileMarkerName = Path.GetRandomFileName();
            string versionTag = "Test Version (1.2.4)";
            bool messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
            {
                messageShown = true;
                Assert.IsTrue(show);
                Assert.IsFalse(crash);
                Assert.IsTrue(cont);
                Assert.AreEqual(versionTag, tag);

                StringAssert.Contains("[Information] Starting to log", log, "Main log start entry wrong");
                StringAssert.Contains("Version = " + versionTag, log, "Main log version tag entry wrong");
                StringAssert.Contains("[Debug] " + _TestMessage, log, "Main log entry wrong");

                StringAssert.Contains(_TestMessage, error, "Error message wrong");
            },
            ELogLevel.Verbose);

            // Add log entry
            CLog.Debug(new Exception(_TestExceptionMessage), _TestMessage, show);
            CLog.CSongLog.Debug(_TestMessage);

            // Close logfile
            CLog.Close();

            // Check log
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileName)), "Mainlog file is missing.");
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileSongName)), "Songlog file is missing.");

            string mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            string songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            StringAssert.Contains("[Information] Starting to log", mainLogContent, "Main log start entry wrong");
            StringAssert.Contains("Version = " + versionTag, mainLogContent, "Main log version tag entry wrong");
            StringAssert.Contains("[Debug] " +_TestMessage, mainLogContent, "Main log entry wrong");
            StringAssert.Contains(_TestExceptionMessage, mainLogContent, "Exception is missing");
            StringAssert.Contains(_TestMessage, songLogContent, "Song log entry wrong");
            if(!show)
            {
                Assert.IsFalse(messageShown);
            }
        }

        [Test]
        public void DebugTestWithoutDataWithoutException([Values(true, false)] bool show)
        {
            string testFileName = Path.GetRandomFileName();
            string testFileSongName = Path.GetRandomFileName();
            string testFileMarkerName = Path.GetRandomFileName();
            string versionTag = "Test Version (1.2.4)";
            bool messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
            {
                messageShown = true;
                Assert.IsTrue(show);
                Assert.IsFalse(crash);
                Assert.IsTrue(cont);
                Assert.AreEqual(versionTag, tag);

                StringAssert.Contains("[Information] Starting to log", log, "Main log start entry wrong");
                StringAssert.Contains("Version = " + versionTag, log, "Main log version tag entry wrong");
                StringAssert.Contains("[Debug] " + _TestMessage, log, "Main log entry wrong");

                StringAssert.Contains(_TestMessage, error, "Error message wrong");
            },
            ELogLevel.Verbose);

            // Add log entry
            CLog.Debug(_TestMessage, show);
            CLog.CSongLog.Debug(_TestMessage);

            // Close logfile
            CLog.Close();

            // Check log
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileName)), "Mainlog file is missing.");
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileSongName)), "Songlog file is missing.");

            string mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            string songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            StringAssert.Contains("[Information] Starting to log", mainLogContent, "Main log start entry wrong");
            StringAssert.Contains("Version = " + versionTag, mainLogContent, "Main log version tag entry wrong");
            StringAssert.Contains("[Debug] " +_TestMessage, mainLogContent, "Main log entry wrong");
            StringAssert.Contains(_TestMessage, songLogContent, "Song log entry wrong");
            if(!show)
            {
                Assert.IsFalse(messageShown);
            }
        }

        #endregion
        #region Information tests

        [Test]
        public void InformationTestWithDataWithException([Values(true, false)] bool show)
        {
            string testFileName = Path.GetRandomFileName();
            string testFileSongName = Path.GetRandomFileName();
            string testFileMarkerName = Path.GetRandomFileName();
            string versionTag = "Test Version (1.2.4)";
            bool messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
            {
                messageShown = true;
                Assert.IsTrue(show);
                Assert.IsFalse(crash);
                Assert.IsTrue(cont);
                Assert.AreEqual(versionTag, tag);

                StringAssert.Contains("[Information] Starting to log", log, "Main log start entry wrong");
                StringAssert.Contains("Version = " + versionTag, log, "Main log version tag entry wrong");
                StringAssert.Contains("[Information] " + _TestMessageWithResolvedData, log, "Main log entry wrong");

                StringAssert.Contains(_TestMessageWithData, error, "Error message wrong");
            },
            ELogLevel.Verbose);

            // Add log entry
            CLog.Information(new Exception(_TestExceptionMessage), _TestMessageWithData, CLog.Params(_FirstParam, _SecondParam), show);
            CLog.CSongLog.Information(_TestMessageWithData, CLog.Params(_FirstParam, _SecondParam));

            // Close logfile
            CLog.Close();

            // Check log
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileName)), "Mainlog file is missing.");
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileSongName)), "Songlog file is missing.");

            string mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            string songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            StringAssert.Contains("[Information] Starting to log", mainLogContent, "Main log start entry wrong");
            StringAssert.Contains("Version = " + versionTag, mainLogContent, "Main log version tag entry wrong");
            StringAssert.Contains("[Information] " +_TestMessageWithResolvedData, mainLogContent, "Main log entry wrong");
            StringAssert.Contains(_TestExceptionMessage, mainLogContent, "Exception is missing");
            StringAssert.Contains(_SecondParam, mainLogContent, "Second data field is missing");
            StringAssert.Contains(_TestMessageWithResolvedData, songLogContent, "Song log entry wrong");
            if(!show)
            {
                Assert.IsFalse(messageShown);
            }
        }

        [Test]
        public void InformationTestWithDataWithoutException([Values(true, false)] bool show)
        {
            string testFileName = Path.GetRandomFileName();
            string testFileSongName = Path.GetRandomFileName();
            string testFileMarkerName = Path.GetRandomFileName();
            string versionTag = "Test Version (1.2.4)";
            bool messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
            {
                messageShown = true;
                Assert.IsTrue(show);
                Assert.IsFalse(crash);
                Assert.IsTrue(cont);
                Assert.AreEqual(versionTag, tag);

                StringAssert.Contains("[Information] Starting to log", log, "Main log start entry wrong");
                StringAssert.Contains("Version = " + versionTag, log, "Main log version tag entry wrong");
                StringAssert.Contains("[Information] " + _TestMessageWithResolvedData, log, "Main log entry wrong");

                StringAssert.Contains(_TestMessageWithData, error, "Error message wrong");
            },
            ELogLevel.Verbose);

            // Add log entry
            CLog.Information(_TestMessageWithData, CLog.Params(_FirstParam, _SecondParam), show);
            CLog.CSongLog.Information(_TestMessageWithData, CLog.Params(_FirstParam, _SecondParam));

            // Close logfile
            CLog.Close();

            // Check log
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileName)), "Mainlog file is missing.");
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileSongName)), "Songlog file is missing.");

            string mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            string songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            StringAssert.Contains("[Information] Starting to log", mainLogContent, "Main log start entry wrong");
            StringAssert.Contains("Version = " + versionTag, mainLogContent, "Main log version tag entry wrong");
            StringAssert.Contains("[Information] " +_TestMessageWithResolvedData, mainLogContent, "Main log entry wrong");
            StringAssert.Contains(_SecondParam, mainLogContent, "Second data field is missing");
            StringAssert.Contains(_TestMessageWithResolvedData, songLogContent, "Song log entry wrong");
            if(!show)
            {
                Assert.IsFalse(messageShown);
            }
        }

        [Test]
        public void InformationTestWithoutDataWithException([Values(true, false)] bool show)
        {
            string testFileName = Path.GetRandomFileName();
            string testFileSongName = Path.GetRandomFileName();
            string testFileMarkerName = Path.GetRandomFileName();
            string versionTag = "Test Version (1.2.4)";
            bool messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
            {
                messageShown = true;
                Assert.IsTrue(show);
                Assert.IsFalse(crash);
                Assert.IsTrue(cont);
                Assert.AreEqual(versionTag, tag);

                StringAssert.Contains("[Information] Starting to log", log, "Main log start entry wrong");
                StringAssert.Contains("Version = " + versionTag, log, "Main log version tag entry wrong");
                StringAssert.Contains("[Information] " + _TestMessage, log, "Main log entry wrong");

                StringAssert.Contains(_TestMessage, error, "Error message wrong");
            },
            ELogLevel.Verbose);

            // Add log entry
            CLog.Information(new Exception(_TestExceptionMessage), _TestMessage, show);
            CLog.CSongLog.Information(_TestMessage);

            // Close logfile
            CLog.Close();

            // Check log
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileName)), "Mainlog file is missing.");
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileSongName)), "Songlog file is missing.");

            string mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            string songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            StringAssert.Contains("[Information] Starting to log", mainLogContent, "Main log start entry wrong");
            StringAssert.Contains("Version = " + versionTag, mainLogContent, "Main log version tag entry wrong");
            StringAssert.Contains("[Information] " +_TestMessage, mainLogContent, "Main log entry wrong");
            StringAssert.Contains(_TestExceptionMessage, mainLogContent, "Exception is missing");
            StringAssert.Contains(_TestMessage, songLogContent, "Song log entry wrong");
            if(!show)
            {
                Assert.IsFalse(messageShown);
            }
        }

        [Test]
        public void InformationTestWithoutDataWithoutException([Values(true, false)] bool show)
        {
            string testFileName = Path.GetRandomFileName();
            string testFileSongName = Path.GetRandomFileName();
            string testFileMarkerName = Path.GetRandomFileName();
            string versionTag = "Test Version (1.2.4)";
            bool messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
            {
                messageShown = true;
                Assert.IsTrue(show);
                Assert.IsFalse(crash);
                Assert.IsTrue(cont);
                Assert.AreEqual(versionTag, tag);

                StringAssert.Contains("[Information] Starting to log", log, "Main log start entry wrong");
                StringAssert.Contains("Version = " + versionTag, log, "Main log version tag entry wrong");
                StringAssert.Contains("[Information] " + _TestMessage, log, "Main log entry wrong");

                StringAssert.Contains(_TestMessage, error, "Error message wrong");
            },
            ELogLevel.Verbose);

            // Add log entry
            CLog.Information(_TestMessage, show);
            CLog.CSongLog.Information(_TestMessage);

            // Close logfile
            CLog.Close();

            // Check log
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileName)), "Mainlog file is missing.");
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileSongName)), "Songlog file is missing.");

            string mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            string songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            StringAssert.Contains("[Information] Starting to log", mainLogContent, "Main log start entry wrong");
            StringAssert.Contains("Version = " + versionTag, mainLogContent, "Main log version tag entry wrong");
            StringAssert.Contains("[Information] " +_TestMessage, mainLogContent, "Main log entry wrong");
            StringAssert.Contains(_TestMessage, songLogContent, "Song log entry wrong");
            if(!show)
            {
                Assert.IsFalse(messageShown);
            }
        }

        #endregion
        #region Warning tests

        [Test]
        public void WarningTestWithDataWithException([Values(true, false)] bool show)
        {
            string testFileName = Path.GetRandomFileName();
            string testFileSongName = Path.GetRandomFileName();
            string testFileMarkerName = Path.GetRandomFileName();
            string versionTag = "Test Version (1.2.4)";
            bool messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
            {
                messageShown = true;
                Assert.IsTrue(show);
                Assert.IsFalse(crash);
                Assert.IsTrue(cont);
                Assert.AreEqual(versionTag, tag);

                StringAssert.Contains("[Information] Starting to log", log, "Main log start entry wrong");
                StringAssert.Contains("Version = " + versionTag, log, "Main log version tag entry wrong");
                StringAssert.Contains("[Warning] " + _TestMessageWithResolvedData, log, "Main log entry wrong");

                StringAssert.Contains(_TestMessageWithData, error, "Error message wrong");
            },
            ELogLevel.Verbose);

            // Add log entry
            CLog.Warning(new Exception(_TestExceptionMessage), _TestMessageWithData, CLog.Params(_FirstParam, _SecondParam), show);
            CLog.CSongLog.Warning(_TestMessageWithData, CLog.Params(_FirstParam, _SecondParam));

            // Close logfile
            CLog.Close();

            // Check log
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileName)), "Mainlog file is missing.");
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileSongName)), "Songlog file is missing.");

            string mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            string songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            StringAssert.Contains("[Information] Starting to log", mainLogContent, "Main log start entry wrong");
            StringAssert.Contains("Version = " + versionTag, mainLogContent, "Main log version tag entry wrong");
            StringAssert.Contains("[Warning] " +_TestMessageWithResolvedData, mainLogContent, "Main log entry wrong");
            StringAssert.Contains(_TestExceptionMessage, mainLogContent, "Exception is missing");
            StringAssert.Contains(_SecondParam, mainLogContent, "Second data field is missing");
            StringAssert.Contains(_TestMessageWithResolvedData, songLogContent, "Song log entry wrong");
            if(!show)
            {
                Assert.IsFalse(messageShown);
            }
        }

        [Test]
        public void WarningTestWithDataWithoutException([Values(true, false)] bool show)
        {
            string testFileName = Path.GetRandomFileName();
            string testFileSongName = Path.GetRandomFileName();
            string testFileMarkerName = Path.GetRandomFileName();
            string versionTag = "Test Version (1.2.4)";
            bool messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
            {
                messageShown = true;
                Assert.IsTrue(show);
                Assert.IsFalse(crash);
                Assert.IsTrue(cont);
                Assert.AreEqual(versionTag, tag);

                StringAssert.Contains("[Information] Starting to log", log, "Main log start entry wrong");
                StringAssert.Contains("Version = " + versionTag, log, "Main log version tag entry wrong");
                StringAssert.Contains("[Warning] " + _TestMessageWithResolvedData, log, "Main log entry wrong");

                StringAssert.Contains(_TestMessageWithData, error, "Error message wrong");
            },
            ELogLevel.Verbose);

            // Add log entry
            CLog.Warning(_TestMessageWithData, CLog.Params(_FirstParam, _SecondParam), show);
            CLog.CSongLog.Warning(_TestMessageWithData, CLog.Params(_FirstParam, _SecondParam));

            // Close logfile
            CLog.Close();

            // Check log
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileName)), "Mainlog file is missing.");
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileSongName)), "Songlog file is missing.");

            string mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            string songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            StringAssert.Contains("[Information] Starting to log", mainLogContent, "Main log start entry wrong");
            StringAssert.Contains("Version = " + versionTag, mainLogContent, "Main log version tag entry wrong");
            StringAssert.Contains("[Warning] " +_TestMessageWithResolvedData, mainLogContent, "Main log entry wrong");
            StringAssert.Contains(_SecondParam, mainLogContent, "Second data field is missing");
            StringAssert.Contains(_TestMessageWithResolvedData, songLogContent, "Song log entry wrong");
            if(!show)
            {
                Assert.IsFalse(messageShown);
            }
        }

        [Test]
        public void WarningTestWithoutDataWithException([Values(true, false)] bool show)
        {
            string testFileName = Path.GetRandomFileName();
            string testFileSongName = Path.GetRandomFileName();
            string testFileMarkerName = Path.GetRandomFileName();
            string versionTag = "Test Version (1.2.4)";
            bool messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
            {
                messageShown = true;
                Assert.IsTrue(show);
                Assert.IsFalse(crash);
                Assert.IsTrue(cont);
                Assert.AreEqual(versionTag, tag);

                StringAssert.Contains("[Information] Starting to log", log, "Main log start entry wrong");
                StringAssert.Contains("Version = " + versionTag, log, "Main log version tag entry wrong");
                StringAssert.Contains("[Warning] " + _TestMessage, log, "Main log entry wrong");

                StringAssert.Contains(_TestMessage, error, "Error message wrong");
            },
            ELogLevel.Verbose);

            // Add log entry
            CLog.Warning(new Exception(_TestExceptionMessage), _TestMessage, show);
            CLog.CSongLog.Warning(_TestMessage);

            // Close logfile
            CLog.Close();

            // Check log
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileName)), "Mainlog file is missing.");
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileSongName)), "Songlog file is missing.");

            string mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            string songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            StringAssert.Contains("[Information] Starting to log", mainLogContent, "Main log start entry wrong");
            StringAssert.Contains("Version = " + versionTag, mainLogContent, "Main log version tag entry wrong");
            StringAssert.Contains("[Warning] " +_TestMessage, mainLogContent, "Main log entry wrong");
            StringAssert.Contains(_TestExceptionMessage, mainLogContent, "Exception is missing");
            StringAssert.Contains(_TestMessage, songLogContent, "Song log entry wrong");
            if(!show)
            {
                Assert.IsFalse(messageShown);
            }
        }

        [Test]
        public void WarningTestWithoutDataWithoutException([Values(true, false)] bool show)
        {
            string testFileName = Path.GetRandomFileName();
            string testFileSongName = Path.GetRandomFileName();
            string testFileMarkerName = Path.GetRandomFileName();
            string versionTag = "Test Version (1.2.4)";
            bool messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
            {
                messageShown = true;
                Assert.IsTrue(show);
                Assert.IsFalse(crash);
                Assert.IsTrue(cont);
                Assert.AreEqual(versionTag, tag);

                StringAssert.Contains("[Information] Starting to log", log, "Main log start entry wrong");
                StringAssert.Contains("Version = " + versionTag, log, "Main log version tag entry wrong");
                StringAssert.Contains("[Warning] " + _TestMessage, log, "Main log entry wrong");

                StringAssert.Contains(_TestMessage, error, "Error message wrong");
            },
            ELogLevel.Verbose);

            // Add log entry
            CLog.Warning(_TestMessage, show);
            CLog.CSongLog.Warning(_TestMessage);

            // Close logfile
            CLog.Close();

            // Check log
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileName)), "Mainlog file is missing.");
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileSongName)), "Songlog file is missing.");

            string mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            string songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            StringAssert.Contains("[Information] Starting to log", mainLogContent, "Main log start entry wrong");
            StringAssert.Contains("Version = " + versionTag, mainLogContent, "Main log version tag entry wrong");
            StringAssert.Contains("[Warning] " +_TestMessage, mainLogContent, "Main log entry wrong");
            StringAssert.Contains(_TestMessage, songLogContent, "Song log entry wrong");
            if(!show)
            {
                Assert.IsFalse(messageShown);
            }
        }

        #endregion
        #region Error tests

        [Test]
        public void ErrorTestWithDataWithException([Values(true, false)] bool show)
        {
            string testFileName = Path.GetRandomFileName();
            string testFileSongName = Path.GetRandomFileName();
            string testFileMarkerName = Path.GetRandomFileName();
            string versionTag = "Test Version (1.2.4)";
            bool messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
            {
                messageShown = true;
                Assert.IsTrue(show);
                Assert.IsFalse(crash);
                Assert.IsTrue(cont);
                Assert.AreEqual(versionTag, tag);

                StringAssert.Contains("[Information] Starting to log", log, "Main log start entry wrong");
                StringAssert.Contains("Version = " + versionTag, log, "Main log version tag entry wrong");
                StringAssert.Contains("[Error] " + _TestMessageWithResolvedData, log, "Main log entry wrong");

                StringAssert.Contains(_TestMessageWithData, error, "Error message wrong");
            },
            ELogLevel.Verbose);

            // Add log entry
            CLog.Error(new Exception(_TestExceptionMessage), _TestMessageWithData, CLog.Params(_FirstParam, _SecondParam), show);
            CLog.CSongLog.Error(_TestMessageWithData, CLog.Params(_FirstParam, _SecondParam));

            // Close logfile
            CLog.Close();

            // Check log
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileName)), "Mainlog file is missing.");
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileSongName)), "Songlog file is missing.");

            string mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            string songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            StringAssert.Contains("[Information] Starting to log", mainLogContent, "Main log start entry wrong");
            StringAssert.Contains("Version = " + versionTag, mainLogContent, "Main log version tag entry wrong");
            StringAssert.Contains("[Error] " +_TestMessageWithResolvedData, mainLogContent, "Main log entry wrong");
            StringAssert.Contains(_TestExceptionMessage, mainLogContent, "Exception is missing");
            StringAssert.Contains(_SecondParam, mainLogContent, "Second data field is missing");
            StringAssert.Contains(_TestMessageWithResolvedData, songLogContent, "Song log entry wrong");
            if(!show)
            {
                Assert.IsFalse(messageShown);
            }
        }

        [Test]
        public void ErrorTestWithDataWithoutException([Values(true, false)] bool show)
        {
            string testFileName = Path.GetRandomFileName();
            string testFileSongName = Path.GetRandomFileName();
            string testFileMarkerName = Path.GetRandomFileName();
            string versionTag = "Test Version (1.2.4)";
            bool messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
            {
                messageShown = true;
                Assert.IsTrue(show);
                Assert.IsFalse(crash);
                Assert.IsTrue(cont);
                Assert.AreEqual(versionTag, tag);

                StringAssert.Contains("[Information] Starting to log", log, "Main log start entry wrong");
                StringAssert.Contains("Version = " + versionTag, log, "Main log version tag entry wrong");
                StringAssert.Contains("[Error] " + _TestMessageWithResolvedData, log, "Main log entry wrong");

                StringAssert.Contains(_TestMessageWithData, error, "Error message wrong");
            },
            ELogLevel.Verbose);

            // Add log entry
            CLog.Error(_TestMessageWithData, CLog.Params(_FirstParam, _SecondParam), show);
            CLog.CSongLog.Error(_TestMessageWithData, CLog.Params(_FirstParam, _SecondParam));

            // Close logfile
            CLog.Close();

            // Check log
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileName)), "Mainlog file is missing.");
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileSongName)), "Songlog file is missing.");

            string mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            string songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            StringAssert.Contains("[Information] Starting to log", mainLogContent, "Main log start entry wrong");
            StringAssert.Contains("Version = " + versionTag, mainLogContent, "Main log version tag entry wrong");
            StringAssert.Contains("[Error] " +_TestMessageWithResolvedData, mainLogContent, "Main log entry wrong");
            StringAssert.Contains(_SecondParam, mainLogContent, "Second data field is missing");
            StringAssert.Contains(_TestMessageWithResolvedData, songLogContent, "Song log entry wrong");
            if(!show)
            {
                Assert.IsFalse(messageShown);
            }
        }

        [Test]
        public void ErrorTestWithoutDataWithException([Values(true, false)] bool show)
        {
            string testFileName = Path.GetRandomFileName();
            string testFileSongName = Path.GetRandomFileName();
            string testFileMarkerName = Path.GetRandomFileName();
            string versionTag = "Test Version (1.2.4)";
            bool messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
            {
                messageShown = true;
                Assert.IsTrue(show);
                Assert.IsFalse(crash);
                Assert.IsTrue(cont);
                Assert.AreEqual(versionTag, tag);

                StringAssert.Contains("[Information] Starting to log", log, "Main log start entry wrong");
                StringAssert.Contains("Version = " + versionTag, log, "Main log version tag entry wrong");
                StringAssert.Contains("[Error] " + _TestMessage, log, "Main log entry wrong");

                StringAssert.Contains(_TestMessage, error, "Error message wrong");
            },
            ELogLevel.Verbose);

            // Add log entry
            CLog.Error(new Exception(_TestExceptionMessage), _TestMessage, show);
            CLog.CSongLog.Error(_TestMessage);

            // Close logfile
            CLog.Close();

            // Check log
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileName)), "Mainlog file is missing.");
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileSongName)), "Songlog file is missing.");

            string mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            string songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            StringAssert.Contains("[Information] Starting to log", mainLogContent, "Main log start entry wrong");
            StringAssert.Contains("Version = " + versionTag, mainLogContent, "Main log version tag entry wrong");
            StringAssert.Contains("[Error] " +_TestMessage, mainLogContent, "Main log entry wrong");
            StringAssert.Contains(_TestExceptionMessage, mainLogContent, "Exception is missing");
            StringAssert.Contains(_TestMessage, songLogContent, "Song log entry wrong");
            if(!show)
            {
                Assert.IsFalse(messageShown);
            }
        }

        [Test]
        public void ErrorTestWithoutDataWithoutException([Values(true, false)] bool show)
        {
            string testFileName = Path.GetRandomFileName();
            string testFileSongName = Path.GetRandomFileName();
            string testFileMarkerName = Path.GetRandomFileName();
            string versionTag = "Test Version (1.2.4)";
            bool messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
            {
                messageShown = true;
                Assert.IsTrue(show);
                Assert.IsFalse(crash);
                Assert.IsTrue(cont);
                Assert.AreEqual(versionTag, tag);

                StringAssert.Contains("[Information] Starting to log", log, "Main log start entry wrong");
                StringAssert.Contains("Version = " + versionTag, log, "Main log version tag entry wrong");
                StringAssert.Contains("[Error] " + _TestMessage, log, "Main log entry wrong");

                StringAssert.Contains(_TestMessage, error, "Error message wrong");
            },
            ELogLevel.Verbose);

            // Add log entry
            CLog.Error(_TestMessage, show);
            CLog.CSongLog.Error(_TestMessage);

            // Close logfile
            CLog.Close();

            // Check log
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileName)), "Mainlog file is missing.");
            Assert.IsTrue(File.Exists(Path.Combine(_TestFolder, testFileSongName)), "Songlog file is missing.");

            string mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            string songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            StringAssert.Contains("[Information] Starting to log", mainLogContent, "Main log start entry wrong");
            StringAssert.Contains("Version = " + versionTag, mainLogContent, "Main log version tag entry wrong");
            StringAssert.Contains("[Error] " +_TestMessage, mainLogContent, "Main log entry wrong");
            StringAssert.Contains(_TestMessage, songLogContent, "Song log entry wrong");
            if(!show)
            {
                Assert.IsFalse(messageShown);
            }
        }

        #endregion

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
