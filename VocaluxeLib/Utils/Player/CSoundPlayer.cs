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

using System.IO;
using VocaluxeLib.Songs.Sources;

namespace VocaluxeLib.Utils.Player
{
    public class CSoundPlayer
    {
        protected int _StreamId = -1;
        protected readonly float _FadeTime = CBase.Settings.GetSoundPlayerFadeTime();

        public bool Loop;

        /// <summary>
        ///     Gets the current stream position or sets it
        /// </summary>
        public float Position
        {
            set
            {
                if (!SoundLoaded)
                {
                    return;
                }

                CBase.Sound.SetPosition(_StreamId, value);
            }
            get { return !SoundLoaded ? -1 : CBase.Sound.GetPosition(_StreamId); }
        }

        public float Length
        {
            get { return !SoundLoaded ? -1 : CBase.Sound.GetLength(_StreamId); }
        }

        public bool IsPlaying { get; private set; }

        public bool IsFinished
        {
            get { return !Loop && (CBase.Sound.IsFinished(_StreamId) || !IsPlaying); }
        }

        public bool SoundLoaded
        {
            get { return _StreamId != -1; }
        }

        public virtual string DisplayName { get; private set; }

        public CSoundPlayer(bool loop = false)
        {
            Loop = loop;
        }

        public void Load(ISoundSource source, float position = -1f, bool autoplay = false)
        {
            Close();

            _StreamId = CBase.Sound.Load(source, false, true);
            if (_StreamId < 0)
            {
                return;
            }

            DisplayName = source.DisplayName;
            if (position > 0f)
            {
                Position = position;
            }

            if (autoplay)
            {
                Play();
            }
        }

        /// <summary>
        ///     Starts or resumes the player
        /// </summary>
        /// <returns>True if state changed, false if nothing loaded or already playing</returns>
        public virtual bool Play()
        {
            if (!SoundLoaded || IsPlaying)
            {
                return false;
            }

            CBase.Sound.SetStreamVolume(_StreamId, 0);
            CBase.Sound.Fade(_StreamId, 100, _FadeTime);
            CBase.Sound.Play(_StreamId);
            IsPlaying = true;
            return true;
        }

        /// <summary>
        ///     Pauses the player
        /// </summary>
        /// <returns>True if state changed, false if nothing loaded or already paused</returns>
        public virtual bool Pause()
        {
            if (!SoundLoaded || CBase.Sound.IsPaused(_StreamId))
            {
                return false;
            }

            CBase.Sound.Fade(_StreamId, 0, _FadeTime, EStreamAction.Pause);
            IsPlaying = false;
            return true;
        }

        /// <summary>
        ///     Stops the player (no playback and position is set to start)
        /// </summary>
        /// <returns>True if playback was stopped</returns>
        public virtual bool Stop()
        {
            if (!SoundLoaded)
            {
                return false;
            }

            CBase.Sound.Fade(_StreamId, 0, _FadeTime, EStreamAction.Stop);
            IsPlaying = false;
            return true;
        }

        public virtual void Close()
        {
            if (!SoundLoaded)
            {
                return;
            }

            CBase.Sound.Fade(_StreamId, 0, _FadeTime, EStreamAction.Close);
            _StreamId = -1;
            DisplayName = string.Empty;
            IsPlaying = false;
        }

        public void Update()
        {
            if (!IsPlaying)
            {
                return;
            }

            var finished = CBase.Sound.IsFinished(_StreamId);
            if (Loop)
            {
                if (finished)
                {
                    // Restart
                    Stop();
                    Play();
                }

                return;
            }

            var len = CBase.Sound.GetLength(_StreamId);
            var timeToPlay = len > 0f ? len - CBase.Sound.GetPosition(_StreamId) : _FadeTime + 1f;

            if (timeToPlay <= _FadeTime || finished)
            {
                Stop();
            }
        }
    }
}