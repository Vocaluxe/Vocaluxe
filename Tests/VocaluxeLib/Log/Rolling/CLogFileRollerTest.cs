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
using VocaluxeLib.Log.Rolling;

namespace Tests.VocaluxeLib.Log.Rolling
{
    [TestFixture]
    public class CLogFileRollerTest
    {
        private const string _TestFileName = "Test.log";
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
        public void RollFilesTest([Range(0,4)] int numFilesExisting, [Range(0, 3)] int numOldFilesToKeep)
        {
            // Create existing files
            if (numFilesExisting > 0)
            {
                CreateFile(_TestFolder, _TestFileName, 0);
            }
            
            for (var i = 1; i < numFilesExisting; i++)
            {
                CreateFile(_TestFolder, GetFileName(i), i);
            }


            // Roll the files
            CLogFileRoller.RollLogs(Path.Combine(_TestFolder, _TestFileName), numOldFilesToKeep);
            

            // Check main file
            Assert.IsFalse(File.Exists(Path.Combine(_TestFolder, _TestFileName)), "Main file was not deleted.");
            //Check other files
            for (int i = 1; i <= Math.Max(numOldFilesToKeep, numFilesExisting); i++)
            {
                var fileToCheck = Path.Combine(_TestFolder, GetFileName(i));
                if (i <= Math.Min(numFilesExisting,numOldFilesToKeep))
                {
                    Assert.IsTrue(File.Exists(fileToCheck), $"File {GetFileName(i)} is missing.");
                    Assert.AreEqual((i - 1).ToString(), File.ReadAllText(fileToCheck), "Rotation is wrong");
                }
                else
                {
                    Assert.IsFalse(File.Exists(fileToCheck));
                }
            }
        }

        #endregion

        #region Helper methods

        private string _GetTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        private void CreateFile(string folder, string file, int i)
        {
            File.WriteAllText(Path.Combine(folder, file), i.ToString());
        }

        private string GetFileName(int i)
        {
            return _TestFileName.Replace(".", $"_{i}.");
        }

        #endregion

    }
}
