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

using System.Collections.Generic;
using Gst;

namespace Vocaluxe.Lib.Sound.Playback.GstreamerSharp
{
    public class CGstreamerSharpAudio : IPlayback
    {
        private readonly Dictionary<int, CGstreamerSharpAudioStream> _Streams = new Dictionary<int, CGstreamerSharpAudioStream>();
        private static int _IDCount;

        public bool Init()
        {
#if ARCH_X86
            const string path = ".\\x86\\gstreamer";
#endif
#if ARCH_X64
            const string path = ".\\x64\\gstreamer";
#endif
            //SetDllDirectory(path);
            Application.Init();
            Registry reg = Registry.Get();
            reg.ScanPath(path);

            return Application.IsInitialized;
        }

        public void SetGlobalVolume(float volume)
        {
            foreach (CGstreamerSharpAudioStream stream in _Streams.Values)
                stream.Volume = volume;
        }

        public int GetStreamCount()
        {
            return _Streams.Count;
        }

        public void CloseAll()
        {
            foreach (CGstreamerSharpAudioStream stream in _Streams.Values)
                stream.Close();
        }

        public int Load(string media)
        {
            return Load(media, false);
        }

        public int Load(string media, bool prescan)
        {
            var stream = new CGstreamerSharpAudioStream(media, prescan);
            _Streams[_IDCount] = stream;
            return _IDCount++;
        }

        public void Close(int stream)
        {
            if (!_Streams.ContainsKey(stream))
                return;
            _Streams[stream].Close();
        }

        public void Play(int stream)
        {
            if (!_Streams.ContainsKey(stream))
                return;
            _Streams[stream].Playing = true;
        }

        public void Play(int stream, bool loop)
        {
            if (!_Streams.ContainsKey(stream))
                return;
            Play(stream, loop);
        }

        public void Pause(int stream)
        {
            if (!_Streams.ContainsKey(stream))
                return;
            _Streams[stream].Paused = true;
        }

        public void Stop(int stream)
        {
            if (!_Streams.ContainsKey(stream))
                return;
            _Streams[stream].Stop();
        }

        public void Fade(int stream, float targetVolume, float seconds)
        {
            if (!_Streams.ContainsKey(stream))
                return;
            _Streams[stream].Fade(targetVolume, seconds);
        }

        public void FadeAndPause(int stream, float targetVolume, float seconds)
        {
            if (!_Streams.ContainsKey(stream))
                return;
            _Streams[stream].FadeAndPause(targetVolume, seconds);
        }

        public void FadeAndClose(int stream, float targetVolume, float seconds)
        {
            if (!_Streams.ContainsKey(stream))
                return;
            _Streams[stream].FadeAndClose(targetVolume, seconds);
        }

        public void FadeAndStop(int stream, float targetVolume, float seconds)
        {
            if (!_Streams.ContainsKey(stream))
                return;
            _Streams[stream].FadeAndStop(targetVolume, seconds);
        }

        public void SetStreamVolume(int stream, float volume)
        {
            if (!_Streams.ContainsKey(stream))
                return;
            _Streams[stream].Volume = volume;
        }

        public void SetStreamVolumeMax(int stream, float volume)
        {
            if (!_Streams.ContainsKey(stream))
                return;
            _Streams[stream].MaxVolume = volume;
        }

        public float GetLength(int stream)
        {
            if (!_Streams.ContainsKey(stream))
                return -1;
            return _Streams[stream].Length;
        }

        public float GetPosition(int stream)
        {
            if (!_Streams.ContainsKey(stream))
                return -1f;
            return _Streams[stream].Position;
        }

        public bool IsPlaying(int stream)
        {
            if (!_Streams.ContainsKey(stream))
                return false;
            return _Streams[stream].Playing;
        }

        public bool IsPaused(int stream)
        {
            if (!_Streams.ContainsKey(stream))
                return false;
            return _Streams[stream].Paused;
        }

        public bool IsFinished(int stream)
        {
            if (!_Streams.ContainsKey(stream))
                return true;
            return _Streams[stream].Finished;
        }

        public void SetPosition(int stream, float position)
        {
            if (!_Streams.ContainsKey(stream))
                return;
            _Streams[stream].Position = position;
        }

        public void Update()
        {
            var streamsToDelete = new List<int>();
            foreach (KeyValuePair<int, CGstreamerSharpAudioStream> stream in _Streams)
            {
                if (stream.Value.Closed)
                    streamsToDelete.Add(stream.Key);
                stream.Value.Update();
            }
            foreach (int key in streamsToDelete)
                _Streams.Remove(key);
        }
    }
}