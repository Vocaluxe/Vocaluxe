using System;
using System.IO;
using VocaluxeLib.Songs.Sources;

namespace Vocaluxe.Lib.Sound.Sources
{
    public sealed class CLocalFileSoundSource : ISoundSource, IEquatable<CLocalFileSoundSource>
    {
        private readonly string _FilePath;

        public CLocalFileSoundSource(string filePath)
        {
            _FilePath = filePath;
        }

        public string GetUri()
        {
            return _FilePath;
        }
        public Stream GetStream()
        {
            return new FileStream(GetUri(), FileMode.Open, FileAccess.Read);
        }

        public string DisplayName => string.IsNullOrEmpty(_FilePath) ? "" : Path.GetFileNameWithoutExtension(_FilePath);

        public bool Equals(CLocalFileSoundSource other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return _FilePath == other._FilePath;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is CLocalFileSoundSource other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (_FilePath != null ? _FilePath.GetHashCode() : 0);
        }
    }
}
