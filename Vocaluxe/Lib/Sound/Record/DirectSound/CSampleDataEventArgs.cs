using System;

namespace Vocaluxe.Lib.Sound.Record.DirectSound
{
    public class CSampleDataEventArgs : EventArgs
    {
        public readonly byte[] Data;
        public readonly string Guid;

        public CSampleDataEventArgs(byte[] data, string guid)
        {
            Data = data;
            Guid = guid;
        }
    }
}