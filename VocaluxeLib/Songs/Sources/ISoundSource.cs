using System.IO;

namespace VocaluxeLib.Songs.Sources
{
    public interface ISoundSource
    {
        public string GetUri();
        public Stream GetStream();
        public string DisplayName { get; }
    }
}
