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
using System.Windows.Forms;

namespace WebserverInitalConfig
{
    static class CProgram
    {
        // ReSharper disable InconsistentNaming
        private static void Main(string[] args)
            // ReSharper restore InconsistentNaming
        {
            int result = 0;
            try
            {
                int port;
                if (args.Length < 3 || !int.TryParse(args[1], out port))
                {
                    Console.WriteLine("Usage: VocaluxeServerConfig exeName port isHttps [reserve]");
                    result = -1;
                }
                else
                {
                    String exeName = args[0];
                    bool isSecure = (args[2].ToLower() == "true");
                    bool doReserve = (args.Length >= 4 && args[3].ToLower() == "true");
                    CConfigHttpApi config = new CConfigHttpApi(exeName, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, exeName), "0.0.0.0", port, true);
                    try
                    {
                        config.AddFirewallRule();
                        if (isSecure)
                            config.CreateAndAddCert(Dns.GetHostName());

                        if (doReserve || CConfigHttpApi.IsAdministrator())
                            config.ReserveUrl(isSecure);
                    }
                    catch (AuthenticationException)
                    {
                        ProcessStartInfo proc = new ProcessStartInfo
                            {
                                UseShellExecute = true,
                                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                                FileName = AppDomain.CurrentDomain.FriendlyName,
                                Arguments = String.Join(" ", args),
                                Verb = "runas",
                                CreateNoWindow = true,
                                WindowStyle = ProcessWindowStyle.Hidden,
                            };

                        try
                        {
                            using (Process p = Process.Start(proc))
                            {
                                p.WaitForExit();
                                result = p.ExitCode;
                                p.Close();
                            }
                        }
                        catch
                        {
                            // The user refused the elevation.
                            // Do nothing and return directly ...
                            result = -3;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Error while installing server: " + e.Message + "\r\n\r\nIn " + e.TargetSite + "\r\nBacktrace: " + e.StackTrace);
                result = -4;
            }
            Environment.Exit(result);
        }
    }
}