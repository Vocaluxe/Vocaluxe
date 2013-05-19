#region license
// /*
//     This file is part of Vocaluxe.
// 
//     Vocaluxe is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     Vocaluxe is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
//  */
#endregion

using System.Diagnostics;
using System.Drawing;
using Vocaluxe.Lib.Video;
using VocaluxeLib;
using VocaluxeLib.Draw;

namespace Vocaluxe.Base
{
    static class CVideo
    {
        #region VideoDecoder
        private static IVideoDecoder _VideoDecoder;

        #region Init
        public static void Init()
        {
            switch (CConfig.VideoDecoder)
            {
                case EVideoDecoder.FFmpeg:
                    _VideoDecoder = new CVideoDecoderFFmpeg();
                    break;
                case EVideoDecoder.Gstreamer:
                    _VideoDecoder = new CVideoDecoderGstreamer();
                    break;
                default:
                    _VideoDecoder = new CVideoDecoderFFmpeg();
                    break;
            }

            _VideoDecoder.Init();
        }
        #endregion Init

        #region Interface
        public static void CloseAll()
        {
            _VideoDecoder.CloseAll();
        }

        public static int GetNumStreams()
        {
            return _VideoDecoder.GetNumStreams();
        }

        public static int Load(string videoFileName)
        {
            return _VideoDecoder.Load(videoFileName);
        }

        public static bool Close(int streamID)
        {
            return _VideoDecoder.Close(streamID);
        }

        public static float GetLength(int streamID)
        {
            return _VideoDecoder.GetLength(streamID);
        }

        public static bool GetFrame(int streamID, ref CTexture frame, float time, out float videoTime)
        {
            return _VideoDecoder.GetFrame(streamID, ref frame, time, out videoTime);
        }

        public static bool Skip(int streamID, float start, float gap)
        {
            return _VideoDecoder.Skip(streamID, start, gap);
        }

        public static void SetLoop(int streamID, bool loop)
        {
            _VideoDecoder.SetLoop(streamID, loop);
        }

        public static void Pause(int streamID)
        {
            _VideoDecoder.Pause(streamID);
        }

        public static void Resume(int streamID)
        {
            _VideoDecoder.Resume(streamID);
        }

        public static bool Finished(int streamID)
        {
            return _VideoDecoder.Finished(streamID);
        }

        public static void Update()
        {
            _VideoDecoder.Update();
        }
        #endregion Interface

        #endregion VideoDecoder
    }

    class CVideoPlayer
    {
        private CTexture _VideoTexture;
        private int _VideoStream;
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
            _VideoStream = CVideo.Load(CTheme.GetVideoFilePath(videoName, -1));
            _Loaded = true;
        }

        public void Start()
        {
            _VideoTimer.Reset();
            _Finished = false;
            //CVideo.VdSkip(_VideoStream, 0f, 0f);
            _VideoTimer.Start();
        }

        public void Pause()
        {
            _VideoTimer.Stop();
        }

        public void Resume()
        {
            _VideoTimer.Start();
        }

        public void Draw()
        {
            if (!_Finished)
            {
                float videoTime = _VideoTimer.ElapsedMilliseconds / 1000f;
                _Finished = CVideo.Finished(_VideoStream);

                CVideo.GetFrame(_VideoStream, ref _VideoTexture, videoTime, out videoTime);
            }
            if (_VideoTexture == null)
                return;
            RectangleF bounds = new RectangleF(0f, 0f, CSettings.RenderW, CSettings.RenderH);
            RectangleF rect;
            CHelper.SetRect(bounds, out rect, _VideoTexture.OrigAspect, EAspect.Crop);

            CDraw.DrawTexture(_VideoTexture, new SRectF(rect.X, rect.Y, rect.Width, rect.Height, CSettings.ZFar / 4));
        }

        public void PreLoad()
        {
            float videoTime = 0f;
            while (_VideoTexture == null && videoTime < 1f)
            {
                float dummy;
                CVideo.GetFrame(_VideoStream, ref _VideoTexture, 0, out dummy);
                videoTime += 0.05f;
            }
        }

        public void Close()
        {
            CVideo.Close(_VideoStream);
            CDraw.RemoveTexture(ref _VideoTexture);
            _Loaded = false;
            _Finished = false;
            _VideoTimer.Reset();
        }
    }
}