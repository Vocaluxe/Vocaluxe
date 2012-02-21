using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using Vocaluxe.Base;
using System.Diagnostics;
using Vocaluxe.Lib.Song;

namespace Vocaluxe.Base
{
    static class CBackgroundMusic
    {
        private static CHelper Helper = new CHelper();
        private static int _CurrentMusicStream = -1;
        private static bool _Fading;
        private static Stopwatch _FadeTimer = new Stopwatch();
        private static Random _Random = new Random();
        private static List<string> _BackgroundMusicFileNames = new List<string>();
        private static int _CurrentBackgroundMusicFileNameIndex;
        private static bool _OwnMusicAdded;
        private static bool _BackgroundMusicAdded;

        public static void Init()
        {
            if(CConfig.BackgroundMusicSource == EBackgroundMusicSource.TR_CONFIG_NO_OWN_MUSIC || CConfig.BackgroundMusicSource == EBackgroundMusicSource.TR_CONFIG_OWN_MUSIC)
                AddBackgroundMusic();
        }

        public static void Play()
        {
            if (CConfig.BackgroundMusic == EOffOn.TR_CONFIG_ON)
            {
                if (_BackgroundMusicFileNames.Count > 0)
                {
                    CSound.Fade(_CurrentMusicStream, CConfig.BackgroundMusicVolume, CConfig.FadeTime);
                    CSound.Play(_CurrentMusicStream);
                }
            }
        }

        public static void Stop()
        {
            if (_BackgroundMusicFileNames.Count > 0)
                CSound.FadeAndStop(_CurrentMusicStream, CConfig.BackgroundMusicVolume, CConfig.FadeTime);
        }

        public static void Pause()
        {
            if (_BackgroundMusicFileNames.Count > 0)
                CSound.FadeAndPause(_CurrentMusicStream, 0, CConfig.FadeTime);
        }

        public static void Update()
        {
            if (_BackgroundMusicFileNames.Count > 0)
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
                    Next();
                    _Fading = true;
                }
            }
        }

        public static void Next()
        {
            if (_BackgroundMusicFileNames.Count > 0)
            {
                if (_CurrentMusicStream != -1)
                    CSound.FadeAndStop(_CurrentMusicStream, 0, CConfig.FadeTime);
                _CurrentBackgroundMusicFileNameIndex = _Random.Next(_BackgroundMusicFileNames.Count);
                _CurrentMusicStream = CSound.Load(_BackgroundMusicFileNames[_CurrentBackgroundMusicFileNameIndex]);
                Play();
            }
            else if (_BackgroundMusicAdded || _OwnMusicAdded)
            {
                if (_CurrentMusicStream != -1)
                    CSound.FadeAndStop(_CurrentMusicStream, 0, CConfig.FadeTime);
            }
        }

        public static void ApplyVolume()
        {
            CSound.Fade(_CurrentMusicStream, CConfig.BackgroundMusicVolume, CConfig.FadeTime); 
        }

        public static void AddOwnMusic()
        {
            if (!_OwnMusicAdded)
            {
                foreach (CSong song in CSongs.AllSongs)
                {
                    _BackgroundMusicFileNames.Add(song.GetMP3());
                }
            }
            _OwnMusicAdded = true;
            Next();
        }

        public static void RemoveOwnMusic()
        {
            foreach (CSong song in CSongs.AllSongs)
            {
                _BackgroundMusicFileNames.Remove(song.GetMP3());
            }
            _OwnMusicAdded = false;
        }

        public static void AddBackgroundMusic()
        {
            if (!_BackgroundMusicAdded)
            {
                _BackgroundMusicFileNames.AddRange(Helper.ListFiles(CSettings.sFolderBackgroundMusic, "*.mp3", true, true));
                _BackgroundMusicFileNames.AddRange(Helper.ListFiles(CSettings.sFolderBackgroundMusic, "*.wav", true, true));
                _BackgroundMusicFileNames.AddRange(Helper.ListFiles(CSettings.sFolderBackgroundMusic, "*.ogg", true, true));
                _BackgroundMusicFileNames.AddRange(Helper.ListFiles(CSettings.sFolderBackgroundMusic, "*.wma", true, true));
                _BackgroundMusicAdded = true;
                Next();
            }
        }

        public static void RemoveBackgroundMusic()
        {
            if (_BackgroundMusicAdded)
            {
                List<string> removeList = new List<string>();
                removeList.AddRange(Helper.ListFiles(CSettings.sFolderBackgroundMusic, "*.mp3", true, true));
                removeList.AddRange(Helper.ListFiles(CSettings.sFolderBackgroundMusic, "*.wav", true, true));
                removeList.AddRange(Helper.ListFiles(CSettings.sFolderBackgroundMusic, "*.ogg", true, true));
                removeList.AddRange(Helper.ListFiles(CSettings.sFolderBackgroundMusic, "*.wma", true, true));

                foreach (string s in removeList)
                {
                    _BackgroundMusicFileNames.Remove(s);
                }
                _BackgroundMusicAdded = false;
            }
        }
    }
}
