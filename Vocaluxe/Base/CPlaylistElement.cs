using System;
using VocaluxeLib.Draw;
using VocaluxeLib.Songs;

namespace Vocaluxe.Base
{
    class CPlaylistElement
    {
        private readonly CSong _Song;
        private readonly string _MusicFilePath = String.Empty;

        public int SongID
        {
            get { return (_Song == null) ? -1 : _Song.ID; }
        }

        public string MusicFilePath
        {
            get { return _Song != null ? _Song.GetMP3() : _MusicFilePath; }
        }

        public string VideoFilePath
        {
            get { return _Song != null ? _Song.GetVideo() : string.Empty; }
        }

        public string Title
        {
            get { return _Song != null ? _Song.Title : ""; }
        }

        public string Artist
        {
            get { return _Song != null ? _Song.Artist : ""; }
        }

        public float Start
        {
            get { return _Song != null ? _Song.Start : 0f; }
        }

        public float Finish
        {
            get { return _Song != null ? _Song.Finish : 0f; }
        }

        public CTexture Cover
        {
            get { return _Song != null ? _Song.CoverTextureSmall : CCover.NoCover; }
        }

        public float VideoGap
        {
            get { return _Song != null ? _Song.VideoGap : 0; }
        }

        public CPlaylistElement(CSong song)
        {
            _Song = song;
        }

        public CPlaylistElement(string filePath)
        {
            _MusicFilePath = filePath;
        }
    }
}