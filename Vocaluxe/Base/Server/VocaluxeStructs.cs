﻿using System;

namespace Vocaluxe.Base.Server
{
    public struct SLoginData
    {
        public byte[] SHA256;
    }

    public struct SPicture
    {
        public int Width;
        public int Height;
        public byte[] data;
    }

    public struct SProfile
    {
        public SPicture Avatar;
        public string PlayerName;
        public int Difficulty;
    }
}