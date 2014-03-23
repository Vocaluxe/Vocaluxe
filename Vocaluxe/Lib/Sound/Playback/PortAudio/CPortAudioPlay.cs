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

namespace Vocaluxe.Lib.Sound.Playback.PortAudio
{
    class CPortAudioPlay : IPlayback
    {
        private volatile bool _Initialized;
        private readonly List<CPortAudioStream> _Streams = new List<CPortAudioStream>();
        private int _NextID;

        public bool Init()
        {
            if (_Initialized)
                return false;

            _Initialized = true;

            return true;
        }

        public void Close()
        {
            _Initialized = false; //Set this first, so threads don't call closeproc anymore
            List<CPortAudioStream> streams = new List<CPortAudioStream>();
            //Get all streams but do not modify list without holding the lock
            //Calling dispose from within the lock may deadlock in closeproc (if a thread is already there)
            lock (_Streams)
            {
                streams.AddRange(_Streams);
                _Streams.Clear();
            }
            foreach (CPortAudioStream stream in streams)
                stream.Dispose();
        }

        public void CloseAll()
        {
            lock (_Streams)
            {
                foreach (CPortAudioStream stream in _Streams)
                    stream.Close();
                _Streams.Clear();
            }
        }

        public void SetGlobalVolume(float volume)
        {
            if (_Initialized)
            {
                //Bass.BASS_SetVolume(Volume / 100f);
            }
        }

        public int GetStreamCount()
        {
            if (!_Initialized)
                return 0;

            lock (_Streams)
            {
                return _Streams.Count;
            }
        }

        public void Update() {}

        #region Stream Handling
        public int Load(string media)
        {
            return Load(media, false);
        }

        public int Load(string media, bool prescan)
        {
            //Note: We ignore prescan here as the stream does it always

            var stream = new CPortAudioStream(_NextID++, _CloseProc);

            if (stream.Open(media))
            {
                lock (_Streams)
                {
                    _Streams.Add(stream);
                    return stream.ID;
                }
            }
            return 0;
        }

        public void Close(int stream)
        {
            if (!_Initialized)
                return;
            lock (_Streams)
            {
                int index = _GetStreamIndex(stream);
                if (index >= 0)
                {
                    _Streams[index].Close();
                    _Streams.RemoveAt(index);
                }
            }
        }

        public void Play(int stream)
        {
            Play(stream, false);
        }

        public void Play(int stream, bool loop)
        {
            if (!_Initialized)
                return;
            lock (_Streams)
            {
                int index = _GetStreamIndex(stream);
                if (index >= 0)
                {
                    _Streams[index].Loop = loop;
                    _Streams[index].Play();
                }
            }
        }

        public void Pause(int stream)
        {
            if (!_Initialized)
                return;
            lock (_Streams)
            {
                if (_AlreadyAdded(stream))
                    _Streams[_GetStreamIndex(stream)].Paused = true;
            }
        }

        public void Stop(int stream)
        {
            if (!_Initialized)
                return;
            lock (_Streams)
            {
                if (_AlreadyAdded(stream))
                    _Streams[_GetStreamIndex(stream)].Stop();
            }
        }

        public void Fade(int stream, float targetVolume, float seconds)
        {
            if (!_Initialized)
                return;
            lock (_Streams)
            {
                if (_AlreadyAdded(stream))
                    _Streams[_GetStreamIndex(stream)].Fade(targetVolume, seconds);
            }
        }

        public void FadeAndPause(int stream, float targetVolume, float seconds)
        {
            if (!_Initialized)
                return;
            lock (_Streams)
            {
                if (_AlreadyAdded(stream))
                    _Streams[_GetStreamIndex(stream)].FadeAndPause(targetVolume, seconds);
            }
        }

        public void FadeAndClose(int stream, float targetVolume, float seconds)
        {
            if (!_Initialized)
                return;
            lock (_Streams)
            {
                if (_AlreadyAdded(stream))
                    _Streams[_GetStreamIndex(stream)].FadeAndClose(targetVolume, seconds);
            }
        }

        public void FadeAndStop(int stream, float targetVolume, float seconds)
        {
            if (!_Initialized)
                return;
            lock (_Streams)
            {
                if (_AlreadyAdded(stream))
                    _Streams[_GetStreamIndex(stream)].FadeAndStop(targetVolume, seconds);
            }
        }

        public void SetStreamVolume(int stream, float volume)
        {
            if (!_Initialized)
                return;
            lock (_Streams)
            {
                if (_AlreadyAdded(stream))
                    _Streams[_GetStreamIndex(stream)].Volume = volume;
            }
        }

        public void SetStreamVolumeMax(int stream, float volume)
        {
            if (!_Initialized)
                return;
            lock (_Streams)
            {
                if (_AlreadyAdded(stream))
                    _Streams[_GetStreamIndex(stream)].VolumeMax = volume;
            }
        }

        public float GetLength(int stream)
        {
            if (!_Initialized)
                return 0f;
            lock (_Streams)
            {
                if (_AlreadyAdded(stream))
                    return _Streams[_GetStreamIndex(stream)].Length;
            }
            return 0f;
        }

        public float GetPosition(int stream)
        {
            if (!_Initialized)
                return 0f;
            lock (_Streams)
            {
                if (_AlreadyAdded(stream))
                    return _Streams[_GetStreamIndex(stream)].Position;
            }

            return 0f;
        }

        public bool IsPlaying(int stream)
        {
            if (!_Initialized)
                return false;
            lock (_Streams)
            {
                if (_AlreadyAdded(stream))
                    return !_Streams[_GetStreamIndex(stream)].Paused && !_Streams[_GetStreamIndex(stream)].Finished;
            }
            return false;
        }

        public bool IsPaused(int stream)
        {
            if (!_Initialized)
                return false;
            lock (_Streams)
            {
                if (_AlreadyAdded(stream))
                    return _Streams[_GetStreamIndex(stream)].Paused;
            }
            return false;
        }

        public bool IsFinished(int stream)
        {
            if (!_Initialized)
                return true;
            lock (_Streams)
            {
                if (_AlreadyAdded(stream))
                    return _Streams[_GetStreamIndex(stream)].Finished;
            }
            return true;
        }

        public void SetPosition(int stream, float position)
        {
            if (!_Initialized)
                return;
            lock (_Streams)
            {
                if (_AlreadyAdded(stream))
                    _Streams[_GetStreamIndex(stream)].Skip(position);
            }
        }
        #endregion Stream Handling

        /// <summary>
        ///     Returns true if strem with given id is found
        ///     MUST hold _Stream-lock
        /// </summary>
        /// <param name="stream">Stream id</param>
        /// <returns></returns>
        private bool _AlreadyAdded(int stream)
        {
            return _GetStreamIndex(stream) >= 0;
        }

        /// <summary>
        ///     Returns the index of the stream with the given id
        ///     MUST hold _Stream-lock
        /// </summary>
        /// <param name="stream">Stream id</param>
        /// <returns></returns>
        private int _GetStreamIndex(int stream)
        {
            for (int i = 0; i < _Streams.Count; i++)
            {
                if (_Streams[i].ID == stream)
                    return i;
            }
            return -1;
        }

        /// <summary>
        ///     Removes the stream with the given handle
        /// </summary>
        /// <param name="stream">Stream handle</param>
        private void _CloseProc(int stream)
        {
            if (!_Initialized)
                return;
            lock (_Streams)
            {
                if (_AlreadyAdded(stream))
                    _Streams.RemoveAt(_GetStreamIndex(stream));
            }
        }
    }
}