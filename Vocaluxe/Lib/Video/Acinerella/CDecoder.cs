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
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Vocaluxe.Base;
using VocaluxeLib.Draw;

namespace Vocaluxe.Lib.Video.Acinerella
{
    class CDecoder
    {
        private readonly Stopwatch _LoopTimer = new Stopwatch();
        private readonly Object _MutexSyncSignals = new Object();

        private float _LastShownTime; // time if the last shown frame in s

        private float _Time;
        private float _Gap;
        private float _Start;
        private bool _Loop;

        private float _SetTime;
        private float _SetGap;
        private float _SetStart;
        private bool _SetSkip;

        private float _SkipTime; // = Start + VideoGap

        private string _FileName; // current video file name
        private bool _Paused;

        private CDecoderThread _Thread;

        public CDecoder() {}

        public float Length { get; private set; }

        public bool Loop { private get; set; }

        public bool Finished { get; private set; }

        public bool Paused
        {
            set
            {
                lock (_MutexSyncSignals)
                {
                    _Paused = value;
                    if (_Paused)
                        _LoopTimer.Stop();
                    else
                        _LoopTimer.Start();
                }
            }
        }

        public bool Open(string fileName)
        {
            if (_Thread != null)
                return false;

            if (!File.Exists(fileName))
                return false;

            _FileName = fileName;

            //Do this here as one may want to get the length afterwards!
            IntPtr pInstance = _LoadFile();
            if (pInstance == IntPtr.Zero)
                return false;


            _Thread = new CDecoderThread(Path.GetFileName(fileName), pInstance);
            return true;
        }

        //Open the file and get the length.
        private IntPtr _LoadFile()
        {
            try
            {
                IntPtr pInstance = CAcinerella.AcInit();
                CAcinerella.AcOpen2(pInstance, _FileName);

                var instance = (SACInstance)Marshal.PtrToStructure(pInstance, typeof(SACInstance));
                Length = instance.Info.Duration / 1000f;
                bool ok = instance.Opened;
                if (ok)
                    return pInstance;
            }
            catch (Exception) {}
            CLog.LogError("Error opening video file: " + _FileName);
            return IntPtr.Zero;
        }

        public void Close()
        {
            if (_Thread != null)
                _Thread.Stop();
            Length = 0;
        }

        public bool GetFrame(ref CTexture frame, float time, out float videoTime)
        {
            if (Loop)
            {
                lock (_MutexSyncSignals)
                {
                    _SetTime += _LoopTimer.ElapsedMilliseconds / 1000f;
                    _LoopTimer.Restart();
                }
            }
            else if (Math.Abs(_SetTime - time) >= 1f / 1000f) //Check 1 ms difference
            {
                lock (_MutexSyncSignals)
                {
                    _SetTime = time;
                }
            }
            else if (Math.Abs(_LastShownTime - time) < 1f / 1000 && frame != null) //Check 1 ms difference
            {
                videoTime = _LastShownTime;
                return true;
            }
            _UploadNewFrame(ref frame);
            videoTime = _LastShownTime;
            return frame != null;
        }

        public bool Skip(float start, float gap)
        {
            lock (_MutexSyncSignals)
            {
                _SetStart = start;
                _SetGap = gap;
                _SetSkip = true;
                _NoMoreFrames = false;
                Finished = false;
            }
            return true;
        }
    }
}