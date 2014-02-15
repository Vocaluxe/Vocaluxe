using System;
using System.IO;
using System.Net;

namespace WebserverInitalConfig
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: VocaluxeServerConfig port isHttps");
                Environment.Exit(-1);
            }

            CConfigHttpApi.AddFirewallRule(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Vocaluxe.exe"), Int32.Parse(args[0]), true);

            if (args[1].ToLower() == "true")
            {
                string hostname = Dns.GetHostName();
                var cert = CConfigHttpApi.GetSelfSignedCert("CN=" + hostname + " C=DE O=Vocaluxe OU=Vocaluxe Server");
                CConfigHttpApi.AddCertToStore(cert);
                CConfigHttpApi.BindCert("0.0.0.0", Int32.Parse(args[0]), cert);
                CConfigHttpApi.ReserveUrl("https://+:" + args[0] + "/");
            }
            else
            {
                CConfigHttpApi.ReserveUrl("http://+:" + args[0] + "/");
            }
        }


    }
}
