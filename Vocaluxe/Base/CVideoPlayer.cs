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

using System.Diagnostics;
using Vocaluxe.Base.ThemeSystem;
using VocaluxeLib;

namespace Vocaluxe.Base
{
    class CVideoPlayer
    {
        private CVideoStream _VideoStream;
        private readonly Stopwatch _VideoTimer = new Stopwatch();
        private bool _Finished;
        private bool _Loaded;

        public bool IsFinished
        {
            get { return _Finished || !_Loaded; }
        }

        public bool Loop
        {
            set { CVideo.SetLoop(_VideoStream, value); }
        }

        public void Load(string videoName)
        {
            _VideoStream = CThemes.GetSkinVideo(videoName, -1, false);
            if (_VideoStream == null)
            {
                return;
            }
            _Loaded = true;
            CVideo.Pause(_VideoStream);
        }

        public void Start()
        {
            _VideoTimer.Reset();
            _Finished = false;
            //CVideo.VdSkip(_VideoStream, 0f, 0f);
            _VideoTimer.Start();
            CVideo.Resume(_VideoStream);
        }

        public void Pause()
        {
            CVideo.Pause(_VideoStream);
            _VideoTimer.Stop();
        }

        public void Resume()
        {
            CVideo.Resume(_VideoStream);
            _VideoTimer.Start();
        }

        public void Draw()
        {
            if (!_Loaded)
            {
                return;
            }

            if (!_Finished)
            {
                float videoTime = _VideoTimer.ElapsedMilliseconds / 1000f;
                _Finished = CVideo.Finished(_VideoStream);

                CVideo.GetFrame(_VideoStream, videoTime);
            }
            if (_VideoStream.Texture == null)
                return;

            CDraw.DrawTexture(_VideoStream.Texture, CSettings.RenderRect, EAspect.Crop);
        }

        public void PreLoad()
        {
            if (!_Loaded)
            {
                return;
            }
            bool paused = _VideoTimer.IsRunning;
            if (paused)
                CVideo.Resume(_VideoStream);
            float videoTime = 0f;
            while (_VideoStream.Texture == null && videoTime < 1f)
            {
                CVideo.GetFrame(_VideoStream, 0);
                videoTime += 0.05f;
            }
            if (paused)
                CVideo.Pause(_VideoStream);
        }

        public void Close()
        {
            CVideo.Close(ref _VideoStream);
            _Loaded = false;
            _Finished = false;
            _VideoTimer.Reset();
        }
    }
}