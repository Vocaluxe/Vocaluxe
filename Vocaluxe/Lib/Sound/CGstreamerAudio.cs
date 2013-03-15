using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Vocaluxe.Base;
using Vocaluxe.Lib.Sound.Gstreamer;

namespace Vocaluxe.Lib.Sound
{
    class CGstreamerAudio : IPlayback
    {
        //static float LastPosition;
        #region log
        public CGstreamerAudioWrapper.LogCallback Log;

        private void LogHandler(string text)
        {
            CLog.LogError(text);
        }
        #endregion log

        public CGstreamerAudio()
        {
            Init();
            Log = new CGstreamerAudioWrapper.LogCallback(LogHandler);
            //Is this really needed? CodaAnalyzer complains about it...
            //GC.SuppressFinalize(Log);
            CGstreamerAudioWrapper.SetLogCallback(Log);
        }

        public bool Init()
        {
            return CGstreamerAudioWrapper.Init();
        }

        public void SetGlobalVolume(float Volume)
        {
            CGstreamerAudioWrapper.SetGlobalVolume(Volume);
        }

        public int GetStreamCount()
        {
            return CGstreamerAudioWrapper.GetStreamCount();
        }

        public void CloseAll()
        {
            CGstreamerAudioWrapper.CloseAll();
        }

        public int Load(string Media)
        {
            Uri u = new Uri(Media);
            int i = CGstreamerAudioWrapper.Load(u.AbsoluteUri);
            return i;
        }

        public int Load(string Media, bool Prescan)
        {
            Uri u = new Uri(Media);
            int i = CGstreamerAudioWrapper.Load(u.AbsoluteUri, Prescan);
            return i;
        }

        public void Close(int Stream)
        {
            CGstreamerAudioWrapper.Close(Stream);
        }

        public void Play(int Stream)
        {
            CGstreamerAudioWrapper.Play(Stream);
        }

        public void Play(int Stream, bool Loop)
        {
            CGstreamerAudioWrapper.Play(Stream, Loop);
        }

        public void Pause(int Stream)
        {
            CGstreamerAudioWrapper.Pause(Stream);
        }

        public void Stop(int Stream)
        {
            CGstreamerAudioWrapper.Stop(Stream);
        }

        public void Fade(int Stream, float TargetVolume, float Seconds)
        {
            CGstreamerAudioWrapper.Fade(Stream, TargetVolume, Seconds);
        }

        public void FadeAndPause(int Stream, float TargetVolume, float Seconds)
        {
            CGstreamerAudioWrapper.FadeAndPause(Stream, TargetVolume, Seconds);
        }

        public void FadeAndStop(int Stream, float TargetVolume, float Seconds)
        {
            CGstreamerAudioWrapper.FadeAndStop(Stream, TargetVolume, Seconds);
        }

        public void SetStreamVolume(int Stream, float Volume)
        {
            CGstreamerAudioWrapper.SetStreamVolume(Stream, Volume);
        }

        public void SetStreamVolumeMax(int Stream, float Volume)
        {
            CGstreamerAudioWrapper.SetStreamVolumeMax(Stream, Volume);
        }

        public float GetLength(int Stream)
        {
            return CGstreamerAudioWrapper.GetLength(Stream);
        }

        public float GetPosition(int Stream)
        {
            return CGstreamerAudioWrapper.GetPosition(Stream);
        }

        public bool IsPlaying(int Stream)
        {
            return CGstreamerAudioWrapper.IsPlaying(Stream);
        }

        public bool IsPaused(int Stream)
        {
            return CGstreamerAudioWrapper.IsPaused(Stream);
        }

        public bool IsFinished(int Stream)
        {
            return CGstreamerAudioWrapper.IsFinished(Stream);
        }

        public void SetPosition(int Stream, float Position)
        {
            CGstreamerAudioWrapper.SetPosition(Stream, Position);
        }

        public void Update()
        {
            CGstreamerAudioWrapper.Update();
        }
    }
}
