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

using System.Diagnostics;
using Vocaluxe.Base;

namespace Vocaluxe.Lib.Sound.Record
{
    class CDelayTest
    {
        private struct SDelayChannel
        {
            public int Channel;
            public bool Finished;
        }

        private const float _MaxDelayTime = 1000;
        public bool Running { get; private set; }
        private readonly Stopwatch _Timer = new Stopwatch();
        private int _Stream = -1;

        private readonly SDelayChannel[] _DelaysChannel;
        public readonly int[] Delays;
        public readonly int NumChannels;

        public CDelayTest(int numChannels)
        {
            NumChannels = numChannels;
            _DelaysChannel = new SDelayChannel[NumChannels];
            Delays = new int[NumChannels];
        }

        public void Start(int[] channels)
        {
            if (Running)
                return;
            Reset();
            for (int i = 0; i < _DelaysChannel.Length; i++)
            {
                if (i < channels.Length)
                {
                    _DelaysChannel[i].Finished = false;
                    _DelaysChannel[i].Channel = channels[i];
                }
                else
                    _DelaysChannel[i].Finished = true;
            }
            _Stream = CSound.PlaySound(ESounds.T440, false);
            Running = true;
        }

        public void Reset()
        {
            Running = false;
            for (int i = 0; i < _DelaysChannel.Length; i++)
                Delays[i] = 0;
            _CloseStream();
        }

        private void _CloseStream()
        {
            if (_Stream != -1)
            {
                CSound.Close(_Stream);
                _Stream = -1;
            }
        }

        public void Update()
        {
            if (!Running)
                return;
            if (!_Timer.IsRunning)
            {
                if (CSound.GetPosition(_Stream) > 0f)
                    _Timer.Restart();
            }
            if (!_Timer.IsRunning)
                return;

            bool isActive = false;
            if (_Timer.ElapsedMilliseconds <= _MaxDelayTime)
            {
                for (int i = 0; i < _DelaysChannel.Length; i++)
                {
                    if (_DelaysChannel[i].Channel < 0 || _DelaysChannel[i].Finished)
                        continue;
                    if (CRecord.GetMaxVolume(_DelaysChannel[i].Channel) > 0.1f && CRecord.GetTone(_DelaysChannel[i].Channel) == 9)
                    {
                        Delays[i] = (int)_Timer.ElapsedMilliseconds;
                        _DelaysChannel[i].Finished = true;
                    }
                    else
                        isActive = true;
                }
            }
            if (!isActive)
            {
                Running = false;
                _CloseStream();
                _Timer.Stop();
            }
        }
    }
}