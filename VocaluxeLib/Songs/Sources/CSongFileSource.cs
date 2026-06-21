using System;
using System.IO;

namespace VocaluxeLib.Songs.Sources
{
    public sealed class CSongFileSource : ISoundSource, IEquatable<CSongFileSource>
    {
        private readonly CSong _Song;
        private readonly string _FileName;

        public CSongFileSource(CSong song, string fileName)
        {
            _Song = song;
            _FileName = fileName;
        }

        public string GetUri()
        {
            return Path.Combine(_Song.Folder, _FileName);
        }

        public Stream GetStream()
        {
            return new FileStream(GetUri(), FileMode.Open, FileAccess.Read);
        }

        public string DisplayName => $"{_Song.Artist} - {_Song.Title}";

        public bool Equals(CSongFileSource other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Equals(_Song, other._Song) && _FileName == other._FileName;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is CSongFileSource other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((_Song != null ? _Song.GetHashCode() : 0) * 397) ^ (_FileName != null ? _FileName.GetHashCode() : 0);
            }
        }
    }
}
