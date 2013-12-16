using Security.Cryptography;
using Security.Cryptography.X509Certificates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using NetFwTypeLib;

namespace WebserverInitalConfig
{
    public static class ConfigHttpApi
    {

        public static void reserveURL(string networkString)
        {
            HttpApi.ReserveURL(networkString, "D:(A;;GX;;;S-1-1-0)");
        }


        public static void bindCert(string ip, int port, X509Certificate2 cert)
        {
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(ip), port);
            if (HttpApi.QuerySslCertificateInfo(endpoint) != null)
            {
                HttpApi.DeleteCertificateBinding(endpoint);
            }

            HttpApi.BindCertificate(endpoint, cert.GetCertHash(), StoreName.My, new Guid("{7baf41f1-48b7-42cb-b145-f815499cb48f}"));
        }


        public static void addCertToStore(X509Certificate2 cert)
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);

            X509Certificate2Collection oldCerts = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName,
                cert.SubjectName.Name, false);

            foreach (var oldCert in oldCerts)
            {
                store.Remove(oldCert);
            }

            store.Add(cert);
            store.Close();
        }


        public static X509Certificate2 getSelfSignedCert(string subjectName)
        {
            var keyParam = new CngKeyCreationParameters
            {
                ExportPolicy = CngExportPolicies.AllowExport,
                KeyCreationOptions = CngKeyCreationOptions.MachineKey | CngKeyCreationOptions.OverwriteExistingKey,
                KeyUsage = CngKeyUsages.AllUsages,
                Provider = CngProvider.MicrosoftSoftwareKeyStorageProvider,
            };

            keyParam.Parameters.Add(new CngProperty("Length", BitConverter.GetBytes(2048), CngPropertyOptions.None));

            CngKey key = CngKey.Create(CngAlgorithm2.Rsa, Guid.NewGuid().ToString(), keyParam);

            X509CertificateCreationParameters param = new X509CertificateCreationParameters(
                new X500DistinguishedName(subjectName));
            param.SubjectName = new X500DistinguishedName(subjectName);
            param.EndTime = DateTime.Today.AddYears(20);
            //param.SignatureAlgorithm = X509CertificateSignatureAlgorithm.RsaSha512;

            OidCollection oc = new OidCollection();
            oc.Add(new Oid("1.3.6.1.5.5.7.3.1"));
            X509Extension eku = new X509EnhancedKeyUsageExtension(oc, true);
            param.Extensions.Add(eku);

            param.TakeOwnershipOfKey = true;

            byte[] rawData = key.CreateSelfSignedCertificate(param).Export(X509ContentType.Pfx, "");
            var cert = new X509Certificate2(rawData, "", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
            cert.FriendlyName = "Vocaluxe Server Certificate";
            return cert;
        }

        public static void addFirewallrule(string exePath, int port, bool isTCP)
        {
            INetFwMgr manage = (INetFwMgr)Activator.CreateInstance(
                        Type.GetTypeFromCLSID(new Guid("{304CE942-6E39-40D8-943A-B913C40C9CD4}")));
            bool isFirewallEnabled = manage.LocalPolicy.CurrentProfile.FirewallEnabled;

            if (isFirewallEnabled)
            {
                INetFwAuthorizedApplications applications = manage.LocalPolicy.CurrentProfile.AuthorizedApplications;
                if ((from INetFwAuthorizedApplication a in applications
                         where a.ProcessImageFileName == exePath
                         select a).Count() == 0)
                {
                    INetFwAuthorizedApplication application = (INetFwAuthorizedApplication)Activator.CreateInstance(
                        Type.GetTypeFromCLSID(new Guid("{EC9846B3-2762-4A6B-A214-6ACB603462D2}")));

                    application.Name = "Vocaluxe";
                    application.ProcessImageFileName = exePath;
                    application.Enabled = true;
                    applications.Add(application);
                }
                INetFwOpenPorts openPorts = manage.LocalPolicy.CurrentProfile.GloballyOpenPorts;
                if ((from INetFwOpenPort p in openPorts
                         where  (p.Port == port && 
                            (p.Protocol == (isTCP?NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP : NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_UDP)))
                         select p).Count() == 0)
                {
                    INetFwOpenPort openPort = (INetFwOpenPort)Activator.CreateInstance(
                        Type.GetTypeFromCLSID(new Guid("{0CA545C6-37AD-4A6C-BF92-9F7610067EF5}")));
                    openPort.Enabled = true;
                    openPort.Port = port;
                    openPort.Protocol = (isTCP ? NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP : NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_UDP);
                    openPort.Name = "Vocaluxe";

                    openPorts.Add(openPort);
                }
            }
        }
        /*public static X509Certificate2 getCert(string subject)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();

            Mono.Security.Authenticode.PrivateKey key = new Mono.Security.Authenticode.PrivateKey { RSA=RSA.Create()};
            
           
            Mono.Security.X509.X509CertificateBuilder x509 = new Mono.Security.X509.X509CertificateBuilder();
            x509.IssuerName = "CN=Vocaluxe Server";
            x509.NotAfter = DateTime.Today.AddYears(20);
            x509.NotBefore = DateTime.Today.AddDays(-1);
            x509.SubjectName = subject;
            x509.SerialNumber = new byte[] { 1 };
            x509.Version = 3;
            x509.SubjectPublicKey = rsa;

            ExtendedKeyUsageExtension eku = new ExtendedKeyUsageExtension();
            eku.KeyPurpose.Add("1.3.6.1.5.5.7.3.1");
            x509.Extensions.Add(eku);

            return new X509Certificate2(x509.Sign(rsa));
        }*/
    }
}
