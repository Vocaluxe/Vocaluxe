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

using Security.Cryptography;
using Security.Cryptography.X509Certificates;
using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using NetFwTypeLib;

namespace WebserverInitalConfig
{
    public static class CConfigHttpApi
    {
        // ReSharper disable InconsistentNaming
        private const String CLSID_NetFwMgr = "{304CE942-6E39-40D8-943A-B913C40C9CD4}";
        private const String CLSID_NetAuthApp = "{EC9846B3-2762-4A6B-A214-6ACB603462D2}";
        private const String CLSID_NetOpenPort = "{0CA545C6-37AD-4A6C-BF92-9F7610067EF5}";
        // ReSharper restore InconsistentNaming
        public static void ReserveUrl(string networkString)
        {
            HttpApi.ReserveURL(networkString, "D:(A;;GX;;;S-1-1-0)");
        }

        public static void BindCert(string ip, int port, X509Certificate2 cert)
        {
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(ip), port);
            if (HttpApi.QuerySslCertificateInfo(endpoint) != null)
                HttpApi.DeleteCertificateBinding(endpoint);

            HttpApi.BindCertificate(endpoint, cert.GetCertHash(), StoreName.My, new Guid("{7baf41f1-48b7-42cb-b145-f815499cb48f}"));
        }

        public static void AddCertToStore(X509Certificate2 cert)
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);

            X509Certificate2Collection oldCerts = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName,
                                                                          cert.SubjectName.Name, false);

            foreach (X509Certificate2 oldCert in oldCerts)
                store.Remove(oldCert);

            store.Add(cert);
            store.Close();
        }

        public static X509Certificate2 GetSelfSignedCert(string subjectName)
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

            X509CertificateCreationParameters param = new X509CertificateCreationParameters(new X500DistinguishedName(subjectName))
                {
                    SubjectName = new X500DistinguishedName(subjectName),
                    EndTime = DateTime.Today.AddYears(20) //,SignatureAlgorithm = X509CertificateSignatureAlgorithm.RsaSha512
                };

            OidCollection oc = new OidCollection {new Oid("1.3.6.1.5.5.7.3.1")};
            X509Extension eku = new X509EnhancedKeyUsageExtension(oc, true);
            param.Extensions.Add(eku);

            param.TakeOwnershipOfKey = true;

            byte[] rawData = key.CreateSelfSignedCertificate(param).Export(X509ContentType.Pfx, "");
            var cert = new X509Certificate2(rawData, "", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet)
                {
                    FriendlyName = "Vocaluxe Server Certificate"
                };
            return cert;
        }

        private static void _AddFirewallRuleForProfile(INetFwProfile profile, string exePath, int port, bool isTCP)
        {
            bool isFirewallEnabled = profile.FirewallEnabled;

            if (!isFirewallEnabled)
                return;
            INetFwAuthorizedApplications applications = profile.AuthorizedApplications;
            if (!(from INetFwAuthorizedApplication a in applications
                  where a.ProcessImageFileName == exePath
                  select a).Any())
            {
                INetFwAuthorizedApplication application = (INetFwAuthorizedApplication)Activator.CreateInstance(
                    Type.GetTypeFromCLSID(new Guid(CLSID_NetAuthApp)));

                application.Name = "Vocaluxe";
                application.ProcessImageFileName = exePath;
                application.Enabled = true;
                application.Scope = NET_FW_SCOPE_.NET_FW_SCOPE_ALL;
                applications.Add(application);
            }
            INetFwOpenPorts openPorts = profile.GloballyOpenPorts;
            NET_FW_IP_PROTOCOL_ protocol = isTCP ? NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP : NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_UDP;
            if (!(from INetFwOpenPort p in openPorts
                  where (p.Port == port &&
                         (p.Protocol == protocol))
                  select p).Any())
            {
                INetFwOpenPort openPort = (INetFwOpenPort)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid(CLSID_NetOpenPort)));
                openPort.Enabled = true;
                openPort.Port = port;
                openPort.Protocol = protocol;
                openPort.Name = "Vocaluxe(" + port + ")";
                openPort.Scope = NET_FW_SCOPE_.NET_FW_SCOPE_ALL;

                openPorts.Add(openPort);
            }
        }

        public static void AddFirewallRule(string exePath, int port, bool isTCP)
        {
            INetFwMgr manage = (INetFwMgr)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid(CLSID_NetFwMgr)));
            //AddFirewallRuleForProfile(manage.LocalPolicy.CurrentProfile, exePath, port, isTCP);
            //if (manage.CurrentProfileType != NET_FW_PROFILE_TYPE_.NET_FW_PROFILE_STANDARD)
            //Add to Home/Work(Private) and current net
            //TODO: This somewhow adds 2 entries each instead of only one that is activated for Home/Work and public nets
            _AddFirewallRuleForProfile(manage.LocalPolicy.GetProfileByType(NET_FW_PROFILE_TYPE_.NET_FW_PROFILE_STANDARD), exePath, port, isTCP);
            _AddFirewallRuleForProfile(manage.LocalPolicy.GetProfileByType(NET_FW_PROFILE_TYPE_.NET_FW_PROFILE_CURRENT), exePath, port, isTCP);
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