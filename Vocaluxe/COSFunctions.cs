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
using System.Runtime.InteropServices;

namespace Vocaluxe
{
    static class COSFunctions
    {
#if WIN
        private static bool _DefaultDllDirSet;
        private static bool _UsePath;
        private static bool _UseSetDllDir;

        private static class CWindowsFunctions
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public static extern void AddDllDirectory(string lpPathName);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public static extern void SetDllDirectory(string lpPathName);

            [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetForegroundWindow(IntPtr hWnd);

            [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetDefaultDllDirectories(int directoryFlags);

            // ReSharper disable InconsistentNaming
            public const int LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000;
            // ReSharper restore InconsistentNaming
        }
#endif

        public static void AddEnvironmentPath(string path)
        {
            string newPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            if (newPath.Length > 0)
                newPath += ";";
            newPath += path;

            Environment.SetEnvironmentVariable("PATH", newPath);
        }

        public static void AddLibrarySearchpath(string path)
        {
#if WIN
            if (!_DefaultDllDirSet)
            {
                try
                {
                    CWindowsFunctions.SetDefaultDllDirectories(CWindowsFunctions.LOAD_LIBRARY_SEARCH_DEFAULT_DIRS);
                }
                catch (Exception)
                {
                    _UsePath = true;
                    _UseSetDllDir = true;
                }
                _DefaultDllDirSet = true;
            }
            if (_UsePath)
                AddEnvironmentPath(path);
            if (!_UseSetDllDir)
            {
                try
                {
                    CWindowsFunctions.AddDllDirectory(path);
                }
                catch (Exception)
                {
                    _UseSetDllDir = true;
                }
            }
            if (_UseSetDllDir)
                CWindowsFunctions.SetDllDirectory(path);
#endif
        }

        public static void SetForegroundWindow(IntPtr hWnd)
        {
#if WIN
            CWindowsFunctions.SetForegroundWindow(hWnd);
#endif
        }
    }
}