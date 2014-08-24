using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VocaluxeLib.Utils.Player
{
    public class CSoundPlayer
    {
        protected int _StreamID = -1;
        protected float _Volume;
        protected float _StartPosition;
        protected float _FadeTime = CBase.Settings.GetSoundPlayerFadeTime();

        public float Volume
        {
            set 
            {
                _Volume = value;
                _ApplyVolume(); 
            }
            get { return _Volume; }
        }
        public bool Loop;

        public float Position
        {
            set { 
                if(_StreamID == -1)
                    return;
                _StartPosition = value;
            }
            get { 
                if(_StreamID == -1)
                    return -1;
                return CBase.Sound.GetPosition(_StreamID);
            }
        }

        public float Length
        {
            get
            {
                if (_StreamID == -1)
                    return 0;
                return CBase.Sound.GetLength(_StreamID);
            }
        }

        public bool RepeatSong;
        public bool IsPlaying { get; private set; }

        public bool IsFinished { get { return CBase.Sound.IsFinished(_StreamID); } }

        public CSoundPlayer(bool loop = false)
        {
            Loop = loop;
        }

        public CSoundPlayer(string file, bool loop = false, float position = 0f, bool autoplay = false)
        {
            _StreamID = CBase.Sound.Load(file, false, true);
            if(position > 0f)
                Position = position;
            Loop = loop;
            if (autoplay)
                Play();
        }

        public void Load(string file, float position = 0f, bool autoplay = false)
        {
            if (IsPlaying)
                Stop();

            _StreamID = CBase.Sound.Load(file, false, true);
            if (position > 0f)
                Position = position;
            if (autoplay)
                Play();
        }

        public void Play()
        {
            if (_StreamID == -1 || IsPlaying)
                return;

            CBase.Sound.SetPosition(_StreamID, _StartPosition);
            CBase.Sound.SetStreamVolume(_StreamID, 0f);
            CBase.Sound.Fade(_StreamID, Volume, _FadeTime);
            CBase.Sound.Play(_StreamID);
            IsPlaying = true;
        }

        public void TogglePause()
        {
            if (_StreamID == -1)
                return;

            if(IsPlaying)
                CBase.Sound.Fade(_StreamID, 0f, _FadeTime, EStreamAction.Pause);
            else
            {
                CBase.Sound.Fade(_StreamID, Volume, _FadeTime);
                CBase.Sound.Play(_StreamID);
            }

            IsPlaying = !IsPlaying;
        }

        public void Stop()
        {
            if (_StreamID == -1 || !IsPlaying)
                return;

            CBase.Sound.Fade(_StreamID, 0f, _FadeTime, EStreamAction.Close);
            _StreamID = -1;

            IsPlaying = false;
        }

        public void Update()
        {
            if (_StreamID == -1 || !IsPlaying)
                return;

            float timeToPlay;
            float len = CBase.Sound.GetLength(_StreamID);
            timeToPlay = (len > 0f) ? len - CBase.Sound.GetPosition(_StreamID) : _FadeTime + 1f;
            
            bool finished = CBase.Sound.IsFinished(_StreamID);
            if (timeToPlay <= _FadeTime || finished)
            {
                if (RepeatSong)
                    Play();
                else
                    Stop();
            }
        
        }

        private void _ApplyVolume()
        {
            if (_StreamID == -1)
                return;

            CBase.Sound.SetStreamVolume(_StreamID, Volume);
        }
    }
}
