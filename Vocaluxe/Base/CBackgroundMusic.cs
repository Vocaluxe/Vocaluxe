using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using Vocaluxe.Base;
using System.Diagnostics;

namespace Vocaluxe.Base
{
    static class CBackgroundMusic
    {
        private static CHelper Helper = new CHelper();
        private static List<int> _BackgroundMusicStreams = new List<int>();
        private static int _CurrentMusicStream;
        private static int _CurrentStreamListIndex = 0;
        private static bool _Fading;
        private static Stopwatch _FadeTimer = new Stopwatch();

        public static void init()
        {
            List<string> files = new List<string>();
            files.AddRange(Helper.ListFiles(CSettings.sFolderBackgroundMusic, "*.mp3", true, true));
            files.AddRange(Helper.ListFiles(CSettings.sFolderBackgroundMusic, "*.wav", true, true));
            files.AddRange(Helper.ListFiles(CSettings.sFolderBackgroundMusic, "*.ogg", true, true));
            files.AddRange(Helper.ListFiles(CSettings.sFolderBackgroundMusic, "*.wma", true, true));

            foreach (string file in files)
            {
                _BackgroundMusicStreams.Add(CSound.Load(file));
            }

            if(_BackgroundMusicStreams.Count > 0)
                _CurrentMusicStream = _BackgroundMusicStreams[0];
        }

        public static void Play()
        {
            if (CConfig.BackgroundMusic == EOffOn.TR_CONFIG_ON)
            {
                if(_BackgroundMusicStreams.Count == 1)
                {
                    CSound.Fade(_BackgroundMusicStreams[0], CConfig.BackgroundMusicVolume, CConfig.FadeTime);
                    CSound.Play(_BackgroundMusicStreams[0], true);
                }
                else if (_BackgroundMusicStreams.Count > 1)
                {
                    CSound.Fade(_CurrentMusicStream, CConfig.BackgroundMusicVolume, CConfig.FadeTime);
                    CSound.Play(_CurrentMusicStream);
                }
            }
        }

        public static void Stop()
        {
            if(_BackgroundMusicStreams.Count > 0)
                CSound.FadeAndStop(_BackgroundMusicStreams[_CurrentStreamListIndex], CConfig.BackgroundMusicVolume, CConfig.FadeTime);
        }

        public static void Pause()
        {
            if (_BackgroundMusicStreams.Count > 0)
                CSound.Pause(_BackgroundMusicStreams[_CurrentStreamListIndex]);
        }

        public static void Update()
        {
            if (_BackgroundMusicStreams.Count > 1)
            {
                float timeToPlay = CSound.GetLength(_CurrentMusicStream) - CSound.GetPosition(_CurrentMusicStream);
                if (_FadeTimer.ElapsedMilliseconds >= CConfig.FadeTime * 1000)
                {
                    _Fading = false;
                    _FadeTimer.Reset();
                }
                if ((timeToPlay <= CConfig.FadeTime) && !_Fading)
                {
                    _FadeTimer.Start();
                    CSound.Fade(_CurrentMusicStream, 0, CConfig.FadeTime);
                    _CurrentStreamListIndex++;
                    if (_CurrentStreamListIndex + 1 > _BackgroundMusicStreams.Count)
                    {
                        _CurrentStreamListIndex = 0;
                        _CurrentMusicStream = _BackgroundMusicStreams[_CurrentStreamListIndex];
                    }
                    else
                        _CurrentMusicStream = _BackgroundMusicStreams[_CurrentStreamListIndex];
                    CSound.SetPosition(_CurrentStreamListIndex, 0);
                    Play();
                    _Fading = true;
                }
            }
        }
    }
}
