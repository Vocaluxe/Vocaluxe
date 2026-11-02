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

using System.Security.Cryptography;
using System.Text;

namespace VocaluxeWeb.Services
{
    /// PINs are hashed with PBKDF2 (16 byte salt). Profiles from the old webserver use a single salted SHA-256 (32 byte salt);
    /// those are still accepted and upgraded on the next successful login.
    public static class CPinHasher
    {
        private const int _SaltSize = 16;
        private const int _LegacySaltSize = 32;
        private const int _HashSize = 32;
        private const int _Iterations = 100000;

        public static void Hash(string pin, out byte[] hash, out byte[] salt)
        {
            salt = new byte[_SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            hash = _Pbkdf2(pin, salt);
        }

        public static bool Verify(string pin, byte[] hash, byte[] salt)
        {
            if (hash == null || salt == null || pin == null)
            {
                return false;
            }

            var computed = salt.Length == _LegacySaltSize ? _LegacySha256(pin, salt) : _Pbkdf2(pin, salt);
            return _FixedTimeEquals(computed, hash);
        }

        public static bool NeedsUpgrade(byte[] salt)
        {
            return salt != null && salt.Length == _LegacySaltSize;
        }

        private static byte[] _Pbkdf2(string pin, byte[] salt)
        {
            return Rfc2898DeriveBytes.Pbkdf2(pin, salt, _Iterations, HashAlgorithmName.SHA256, _HashSize);
        }

        private static byte[] _LegacySha256(string pin, byte[] salt)
        {
            var password = Encoding.UTF8.GetBytes(pin);
            var data = new byte[password.Length + salt.Length];
            password.CopyTo(data, 0);
            salt.CopyTo(data, password.Length);
            using (var sha = SHA256.Create())
            {
                return sha.ComputeHash(data);
            }
        }

        private static bool _FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            var diff = 0;
            for (var i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }

            return diff == 0;
        }
    }
}
