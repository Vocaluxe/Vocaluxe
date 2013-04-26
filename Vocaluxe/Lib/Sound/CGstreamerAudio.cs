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

using System;
using Vocaluxe.Base;
using Vocaluxe.Lib.Sound.Gstreamer;

namespace Vocaluxe.Lib.Sound
{
    class CGstreamerAudio : IPlayback
    {
        //static float LastPosition;
        private CGstreamerAudioWrapper.LogCallback _LogCallback;

        #region log
        private void _LogHandler(string text)
        {
            CLog.LogError(text);
        }
        #endregion log

        public CGstreamerAudio()
        {
            _LogCallback = _LogHandler;
            //Is this really needed? CodaAnalyzer complains about it...
            //GC.SuppressFinalize(_LogCallback);
            CGstreamerAudioWrapper.SetLogCallback(_LogCallback);
        }

        public bool Init()
        {
            return CGstreamerAudioWrapper.Init();
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

        public int Load(string media)
        {
            Uri u = new Uri(media);
            int i = CGstreamerAudioWrapper.Load(u.AbsoluteUri);
            return i;
        }

        public int Load(string media, bool prescan)
        {
            Uri u = new Uri(media);
            int i = CGstreamerAudioWrapper.Load(u.AbsoluteUri, prescan);
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

        public void Play(int stream, bool loop)
        {
            CGstreamerAudioWrapper.Play(stream, loop);
        }

        public void Pause(int stream)
        {
            CGstreamerAudioWrapper.Pause(stream);
        }

        public void Stop(int stream)
        {
            CGstreamerAudioWrapper.Stop(stream);
        }

        public void Fade(int stream, float targetVolume, float seconds)
        {
            CGstreamerAudioWrapper.Fade(stream, targetVolume, seconds);
        }

        public void FadeAndPause(int stream, float targetVolume, float seconds)
        {
            CGstreamerAudioWrapper.FadeAndPause(stream, targetVolume, seconds);
        }

        public void FadeAndStop(int stream, float targetVolume, float seconds)
        {
            CGstreamerAudioWrapper.FadeAndStop(stream, targetVolume, seconds);
        }

        public void SetStreamVolume(int stream, float volume)
        {
            CGstreamerAudioWrapper.SetStreamVolume(stream, volume);
        }

        public void SetStreamVolumeMax(int stream, float volume)
        {
            CGstreamerAudioWrapper.SetStreamVolumeMax(stream, volume);
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