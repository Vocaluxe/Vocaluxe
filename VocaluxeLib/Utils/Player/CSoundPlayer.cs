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

namespace VocaluxeLib.Utils.Player
{
    public class CSoundPlayer
    {
        protected int _StreamID = -1;
        protected readonly float _FadeTime = CBase.Settings.GetSoundPlayerFadeTime();
        public string FilePath { get; private set; }

        public bool Loop;

        /// <summary>
        ///     Gets the current stream position or sets it
        /// </summary>
        public float Position
        {
            set
            {
                if (!SoundLoaded)
                    return;
                CBase.Sound.SetPosition(_StreamID, value);
            }
            get { return !SoundLoaded ? -1 : CBase.Sound.GetPosition(_StreamID); }
        }

        public float Length
        {
            get { return !SoundLoaded ? -1 : CBase.Sound.GetLength(_StreamID); }
        }

        public bool IsPlaying { get; private set; }

        public bool IsFinished
        {
            get { return !Loop && (CBase.Sound.IsFinished(_StreamID) || !IsPlaying); }
        }

        public bool SoundLoaded
        {
            get { return _StreamID != -1; }
        }

        public virtual string ArtistAndTitle
        {
            get { return string.IsNullOrEmpty(FilePath) ? "" : Path.GetFileNameWithoutExtension(FilePath); }
        }

        public CSoundPlayer(bool loop = false)
        {
            Loop = loop;
        }

        public void Load(string file, float position = -1f, bool autoplay = false)
        {
            Close();

            _StreamID = CBase.Sound.Load(file, false, true);
            if (_StreamID < 0)
                return;
            FilePath = file;
            if (position > 0f)
                Position = position;
            if (autoplay)
                Play();
        }

        /// <summary>
        ///     Starts or resumes the player
        /// </summary>
        /// <returns>True if state changed, false if nothing loaded or already playing</returns>
        public virtual bool Play()
        {
            if (!SoundLoaded || IsPlaying)
                return false;

            CBase.Sound.SetStreamVolume(_StreamID, 0);
            CBase.Sound.Fade(_StreamID, 100, _FadeTime);
            CBase.Sound.Play(_StreamID);
            IsPlaying = true;
            return true;
        }

        /// <summary>
        ///     Pauses the player
        /// </summary>
        /// <returns>True if state changed, false if nothing loaded or already paused</returns>
        public virtual bool Pause()
        {
            if (!SoundLoaded || CBase.Sound.IsPaused(_StreamID))
                return false;

            CBase.Sound.Fade(_StreamID, 0, _FadeTime, EStreamAction.Pause);
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
                return false;

            CBase.Sound.Fade(_StreamID, 0, _FadeTime, EStreamAction.Stop);
            IsPlaying = false;
            return true;
        }

        public virtual void Close()
        {
            if (!SoundLoaded)
                return;

            CBase.Sound.Fade(_StreamID, 0, _FadeTime, EStreamAction.Close);
            _StreamID = -1;
            FilePath = "";
            IsPlaying = false;
        }

        public void Update()
        {
            if (!IsPlaying)
                return;

            bool finished = CBase.Sound.IsFinished(_StreamID);
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

            float len = CBase.Sound.GetLength(_StreamID);
            float timeToPlay = (len > 0f) ? len - CBase.Sound.GetPosition(_StreamID) : _FadeTime + 1f;

            if (timeToPlay <= _FadeTime || finished)
                Stop();
        }
    }
}