using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

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

            ConfigHttpApi.addFirewallrule(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Vocaluxe.exe"), Int32.Parse(args[0]), true);

            if (args[1].ToLower() == "true")
            {
                string hostname = Dns.GetHostName();
                var cert = ConfigHttpApi.getSelfSignedCert("CN=" + hostname + " C=DE O=Vocaluxe OU=Vocaluxe Server");
                ConfigHttpApi.addCertToStore(cert);
                ConfigHttpApi.bindCert("0.0.0.0", Int32.Parse(args[0]), cert);
                ConfigHttpApi.reserveURL("https://+:" + args[0] + "/");
            }
            else
            {
                ConfigHttpApi.reserveURL("http://+:" + args[0] + "/");
            }
        }


    }
}
