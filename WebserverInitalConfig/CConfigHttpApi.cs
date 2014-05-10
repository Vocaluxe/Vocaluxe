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

using System.Security.Authentication;
using System.Security.Principal;
using System.Windows.Forms;
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
    public class CConfigHttpApi
    {
        // ReSharper disable InconsistentNaming
        private const String CLSID_NetFwMgr = "{304CE942-6E39-40D8-943A-B913C40C9CD4}";
        private const String CLSID_NetAuthApp = "{EC9846B3-2762-4A6B-A214-6ACB603462D2}";
        private const String CLSID_NetOpenPort = "{0CA545C6-37AD-4A6C-BF92-9F7610067EF5}";
        private const string _AppGUID = "{7baf41f1-48b7-42cb-b145-f815499cb48f}";
        // ReSharper restore InconsistentNaming

        private readonly string _RuleName;
        private readonly string _ExePath;
        private readonly string _IP;
        private readonly int _Port;
        private readonly bool _IsTCP;

        public CConfigHttpApi(string ruleName, string exePath, string ip, int port, bool isTCP)
        {
            _ExePath = exePath;
            _Port = port;
            _IsTCP = isTCP;
            _IP = ip;
            _RuleName = ruleName;
        }

        private static void _ReserveUrl(string networkString)
        {
            if (!IsAdministrator())
                throw new AuthenticationException();
            HttpApi.ReserveURL(networkString, "D:(A;;GX;;;S-1-1-0)");
        }

        public void ReserveUrl()
        {
            _ReserveUrl("https://+:" + _Port + "/");
        }

        private void _BindCert(X509Certificate cert)
        {
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(_IP), _Port);
            HttpApi.SslCertificateInfo certificateInfo = HttpApi.QuerySslCertificateInfo(endpoint);
            if (certificateInfo != null && !certificateInfo.Hash.SequenceEqual(cert.GetCertHash()))
            {
                if (!IsAdministrator())
                    throw new AuthenticationException();
                HttpApi.DeleteCertificateBinding(endpoint);

                HttpApi.BindCertificate(endpoint, cert.GetCertHash(), StoreName.My, new Guid(_AppGUID));
            }
        }

        private static void _AddCertToStore(X509Certificate2 cert)
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);

            // ReSharper disable AssignNullToNotNullAttribute
            X509Certificate2Collection oldCerts = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, cert.SubjectName.Name, false);
            // ReSharper restore AssignNullToNotNullAttribute

            foreach (X509Certificate2 oldCert in oldCerts)
                store.Remove(oldCert);

            store.Add(cert);
            store.Close();
        }

        private static X509Certificate2 _GetCert(string name)
        {
            X509Certificate2 result = null;
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);

            var name2 = new X500DistinguishedName(name);
            // ReSharper disable AssignNullToNotNullAttribute
            X509Certificate2Collection certs = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, name2.Name, false);
            // ReSharper restore AssignNullToNotNullAttribute
            if (certs.Count > 0)
                result = certs[0];

            store.Close();

            return result;
        }

        private X509Certificate2 _GetSelfSignedCert(string subjectName)
        {
            var keyParam = new CngKeyCreationParameters
                {
                    ExportPolicy = CngExportPolicies.AllowExport,
                    KeyCreationOptions = CngKeyCreationOptions.MachineKey | CngKeyCreationOptions.OverwriteExistingKey,
                    KeyUsage = CngKeyUsages.AllUsages,
                    Provider = CngProvider.MicrosoftSoftwareKeyStorageProvider,
                };

            keyParam.Parameters.Add(new CngProperty("Length", BitConverter.GetBytes(2048), CngPropertyOptions.None));

            CngKey key;
            try
            {
                key = CngKey.Create(CngAlgorithm2.Rsa, Guid.NewGuid().ToString(), keyParam);
            }
            catch (PlatformNotSupportedException)
            {
                try
                {
                    key = CngKey.Create(CngAlgorithm2.Aes, Guid.NewGuid().ToString(), keyParam);
                }
                catch (PlatformNotSupportedException)
                {
                    return null;
                }
            }

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
                    FriendlyName = _RuleName + " Server Certificate"
                };
            return cert;
        }

        public void CreateAndAddCert(string hostName)
        {
            string certName = "CN=" + hostName + ", C=DE, O=" + _RuleName + ", OU=" + _RuleName + " Server";

            X509Certificate2 cert = _GetCert(certName);
            if (cert == null)
            {
                if (!IsAdministrator())
                    throw new AuthenticationException();
                cert = _GetSelfSignedCert(certName);
                if (cert == null)
                {
                    MessageBox.Show("Could not create certificate. Secure server connection may not work. Disable it if required.");
                    return;
                }
                _AddCertToStore(cert);
            }
            _BindCert(cert);
        }

        private bool _IsAppAuthorized(INetFwProfile profile)
        {
            if (!profile.FirewallEnabled)
                return true;
            return profile.AuthorizedApplications.Cast<INetFwAuthorizedApplication>().Any(a => a.ProcessImageFileName == _ExePath);
        }

        private bool _IsPortOpen(INetFwProfile profile)
        {
            NET_FW_IP_PROTOCOL_ protocol = _GetProtocol();

            if (!profile.FirewallEnabled)
                return true;
            return profile.GloballyOpenPorts.Cast<INetFwOpenPort>().Any(p => p.Protocol == protocol && p.Port == _Port);
        }

        private NET_FW_IP_PROTOCOL_ _GetProtocol()
        {
            return _IsTCP ? NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP : NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_UDP;
        }

        private void _AddFirewallRuleForProfile(INetFwProfile profile)
        {
            bool isAppAuthorized = _IsAppAuthorized(profile);
            bool isPortOpen = _IsPortOpen(profile);

            if ((!isAppAuthorized || !isPortOpen) && !IsAdministrator())
                throw new AuthenticationException();
            if (!isAppAuthorized)
                _AddAppToFirewall(profile);

            if (!isPortOpen)
                _AddPortToFirewall(profile);
        }

        private void _AddPortToFirewall(INetFwProfile profile)
        {
            INetFwOpenPort openPort = (INetFwOpenPort)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid(CLSID_NetOpenPort)));
            openPort.Enabled = true;
            openPort.Port = _Port;
            openPort.Protocol = _GetProtocol();
            openPort.Name = _RuleName + "(" + _Port + ")";
            openPort.Scope = NET_FW_SCOPE_.NET_FW_SCOPE_ALL;

            profile.GloballyOpenPorts.Add(openPort);
        }

        private void _AddAppToFirewall(INetFwProfile profile)
        {
            INetFwAuthorizedApplication application = (INetFwAuthorizedApplication)Activator.CreateInstance(
                Type.GetTypeFromCLSID(new Guid(CLSID_NetAuthApp)));

            application.Name = _RuleName;
            application.ProcessImageFileName = _ExePath;
            application.Enabled = true;
            application.Scope = NET_FW_SCOPE_.NET_FW_SCOPE_ALL;
            profile.AuthorizedApplications.Add(application);
        }

        public static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            // ReSharper disable AssignNullToNotNullAttribute
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            // ReSharper restore AssignNullToNotNullAttribute
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public void AddFirewallRule()
        {
            INetFwMgr manage = (INetFwMgr)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid(CLSID_NetFwMgr)));
            //_AddFirewallRuleForProfile(manage.LocalPolicy.CurrentProfile);
            //if (manage.CurrentProfileType != NET_FW_PROFILE_TYPE_.NET_FW_PROFILE_STANDARD)
            //Add to Home/Work(Private) and current net
            //TODO: This somewhow adds 2 entries each instead of only one that is activated for Home/Work and public nets
            _AddFirewallRuleForProfile(manage.LocalPolicy.GetProfileByType(NET_FW_PROFILE_TYPE_.NET_FW_PROFILE_STANDARD));
            _AddFirewallRuleForProfile(manage.LocalPolicy.GetProfileByType(NET_FW_PROFILE_TYPE_.NET_FW_PROFILE_CURRENT));
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