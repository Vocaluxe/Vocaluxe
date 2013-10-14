﻿#region license
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
using Vocaluxe.Base;
using Vocaluxe.Lib.Video.Gstreamer;
using VocaluxeLib.Draw;

namespace Vocaluxe.Lib.Video
{
    class CVideoDecoderGstreamer : IVideoDecoder
    {
        #region log
        private CGstreamerVideoWrapper.LogCallback _Log;

        private void _LogHandler(string text)
        {
            CLog.LogError(text);
        }
        #endregion log

        public bool Init()
        {
            bool retval = CGstreamerVideoWrapper.InitVideo();
            _Log = _LogHandler;
            //Really needed? CodeAnalysis complains
            //GC.SuppressFinalize(Log);
            CGstreamerVideoWrapper.SetVideoLogCallback(_Log);
            return retval;
        }

        public void CloseAll()
        {
            CGstreamerVideoWrapper.CloseAllVideos();
        }

        public int Load(string videoFileName)
        {
            int i = -1;
            try
            {
                var u = new Uri(videoFileName);
                i = CGstreamerVideoWrapper.LoadVideo(u.AbsoluteUri);
                return i;
            }
            catch (Exception) {}
            return i;
        }

        public bool Close(int streamID)
        {
            return CGstreamerVideoWrapper.CloseVideo(streamID);
        }

        public int GetNumStreams()
        {
            return CGstreamerVideoWrapper.GetVideoNumStreams();
        }

        public float GetLength(int streamID)
        {
            return CGstreamerVideoWrapper.GetVideoLength(streamID);
        }

        public bool GetFrame(int streamID, ref CTexture frame, float time, out float videoTime)
        {
            SManagedFrame managedFrame = CGstreamerVideoWrapper.GetFrame(streamID, time);
            videoTime = managedFrame.Videotime;

            _UploadNewFrame(ref frame, ref managedFrame.Buffer, managedFrame.Width, managedFrame.Height);
            return frame != null;
        }

        public bool Skip(int streamID, float start, float gap)
        {
            return CGstreamerVideoWrapper.Skip(streamID, start, gap);
        }

        public void SetLoop(int streamID, bool loop)
        {
            CGstreamerVideoWrapper.SetVideoLoop(streamID, loop);
        }

        public void Pause(int streamID)
        {
            CGstreamerVideoWrapper.PauseVideo(streamID);
        }

        public void Resume(int streamID)
        {
            CGstreamerVideoWrapper.ResumeVideo(streamID);
        }

        public bool Finished(int streamID)
        {
            return CGstreamerVideoWrapper.Finished(streamID);
        }

        public void Update()
        {
            CGstreamerVideoWrapper.UpdateVideo();
        }

        private void _UploadNewFrame(ref CTexture frame, ref byte[] data, int width, int height)
        {
            if (data == null)
                return;
            CDraw.UpdateOrAddTexture(ref frame, width, height, data);
            data = null;
        }
    }
}