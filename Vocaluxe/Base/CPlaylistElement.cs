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
        public readonly CSong Song;
        private readonly string _MusicFilePath = String.Empty;

        public bool HasMetaData
        {
            get { return Song != null; }
        }

        public int SongID
        {
            get { return HasMetaData ? Song.ID : -1; }
        }

        public string MusicFilePath
        {
            get { return HasMetaData ? Song.GetMP3() : _MusicFilePath; }
        }

        public string VideoFilePath
        {
            get { return HasMetaData ? Song.GetVideo() : string.Empty; }
        }

        public string Title
        {
            get { return HasMetaData ? Song.Title : ""; }
        }

        public string Artist
        {
            get { return HasMetaData ? Song.Artist : ""; }
        }

        public float Start
        {
            get { return HasMetaData ? Song.Start : 0f; }
        }

        public float Finish
        {
            get { return HasMetaData ? Song.Finish : 0f; }
        }

        public CTextureRef Cover
        {
            get { return HasMetaData ? Song.CoverTextureSmall : CCover.NoCover; }
        }

        public float VideoGap
        {
            get { return HasMetaData ? Song.VideoGap : 0; }
        }

        public CPlaylistElement(CSong song)
        {
            if (song == null)
                throw new ArgumentNullException("song");
            Song = song;
        }

        public CPlaylistElement(string filePath)
        {
            if (String.IsNullOrEmpty(filePath))
                throw new ArgumentNullException("filePath");
            _MusicFilePath = filePath;
        }
    }
}