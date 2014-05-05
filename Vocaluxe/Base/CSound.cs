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
using Vocaluxe.Lib.Sound.Playback;
using Vocaluxe.Lib.Sound.Playback.Gstreamer;
using Vocaluxe.Lib.Sound.Playback.GstreamerSharp;
using Vocaluxe.Lib.Sound.Playback.OpenAL;
using Vocaluxe.Lib.Sound.Playback.PortAudio;
using VocaluxeLib;

namespace Vocaluxe.Base
{
    enum ESounds
    {
        T440
    }

    static class CSound
    {
        #region Playback
        private static IPlayback _Playback;

        public static bool Init()
        {
            if (_Playback != null)
                return false;
            switch (CConfig.PlayBackLib)
            {
                case EPlaybackLib.PortAudio:
                    _Playback = new CPortAudioPlay();
                    break;

                case EPlaybackLib.OpenAL:
                    _Playback = new COpenALPlay();
                    break;

                case EPlaybackLib.Gstreamer:
                    _Playback = new CGstreamerAudio();
                    break;

                case EPlaybackLib.GstreamerSharp:
                    _Playback = new CGstreamerSharpAudio();
                    break;

                default:
                    _Playback = new CPortAudioPlay();
                    break;
            }
            return _Playback.Init();
        }

        public static void SetGlobalVolume(float volume)
        {
            _Playback.SetGlobalVolume(volume);
        }

        public static int GetStreamCount()
        {
            return _Playback.GetStreamCount();
        }

        public static void CloseAllStreams()
        {
            if (_Playback != null)
                _Playback.CloseAll();
        }

        public static void Close()
        {
            if (_Playback != null)
                _Playback.Close();
            _Playback = null;
        }

        #region Stream Handling
        public static int Load(string media)
        {
            return _Playback.Load(media);
        }

        public static int Load(string media, bool prescan)
        {
            return _Playback.Load(media, prescan);
        }

        public static void Close(int stream)
        {
            _Playback.Close(stream);
        }

        public static void Play(int stream)
        {
            _Playback.Play(stream);
        }

        public static void Pause(int stream)
        {
            _Playback.Pause(stream);
        }

        public static void Stop(int stream)
        {
            _Playback.Stop(stream);
        }

        public static void Fade(int stream, float targetVolume, float seconds, EStreamAction afterFadeAction = EStreamAction.Nothing)
        {
            _Playback.Fade(stream, targetVolume, seconds, afterFadeAction);
        }

        public static void FadeAndPause(int stream, float targetVolume, float seconds)
        {
            Fade(stream, targetVolume, seconds, EStreamAction.Pause);
        }

        public static void FadeAndClose(int stream, float targetVolume, float seconds)
        {
            Fade(stream, targetVolume, seconds, EStreamAction.Close);
        }

        public static void FadeAndStop(int stream, float targetVolume, float seconds)
        {
            Fade(stream, targetVolume, seconds, EStreamAction.Stop);
        }

        public static void SetStreamVolume(int stream, float volume)
        {
            _Playback.SetStreamVolume(stream, volume);
        }

        public static float GetLength(int stream)
        {
            return _Playback.GetLength(stream);
        }

        public static float GetPosition(int stream)
        {
            return _Playback.GetPosition(stream);
        }

        public static bool IsPlaying(int stream)
        {
            return _Playback.IsPlaying(stream);
        }

        public static bool IsPaused(int stream)
        {
            return _Playback.IsPaused(stream);
        }

        public static bool IsFinished(int stream)
        {
            return _Playback.IsFinished(stream);
        }

        public static void Update()
        {
            _Playback.Update();
        }

        public static void SetPosition(int stream, float position)
        {
            _Playback.SetPosition(stream, position);
        }
        #endregion Stream Handling

        #endregion Playback

        #region Sounds
        public static int PlaySound(ESounds sound, bool fade = true)
        {
            string file = Path.Combine(Environment.CurrentDirectory, CSettings.FolderSounds);
            switch (sound)
            {
                case ESounds.T440:
                    file = Path.Combine(file, CSettings.SoundT440);
                    break;
                default:
                    return -1;
            }

            if (!File.Exists(file))
                return -1;

            int stream = Load(file, true);
            float length = GetLength(stream);
            Play(stream);
            if (fade)
                FadeAndClose(stream, 100f, length);
            return stream;
        }
        #endregion Sounds
    }
}