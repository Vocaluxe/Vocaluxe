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
using System.Collections.Generic;
using System.Threading;
using VocaluxeLib;

namespace Vocaluxe.Lib.Sound.Playback
{
    public abstract class CPlaybackBase : IPlayback, ICloseStreamListener
    {
        protected bool _Initialized;
        protected readonly List<IAudioStream> _Streams = new List<IAudioStream>();
        private readonly List<IAudioStream> _StreamsToDelete = new List<IAudioStream>();
        private bool _InUpdate;
        private int _NextId;
        protected float _GlobalVolume = 1f;

        public abstract bool Init();

        public virtual void Close()
        {
            if (!_Initialized)
            {
                return;
            }

            var streams = new List<IAudioStream>();
            //Get all streams but do not modify list without holding the lock
            //Calling dispose from within the lock may deadlock in closeproc (if a thread is already there)
            lock (_Streams)
            {
                streams.AddRange(_Streams);
            }

            foreach (var stream in streams)
            {
                stream.Dispose();
            }

            while (_Streams.Count > 0)
            {
                Thread.Sleep(5);
            }

            _Initialized = false;
        }

        public void CloseAll()
        {
            if (!_Initialized)
            {
                return;
            }

            lock (_Streams)
            {
                foreach (var stream in _Streams)
                {
                    stream.Close();
                }

                _Streams.Clear();
            }
        }

        public int GetGlobalVolume()
        {
            return _Initialized ? (int)Math.Round(_GlobalVolume * 100) : 100;
        }

        public void SetGlobalVolume(int volume)
        {
            if (!_Initialized)
            {
                return;
            }

            var volumeF = volume.Clamp(0, 100) / 100f;
            lock (_Streams)
            {
                foreach (var stream in _Streams)
                {
                    if (!stream.IsFading)
                    {
                        stream.VolumeMax = volumeF;
                    }
                }
            }

            _GlobalVolume = volumeF;
        }

        public int GetStreamCount()
        {
            if (!_Initialized)
            {
                return 0;
            }

            lock (_Streams)
            {
                return _Streams.Count;
            }
        }

        public void Update()
        {
            if (!_Initialized)
            {
                return;
            }

            lock (_Streams)
            {
                _InUpdate = true;
                foreach (var stream in _Streams)
                {
                    stream.Update();
                }

                //This is required because a stream may call the close listener from inside the update method
                //lock() only protects from different threads not from the same, so we use StreamsToDelete to not modify _Streams while iterating it
                _InUpdate = false;
                if (_StreamsToDelete.Count > 0)
                {
                    foreach (var stream in _StreamsToDelete)
                    {
                        _Streams.Remove(stream);
                    }

                    _StreamsToDelete.Clear();
                }
            }
        }

        #region Stream Handling
        //Factory method to get a stream instance
        protected abstract IAudioStream _CreateStream(int id, string media, bool loop, EAudioEffect effect = EAudioEffect.None);

        public int Load(string medium, bool loop = false, bool prescan = false, EAudioEffect effect = EAudioEffect.None)
        {
            if (!_Initialized)
            {
                return -1;
            }

            var stream = _CreateStream(_NextId++, medium, loop, effect);

            if (stream.Open(prescan))
            {
                lock (_Streams)
                {
                    stream.Volume = 1f;
                    stream.VolumeMax = _GlobalVolume;
                    stream.SetOnCloseListener(this);
                    _Streams.Add(stream);
                    return stream.Id;
                }
            }

            return -1;
        }

        public void Close(int streamId)
        {
            if (!_Initialized)
            {
                return;
            }

            lock (_Streams)
            {
                var index = _GetStreamIndex(streamId);
                if (index >= 0)
                {
                    _Streams[index].Close();
                }
            }
        }

        public void Play(int streamId)
        {
            if (!_Initialized)
            {
                return;
            }

            lock (_Streams)
            {
                if (_StreamExists(streamId))
                {
                    _Streams[_GetStreamIndex(streamId)].Play();
                }
            }
        }

        public void Pause(int streamId)
        {
            if (!_Initialized)
            {
                return;
            }

            lock (_Streams)
            {
                if (_StreamExists(streamId))
                {
                    _Streams[_GetStreamIndex(streamId)].IsPaused = true;
                }
            }
        }

        public void Stop(int streamId)
        {
            if (!_Initialized)
            {
                return;
            }

            lock (_Streams)
            {
                if (_StreamExists(streamId))
                {
                    _Streams[_GetStreamIndex(streamId)].Stop();
                }
            }
        }

        public void Fade(int streamId, int targetVolume, float seconds, EStreamAction afterFadeAction = EStreamAction.Nothing)
        {
            if (!_Initialized)
            {
                return;
            }

            var targetVolumeF = targetVolume.Clamp(0, 100) / 100f;
            lock (_Streams)
            {
                if (_StreamExists(streamId))
                {
                    _Streams[_GetStreamIndex(streamId)].Fade(targetVolumeF, seconds, afterFadeAction);
                }
            }
        }

        public void SetStreamVolume(int streamId, int volume)
        {
            if (!_Initialized)
            {
                return;
            }

            var volumeF = volume.Clamp(0, 100) / 100f;
            lock (_Streams)
            {
                if (_StreamExists(streamId))
                {
                    _Streams[_GetStreamIndex(streamId)].Volume = volumeF;
                    _Streams[_GetStreamIndex(streamId)].CancelFading();
                }
            }
        }

        public float GetLength(int streamId)
        {
            if (!_Initialized)
            {
                return -1f;
            }

            lock (_Streams)
            {
                if (_StreamExists(streamId))
                {
                    return _Streams[_GetStreamIndex(streamId)].Length;
                }
            }

            return -1f;
        }

        public float GetPosition(int streamId)
        {
            if (!_Initialized)
            {
                return -1f;
            }

            lock (_Streams)
            {
                if (_StreamExists(streamId))
                {
                    return _Streams[_GetStreamIndex(streamId)].Position;
                }
            }

            return -1f;
        }

        public void SetPosition(int streamId, float position)
        {
            if (!_Initialized)
            {
                return;
            }

            lock (_Streams)
            {
                if (_StreamExists(streamId))
                {
                    _Streams[_GetStreamIndex(streamId)].Position = position;
                }
            }
        }

        public bool IsPlaying(int streamId)
        {
            if (!_Initialized)
            {
                return false;
            }

            lock (_Streams)
            {
                if (_StreamExists(streamId))
                {
                    return !_Streams[_GetStreamIndex(streamId)].IsPaused && !_Streams[_GetStreamIndex(streamId)].IsFinished;
                }
            }

            return false;
        }

        public bool IsPaused(int streamId)
        {
            if (!_Initialized)
            {
                return false;
            }

            lock (_Streams)
            {
                if (_StreamExists(streamId))
                {
                    return _Streams[_GetStreamIndex(streamId)].IsPaused;
                }
            }

            return false;
        }

        public bool IsFinished(int streamId)
        {
            if (!_Initialized)
            {
                return true;
            }

            lock (_Streams)
            {
                if (_StreamExists(streamId))
                {
                    return _Streams[_GetStreamIndex(streamId)].IsFinished;
                }
            }

            return true;
        }

        /// <summary>
        ///     Returns true if stream with given id is found
        ///     MUST hold _Stream-lock
        /// </summary>
        /// <param name="streamId">Stream id</param>
        /// <returns></returns>
        protected bool _StreamExists(int streamId)
        {
            return _GetStreamIndex(streamId) != -1;
        }

        /// <summary>
        ///     Returns the index of the stream with the given id
        ///     MUST hold _Stream-lock
        /// </summary>
        /// <param name="streamId">Stream id</param>
        /// <returns></returns>
        protected int _GetStreamIndex(int streamId)
        {
            for (var i = 0; i < _Streams.Count; i++)
            {
                if (_Streams[i].Id == streamId)
                {
                    return i;
                }
            }

            return -1;
        }
        #endregion Stream Handling

        public void OnCloseStream(IAudioStream stream)
        {
            lock (_Streams)
            {
                if (_InUpdate) //This is only possible if this is called from inside the update method
                {
                    _StreamsToDelete.Add(stream);
                }
                else
                {
                    _Streams.Remove(stream);
                }
            }
        }
    }
}