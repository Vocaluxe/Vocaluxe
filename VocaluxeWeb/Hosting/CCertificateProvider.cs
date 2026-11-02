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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace VocaluxeWeb.Hosting
{
    /// Self-signed certificate for the optional HTTPS mode. Stored as PFX file, no certificate store or admin rights needed.
    /// A new one is created when the file is missing, expired or does not cover the current addresses.
    static class CCertificateProvider
    {
        private const string _SubjectAlternativeNameOid = "2.5.29.17";

        public static X509Certificate2 GetOrCreate(string path, string hostName, IList<IPAddress> addresses)
        {
            if (File.Exists(path))
            {
                try
                {
                    var existing = X509CertificateLoader.LoadPkcs12(File.ReadAllBytes(path), null, X509KeyStorageFlags.UserKeySet);
                    if (existing.NotAfter > DateTime.Now.AddDays(7) && _Covers(existing, hostName, addresses))
                    {
                        return existing;
                    }
                }
                catch (CryptographicException)
                {
                    //Broken file, a new certificate is created below
                }
            }

            var pfx = _Create(hostName, addresses);
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(path, pfx);
            return X509CertificateLoader.LoadPkcs12(pfx, null, X509KeyStorageFlags.UserKeySet);
        }

        private static bool _Covers(X509Certificate2 cert, string hostName, IEnumerable<IPAddress> addresses)
        {
            var san = cert.Extensions[_SubjectAlternativeNameOid]?.Format(false) ?? "";
            return san.Contains(hostName) && addresses.All(a => san.Contains(a.ToString()));
        }

        private static byte[] _Create(string hostName, IEnumerable<IPAddress> addresses)
        {
            using (var rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest("CN=Vocaluxe (" + hostName + ")", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                var san = new SubjectAlternativeNameBuilder();
                san.AddDnsName(hostName);
                san.AddDnsName("localhost");
                san.AddIpAddress(IPAddress.Loopback);
                foreach (var address in addresses)
                {
                    san.AddIpAddress(address);
                }

                request.CertificateExtensions.Add(san.Build());
                request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
                request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
                request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false)); //Server authentication

                using (var cert = request.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(2)))
                {
                    return cert.Export(X509ContentType.Pfx);
                }
            }
        }
    }
}
