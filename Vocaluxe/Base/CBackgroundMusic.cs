using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using Vocaluxe.Base;
using System.Diagnostics;
using Vocaluxe.Lib.Song;
using Vocaluxe.Lib.Draw;
using System.Drawing;
using Vocaluxe.Menu;

namespace Vocaluxe.Base
{
    static class CBackgroundMusic
    {
        private static CHelper Helper = new CHelper();
        private static int _CurrentMusicStream = -1;
        private static int _PreviousMusicIndex = 0;
        private static PlaylistElement _CurrentPlaylistElement = new PlaylistElement();

        private static List<PlaylistElement> _AllFileNames = new List<PlaylistElement>();
        private static List<PlaylistElement> _NotPlayedFileNames = new List<PlaylistElement>();
        private static List<PlaylistElement> _BGMusicFileNames = new List<PlaylistElement>();
        private static List<PlaylistElement> _PreviousFileNames = new List<PlaylistElement>();
        
        private static bool _OwnMusicAdded;
        private static bool _BackgroundMusicAdded;
        private static bool _Playing;
        private static bool _VideoEnabled;
        private static bool _Disabled;

        private static int _Video = -1;
        private static STexture _CurrentVideoTexture = new STexture(-1);
        private static Stopwatch _FadeTimer = new Stopwatch();

        public static void Init()
        {
            List<string> templist = new List<string>();

            foreach(string ending in CSettings.MusicFileTypes)
            {
                templist.AddRange(Helper.ListFiles(CSettings.sFolderBackgroundMusic, ending, true, true));
            }

            foreach (string path in templist)
            {
                _BGMusicFileNames.Add(new PlaylistElement(path));
            }

            if(CConfig.BackgroundMusicSource != EBackgroundMusicSource.TR_CONFIG_ONLY_OWN_MUSIC)
                AddBackgroundMusic();
            if (CConfig.VideoBackgrounds == EOffOn.TR_CONFIG_ON)
                EnableVideo();

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
                        if (_VideoEnabled && _Video != -1)
                            CVideo.VdResume(_Video);
                        _Playing = true;
                    }
                    else
                        Next();
                }
            }
        }

        public static void Stop()
        {
            if (_VideoEnabled && _Video != -1)
            {
                CVideo.VdClose(_Video);
                CDraw.RemoveTexture(ref _CurrentVideoTexture);
                _Video = -1;
            }
            CSound.FadeAndStop(_CurrentMusicStream, 0f, CSettings.BackgroundMusicFadeTime);

            _CurrentPlaylistElement = new PlaylistElement();
            _Playing = false;
        }

        public static void Pause()
        {
            if (_VideoEnabled && _Video != -1)
            {
                CVideo.VdPause(_Video);
                CVideo.VdSkip(_Video, CSound.GetPosition(_CurrentMusicStream) + CSettings.BackgroundMusicFadeTime, _CurrentPlaylistElement.GetVideoGap());
            }
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

                    _CurrentPlaylistElement = _NotPlayedFileNames[CGame.Rand.Next(_NotPlayedFileNames.Count)];
                    _NotPlayedFileNames.Remove(_CurrentPlaylistElement);

                    _PreviousFileNames.Add(_CurrentPlaylistElement);
                    _PreviousMusicIndex = _PreviousFileNames.Count - 1;
                }
                else if(_PreviousFileNames.Count > 0) //We are in the previous list
                {
                    Stop();
                    _PreviousMusicIndex++;

                    _CurrentPlaylistElement = _PreviousFileNames[_PreviousMusicIndex];
                }
                _CurrentMusicStream = CSound.Load(_CurrentPlaylistElement.GetMusicFilePath());
                if (_VideoEnabled)
                    LoadVideo();
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

                _CurrentPlaylistElement = _PreviousFileNames[_PreviousMusicIndex];

                _CurrentMusicStream = CSound.Load(_CurrentPlaylistElement.GetMusicFilePath());
                if (_VideoEnabled)
                    LoadVideo();
                CSound.SetStreamVolume(_CurrentMusicStream, 0f);
                Play();
            }
        }

        public static void Repeat()
        {
            if (_AllFileNames.Count > 0 && _CurrentMusicStream != -1)
            {
                CSound.SetPosition(_CurrentMusicStream, 0);
                if (_VideoEnabled && _Video != -1)
                    CVideo.VdSkip(_Video, CSound.GetPosition(_CurrentMusicStream), _CurrentPlaylistElement.GetVideoGap());
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
                    _AllFileNames.Add(new PlaylistElement(song));
                    _NotPlayedFileNames.Add(new PlaylistElement(song));
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

            if (IsPlaying() && !IsBackgroundFile(_CurrentPlaylistElement) || _AllFileNames.Count == 0)
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

                if (IsPlaying() && IsBackgroundFile(_CurrentPlaylistElement) || _AllFileNames.Count == 0)
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

        public static string GetSongArtistAndTitle()
        {
            return _CurrentPlaylistElement.GetArtist() +" - " + _CurrentPlaylistElement.GetTitle();
        }

        public static STexture GetCover()
        {
            return _CurrentPlaylistElement.GetCover();
        }

        public static void ToggleVideo()
        {
            if (_Video != -1)
            {
                if (_VideoEnabled)
                {
                    _VideoEnabled = false;
                    CVideo.VdClose(_Video);
                    _Video = -1;
                    CDraw.RemoveTexture(ref _CurrentVideoTexture);
                    return;
                }
                if (CVideo.VdFinished(_Video))
                {
                    CVideo.VdClose(_Video);
                    CDraw.RemoveTexture(ref _CurrentVideoTexture);
                    _Video = -1;
                    return;
                }
            }
            else
                LoadVideo();
        }

        public static STexture GetVideoTexture()
        {
            if (_Video != -1)
            {
                float vtime = 0f;
                CVideo.VdGetFrame(_Video, ref _CurrentVideoTexture, CSound.GetPosition(_CurrentMusicStream), ref vtime);
                if (_FadeTimer.ElapsedMilliseconds <= 3000L)
                {
                    _CurrentVideoTexture.color.A = (_FadeTimer.ElapsedMilliseconds / 3000f);
                }
                else
                {
                    _CurrentVideoTexture.color.A = 1f;
                    _FadeTimer.Stop();
                }
                return _CurrentVideoTexture;
            }
            return new STexture(-1);
        }

        public static int GetSongNr()
        {
            return _CurrentPlaylistElement.GetSongNr();
        }

        public static bool IsDuet()
        {
            return _CurrentPlaylistElement.IsDuet();
        }

        public static bool IsVideoEnabled()
        {
            return _VideoEnabled;
        }

        public static bool HasVideo()
        {
            return File.Exists(_CurrentPlaylistElement.GetVideoFilePath());
        }

        private static bool IsBackgroundFile(PlaylistElement element)
        {
            foreach (PlaylistElement elements in _BGMusicFileNames)
            {
                if (elements.GetMusicFilePath() == element.GetMusicFilePath())
                    return true;
            }
            return false;
        }

        private static void LoadVideo()
        {
            if (_Video == -1)
            {
                _Video = CVideo.VdLoad(_CurrentPlaylistElement.GetVideoFilePath());
                CVideo.VdSkip(_Video, CSound.GetPosition(_CurrentMusicStream), _CurrentPlaylistElement.GetVideoGap());
                _VideoEnabled = true;
                _FadeTimer.Reset();
                _FadeTimer.Start();
            }
        }

        public static void EnableVideo()
        {
            if (!_VideoEnabled)
                ToggleVideo();
        }

        public static void Disable()
        {
            Pause();
            _Disabled = true;
        }

        public static void Enable()
        {
            Play();
            _Disabled = false;
        }

        public static bool IsEnabled()
        {
            return !_Disabled;
        }
    }
}

class PlaylistElement
{
    string _MusicFilePath;
    int _SongNr;

    public PlaylistElement(CSong song)
    {
        _MusicFilePath = string.Empty;
        _SongNr = song.ID;
    }

    public PlaylistElement(string FilePath)
    {
        _MusicFilePath = FilePath;
        _SongNr = -1;
    }

    public PlaylistElement()
    {
        _MusicFilePath = string.Empty;
        _SongNr = -1;
    }

    public string GetMusicFilePath()
    {
        if (_SongNr >= 0)
            if (CSongs.GetSong(_SongNr) != null)
                return CSongs.GetSong(_SongNr).GetMP3();
            else
            {
                _SongNr = -1;
                CLog.LogError("Song not found");
            }
        return _MusicFilePath;
    }

    public string GetVideoFilePath()
    {
        if (_SongNr >= 0)
            if (CSongs.GetSong(_SongNr) != null)
                return CSongs.GetSong(_SongNr).GetVideo();
            else
            {
                _SongNr = -1;
                CLog.LogError("Song not found");
            }
        return string.Empty;
    }

    public string GetTitle()
    {
        if (_SongNr >= 0)
            if (CSongs.GetSong(_SongNr) != null)
                return CSongs.GetSong(_SongNr).Title;
            else
            {
                _SongNr = -1;
                CLog.LogError("Song not found");
            }
        return "Unknown";
    }

    public string GetArtist()
    {
        if (_SongNr >= 0)
            if (CSongs.GetSong(_SongNr) != null)
                return CSongs.GetSong(_SongNr).Artist;
            else
            {
                _SongNr = -1;
                CLog.LogError("Song not found");
            }
        return "Unknown";
    }

    public STexture GetCover()
    {
        if (_SongNr >= 0)
            if (CSongs.GetSong(_SongNr) != null)
                return CSongs.GetSong(_SongNr).CoverTextureSmall;
            else
            {
                _SongNr = -1;
                CLog.LogError("Song not found");
            }
        return CCover.NoCover;
    }

    public float GetVideoGap()
    {
        if (_SongNr >= 0)
            if (CSongs.GetSong(_SongNr) != null)
                return CSongs.GetSong(_SongNr).VideoGap;
            else
            {
                _SongNr = -1;
                CLog.LogError("Song not found");
            }
        return 0;
    }

    public int GetSongNr()
    {
        return _SongNr;
    }

    public bool IsDuet()
    {
        if (_SongNr >= 0)
            if (CSongs.GetSong(_SongNr) != null)
                return CSongs.GetSong(_SongNr).IsDuet;
            else
            {
                _SongNr = -1;
                CLog.LogError("Song not found");
            }
        return false;
    }
}
