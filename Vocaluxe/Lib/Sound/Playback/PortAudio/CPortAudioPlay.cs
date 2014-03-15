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
using System.Linq;

namespace Vocaluxe.Lib.Sound.Playback.PortAudio
{
    class CPortAudioPlay : IPlayback
    {
        private bool _Initialized;
        private readonly List<CPortAudioStream> _Decoder = new List<CPortAudioStream>();
        private Closeproc _Closeproc;
        private int _Count = 1;

        private readonly Object _MutexDecoder = new Object();

        private List<SAudioStreams> _Streams;

        public bool Init()
        {
            if (_Initialized)
                CloseAll();

            _Closeproc = _CloseProc;
            _Initialized = true;

            _Streams = new List<SAudioStreams>();
            return true;
        }

        public void CloseAll()
        {
            lock (_MutexDecoder)
            {
                for (int i = 0; i < _Decoder.Count; i++)
                    _Decoder[i].Free(_Closeproc, i + 1);
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

            lock (_MutexDecoder)
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
            var stream = new SAudioStreams(0);
            var decoder = new CPortAudioStream();

            if (decoder.Open(media) > -1)
            {
                lock (_MutexDecoder)
                {
                    _Decoder.Add(decoder);
                    stream.Handle = _Count++;
                    _Streams.Add(stream);
                    return stream.Handle;
                }
            }
            return 0;
        }

        public void Close(int stream)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(stream))
                        _Decoder[_GetStreamIndex(stream)].Free(_Closeproc, stream);
                }
            }
        }

        public void Play(int stream)
        {
            Play(stream, false);
        }

        public void Play(int stream, bool loop)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(stream))
                    {
                        _Decoder[_GetStreamIndex(stream)].Loop = loop;
                        _Decoder[_GetStreamIndex(stream)].Play();
                    }
                }
            }
        }

        public void Pause(int stream)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(stream))
                        _Decoder[_GetStreamIndex(stream)].Paused = true;
                }
            }
        }

        public void Stop(int stream)
        {
            Pause(stream);
        }

        public void Fade(int stream, float targetVolume, float seconds)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(stream))
                        _Decoder[_GetStreamIndex(stream)].Fade(targetVolume, seconds);
                }
            }
        }

        public void FadeAndPause(int stream, float targetVolume, float seconds)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(stream))
                        _Decoder[_GetStreamIndex(stream)].FadeAndPause(targetVolume, seconds);
                }
            }
        }

        public void FadeAndClose(int stream, float targetVolume, float seconds)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(stream))
                        _Decoder[_GetStreamIndex(stream)].FadeAndClose(targetVolume, seconds, _Closeproc, stream);
                }
            }
        }

        public void FadeAndStop(int stream, float targetVolume, float seconds)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(stream))
                        _Decoder[_GetStreamIndex(stream)].FadeAndStop(targetVolume, seconds, _Closeproc, stream);
                }
            }
        }

        public void SetStreamVolume(int stream, float volume)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(stream))
                        _Decoder[_GetStreamIndex(stream)].Volume = volume;
                }
            }
        }

        public void SetStreamVolumeMax(int stream, float volume)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(stream))
                        _Decoder[_GetStreamIndex(stream)].VolumeMax = volume;
                }
            }
        }

        public float GetLength(int stream)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(stream))
                        return _Decoder[_GetStreamIndex(stream)].Length;
                }
            }
            return 0f;
        }

        public float GetPosition(int stream)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(stream))
                        return _Decoder[_GetStreamIndex(stream)].Position;
                }

                return 0f;
            }
            return 0f;
        }

        public bool IsPlaying(int stream)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(stream))
                        return !_Decoder[_GetStreamIndex(stream)].Paused && !_Decoder[_GetStreamIndex(stream)].Finished;
                }
            }
            return false;
        }

        public bool IsPaused(int stream)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(stream))
                        return _Decoder[_GetStreamIndex(stream)].Paused;
                }
            }
            return false;
        }

        public bool IsFinished(int stream)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(stream))
                        return _Decoder[_GetStreamIndex(stream)].Finished;
                }
            }
            return true;
        }

        public void SetPosition(int stream, float position)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(stream))
                        _Decoder[_GetStreamIndex(stream)].Skip(position);
                }
            }
        }
        #endregion Stream Handling

        private bool _AlreadyAdded(int stream)
        {
            return _Streams.Any(st => st.Handle == stream);
        }

        private int _GetStreamIndex(int stream)
        {
            for (int i = 0; i < _Streams.Count; i++)
            {
                if (_Streams[i].Handle == stream)
                    return i;
            }
            return -1;
        }

        private void _CloseProc(int streamID)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(streamID))
                    {
                        int index = _GetStreamIndex(streamID);
                        _Decoder.RemoveAt(index);
                        _Streams.RemoveAt(index);
                    }
                }
            }
        }
    }
}