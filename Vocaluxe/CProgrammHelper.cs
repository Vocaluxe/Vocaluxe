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

using System.IO;
using System.Reflection;
using Vocaluxe.Base;

namespace Vocaluxe
{
    /// <summary>
    ///     Some helper functions for the programm
    /// </summary>
    static class CProgrammHelper
    {
#if LINUX || MAC || INSTALLER
        /// <summary>
        ///     Copies the default profiles (e.g. the guests) from the program folder to the data folder on the first start.
        ///     The program folder might not be writable (e.g. C:\Program Files), so profiles must be edited in the data folder.
        ///     Avatars stay in the program folder (see CConfig.AvatarFolders).
        /// </summary>
        private static void _CopyDefaultProfiles()
        {
            var profilePath = Path.Combine(CSettings.DataFolder, CSettings.FolderNameProfiles);
            var defaultProfileDir = new DirectoryInfo(Path.Combine(CSettings.ProgramFolder, CSettings.FolderNameProfiles));
            if (Directory.Exists(profilePath) || !defaultProfileDir.Exists)
            {
                return;
            }

            Directory.CreateDirectory(profilePath);
            foreach (var file in defaultProfileDir.GetFiles("*.xml"))
            {
                file.CopyTo(Path.Combine(profilePath, file.Name), false);
            }
        }
#endif

        public static bool CheckRequirements()
        {
            //TODO: check for dependencies on linux?
            return true;
        }

        public static void Init()
        {
#if ARCH_X86
            string path = "x86";
#endif
#if ARCH_X64
            var path = "x64";
#endif
#if ARCH_ARM64
            var path = "arm64";
#endif
            path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + Path.DirectorySeparatorChar + path;
            COSFunctions.AddEnvironmentPath(path);
#if LINUX || MAC || INSTALLER
            _CopyDefaultProfiles();
#endif
        }
    }
}