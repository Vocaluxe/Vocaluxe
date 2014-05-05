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
using Vocaluxe.Base;

namespace Vocaluxe.Lib.Sound.Playback.Gstreamer
{
    class CGstreamerAudio : IPlayback
    {
        private readonly CGstreamerAudioWrapper.LogCallback _LogCallback;

        #region log
        private static void _LogHandler(string text)
        {
            CLog.LogError(text);
        }
        #endregion log

        public CGstreamerAudio()
        {
            _LogCallback = _LogHandler;
            CGstreamerAudioWrapper.SetLogCallback(_LogCallback);
        }

        public bool Init()
        {
            return CGstreamerAudioWrapper.Init();
        }

        public void Close()
        {
            CloseAll();
            CGstreamerAudioWrapper.SetLogCallback(null);
        }

        public float GetGlobalVolume()
        {
            return CGstreamerAudioWrapper.GetGlobalVolume();
        }

        public void SetGlobalVolume(float volume)
        {
            CGstreamerAudioWrapper.SetGlobalVolume(volume);
        }

        public int GetStreamCount()
        {
            return CGstreamerAudioWrapper.GetStreamCount();
        }

        public void CloseAll()
        {
            CGstreamerAudioWrapper.CloseAll();
        }

        public int Load(string medium, bool loop = false, bool prescan = false)
        {
            var u = new Uri(medium);
            int i = CGstreamerAudioWrapper.Load(u.AbsoluteUri, prescan);
            //Workaround b/c I'm lazy:
            CGstreamerAudioWrapper.Play(i, loop);
            CGstreamerAudioWrapper.Pause(i);
            return i;
        }

        public void Close(int stream)
        {
            CGstreamerAudioWrapper.Close(stream);
        }

        public void Play(int stream)
        {
            CGstreamerAudioWrapper.Play(stream);
        }

        public void Pause(int stream)
        {
            CGstreamerAudioWrapper.Pause(stream);
        }

        public void Stop(int stream)
        {
            CGstreamerAudioWrapper.Stop(stream);
        }

        public void Fade(int streamID, float targetVolume, float seconds, EStreamAction afterFadeAction = EStreamAction.Nothing)
        {
            switch (afterFadeAction)
            {
                case EStreamAction.Nothing:
                    CGstreamerAudioWrapper.Fade(streamID, targetVolume, seconds);
                    break;
                case EStreamAction.Pause:
                    CGstreamerAudioWrapper.FadeAndPause(streamID, targetVolume, seconds);
                    break;
                case EStreamAction.Stop:
                    CGstreamerAudioWrapper.FadeAndStop(streamID, targetVolume, seconds);
                    break;
                case EStreamAction.Close:
                    CGstreamerAudioWrapper.FadeAndStop(streamID, targetVolume, seconds);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("afterFadeAction");
            }
        }

        public void SetStreamVolume(int stream, float volume)
        {
            CGstreamerAudioWrapper.SetStreamVolume(stream, volume);
        }

        public float GetLength(int stream)
        {
            return CGstreamerAudioWrapper.GetLength(stream);
        }

        public float GetPosition(int stream)
        {
            return CGstreamerAudioWrapper.GetPosition(stream);
        }

        public bool IsPlaying(int stream)
        {
            return CGstreamerAudioWrapper.IsPlaying(stream);
        }

        public bool IsPaused(int stream)
        {
            return CGstreamerAudioWrapper.IsPaused(stream);
        }

        public bool IsFinished(int stream)
        {
            return CGstreamerAudioWrapper.IsFinished(stream);
        }

        public void SetPosition(int stream, float position)
        {
            CGstreamerAudioWrapper.SetPosition(stream, position);
        }

        public void Update()
        {
            CGstreamerAudioWrapper.Update();
        }
    }
}