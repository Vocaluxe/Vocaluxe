using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace DiffieHellman
{
    internal class StrongNumberProvider
    {
        private static RNGCryptoServiceProvider csp = new RNGCryptoServiceProvider();

        public uint NextUInt32()
        {
            byte[] res = new byte[4];
            csp.GetBytes(res);
            return BitConverter.ToUInt32(res, 0);
        }

        public int NextInt()
        {
            byte[] res = new byte[4];
            csp.GetBytes(res);
            return BitConverter.ToInt32(res, 0);
        }

        public Single NextSingle()
        {
            float numerator = NextUInt32();
            float denominator = uint.MaxValue;
            return numerator / denominator;
        }
    }
}
