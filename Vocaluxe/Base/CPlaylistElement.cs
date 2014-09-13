#region license
// This file is part of Vocaluxe.
// 
// Vocaluxe is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Vocaluxe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
#endregion

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

        public CTextureRef Cover
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