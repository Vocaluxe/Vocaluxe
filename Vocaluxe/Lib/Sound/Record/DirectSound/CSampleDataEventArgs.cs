using System;

namespace Vocaluxe.Lib.Sound.Record.DirectSound
{
    public class CSampleDataEventArgs : EventArgs
    {
        public readonly byte[] Data;
        public readonly Guid Guid;

        public CSampleDataEventArgs(byte[] data, Guid guid)
        {
            Data = data;
            Guid = guid;
        }
    }
}