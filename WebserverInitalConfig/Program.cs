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
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace WebserverInitalConfig
{
    class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: VocaluxeServerConfig port isHttps");
                Environment.Exit(-1);
            }

            CConfigHttpApi.AddFirewallRule(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Vocaluxe.exe"), Int32.Parse(args[0]), true);

            if (args[1].ToLower() == "true")
            {
                string hostname = Dns.GetHostName();
                X509Certificate2 cert = CConfigHttpApi.GetSelfSignedCert("CN=" + hostname + " C=DE O=Vocaluxe OU=Vocaluxe Server");
                CConfigHttpApi.AddCertToStore(cert);
                CConfigHttpApi.BindCert("0.0.0.0", Int32.Parse(args[0]), cert);
                CConfigHttpApi.ReserveUrl("https://+:" + args[0] + "/");
            }
            else
                CConfigHttpApi.ReserveUrl("http://+:" + args[0] + "/");
        }
    }
}