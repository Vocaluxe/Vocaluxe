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
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using VocaluxeWeb.Services;

namespace Tests.VocaluxeWeb
{
    [TestFixture]
    public class CPinHasherTest
    {
        [Test]
        public void TestHashAndVerify()
        {
            CPinHasher.Hash("1234", out var hash, out var salt);
            Assert.That(CPinHasher.Verify("1234", hash, salt), Is.True);
            Assert.That(CPinHasher.Verify("4321", hash, salt), Is.False);
            Assert.That(CPinHasher.Verify("", hash, salt), Is.False);
            Assert.That(CPinHasher.NeedsUpgrade(salt), Is.False);
        }

        [Test]
        public void TestSaltIsRandom()
        {
            CPinHasher.Hash("1234", out var hash1, out var salt1);
            CPinHasher.Hash("1234", out var hash2, out var salt2);
            Assert.That(salt1, Is.Not.EqualTo(salt2));
            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public void TestMissingValues()
        {
            CPinHasher.Hash("1234", out var hash, out var salt);
            Assert.That(CPinHasher.Verify(null, hash, salt), Is.False);
            Assert.That(CPinHasher.Verify("1234", null, salt), Is.False);
            Assert.That(CPinHasher.Verify("1234", hash, null), Is.False);
        }

        [Test]
        public void TestLegacyPin()
        {
            var salt = new byte[32];
            new Random(1).NextBytes(salt);
            var hash = _LegacyHash("0815", salt);

            Assert.That(CPinHasher.NeedsUpgrade(salt), Is.True);
            Assert.That(CPinHasher.Verify("0815", hash, salt), Is.True);
            Assert.That(CPinHasher.Verify("0816", hash, salt), Is.False);
        }

        private static byte[] _LegacyHash(string pin, byte[] salt)
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
    }
}
