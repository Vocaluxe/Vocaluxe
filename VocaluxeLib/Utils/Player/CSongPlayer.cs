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
using System.IO;
using VocaluxeLib.Draw;
using VocaluxeLib.Songs;

namespace VocaluxeLib.Utils.Player
{
    public class CSongPlayer : CSoundPlayer
    {
        private CSong _Song;
        private bool _VideoEnabled;
        private CVideoStream _Video;
        private CFading _VideoFading;

        public bool VideoEnabled
        {
            get { return _VideoEnabled; }
            set
            {
                if (_VideoEnabled == value)
                    return;
                _VideoEnabled = value;
                if (_VideoEnabled)
                    _LoadVideo();
                else if (_Video != null)
                    CBase.Video.Close(ref _Video);
            }
        }
        public int SongID
        {
            get { return _Song == null ? -1 : _Song.ID; }
        }
        public bool SongHasVideo
        {
            get { return _Song != null && File.Exists(Path.Combine(_Song.Folder, _Song.VideoFileName)); }
        }

        public string ArtistAndTitle
        {
            get
            {
                if (_Song == null)
                    return "";
                if (_Song.Artist != "" && _Song.Title != "")
                    return _Song.Artist + " - " + _Song.Title;
                return Path.GetFileNameWithoutExtension(_Song.MP3FileName);
            }
        }

        public CTextureRef Cover
        {
            get { return _Song == null ? CBase.Cover.GetNoCover() : _Song.CoverTextureBig; }
        }

        public bool SongLoaded
        {
            get { return _StreamID != -1; }
        }

        public CSongPlayer(bool loop = false) : base(loop) {}

        public CSongPlayer(CSong song, bool loop = false, float position = 0f, bool autoplay = false)
            : base(Path.Combine(song.Folder, song.MP3FileName), loop, position, autoplay)
        {
            _Song = song;
            _LoadVideo();
        }

        public CSongPlayer(string file, bool loop = false, float position = 0f, bool autoplay = false)
            : base(file, loop, position, autoplay)
        {
            _Song = null;
        }

        public CTextureRef GetVideoTexture()
        {
            if (_Video == null || _Song == null)
                return null;
            if (CBase.Video.GetFrame(_Video, CBase.Sound.GetPosition(_StreamID)))
            {
                if (_VideoFading != null)
                {
                    bool finished;
                    _Video.Texture.Color.A = _VideoFading.GetValue(out finished);
                    if (finished)
                        _VideoFading = null;
                }
                return _Video.Texture;
            }
            return null;
        }

        public void Load(CSong song, float position = 0f, bool autoplay = false)
        {
            if (song == null)
                return;

            Stop();

            _Song = song;

            Load(Path.Combine(song.Folder, song.MP3FileName), position, autoplay);
            _LoadVideo();

            if (autoplay)
                Play();
        }

        public void LoadFile(string file, float position = 0f, bool autoplay = false)
        {
            if (String.IsNullOrEmpty(file))
                return;

            Stop();

            Load(file, position, autoplay);
        }

        public override void Stop()
        {
            base.Stop();

            _Song = null;

            if (_Video != null)
                CBase.Video.Close(ref _Video);
        }

        public override void TogglePause()
        {
            if (IsPlaying && _Video != null)
            {
                CBase.Video.Pause(_Video);
                CBase.Video.Skip(_Video, CBase.Sound.GetPosition(_StreamID) + _FadeTime, _Song.VideoGap);
            }
            else if (_Video != null)
                CBase.Video.Resume(_Video);
            base.TogglePause();
        }

        private void _LoadVideo()
        {
            if (_Song == null)
                return;

            string videoFilePath = Path.Combine(_Song.Folder, _Song.VideoFileName);
            if (_Video != null || String.IsNullOrEmpty(videoFilePath))
                return;
            _Video = CBase.Video.Load(videoFilePath);
            CBase.Video.Skip(_Video, 0f, _Song.VideoGap); //Use gap, otherwhise we will overwrite position in GetVideoTexture
            _VideoFading = new CFading(0f, 1f, 3f);

            if (IsPlaying)
            {
                CBase.Video.Skip(_Video, Position, _Song.VideoGap);
                CBase.Video.Resume(_Video);
            }
        }
    }
}