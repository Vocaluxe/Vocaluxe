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
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Authentication;

namespace WebserverInitalConfig
{
    static class CProgram
    {
        // ReSharper disable InconsistentNaming
        private static void Main(string[] args)
            // ReSharper restore InconsistentNaming
        {
            int port;
            if (args.Length < 2 || !int.TryParse(args[0], out port))
            {
                Console.WriteLine("Usage: VocaluxeServerConfig port isHttps");
                Environment.Exit(-1);
            }
            else
            {
                CConfigHttpApi config = new CConfigHttpApi("Vocaluxe", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Vocaluxe.exe"), "0.0.0.0", port, true);
                try
                {
                    config.AddFirewallRule();
                    if (args[1].ToLower() == "true")
                        config.CreateAndAddCert(Dns.GetHostName());

                    if ((args.Length >= 3 && args[2] == "reserve") || CConfigHttpApi.IsAdministrator())
                        config.ReserveUrl();
                }
                catch (AuthenticationException)
                {
                    ProcessStartInfo proc = new ProcessStartInfo
                        {
                            UseShellExecute = true,
                            WorkingDirectory = Environment.CurrentDirectory,
                            FileName = Process.GetCurrentProcess().MainModule.FileName,
                            Arguments = args[0] + " " + args[1],
                            Verb = "runas",
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden,
                        };

                    try
                    {
                        Process.Start(proc);
                    }
                    catch
                    {
                        // The user refused the elevation.
                        // Do nothing and return directly ...
                        Environment.Exit(-1);
                    }
                }
            }
        }
    }
}