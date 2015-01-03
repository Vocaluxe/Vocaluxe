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
                else
                    _CloseVideo();
            }
        }

        public int SongID
        {
            get { return _Song == null ? -1 : _Song.ID; }
        }

        public bool SongHasVideo
        {
            get
            {
                return _Song != null && !String.IsNullOrEmpty(_Song.Folder) && !String.IsNullOrEmpty(_Song.VideoFileName) &&
                       File.Exists(Path.Combine(_Song.Folder, _Song.VideoFileName));
            }
        }

        public override string ArtistAndTitle
        {
            get
            {
                if (_Song != null && !String.IsNullOrEmpty(_Song.Artist) && !String.IsNullOrEmpty(_Song.Title))
                    return _Song.Artist + " - " + _Song.Title;
                return base.ArtistAndTitle;
            }
        }

        public CTextureRef Cover
        {
            get { return _Song == null ? CBase.Cover.GetNoCover() : _Song.CoverTextureBig; }
        }

        public CSongPlayer(bool loop = false) : base(loop) {}

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
                throw new ArgumentNullException("song");

            Load(song.GetMP3(), position, autoplay);
            _Song = song;
            _LoadVideo();
        }

        private void _LoadVideo()
        {
            if (_Song == null)
                return;

            if (_Video != null || !SongHasVideo)
                return;
            string videoFilePath = Path.Combine(_Song.Folder, _Song.VideoFileName);
            _Video = CBase.Video.Load(videoFilePath);
            if (_Video == null)
                return;

            _VideoFading = new CFading(0f, 1f, 3f);

            if (IsPlaying)
            {
                CBase.Video.Skip(_Video, Position, _Song.VideoGap);
                CBase.Video.Resume(_Video);
            }
            else
                CBase.Video.Skip(_Video, 0f, _Song.VideoGap);
        }

        public override bool Play()
        {
            if (!base.Play())
                return false;

            if (_Video != null)
            {
                CBase.Video.Skip(_Video, Position, _Song.VideoGap);
                CBase.Video.Resume(_Video);
            }
            return true;
        }

        public override bool Pause()
        {
            if (!base.Pause())
                return false;

            if (_Video != null)
                CBase.Video.Pause(_Video);
            return true;
        }

        public override bool Stop()
        {
            if (!base.Stop())
                return false;
            if (_Video != null)
            {
                CBase.Video.Pause(_Video);
                CBase.Video.Skip(_Video, 0f, _Song.VideoGap);
            }
            return true;
        }

        private void _CloseVideo()
        {
            if (_Video != null)
                CBase.Video.Close(ref _Video);
        }

        public override void Close()
        {
            base.Close();

            _Song = null;
            _CloseVideo();
        }
    }
}