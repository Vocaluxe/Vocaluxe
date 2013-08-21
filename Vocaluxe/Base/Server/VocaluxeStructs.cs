namespace Vocaluxe.Base.Server
{
    public struct SLoginData
    {
        public byte[] Sha256;
    }

    public struct SAvatarPicture
    {
        public int Width;
        public int Height;
        public byte[] Data;
    }

    public struct SProfile
    {
        public SAvatarPicture Avatar;
        public string PlayerName;
        public int Difficulty;
    }
}