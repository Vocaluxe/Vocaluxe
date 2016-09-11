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
using VocaluxeLib;

namespace Vocaluxe.Lib.Sound.Playback
{
    abstract class CAudioStreamBase : IAudioStream
    {
        private readonly int _ID;
        protected readonly string _Medium;
        protected readonly bool _Loop;
        protected readonly EAudioEffect _Effect;

        protected ICloseStreamListener _CloseStreamListener;

        protected EStreamAction _AfterFadeAction = EStreamAction.Nothing;
        protected CFading _Fading;

        public int ID
        {
            get { return _ID; }
        }

        public bool IsFading
        {
            get { return _Fading != null; }
        }

        public virtual float Volume { get; set; }
        public virtual float VolumeMax { get; set; }
        public virtual float Length { get; protected set; }
        public abstract float Position { get; set; }
        public abstract bool IsPaused { get; set; }
        public abstract bool IsFinished { get; }

        protected CAudioStreamBase(int id, string medium, bool loop, EAudioEffect effect = EAudioEffect.None)
        {
            _ID = id;
            _Medium = medium;
            _Loop = loop;
            _Effect = effect;
        }

        ~CAudioStreamBase()
        {
            _Dispose(false);
        }

        public void Dispose()
        {
            _Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void _Dispose(bool disposing)
        {
            if (!disposing)
                CBase.Log.LogDebug("Audio stream " + _Medium + " was not closed.");
        }

        public void Close()
        {
            Dispose();
        }

        public void SetOnCloseListener(ICloseStreamListener listener)
        {
            _CloseStreamListener = listener;
        }

        public void Fade(float targetVolume, float seconds, EStreamAction afterFadeAction)
        {
            _Fading = new CFading(Volume, targetVolume, seconds);
            _AfterFadeAction = afterFadeAction;
        }

        public void CancelFading()
        {
            _Fading = null;
        }

        public virtual void Update()
        {
            if (_Fading != null)
            {
                bool finished;
                Volume = _Fading.GetValue(out finished);
                if (finished)
                {
                    switch (_AfterFadeAction)
                    {
                        case EStreamAction.Close:
                            Close();
                            break;
                        case EStreamAction.Stop:
                            Stop();
                            break;
                        case EStreamAction.Pause:
                            IsPaused = true;
                            break;
                    }
                    _Fading = null;
                }
            }
        }

        public abstract bool Open(bool prescan);
        public abstract void Play();
        public abstract void Stop();
    }
}