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
        private static int _PreviousMusicIndex = 0;
        private static string _CurrentMusicFilePath = String.Empty;
        
        private static List<string> _AllFileNames = new List<string>();
        private static List<string> _NotPlayedFileNames = new List<string>();
        private static List<string> _BGMusicFileNames = new List<string>();
        private static List<string> _PreviousFileNames = new List<string>();
        
        private static bool _OwnMusicAdded;
        private static bool _BackgroundMusicAdded;
        private static bool _Playing;

        public static void Init()
        {
            _BGMusicFileNames.AddRange(Helper.ListFiles(CSettings.sFolderBackgroundMusic, "*.mp3", true, true));
            _BGMusicFileNames.AddRange(Helper.ListFiles(CSettings.sFolderBackgroundMusic, "*.wav", true, true));
            _BGMusicFileNames.AddRange(Helper.ListFiles(CSettings.sFolderBackgroundMusic, "*.ogg", true, true));
            _BGMusicFileNames.AddRange(Helper.ListFiles(CSettings.sFolderBackgroundMusic, "*.wma", true, true));

            if(CConfig.BackgroundMusicSource != EBackgroundMusicSource.TR_CONFIG_ONLY_OWN_MUSIC)
                AddBackgroundMusic();

            _Playing = false;
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
                        _Playing = true;
                    }
                    else
                        Next();
                }
            }
        }

        public static void Stop()
        {
            CSound.FadeAndStop(_CurrentMusicStream, 0f, CSettings.BackgroundMusicFadeTime);

            _CurrentMusicFilePath = String.Empty;
            _Playing = false;
        }

        public static void Pause()
        {
            CSound.FadeAndPause(_CurrentMusicStream, 0f, CSettings.BackgroundMusicFadeTime);
            _Playing = false;
        }

        public static void Update()
        {
            if (_AllFileNames.Count > 0 && _CurrentMusicStream != -1)
            {
                float timeToPlay = CSound.GetLength(_CurrentMusicStream) - CSound.GetPosition(_CurrentMusicStream);
                bool finished = CSound.IsFinished(_CurrentMusicStream);
                if (_Playing && (timeToPlay <= CSettings.BackgroundMusicFadeTime || finished))
                    Next();
            }
        }

        public static void Next()
        {
            if (_AllFileNames.Count > 0)
            {
                if (_PreviousMusicIndex == _PreviousFileNames.Count - 1 || _PreviousFileNames.Count == 0) //We are not currently in the previous list
                {
                    Stop();
                    if (_NotPlayedFileNames.Count == 0)
                        _NotPlayedFileNames.AddRange(_AllFileNames);

                    _CurrentMusicFilePath = _NotPlayedFileNames[CGame.Rand.Next(_NotPlayedFileNames.Count)];
                    _NotPlayedFileNames.Remove(_CurrentMusicFilePath);

                    _PreviousFileNames.Add(_CurrentMusicFilePath);
                    _PreviousMusicIndex = _PreviousFileNames.Count - 1;
                }
                else if(_PreviousFileNames.Count > 0) //We are in the previous list
                {
                    Stop();
                    _PreviousMusicIndex++;

                    _CurrentMusicFilePath = _PreviousFileNames[_PreviousMusicIndex];
                }

                _CurrentMusicStream = CSound.Load(_CurrentMusicFilePath);
                CSound.SetStreamVolume(_CurrentMusicStream, 0f);
                Play();
            }
            else
                Stop();
        }

        public static void Previous()
        {
            if (_PreviousFileNames.Count > 0 || _PreviousMusicIndex >= 0)
            {
                Stop();

                _PreviousMusicIndex--;
                if (_PreviousMusicIndex < 0)
                    _PreviousMusicIndex = 0; //No previous songs left, so play the first

                _CurrentMusicFilePath = _PreviousFileNames[_PreviousMusicIndex];
               

                _CurrentMusicStream = CSound.Load(_CurrentMusicFilePath);
                CSound.SetStreamVolume(_CurrentMusicStream, 0f);
                Play();
            }
        }

        public static void Repeat()
        {
            if (_AllFileNames.Count > 0 && _CurrentMusicStream != -1)
            {
                CSound.SetPosition(_CurrentMusicStream, 0);
            }
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
                    _NotPlayedFileNames.Add(song.GetMP3());
                }
            }
            _OwnMusicAdded = true;
        }

        public static void RemoveOwnMusic()
        {
            _AllFileNames.Clear();
            _NotPlayedFileNames.Clear();

            if (_BackgroundMusicAdded)
            {
                _AllFileNames.AddRange(_BGMusicFileNames);
                _NotPlayedFileNames.AddRange(_BGMusicFileNames);
            }

            if (IsPlaying() && !IsBackgroundFile(_CurrentMusicFilePath) || _AllFileNames.Count == 0)
                Next();

            _OwnMusicAdded = false;
        }

        public static void AddBackgroundMusic()
        {
            if (!_BackgroundMusicAdded)
            {
                _AllFileNames.AddRange(_BGMusicFileNames);
                _NotPlayedFileNames.AddRange(_BGMusicFileNames);
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

                if (IsPlaying() && IsBackgroundFile(_CurrentMusicFilePath) || _AllFileNames.Count == 0)
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
