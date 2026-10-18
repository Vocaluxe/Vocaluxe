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
            var testFileName = Path.GetRandomFileName();
            var testFileSongName = Path.GetRandomFileName();
            var testFileMarkerName = Path.GetRandomFileName();
            var versionTag = "Test Version (1.2.4)";
            var messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
                {
                    messageShown = true;
                    Assert.That(show, Is.True);
                    Assert.That(crash, Is.False);
                    Assert.That(cont, Is.True);
                    Assert.That(tag, Is.EqualTo(versionTag));

                    Assert.That(log, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
                    Assert.That(log, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
                    Assert.That(log, Contains.Substring("[Verbose] " + _TestMessageWithResolvedData), "Main log entry wrong");

                    Assert.That(error, Contains.Substring(_TestMessageWithResolvedData), "Error message wrong");
                },
                ELogLevel.Verbose);

            // Add log entry
            CLog.Verbose(new Exception(_TestExceptionMessage), _TestMessageWithData, CLog.Params(_FirstParam, _SecondParam), show);
            CLog.CSongLog.Verbose(_TestMessageWithData, CLog.Params(_FirstParam, _SecondParam));

            // Close logfile
            CLog.Close();

            // Check log
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileName)), Is.True, "Mainlog file is missing.");
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileSongName)), Is.True, "Songlog file is missing.");

            var mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            var songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            Assert.That(mainLogContent, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
            Assert.That(mainLogContent, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
            Assert.That(mainLogContent, Contains.Substring("[Verbose] " + _TestMessageWithResolvedData), "Main log entry wrong");
            Assert.That(mainLogContent, Contains.Substring(_TestExceptionMessage), "Exception is missing");
            Assert.That(mainLogContent, Contains.Substring(_SecondParam), "Second data field is missing");
            Assert.That(songLogContent, Contains.Substring(_TestMessageWithResolvedData), "Song log entry wrong");
            if (!show)
            {
                Assert.That(messageShown, Is.False);
            }
        }

        [Test]
        public void VerboseTestWithDataWithoutException([Values(true, false)] bool show)
        {
            var testFileName = Path.GetRandomFileName();
            var testFileSongName = Path.GetRandomFileName();
            var testFileMarkerName = Path.GetRandomFileName();
            var versionTag = "Test Version (1.2.4)";
            var messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
                {
                    messageShown = true;
                    Assert.That(show, Is.True);
                    Assert.That(crash, Is.False);
                    Assert.That(cont, Is.True);
                    Assert.That(tag, Is.EqualTo(versionTag));

                    Assert.That(log, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
                    Assert.That(log, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
                    Assert.That(log, Contains.Substring("[Verbose] " + _TestMessageWithResolvedData), "Main log entry wrong");

                    Assert.That(error, Contains.Substring(_TestMessageWithResolvedData), "Error message wrong");
                },
                ELogLevel.Verbose);

            // Add log entry
            CLog.Verbose(_TestMessageWithData, CLog.Params(_FirstParam, _SecondParam), show);
            CLog.CSongLog.Verbose(_TestMessageWithData, CLog.Params(_FirstParam, _SecondParam));

            // Close logfile
            CLog.Close();

            // Check log
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileName)), Is.True, "Mainlog file is missing.");
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileSongName)), Is.True, "Songlog file is missing.");

            var mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            var songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            Assert.That(mainLogContent, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
            Assert.That(mainLogContent, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
            Assert.That(mainLogContent, Contains.Substring("[Verbose] " + _TestMessageWithResolvedData), "Main log entry wrong");
            Assert.That(mainLogContent, Contains.Substring(_SecondParam), "Second data field is missing");
            Assert.That(songLogContent, Contains.Substring(_TestMessageWithResolvedData), "Song log entry wrong");
            if (!show)
            {
                Assert.That(messageShown, Is.False);
            }
        }

        [Test]
        public void VerboseTestWithoutDataWithException([Values(true, false)] bool show)
        {
            var testFileName = Path.GetRandomFileName();
            var testFileSongName = Path.GetRandomFileName();
            var testFileMarkerName = Path.GetRandomFileName();
            var versionTag = "Test Version (1.2.4)";
            var messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
                {
                    messageShown = true;
                    Assert.That(show, Is.True);
                    Assert.That(crash, Is.False);
                    Assert.That(cont, Is.True);
                    Assert.That(tag, Is.EqualTo(versionTag));

                    Assert.That(log, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
                    Assert.That(log, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
                    Assert.That(log, Contains.Substring("[Verbose] " + _TestMessage), "Main log entry wrong");

                    Assert.That(error, Contains.Substring(_TestMessage), "Error message wrong");
                },
                ELogLevel.Verbose);

            // Add log entry
            CLog.Verbose(new Exception(_TestExceptionMessage), _TestMessage, show);
            CLog.CSongLog.Verbose(_TestMessage);

            // Close logfile
            CLog.Close();

            // Check log
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileName)), Is.True, "Mainlog file is missing.");
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileSongName)), Is.True, "Songlog file is missing.");

            var mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            var songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            Assert.That(mainLogContent, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
            Assert.That(mainLogContent, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
            Assert.That(mainLogContent, Contains.Substring("[Verbose] " + _TestMessage), "Main log entry wrong");
            Assert.That(mainLogContent, Contains.Substring(_TestExceptionMessage), "Exception is missing");
            Assert.That(songLogContent, Contains.Substring(_TestMessage), "Song log entry wrong");
            if (!show)
            {
                Assert.That(messageShown, Is.False);
            }
        }

        [Test]
        public void VerboseTestWithoutDataWithoutException([Values(true, false)] bool show)
        {
            var testFileName = Path.GetRandomFileName();
            var testFileSongName = Path.GetRandomFileName();
            var testFileMarkerName = Path.GetRandomFileName();
            var versionTag = "Test Version (1.2.4)";
            var messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
                {
                    messageShown = true;
                    Assert.That(show, Is.True);
                    Assert.That(crash, Is.False);
                    Assert.That(cont, Is.True);
                    Assert.That(tag, Is.EqualTo(versionTag));

                    Assert.That(log, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
                    Assert.That(log, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
                    Assert.That(log, Contains.Substring("[Verbose] " + _TestMessage), "Main log entry wrong");

                    Assert.That(error, Contains.Substring(_TestMessage), "Error message wrong");
                },
                ELogLevel.Verbose);

            // Add log entry
            CLog.Verbose(_TestMessage, show);
            CLog.CSongLog.Verbose(_TestMessage);

            // Close logfile
            CLog.Close();

            // Check log
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileName)), Is.True, "Mainlog file is missing.");
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileSongName)), Is.True, "Songlog file is missing.");

            var mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            var songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            Assert.That(mainLogContent, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
            Assert.That(mainLogContent, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
            Assert.That(mainLogContent, Contains.Substring("[Verbose] " + _TestMessage), "Main log entry wrong");
            Assert.That(songLogContent, Contains.Substring(_TestMessage), "Song log entry wrong");
            if (!show)
            {
                Assert.That(messageShown, Is.False);
            }
        }
        #endregion

        #region Debug tests
        [Test]
        public void DebugTestWithDataWithException([Values(true, false)] bool show)
        {
            var testFileName = Path.GetRandomFileName();
            var testFileSongName = Path.GetRandomFileName();
            var testFileMarkerName = Path.GetRandomFileName();
            var versionTag = "Test Version (1.2.4)";
            var messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
                {
                    messageShown = true;
                    Assert.That(show, Is.True);
                    Assert.That(crash, Is.False);
                    Assert.That(cont, Is.True);
                    Assert.That(tag, Is.EqualTo(versionTag));

                    Assert.That(log, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
                    Assert.That(log, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
                    Assert.That(log, Contains.Substring("[Debug] " + _TestMessageWithResolvedData), "Main log entry wrong");

                    Assert.That(error, Contains.Substring(_TestMessageWithResolvedData), "Error message wrong");
                },
                ELogLevel.Verbose);

            // Add log entry
            CLog.Debug(new Exception(_TestExceptionMessage), _TestMessageWithData, CLog.Params(_FirstParam, _SecondParam), show);
            CLog.CSongLog.Debug(_TestMessageWithData, CLog.Params(_FirstParam, _SecondParam));

            // Close logfile
            CLog.Close();

            // Check log
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileName)), Is.True, "Mainlog file is missing.");
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileSongName)), Is.True, "Songlog file is missing.");

            var mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            var songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            Assert.That(mainLogContent, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
            Assert.That(mainLogContent, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
            Assert.That(mainLogContent, Contains.Substring("[Debug] " + _TestMessageWithResolvedData), "Main log entry wrong");
            Assert.That(mainLogContent, Contains.Substring(_TestExceptionMessage), "Exception is missing");
            Assert.That(mainLogContent, Contains.Substring(_SecondParam), "Second data field is missing");
            Assert.That(songLogContent, Contains.Substring(_TestMessageWithResolvedData), "Song log entry wrong");
            if (!show)
            {
                Assert.That(messageShown, Is.False);
            }
        }

        [Test]
        public void DebugTestWithDataWithoutException([Values(true, false)] bool show)
        {
            var testFileName = Path.GetRandomFileName();
            var testFileSongName = Path.GetRandomFileName();
            var testFileMarkerName = Path.GetRandomFileName();
            var versionTag = "Test Version (1.2.4)";
            var messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
                {
                    messageShown = true;
                    Assert.That(show, Is.True);
                    Assert.That(crash, Is.False);
                    Assert.That(cont, Is.True);
                    Assert.That(tag, Is.EqualTo(versionTag));

                    Assert.That(log, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
                    Assert.That(log, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
                    Assert.That(log, Contains.Substring("[Debug] " + _TestMessageWithResolvedData), "Main log entry wrong");

                    Assert.That(error, Contains.Substring(_TestMessageWithResolvedData), "Error message wrong");
                },
                ELogLevel.Verbose);

            // Add log entry
            CLog.Debug(_TestMessageWithData, CLog.Params(_FirstParam, _SecondParam), show);
            CLog.CSongLog.Debug(_TestMessageWithData, CLog.Params(_FirstParam, _SecondParam));

            // Close logfile
            CLog.Close();

            // Check log
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileName)), Is.True, "Mainlog file is missing.");
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileSongName)), Is.True, "Songlog file is missing.");

            var mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            var songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            Assert.That(mainLogContent, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
            Assert.That(mainLogContent, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
            Assert.That(mainLogContent, Contains.Substring("[Debug] " + _TestMessageWithResolvedData), "Main log entry wrong");
            Assert.That(mainLogContent, Contains.Substring(_SecondParam), "Second data field is missing");
            Assert.That(songLogContent, Contains.Substring(_TestMessageWithResolvedData), "Song log entry wrong");
            if (!show)
            {
                Assert.That(messageShown, Is.False);
            }
        }

        [Test]
        public void DebugTestWithoutDataWithException([Values(true, false)] bool show)
        {
            var testFileName = Path.GetRandomFileName();
            var testFileSongName = Path.GetRandomFileName();
            var testFileMarkerName = Path.GetRandomFileName();
            var versionTag = "Test Version (1.2.4)";
            var messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
                {
                    messageShown = true;
                    Assert.That(show, Is.True);
                    Assert.That(crash, Is.False);
                    Assert.That(cont, Is.True);
                    Assert.That(tag, Is.EqualTo(versionTag));

                    Assert.That(log, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
                    Assert.That(log, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
                    Assert.That(log, Contains.Substring("[Debug] " + _TestMessage), "Main log entry wrong");

                    Assert.That(error, Contains.Substring(_TestMessage), "Error message wrong");
                },
                ELogLevel.Verbose);

            // Add log entry
            CLog.Debug(new Exception(_TestExceptionMessage), _TestMessage, show);
            CLog.CSongLog.Debug(_TestMessage);

            // Close logfile
            CLog.Close();

            // Check log
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileName)), Is.True, "Mainlog file is missing.");
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileSongName)), Is.True, "Songlog file is missing.");

            var mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            var songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            Assert.That(mainLogContent, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
            Assert.That(mainLogContent, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
            Assert.That(mainLogContent, Contains.Substring("[Debug] " + _TestMessage), "Main log entry wrong");
            Assert.That(mainLogContent, Contains.Substring(_TestExceptionMessage), "Exception is missing");
            Assert.That(songLogContent, Contains.Substring(_TestMessage), "Song log entry wrong");
            if (!show)
            {
                Assert.That(messageShown, Is.False);
            }
        }

        [Test]
        public void DebugTestWithoutDataWithoutException([Values(true, false)] bool show)
        {
            var testFileName = Path.GetRandomFileName();
            var testFileSongName = Path.GetRandomFileName();
            var testFileMarkerName = Path.GetRandomFileName();
            var versionTag = "Test Version (1.2.4)";
            var messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
                {
                    messageShown = true;
                    Assert.That(show, Is.True);
                    Assert.That(crash, Is.False);
                    Assert.That(cont, Is.True);
                    Assert.That(tag, Is.EqualTo(versionTag));

                    Assert.That(log, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
                    Assert.That(log, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
                    Assert.That(log, Contains.Substring("[Debug] " + _TestMessage), "Main log entry wrong");

                    Assert.That(error, Contains.Substring(_TestMessage), "Error message wrong");
                },
                ELogLevel.Verbose);

            // Add log entry
            CLog.Debug(_TestMessage, show);
            CLog.CSongLog.Debug(_TestMessage);

            // Close logfile
            CLog.Close();

            // Check log
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileName)), Is.True, "Mainlog file is missing.");
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileSongName)), Is.True, "Songlog file is missing.");

            var mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            var songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            Assert.That(mainLogContent, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
            Assert.That(mainLogContent, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
            Assert.That(mainLogContent, Contains.Substring("[Debug] " + _TestMessage), "Main log entry wrong");
            Assert.That(songLogContent, Contains.Substring(_TestMessage), "Song log entry wrong");
            if (!show)
            {
                Assert.That(messageShown, Is.False);
            }
        }
        #endregion

        #region Information tests
        [Test]
        public void InformationTestWithDataWithException([Values(true, false)] bool show)
        {
            var testFileName = Path.GetRandomFileName();
            var testFileSongName = Path.GetRandomFileName();
            var testFileMarkerName = Path.GetRandomFileName();
            var versionTag = "Test Version (1.2.4)";
            var messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
                {
                    messageShown = true;
                    Assert.That(show, Is.True);
                    Assert.That(crash, Is.False);
                    Assert.That(cont, Is.True);
                    Assert.That(tag, Is.EqualTo(versionTag));

                    Assert.That(log, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
                    Assert.That(log, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
                    Assert.That(log, Contains.Substring("[Information] " + _TestMessageWithResolvedData), "Main log entry wrong");

                    Assert.That(error, Contains.Substring(_TestMessageWithResolvedData), "Error message wrong");
                },
                ELogLevel.Verbose);

            // Add log entry
            CLog.Information(new Exception(_TestExceptionMessage), _TestMessageWithData, CLog.Params(_FirstParam, _SecondParam), show);
            CLog.CSongLog.Information(_TestMessageWithData, CLog.Params(_FirstParam, _SecondParam));

            // Close logfile
            CLog.Close();

            // Check log
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileName)), Is.True, "Mainlog file is missing.");
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileSongName)), Is.True, "Songlog file is missing.");

            var mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            var songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            Assert.That(mainLogContent, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
            Assert.That(mainLogContent, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
            Assert.That(mainLogContent, Contains.Substring("[Information] " + _TestMessageWithResolvedData), "Main log entry wrong");
            Assert.That(mainLogContent, Contains.Substring(_TestExceptionMessage), "Exception is missing");
            Assert.That(mainLogContent, Contains.Substring(_SecondParam), "Second data field is missing");
            Assert.That(songLogContent, Contains.Substring(_TestMessageWithResolvedData), "Song log entry wrong");
            if (!show)
            {
                Assert.That(messageShown, Is.False);
            }
        }

        [Test]
        public void InformationTestWithDataWithoutException([Values(true, false)] bool show)
        {
            var testFileName = Path.GetRandomFileName();
            var testFileSongName = Path.GetRandomFileName();
            var testFileMarkerName = Path.GetRandomFileName();
            var versionTag = "Test Version (1.2.4)";
            var messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
                {
                    messageShown = true;
                    Assert.That(show, Is.True);
                    Assert.That(crash, Is.False);
                    Assert.That(cont, Is.True);
                    Assert.That(tag, Is.EqualTo(versionTag));

                    Assert.That(log, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
                    Assert.That(log, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
                    Assert.That(log, Contains.Substring("[Information] " + _TestMessageWithResolvedData), "Main log entry wrong");

                    Assert.That(error, Contains.Substring(_TestMessageWithResolvedData), "Error message wrong");
                },
                ELogLevel.Verbose);

            // Add log entry
            CLog.Information(_TestMessageWithData, CLog.Params(_FirstParam, _SecondParam), show);
            CLog.CSongLog.Information(_TestMessageWithData, CLog.Params(_FirstParam, _SecondParam));

            // Close logfile
            CLog.Close();

            // Check log
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileName)), Is.True, "Mainlog file is missing.");
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileSongName)), Is.True, "Songlog file is missing.");

            var mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            var songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            Assert.That(mainLogContent, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
            Assert.That(mainLogContent, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
            Assert.That(mainLogContent, Contains.Substring("[Information] " + _TestMessageWithResolvedData), "Main log entry wrong");
            Assert.That(mainLogContent, Contains.Substring(_SecondParam), "Second data field is missing");
            Assert.That(songLogContent, Contains.Substring(_TestMessageWithResolvedData), "Song log entry wrong");
            if (!show)
            {
                Assert.That(messageShown, Is.False);
            }
        }

        [Test]
        public void InformationTestWithoutDataWithException([Values(true, false)] bool show)
        {
            var testFileName = Path.GetRandomFileName();
            var testFileSongName = Path.GetRandomFileName();
            var testFileMarkerName = Path.GetRandomFileName();
            var versionTag = "Test Version (1.2.4)";
            var messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
                {
                    messageShown = true;
                    Assert.That(show, Is.True);
                    Assert.That(crash, Is.False);
                    Assert.That(cont, Is.True);
                    Assert.That(tag, Is.EqualTo(versionTag));

                    Assert.That(log, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
                    Assert.That(log, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
                    Assert.That(log, Contains.Substring("[Information] " + _TestMessage), "Main log entry wrong");

                    Assert.That(error, Contains.Substring(_TestMessage), "Error message wrong");
                },
                ELogLevel.Verbose);

            // Add log entry
            CLog.Information(new Exception(_TestExceptionMessage), _TestMessage, show);
            CLog.CSongLog.Information(_TestMessage);

            // Close logfile
            CLog.Close();

            // Check log
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileName)), Is.True, "Mainlog file is missing.");
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileSongName)), Is.True, "Songlog file is missing.");

            var mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            var songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            Assert.That(mainLogContent, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
            Assert.That(mainLogContent, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
            Assert.That(mainLogContent, Contains.Substring("[Information] " + _TestMessage), "Main log entry wrong");
            Assert.That(mainLogContent, Contains.Substring(_TestExceptionMessage), "Exception is missing");
            Assert.That(songLogContent, Contains.Substring(_TestMessage), "Song log entry wrong");
            if (!show)
            {
                Assert.That(messageShown, Is.False);
            }
        }

        [Test]
        public void InformationTestWithoutDataWithoutException([Values(true, false)] bool show)
        {
            var testFileName = Path.GetRandomFileName();
            var testFileSongName = Path.GetRandomFileName();
            var testFileMarkerName = Path.GetRandomFileName();
            var versionTag = "Test Version (1.2.4)";
            var messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
                {
                    messageShown = true;
                    Assert.That(show, Is.True);
                    Assert.That(crash, Is.False);
                    Assert.That(cont, Is.True);
                    Assert.That(tag, Is.EqualTo(versionTag));

                    Assert.That(log, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
                    Assert.That(log, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
                    Assert.That(log, Contains.Substring("[Information] " + _TestMessage), "Main log entry wrong");

                    Assert.That(error, Contains.Substring(_TestMessage), "Error message wrong");
                },
                ELogLevel.Verbose);

            // Add log entry
            CLog.Information(_TestMessage, show);
            CLog.CSongLog.Information(_TestMessage);

            // Close logfile
            CLog.Close();

            // Check log
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileName)), Is.True, "Mainlog file is missing.");
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileSongName)), Is.True, "Songlog file is missing.");

            var mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            var songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            Assert.That(mainLogContent, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
            Assert.That(mainLogContent, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
            Assert.That(mainLogContent, Contains.Substring("[Information] " + _TestMessage), "Main log entry wrong");
            Assert.That(songLogContent, Contains.Substring(_TestMessage), "Song log entry wrong");
            if (!show)
            {
                Assert.That(messageShown, Is.False);
            }
        }
        #endregion

        #region Warning tests
        [Test]
        public void WarningTestWithDataWithException([Values(true, false)] bool show)
        {
            var testFileName = Path.GetRandomFileName();
            var testFileSongName = Path.GetRandomFileName();
            var testFileMarkerName = Path.GetRandomFileName();
            var versionTag = "Test Version (1.2.4)";
            var messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
                {
                    messageShown = true;
                    Assert.That(show, Is.True);
                    Assert.That(crash, Is.False);
                    Assert.That(cont, Is.True);
                    Assert.That(tag, Is.EqualTo(versionTag));

                    Assert.That(log, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
                    Assert.That(log, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
                    Assert.That(log, Contains.Substring("[Warning] " + _TestMessageWithResolvedData), "Main log entry wrong");

                    Assert.That(error, Contains.Substring(_TestMessageWithResolvedData), "Error message wrong");
                },
                ELogLevel.Verbose);

            // Add log entry
            CLog.Warning(new Exception(_TestExceptionMessage), _TestMessageWithData, CLog.Params(_FirstParam, _SecondParam), show);
            CLog.CSongLog.Warning(_TestMessageWithData, CLog.Params(_FirstParam, _SecondParam));

            // Close logfile
            CLog.Close();

            // Check log
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileName)), Is.True, "Mainlog file is missing.");
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileSongName)), Is.True, "Songlog file is missing.");

            var mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            var songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            Assert.That(mainLogContent, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
            Assert.That(mainLogContent, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
            Assert.That(mainLogContent, Contains.Substring("[Warning] " + _TestMessageWithResolvedData), "Main log entry wrong");
            Assert.That(mainLogContent, Contains.Substring(_TestExceptionMessage), "Exception is missing");
            Assert.That(mainLogContent, Contains.Substring(_SecondParam), "Second data field is missing");
            Assert.That(songLogContent, Contains.Substring(_TestMessageWithResolvedData), "Song log entry wrong");
            if (!show)
            {
                Assert.That(messageShown, Is.False);
            }
        }

        [Test]
        public void WarningTestWithDataWithoutException([Values(true, false)] bool show)
        {
            var testFileName = Path.GetRandomFileName();
            var testFileSongName = Path.GetRandomFileName();
            var testFileMarkerName = Path.GetRandomFileName();
            var versionTag = "Test Version (1.2.4)";
            var messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
                {
                    messageShown = true;
                    Assert.That(show, Is.True);
                    Assert.That(crash, Is.False);
                    Assert.That(cont, Is.True);
                    Assert.That(tag, Is.EqualTo(versionTag));

                    Assert.That(log, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
                    Assert.That(log, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
                    Assert.That(log, Contains.Substring("[Warning] " + _TestMessageWithResolvedData), "Main log entry wrong");

                    Assert.That(error, Contains.Substring(_TestMessageWithResolvedData), "Error message wrong");
                },
                ELogLevel.Verbose);

            // Add log entry
            CLog.Warning(_TestMessageWithData, CLog.Params(_FirstParam, _SecondParam), show);
            CLog.CSongLog.Warning(_TestMessageWithData, CLog.Params(_FirstParam, _SecondParam));

            // Close logfile
            CLog.Close();

            // Check log
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileName)), Is.True, "Mainlog file is missing.");
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileSongName)), Is.True, "Songlog file is missing.");

            var mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            var songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            Assert.That(mainLogContent, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
            Assert.That(mainLogContent, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
            Assert.That(mainLogContent, Contains.Substring("[Warning] " + _TestMessageWithResolvedData), "Main log entry wrong");
            Assert.That(mainLogContent, Contains.Substring(_SecondParam), "Second data field is missing");
            Assert.That(songLogContent, Contains.Substring(_TestMessageWithResolvedData), "Song log entry wrong");
            if (!show)
            {
                Assert.That(messageShown, Is.False);
            }
        }

        [Test]
        public void WarningTestWithoutDataWithException([Values(true, false)] bool show)
        {
            var testFileName = Path.GetRandomFileName();
            var testFileSongName = Path.GetRandomFileName();
            var testFileMarkerName = Path.GetRandomFileName();
            var versionTag = "Test Version (1.2.4)";
            var messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
                {
                    messageShown = true;
                    Assert.That(show, Is.True);
                    Assert.That(crash, Is.False);
                    Assert.That(cont, Is.True);
                    Assert.That(tag, Is.EqualTo(versionTag));

                    Assert.That(log, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
                    Assert.That(log, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
                    Assert.That(log, Contains.Substring("[Warning] " + _TestMessage), "Main log entry wrong");

                    Assert.That(error, Contains.Substring(_TestMessage), "Error message wrong");
                },
                ELogLevel.Verbose);

            // Add log entry
            CLog.Warning(new Exception(_TestExceptionMessage), _TestMessage, show);
            CLog.CSongLog.Warning(_TestMessage);

            // Close logfile
            CLog.Close();

            // Check log
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileName)), Is.True, "Mainlog file is missing.");
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileSongName)), Is.True, "Songlog file is missing.");

            var mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            var songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            Assert.That(mainLogContent, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
            Assert.That(mainLogContent, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
            Assert.That(mainLogContent, Contains.Substring("[Warning] " + _TestMessage), "Main log entry wrong");
            Assert.That(mainLogContent, Contains.Substring(_TestExceptionMessage), "Exception is missing");
            Assert.That(songLogContent, Contains.Substring(_TestMessage), "Song log entry wrong");
            if (!show)
            {
                Assert.That(messageShown, Is.False);
            }
        }

        [Test]
        public void WarningTestWithoutDataWithoutException([Values(true, false)] bool show)
        {
            var testFileName = Path.GetRandomFileName();
            var testFileSongName = Path.GetRandomFileName();
            var testFileMarkerName = Path.GetRandomFileName();
            var versionTag = "Test Version (1.2.4)";
            var messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
                {
                    messageShown = true;
                    Assert.That(show, Is.True);
                    Assert.That(crash, Is.False);
                    Assert.That(cont, Is.True);
                    Assert.That(tag, Is.EqualTo(versionTag));

                    Assert.That(log, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
                    Assert.That(log, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
                    Assert.That(log, Contains.Substring("[Warning] " + _TestMessage), "Main log entry wrong");

                    Assert.That(error, Contains.Substring(_TestMessage), "Error message wrong");
                },
                ELogLevel.Verbose);

            // Add log entry
            CLog.Warning(_TestMessage, show);
            CLog.CSongLog.Warning(_TestMessage);

            // Close logfile
            CLog.Close();

            // Check log
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileName)), Is.True, "Mainlog file is missing.");
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileSongName)), Is.True, "Songlog file is missing.");

            var mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            var songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            Assert.That(mainLogContent, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
            Assert.That(mainLogContent, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
            Assert.That(mainLogContent, Contains.Substring("[Warning] " + _TestMessage), "Main log entry wrong");
            Assert.That(songLogContent, Contains.Substring(_TestMessage), "Song log entry wrong");
            if (!show)
            {
                Assert.That(messageShown, Is.False);
            }
        }
        #endregion

        #region Error tests
        [Test]
        public void ErrorTestWithDataWithException([Values(true, false)] bool show)
        {
            var testFileName = Path.GetRandomFileName();
            var testFileSongName = Path.GetRandomFileName();
            var testFileMarkerName = Path.GetRandomFileName();
            var versionTag = "Test Version (1.2.4)";
            var messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
                {
                    messageShown = true;
                    Assert.That(show, Is.True);
                    Assert.That(crash, Is.False);
                    Assert.That(cont, Is.True);
                    Assert.That(tag, Is.EqualTo(versionTag));

                    Assert.That(log, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
                    Assert.That(log, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
                    Assert.That(log, Contains.Substring("[Error] " + _TestMessageWithResolvedData), "Main log entry wrong");

                    Assert.That(error, Contains.Substring(_TestMessageWithResolvedData), "Error message wrong");
                },
                ELogLevel.Verbose);

            // Add log entry
            CLog.Error(new Exception(_TestExceptionMessage), _TestMessageWithData, CLog.Params(_FirstParam, _SecondParam), show);
            CLog.CSongLog.Error(_TestMessageWithData, CLog.Params(_FirstParam, _SecondParam));

            // Close logfile
            CLog.Close();

            // Check log
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileName)), Is.True, "Mainlog file is missing.");
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileSongName)), Is.True, "Songlog file is missing.");

            var mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            var songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            Assert.That(mainLogContent, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
            Assert.That(mainLogContent, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
            Assert.That(mainLogContent, Contains.Substring("[Error] " + _TestMessageWithResolvedData), "Main log entry wrong");
            Assert.That(mainLogContent, Contains.Substring(_TestExceptionMessage), "Exception is missing");
            Assert.That(mainLogContent, Contains.Substring(_SecondParam), "Second data field is missing");
            Assert.That(songLogContent, Contains.Substring(_TestMessageWithResolvedData), "Song log entry wrong");
            if (!show)
            {
                Assert.That(messageShown, Is.False);
            }
        }

        [Test]
        public void ErrorTestWithDataWithoutException([Values(true, false)] bool show)
        {
            var testFileName = Path.GetRandomFileName();
            var testFileSongName = Path.GetRandomFileName();
            var testFileMarkerName = Path.GetRandomFileName();
            var versionTag = "Test Version (1.2.4)";
            var messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
                {
                    messageShown = true;
                    Assert.That(show, Is.True);
                    Assert.That(crash, Is.False);
                    Assert.That(cont, Is.True);
                    Assert.That(tag, Is.EqualTo(versionTag));

                    Assert.That(log, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
                    Assert.That(log, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
                    Assert.That(log, Contains.Substring("[Error] " + _TestMessageWithResolvedData), "Main log entry wrong");

                    Assert.That(error, Contains.Substring(_TestMessageWithResolvedData), "Error message wrong");
                },
                ELogLevel.Verbose);

            // Add log entry
            CLog.Error(_TestMessageWithData, CLog.Params(_FirstParam, _SecondParam), show);
            CLog.CSongLog.Error(_TestMessageWithData, CLog.Params(_FirstParam, _SecondParam));

            // Close logfile
            CLog.Close();

            // Check log
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileName)), Is.True, "Mainlog file is missing.");
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileSongName)), Is.True, "Songlog file is missing.");

            var mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            var songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            Assert.That(mainLogContent, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
            Assert.That(mainLogContent, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
            Assert.That(mainLogContent, Contains.Substring("[Error] " + _TestMessageWithResolvedData), "Main log entry wrong");
            Assert.That(mainLogContent, Contains.Substring(_SecondParam), "Second data field is missing");
            Assert.That(songLogContent, Contains.Substring(_TestMessageWithResolvedData), "Song log entry wrong");
            if (!show)
            {
                Assert.That(messageShown, Is.False);
            }
        }

        [Test]
        public void ErrorTestWithoutDataWithException([Values(true, false)] bool show)
        {
            var testFileName = Path.GetRandomFileName();
            var testFileSongName = Path.GetRandomFileName();
            var testFileMarkerName = Path.GetRandomFileName();
            var versionTag = "Test Version (1.2.4)";
            var messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
                {
                    messageShown = true;
                    Assert.That(show, Is.True);
                    Assert.That(crash, Is.False);
                    Assert.That(cont, Is.True);
                    Assert.That(tag, Is.EqualTo(versionTag));

                    Assert.That(log, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
                    Assert.That(log, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
                    Assert.That(log, Contains.Substring("[Error] " + _TestMessage), "Main log entry wrong");

                    Assert.That(error, Contains.Substring(_TestMessage), "Error message wrong");
                },
                ELogLevel.Verbose);

            // Add log entry
            CLog.Error(new Exception(_TestExceptionMessage), _TestMessage, show);
            CLog.CSongLog.Error(_TestMessage);

            // Close logfile
            CLog.Close();

            // Check log
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileName)), Is.True, "Mainlog file is missing.");
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileSongName)), Is.True, "Songlog file is missing.");

            var mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            var songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            Assert.That(mainLogContent, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
            Assert.That(mainLogContent, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
            Assert.That(mainLogContent, Contains.Substring("[Error] " + _TestMessage), "Main log entry wrong");
            Assert.That(mainLogContent, Contains.Substring(_TestExceptionMessage), "Exception is missing");
            Assert.That(songLogContent, Contains.Substring(_TestMessage), "Song log entry wrong");
            if (!show)
            {
                Assert.That(messageShown, Is.False);
            }
        }

        [Test]
        public void ErrorTestWithoutDataWithoutException([Values(true, false)] bool show)
        {
            var testFileName = Path.GetRandomFileName();
            var testFileSongName = Path.GetRandomFileName();
            var testFileMarkerName = Path.GetRandomFileName();
            var versionTag = "Test Version (1.2.4)";
            var messageShown = false;

            // Init Log
            CLog.Init(_TestFolder, testFileName, testFileSongName, testFileMarkerName, versionTag,
                (crash, cont, tag, log, error) =>
                {
                    messageShown = true;
                    Assert.That(show, Is.True);
                    Assert.That(crash, Is.False);
                    Assert.That(cont, Is.True);
                    Assert.That(tag, Is.EqualTo(versionTag));

                    Assert.That(log, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
                    Assert.That(log, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
                    Assert.That(log, Contains.Substring("[Error] " + _TestMessage), "Main log entry wrong");

                    Assert.That(error, Contains.Substring(_TestMessage), "Error message wrong");
                },
                ELogLevel.Verbose);

            // Add log entry
            CLog.Error(_TestMessage, show);
            CLog.CSongLog.Error(_TestMessage);

            // Close logfile
            CLog.Close();

            // Check log
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileName)), Is.True, "Mainlog file is missing.");
            Assert.That(File.Exists(Path.Combine(_TestFolder, testFileSongName)), Is.True, "Songlog file is missing.");

            var mainLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileName));
            var songLogContent = File.ReadAllText(Path.Combine(_TestFolder, testFileSongName));

            Assert.That(mainLogContent, Contains.Substring("[Information] Starting to log"), "Main log start entry wrong");
            Assert.That(mainLogContent, Contains.Substring("Version = " + versionTag), "Main log version tag entry wrong");
            Assert.That(mainLogContent, Contains.Substring("[Error] " + _TestMessage), "Main log entry wrong");
            Assert.That(songLogContent, Contains.Substring(_TestMessage), "Song log entry wrong");
            if (!show)
            {
                Assert.That(messageShown, Is.False);
            }
        }
        #endregion
        #endregion

        #region Helper methods
        private string _GetTemporaryDirectory()
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }
        #endregion
    }
}