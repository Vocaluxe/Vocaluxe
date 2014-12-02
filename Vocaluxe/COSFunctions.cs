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
        private static class CWindowsFunctions
        {
            // ReSharper disable MemberHidesStaticFromOuterClass
            [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetForegroundWindow(IntPtr hWnd);

            // ReSharper restore MemberHidesStaticFromOuterClass
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

        public static void SetForegroundWindow(IntPtr hWnd)
        {
#if WIN
            CWindowsFunctions.SetForegroundWindow(hWnd);
#endif
        }
    }
}