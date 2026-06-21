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
using Vocaluxe.Lib.Sound.Sources;
using VocaluxeLib.Draw;
using VocaluxeLib.Songs;
using VocaluxeLib.Songs.Sources;

namespace Vocaluxe.Base
{
    class CPlaylistElement
    {
        public readonly CSong Song;
        private readonly string _MusicFilePath = string.Empty;

        public bool HasMetaData => Song != null;

        public int SongId => HasMetaData ? Song.Id : -1;

        public ISoundSource SoundSource => HasMetaData ? Song.GetAudio() : new CLocalFileSoundSource(_MusicFilePath);

        public string Title => HasMetaData ? Song.Title : "";

        public string Artist => HasMetaData ? Song.Artist : "";

        public float Start => HasMetaData ? Song.Start : 0f;

        public float End => HasMetaData ? Song.End : 0f;

        public CTextureRef Cover => HasMetaData ? Song.CoverTexture : CCover.NoCover;

        public float VideoGap => HasMetaData ? Song.VideoGap : 0;

        public CPlaylistElement(CSong song)
        {
            Song = song ?? throw new ArgumentNullException("song");
        }

        public CPlaylistElement(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            _MusicFilePath = filePath;
        }
    }
}