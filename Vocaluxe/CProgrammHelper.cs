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
using System.Security;
using Microsoft.Win32;
using Vocaluxe.Base;
using VocaluxeLib.Log;

namespace Vocaluxe
{
    /// <summary>
    ///     Some helper functions for the programm
    /// </summary>
    static class CProgrammHelper
    {
        private static bool _CheckUninstallKey(string name, string key)
        {
            using (var rk = Registry.LocalMachine.OpenSubKey(key))
            {
                if (rk == null)
                {
                    throw new SecurityException();
                }

                foreach (var skName in rk.GetSubKeyNames())
                {
                    using (var sk = rk.OpenSubKey(skName))
                    {
                        if (sk == null || sk.GetValue("DisplayName") == null)
                        {
                            continue;
                        }

                        var displayName = sk.GetValue("DisplayName").ToString().ToLower();
                        if (displayName.Equals(name) || displayName.StartsWith(name) || name.StartsWith(displayName))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool _KeyExists(string key)
        {
            using (var rk = Registry.LocalMachine.OpenSubKey(key))
            {
                if (rk != null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Checks if a given program is installed by checking the Uninstall registry key.
        /// </summary>
        /// <param name="name">Name of the programm. Got to equal the displayname in the key or one needs to be the start of the other one. NOT case sensitive.</param>
        /// <returns></returns>
        private static bool _IsProgramInstalled(string name)
        {
            const string baseKey = @"SOFTWARE\";
            const string baseKey64 = @"SOFTWARE\WoW6432Node\";
            const string uninstallKey = @"Microsoft\Windows\CurrentVersion\Uninstall";
            name = name.ToLower();
            return _CheckUninstallKey(name, baseKey + uninstallKey) || (_KeyExists(baseKey64) && _CheckUninstallKey(name, baseKey64 + uninstallKey));
        }

        private static bool _IsVC2010Installed()
        {
            //Note: Maybe check for x64 or x86
            return _IsProgramInstalled("Microsoft Visual C++ 2010");
        }

        private static bool _IsVC2015To2022Installed()
        {
            const string baseKey = @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\";
            const string wow6432BaseKey = @"SOFTWARE\Wow6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\";

#if ARCH_X64
            string[] arches = { "x64" };
#elif ARCH_X86
            string[] arches = { "x86" };
#else
            string[] arches = { "x86", "x64" };
#endif

            foreach (var arch in arches)
            {
                using (var rk = Registry.LocalMachine.OpenSubKey(baseKey + arch) ??
                                Registry.LocalMachine.OpenSubKey(wow6432BaseKey + arch))
                {
                    if (rk == null)
                    {
                        continue;
                    }

                    var installed = rk.GetValue("Installed");
                    if (installed == null)
                    {
                        continue;
                    }

                    int installedValue;
                    if (int.TryParse(installed.ToString(), out installedValue) && installedValue == 1)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void _EnsureDataFolderExists()
        {
            if (!Directory.Exists(CSettings.DataFolder))
            {
                Directory.CreateDirectory(CSettings.DataFolder);
                // copy default profiles to DataFolder instead of adding ProgramFolder to CConfig.ProfileFolders
                // because we want to be able to edit them, but might not have permission to write to ProgramFolder
                var profilePath = Path.Combine(CSettings.DataFolder, CSettings.FolderNameProfiles);
                Directory.CreateDirectory(profilePath);
                var defaultProfileDir = new DirectoryInfo(Path.Combine(CSettings.ProgramFolder, CSettings.FolderNameProfiles));
                var files = defaultProfileDir.GetFiles();
                foreach (var file in files)
                {
                    var newPath = Path.Combine(profilePath, file.Name);
                    file.CopyTo(newPath, false);
                }
            }
        }

        public static bool CheckRequirements()
        {
#if WIN
            if (!_IsVC2010Installed())
            {
                CLog.Fatal(
                    "VC++ 2010 Redistributables are missing. Please install them first.\r\nDownload: https://www.microsoft.com/download/details.aspx?id=26999");
                return false;
            }

            if (!_IsVC2015To2022Installed())
                {
                    CLog.Fatal(
                        "VC++ 2015-2022 Redistributables are missing. Please install them first.\r\n" +
                        "Download x64: https://aka.ms/vc14/vc_redist.x64.exe\r\n" +
                        "Download x86: https://aka.ms/vc14/vc_redist.x86.exe");
                    return false;
                }

#endif //TODO: check for dependencies on linux?
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
            path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + Path.DirectorySeparatorChar + path;
            COSFunctions.AddEnvironmentPath(path);
#if LINUX
            _EnsureDataFolderExists();
#endif
        }
    }
}