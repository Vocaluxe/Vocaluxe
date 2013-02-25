using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.IO;

using Vocaluxe.Base;
using System.Diagnostics;
using Vocaluxe.Lib.Draw;

using Vocaluxe.Menu;
using Vocaluxe.Menu.SongMenu;

namespace Vocaluxe.Base
{
    static class CBackgroundMusic
    {
        private static int _CurrentMusicStream = -1;
        private static int _PreviousMusicIndex = 0;
        private static PlaylistElement _CurrentPlaylistElement = new PlaylistElement();

        private static List<PlaylistElement> _AllFileNames = new List<PlaylistElement>();
        private static List<PlaylistElement> _NotPlayedFileNames = new List<PlaylistElement>();
        private static List<PlaylistElement> _BGMusicFileNames = new List<PlaylistElement>();
        private static List<PlaylistElement> _PreviousFileNames = new List<PlaylistElement>();

        private static int _Video = -1;
        private static STexture _CurrentVideoTexture = new STexture(-1);
        private static Stopwatch _FadeTimer = new Stopwatch();
        private static bool _VideoEnabled;
        
        private static bool _OwnMusicAdded;
        private static bool _BackgroundMusicAdded;
        private static bool _Playing;
        private static bool _Disabled;
        private static bool _CanSing;
        private static bool _RepeatSong;

        public static bool VideoEnabled
        {
            get
            {
                return _VideoEnabled;
            }
            set
            {
                if (_VideoEnabled && value)
                    return;
                if (!_VideoEnabled && !value)
                    return;
                if(_VideoEnabled && !value)
                    ToggleVideo();
                if (!_VideoEnabled && value)
                    ToggleVideo();
                _VideoEnabled = value;
            }
        }

        public static bool CanSing
        {
            get
            {
                return _CanSing;
            }

            set
            {
                _CanSing = value;
            }
        }

        public static bool Disabled
        {
            get
            {
                return _Disabled;
            }
            set
            {
                _Disabled = value;
                if (_Disabled)
                    Pause();
                else
                    Play();
            }
        }

        public static bool RepeatSong
        {
            get
            {
                return _RepeatSong;
            }
            set
            {
                _RepeatSong = value;
            }
        }

        public static int SongID
        {
            get
            {
                return _CurrentPlaylistElement.SongID;
            }
        }

        public static bool Duet
        {
            get
            {
                return _CurrentPlaylistElement.Duet;
            }
        }

        public static bool SongHasVideo
        {
            get
            {
                return File.Exists(_CurrentPlaylistElement.VideoFilePath);
            }
        }

        public static bool Playing
        {
            get
            {
                return _Playing;
            }
        }

        public static string ArtistAndTitle
        {
            get
            {
                if (_CurrentPlaylistElement.Artist != "" && _CurrentPlaylistElement.Title != "")
                    return _CurrentPlaylistElement.Artist + " - " + _CurrentPlaylistElement.Title;
                else
                    return Path.GetFileNameWithoutExtension(_CurrentPlaylistElement.MusicFilePath);
            }
        }

        public static STexture Cover
        {
            get
            {
                return _CurrentPlaylistElement.Cover;
            }
        }

        public static void Init()
        {
            List<string> templist = new List<string>();

            foreach(string ending in CSettings.MusicFileTypes)
            {
                templist.AddRange(CHelper.ListFiles(CSettings.sFolderBackgroundMusic, ending, true, true));
            }

            foreach (string path in templist)
            {
                _BGMusicFileNames.Add(new PlaylistElement(path));
            }

            if(CConfig.BackgroundMusicSource != EBackgroundMusicSource.TR_CONFIG_ONLY_OWN_MUSIC)
                AddBackgroundMusic();
            if (CConfig.VideoBackgrounds == EOffOn.TR_CONFIG_ON)
                _VideoEnabled = true;

            _Playing = false;
        }

        public static void Play()
        {
            if (_Playing)
                return;

            if (CConfig.BackgroundMusic == EOffOn.TR_CONFIG_ON)
            {
                if (_AllFileNames.Count > 0)
                {
                    if (_CurrentMusicStream != -1)
                    {
                        CSound.Fade(_CurrentMusicStream, 100f, CSettings.BackgroundMusicFadeTime);
                        CSound.Play(_CurrentMusicStream);
                        if (_VideoEnabled && _Video != -1)
                            CVideo.VdResume(_Video);
                        _Playing = true;
                    }
                    else
                        Next();

                    if (!IsBackgroundFile(_CurrentPlaylistElement))
                        _CanSing = true;
                    else
                        _CanSing = false;
                }
            }
        }

        public static void Stop()
        {
            if (!_Playing)
                return;

            if (_VideoEnabled && _Video != -1)
            {
                CVideo.VdClose(_Video);
                CDraw.RemoveTexture(ref _CurrentVideoTexture);
                _Video = -1;
            }
            CSound.FadeAndStop(_CurrentMusicStream, 0f, CSettings.BackgroundMusicFadeTime);
            _CurrentMusicStream = -1;

            _CurrentPlaylistElement = new PlaylistElement();
            _Playing = false;
        }

        public static void Pause()
        {
            if (!_Playing)
                return;

            if (_VideoEnabled && _Video != -1)
            {
                CVideo.VdPause(_Video);
                CVideo.VdSkip(_Video, CSound.GetPosition(_CurrentMusicStream) + CSettings.BackgroundMusicFadeTime, _CurrentPlaylistElement.VideoGap);
            }
            CSound.FadeAndPause(_CurrentMusicStream, 0f, CSettings.BackgroundMusicFadeTime);
            _Playing = false;
        }

        public static void Update()
        {
            if (_AllFileNames.Count > 0 && _CurrentMusicStream != -1)
            {
                float timeToPlay;
                if(_CurrentPlaylistElement.Finish == 0f) //No End-Tag defined
                    timeToPlay = CSound.GetLength(_CurrentMusicStream) - CSound.GetPosition(_CurrentMusicStream);
                else //End-Tag found
                    timeToPlay = _CurrentPlaylistElement.Finish - CSound.GetPosition(_CurrentMusicStream);

                bool finished = CSound.IsFinished(_CurrentMusicStream);
                if (_Playing && (timeToPlay <= CSettings.BackgroundMusicFadeTime || finished))
                {
                    if (_RepeatSong)
                    {
                        CSound.SetPosition(_CurrentMusicStream, 0);
                        if (_VideoEnabled && _Video != -1)
                            CVideo.VdSkip(_Video, 0f, _CurrentPlaylistElement.VideoGap);
                    }
                    else
                        Next();
                }
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
                _CurrentMusicStream = CSound.Load(_CurrentPlaylistElement.MusicFilePath);
                CSound.SetStreamVolumeMax(_CurrentMusicStream, CConfig.BackgroundMusicVolume);

                //Seek to #Start-Tag, if found
                if (_CurrentPlaylistElement.Start != 0f)
                    CSound.SetPosition(_CurrentMusicStream, _CurrentPlaylistElement.Start);

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
            if (_PreviousFileNames.Count > 0 && _PreviousMusicIndex >= 0)
            {
                float pos = CSound.GetPosition(_CurrentMusicStream);
                if (CSound.GetPosition(_CurrentMusicStream) >= 1.5f)
                {
                    CSound.SetPosition(_CurrentMusicStream, 0);
                    if (_VideoEnabled && _Video != -1)
                        CVideo.VdSkip(_Video, 0f, _CurrentPlaylistElement.VideoGap);
                }
                else
                {
                    Stop();
                    _PreviousMusicIndex--;
                    if (_PreviousMusicIndex < 0)
                        _PreviousMusicIndex = 0; //No previous songs left, so play the first

                    _CurrentPlaylistElement = _PreviousFileNames[_PreviousMusicIndex];

                    _CurrentMusicStream = CSound.Load(_CurrentPlaylistElement.MusicFilePath);
                    CSound.SetStreamVolumeMax(_CurrentMusicStream, CConfig.BackgroundMusicVolume);
                    if (_VideoEnabled)
                        LoadVideo();
                    CSound.SetStreamVolume(_CurrentMusicStream, 0f);
                    Play();
                }
            }
            else if (_CurrentMusicStream != -1)
            {
                CSound.SetPosition(_CurrentMusicStream, 0);
                if (_VideoEnabled && _Video != -1)
                    CVideo.VdSkip(_Video, 0f, _CurrentPlaylistElement.VideoGap);
            }
        }

        public static void ApplyVolume()
        {
            CSound.SetStreamVolumeMax(_CurrentMusicStream, CConfig.BackgroundMusicVolume); 
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

            if (Playing && !IsBackgroundFile(_CurrentPlaylistElement) || _AllFileNames.Count == 0)
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

                if (Playing && IsBackgroundFile(_CurrentPlaylistElement) || _AllFileNames.Count == 0)
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

        private static bool IsBackgroundFile(PlaylistElement element)
        {
            foreach (PlaylistElement elements in _BGMusicFileNames)
            {
                if (elements.MusicFilePath == element.MusicFilePath)
                    return true;
            }
            return false;
        }

        private static void LoadVideo()
        {
            if (_Video == -1)
            {
                _Video = CVideo.VdLoad(_CurrentPlaylistElement.VideoFilePath);
                CVideo.VdSkip(_Video, 0f, _CurrentPlaylistElement.VideoGap);
                _VideoEnabled = true;
                _FadeTimer.Reset();
                _FadeTimer.Start();
            }
        }
    }
}

class PlaylistElement
{
    private CSong _Song = null;

    private int _SongID;
    public int SongID
    {
        get { return _SongID; }
    }

    private string _MusicFilePath = String.Empty;

    public string MusicFilePath
    {
        get
        {
            if (_Song != null)
                return _Song.GetMP3();
                
            return _MusicFilePath;
        }

        set
        {
            _MusicFilePath = value;
        }
    }

    public string VideoFilePath
    {
        get
        {
            if (_Song != null)
                return _Song.GetVideo();
                
            return string.Empty;
        }
    }

    public string Title
    {
        get
        {
            if (_Song != null)
                return _Song.Title;
                
            return "";
        }
    }

    public string Artist
    {
        get
        {
            if (_Song != null)
                return _Song.Artist;
                
            return "";
        }
    }

    public float Start
    {
        get
        {
            if (_Song != null)
                return _Song.Start;

            return 0f;
        }
    }

    public float Finish
    {
        get
        {
            if (_Song != null)
                return _Song.Finish;

            return 0f;
        }
    }

    public STexture Cover
    {
        get
        {
            if (_Song != null)
                return _Song.CoverTextureSmall;
                
            return CCover.NoCover;
        }
    }

    public float VideoGap
    {
        get
        {
            if (_Song != null)
                return _Song.VideoGap;
         
            return 0;
        }
    }

    public bool Duet
    {
        get
        {
            if (_Song != null)
                return _Song.IsDuet;
                
            return false;
        }
    }

    public PlaylistElement(CSong song)
    {
        MusicFilePath = string.Empty;
        _SongID = song.ID;

        _Song = song;
    }

    public PlaylistElement(string FilePath)
    {
        MusicFilePath = FilePath;
        _SongID = -1;
    }

    public PlaylistElement()
    {
        MusicFilePath = string.Empty;
        _SongID = -1;
    }
}
