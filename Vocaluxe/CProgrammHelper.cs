﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using Microsoft.Win32;
using Vocaluxe.Base;

namespace Vocaluxe
{
    /// <summary>
    /// Some helper functions for the programm
    /// </summary>
    static class CProgrammHelper
    {
        private static bool _CheckUninstallKey(string name, string key)
        {
            using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(key))
            {
                if (rk == null)
                    throw new SecurityException();
                foreach (string skName in rk.GetSubKeyNames())
                {
                    using (RegistryKey sk = rk.OpenSubKey(skName))
                    {
                        if (sk == null || sk.GetValue("DisplayName") == null)
                            continue;
                        string displayName = sk.GetValue("DisplayName").ToString().ToLower();
                        if (displayName.Equals(name) || displayName.StartsWith(name) || name.StartsWith(displayName))
                            return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if a given program is installed by checking the Uninstall registry key.
        /// </summary>
        /// <param name="name">Name of the programm. Got to equal the displayname in the key or one needs to be the start of the other one. NOT case sensitive.</param>
        /// <returns></returns>
        private static bool _IsProgramInstalled(string name)
        {
            const string uninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            const string uninstallKeyx64 = @"SOFTWARE\WoW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
            name = name.ToLower();
            return _CheckUninstallKey(name, uninstallKey) || _CheckUninstallKey(name, uninstallKeyx64);
        }

        private static bool _SystemDllExists(string dllName)
        {
            const string sysDir = "%windir%\\system32\\";
            return File.Exists(Environment.ExpandEnvironmentVariables(sysDir + dllName + ".dll"));
        }

        private static bool _IsVC2012Installed()
        {
            return _SystemDllExists("msvcr110") && _SystemDllExists("msvcp110");
        }

        private static bool _IsVC2008Installed()
        {
            return _IsProgramInstalled("Microsoft Visual C++ 2008 Redistributable");
        }

        private static bool _IsVC2010Installed()
        {
            //Note: Maybe check for x64 or x86
            return _IsProgramInstalled("Microsoft Visual C++ 2010");
        }

        public static bool CheckRequirements()
        {
            if (!_IsVC2012Installed())
            {
                CLog.LogError(
                    "VC++ 2012 Redistributables are missing. Please install them first.\r\nDownload: http://www.microsoft.com/de-de/download/details.aspx?id=30679",
                    true, true);
                return false;
            }
            bool vc2008Installed = _IsVC2008Installed();
            if (!vc2008Installed)
            {
                CLog.LogError(
                    "VC++ 2008 Redistributables are missing. Portaudio might not be working.\r\nDownload: http://www.microsoft.com/de-de/download/details.aspx?id=29");
            }
            if (!vc2008Installed && !_IsVC2010Installed())
            {
                CLog.LogError(
                    "VC++ 2010 and 2008 Redistributables are missing. Please install them first. VC++ 2008 is preferred as Portaudio doesn't work with VC++ 2010.\r\nDownload(2008): http://www.microsoft.com/de-de/download/details.aspx?id=29 \r\nDownload(2010): http://www.microsoft.com/de-de/download/details.aspx?id=5555",
                    true, true);
                return false;
            }
            return true;
        }
    }
}