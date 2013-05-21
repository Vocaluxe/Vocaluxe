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
using System.Collections.Generic;
using Vocaluxe.Lib.Video.Acinerella;
using VocaluxeLib.Draw;

namespace Vocaluxe.Lib.Video
{
    delegate void Closeproc(int streamID);

    class CVideoDecoderFFmpeg : CVideoDecoder
    {
        private readonly List<CDecoder> _Decoder = new List<CDecoder>();
        private Closeproc _Closeproc;
        private int _Count = 1;

        private readonly Object _MutexDecoder = new Object();

        public override bool Init()
        {
            _Closeproc = _CloseProc;
            CloseAll();

            return base.Init();
        }

        public override void CloseAll()
        {
            lock (_MutexDecoder)
            {
                for (int i = 0; i < _Decoder.Count; i++)
                    _Decoder[i].Free(_Closeproc, i + 1);
            }
        }

        public override int Load(string videoFileName)
        {
            SVideoStreams stream = new SVideoStreams(0);
            CDecoder decoder = new CDecoder();

            if (decoder.Open(videoFileName))
            {
                lock (_MutexDecoder)
                {
                    _Decoder.Add(decoder);
                    stream.Handle = _Count++;
                    _Streams.Add(stream);
                    return stream.Handle;
                }
            }
            return -1;
        }

        public override bool Close(int streamID)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(streamID))
                    {
                        _Decoder[_GetStreamIndex(streamID)].Free(_Closeproc, streamID);
                        return true;
                    }
                }
            }
            return false;
        }

        public override bool GetFrame(int streamID, ref CTexture frame, float time, out float videoTime)
        {
            videoTime = 0;
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(streamID))
                        return _Decoder[_GetStreamIndex(streamID)].GetFrame(ref frame, time, out videoTime);
                }
            }
            return false;
        }

        public override float GetLength(int streamID)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(streamID))
                        return _Decoder[_GetStreamIndex(streamID)].Length;
                }
            }
            return 0f;
        }

        public override bool Skip(int streamID, float start, float gap)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(streamID))
                        return _Decoder[_GetStreamIndex(streamID)].Skip(start, gap);
                }
            }
            return false;
        }

        public override void SetLoop(int streamID, bool loop)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(streamID))
                        _Decoder[_GetStreamIndex(streamID)].Loop = loop;
                }
            }
        }

        public override void Pause(int streamID)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(streamID))
                        _Decoder[_GetStreamIndex(streamID)].Paused = true;
                }
            }
        }

        public override void Resume(int streamID)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(streamID))
                        _Decoder[_GetStreamIndex(streamID)].Paused = false;
                }
            }
        }

        public override bool Finished(int streamID)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(streamID))
                        return _Decoder[_GetStreamIndex(streamID)].Finished;
                }
            }
            return true;
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

    struct SFrameBuffer
    {
        public byte[] Data;
        public float Time;
        public bool Displayed;
    }
}