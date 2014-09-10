using System;
using System.Diagnostics;
using System.Drawing;
using VocaluxeLib.Draw;

namespace Vocaluxe.Lib.Draw
{
    static class CDrawHelper
    {
        public static bool NonPowerOf2TextureSupported;

        private static readonly Object _MutexID = new object();
        private static int _NextID;

        /// <summary>
        ///     Calculates the next power of two if the device has the POW2 flag set
        /// </summary>
        /// <param name="n">The value of which the next power of two will be calculated</param>
        /// <returns>The next power of two</returns>
        private static int _CheckForNextPowerOf2(int n)
        {
            if (NonPowerOf2TextureSupported)
                return n;
            if (n < 0)
                throw new ArgumentOutOfRangeException("n", "Must be positive.");
            return (int)Math.Pow(2, Math.Ceiling(Math.Log(n, 2)));
        }

        public static CTexture GetNewTexture(int origWidth, int origHeight, int dataWidth = 0, int dataHeight = 0)
        {
            Debug.Assert(origWidth > 0 && origHeight > 0);
            Debug.Assert(dataWidth > 0 || dataHeight <= 0);
            int id;
            lock (_MutexID)
            {
                id = _NextID++;
            }
            Size origSize = new Size(origWidth, origHeight);
            Size dataSize = (dataHeight > 0) ? new Size(dataWidth, dataHeight) : origSize;
            return new CTexture(id, origSize, dataSize, _CheckForNextPowerOf2(dataSize.Width), _CheckForNextPowerOf2(dataSize.Height));
        }
    }
}