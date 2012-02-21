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
        private static string _CurrentMusicFilePath = String.Empty;
        
        private static List<string> _AllFileNames = new List<string>();
        private static List<string> _BGMusicFileNames = new List<string>();
        
        private static bool _OwnMusicAdded;
        private static bool _BackgroundMusicAdded;

        public static void Init()
        {
            _BGMusicFileNames.AddRange(Helper.ListFiles(CSettings.sFolderBackgroundMusic, "*.mp3", true, true));
            _BGMusicFileNames.AddRange(Helper.ListFiles(CSettings.sFolderBackgroundMusic, "*.wav", true, true));
            _BGMusicFileNames.AddRange(Helper.ListFiles(CSettings.sFolderBackgroundMusic, "*.ogg", true, true));
            _BGMusicFileNames.AddRange(Helper.ListFiles(CSettings.sFolderBackgroundMusic, "*.wma", true, true));

            if(CConfig.BackgroundMusicSource != EBackgroundMusicSource.TR_CONFIG_ONLY_OWN_MUSIC)
                AddBackgroundMusic();
        }

        public static void Play()
        {
            if (CConfig.BackgroundMusic == EOffOn.TR_CONFIG_ON)
            {
                if (_AllFileNames.Count > 0)
                {
                    if (_CurrentMusicStream != -1)
                    {
                        CSound.Fade(_CurrentMusicStream, CConfig.BackgroundMusicVolume, CSettings.BackgroundMusicFadeTime);
                        CSound.Play(_CurrentMusicStream);
                    }
                    else
                        Next();
                }
            }
        }

        public static void Stop()
        {
            if (_AllFileNames.Count > 0)
                CSound.FadeAndStop(_CurrentMusicStream, 0f, CSettings.BackgroundMusicFadeTime);

            _CurrentMusicFilePath = String.Empty;
        }

        public static void Pause()
        {
            if (_AllFileNames.Count > 0)
                CSound.FadeAndPause(_CurrentMusicStream, 0f, CSettings.BackgroundMusicFadeTime);
        }

        public static void Update()
        {
            if (_AllFileNames.Count > 0 && _CurrentMusicStream != -1)
            {
                float timeToPlay = CSound.GetLength(_CurrentMusicStream) - CSound.GetPosition(_CurrentMusicStream);

                if (timeToPlay <= CSettings.BackgroundMusicFadeTime)
                    Next();
            }
        }

        public static void Next()
        {
            if (_AllFileNames.Count > 0)
            {
                Stop();

                _CurrentMusicFilePath = _AllFileNames[CGame.Rand.Next(_AllFileNames.Count)];
                _CurrentMusicStream = CSound.Load(_CurrentMusicFilePath);
                CSound.SetStreamVolume(_CurrentMusicStream, 0f);
                Play();
            }
            else
                Stop();
        }

        public static void ApplyVolume()
        {
            CSound.SetStreamVolume(_CurrentMusicStream, CConfig.BackgroundMusicVolume); 
        }

        public static void AddOwnMusic()
        {
            if (!_OwnMusicAdded)
            {
                foreach (CSong song in CSongs.AllSongs)
                {
                    _AllFileNames.Add(song.GetMP3());
                }
            }
            _OwnMusicAdded = true;
        }

        public static void RemoveOwnMusic()
        {
            _AllFileNames.Clear();

            if (_BackgroundMusicAdded)
                _AllFileNames.AddRange(_BGMusicFileNames);

            if (IsPlaying() && !IsBackgroundFile(_CurrentMusicFilePath))
                Next();

            _OwnMusicAdded = false;
        }

        public static void AddBackgroundMusic()
        {
            if (!_BackgroundMusicAdded)
            {
                _AllFileNames.AddRange(_BGMusicFileNames);
                _BackgroundMusicAdded = true;
            }
        }

        public static void RemoveBackgroundMusic()
        {
            if (_BackgroundMusicAdded)
            {
                _AllFileNames.Clear();
                _OwnMusicAdded = false;
                AddOwnMusic();
                _BackgroundMusicAdded = false;

                if (IsPlaying() && IsBackgroundFile(_CurrentMusicFilePath))
                    Next();
            }
        }

        public static void CheckAndApplyConfig(EOffOn NewOnOff, EBackgroundMusicSource NewSource, float NewVolume)
        {
            if (CConfig.BackgroundMusic != NewOnOff)
            {
                CConfig.BackgroundMusic = NewOnOff;
                if (NewOnOff == EOffOn.TR_CONFIG_ON)
                    Play();
                else
                    Pause();
            }

            if (CConfig.BackgroundMusicSource != NewSource)
            {
                CConfig.BackgroundMusicSource = NewSource;

                switch (NewSource)
                {
                    case EBackgroundMusicSource.TR_CONFIG_NO_OWN_MUSIC:
                        if (!_BackgroundMusicAdded)
                            AddBackgroundMusic();

                        RemoveOwnMusic();
                        break;

                    case EBackgroundMusicSource.TR_CONFIG_ONLY_OWN_MUSIC:
                        if (!_OwnMusicAdded)
                            AddOwnMusic();

                        RemoveBackgroundMusic();
                        break;
                    case EBackgroundMusicSource.TR_CONFIG_OWN_MUSIC:
                        if (!_BackgroundMusicAdded)
                            AddBackgroundMusic();

                        if (!_OwnMusicAdded)
                            AddOwnMusic();
                        break;
                }
            }

            if (CConfig.BackgroundMusicVolume != NewVolume)
            {
                CConfig.BackgroundMusicVolume = (int)NewVolume;
                ApplyVolume();
            }

            CConfig.SaveConfig();
        }

        public static bool IsPlaying()
        {
            return CSound.IsPlaying(_CurrentMusicStream);
        }

        private static bool IsBackgroundFile(string FilePath)
        {
            foreach (string file in _BGMusicFileNames)
            {
                if (file == FilePath)
                    return true;
            }
            return false;
        }
    }
}
