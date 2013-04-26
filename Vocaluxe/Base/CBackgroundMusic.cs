#region license
// /*
//     This file is part of Vocaluxe.
// 
//     Vocaluxe is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     Vocaluxe is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
//  */
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Vocaluxe.Base;
using VocaluxeLib.Menu;
using VocaluxeLib.Menu.SongMenu;

namespace Vocaluxe.Base
{
    static class CBackgroundMusic
    {
        private static int _CurrentMusicStream = -1;
        private static int _PreviousMusicIndex;
        private static CPlaylistElement _CurrentPlaylistElement = new CPlaylistElement();

        private static readonly List<CPlaylistElement> _AllFileNames = new List<CPlaylistElement>();
        private static readonly List<CPlaylistElement> _NotPlayedFileNames = new List<CPlaylistElement>();
        private static readonly List<CPlaylistElement> _BGMusicFileNames = new List<CPlaylistElement>();
        private static readonly List<CPlaylistElement> _PreviousFileNames = new List<CPlaylistElement>();

        private static int _Video = -1;
        private static STexture _CurrentVideoTexture = new STexture(-1);
        private static readonly Stopwatch _FadeTimer = new Stopwatch();
        private static bool _VideoEnabled;

        private static bool _OwnMusicAdded;
        private static bool _BackgroundMusicAdded;
        private static bool _Disabled;
        private static bool _CanSing;
        private static bool _RepeatSong;

        public static bool VideoEnabled
        {
            get { return _VideoEnabled; }
            set
            {
                if (_VideoEnabled && value)
                    return;
                if (!_VideoEnabled && !value)
                    return;
                if (_VideoEnabled && !value)
                    ToggleVideo();
                if (!_VideoEnabled && value)
                    ToggleVideo();
                _VideoEnabled = value;
            }
        }

        public static bool CanSing
        {
            get { return _CanSing; }
            set { _CanSing = value; }
        }

        public static bool Disabled
        {
            get { return _Disabled; }
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
            get { return _RepeatSong; }
            set { _RepeatSong = value; }
        }

        public static int SongID
        {
            get { return _CurrentPlaylistElement.SongID; }
        }

        public static bool Duet
        {
            get { return _CurrentPlaylistElement.Duet; }
        }

        public static bool SongHasVideo
        {
            get { return File.Exists(_CurrentPlaylistElement.VideoFilePath); }
        }

        public static bool IsPlaying { get; private set; }

        public static string ArtistAndTitle
        {
            get
            {
                if (_CurrentPlaylistElement.Artist != "" && _CurrentPlaylistElement.Title != "")
                    return _CurrentPlaylistElement.Artist + " - " + _CurrentPlaylistElement.Title;
                return Path.GetFileNameWithoutExtension(_CurrentPlaylistElement.MusicFilePath);
            }
        }

        public static STexture Cover
        {
            get { return _CurrentPlaylistElement.Cover; }
        }

        public static void Init()
        {
            List<string> templist = new List<string>();

            foreach (string ending in CSettings.MusicFileTypes)
                templist.AddRange(CHelper.ListFiles(CSettings.FolderBackgroundMusic, ending, true, true));

            foreach (string path in templist)
                _BGMusicFileNames.Add(new CPlaylistElement(path));

            if (CConfig.BackgroundMusicSource != EBackgroundMusicSource.TR_CONFIG_ONLY_OWN_MUSIC)
                AddBackgroundMusic();
            if (CConfig.VideoBackgrounds == EOffOn.TR_CONFIG_ON)
                _VideoEnabled = true;

            IsPlaying = false;
        }

        public static void Play()
        {
            if (IsPlaying)
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
                        IsPlaying = true;
                    }
                    else
                        Next();

                    _CanSing = !_IsBackgroundFile(_CurrentPlaylistElement);
                }
            }
        }

        public static void Stop()
        {
            if (!IsPlaying)
                return;

            if (_VideoEnabled && _Video != -1)
            {
                CVideo.VdClose(_Video);
                CDraw.RemoveTexture(ref _CurrentVideoTexture);
                _Video = -1;
            }
            CSound.FadeAndStop(_CurrentMusicStream, 0f, CSettings.BackgroundMusicFadeTime);
            _CurrentMusicStream = -1;

            _CurrentPlaylistElement = new CPlaylistElement();
            IsPlaying = false;
        }

        public static void Pause()
        {
            if (!IsPlaying)
                return;

            if (_VideoEnabled && _Video != -1)
            {
                CVideo.VdPause(_Video);
                CVideo.VdSkip(_Video, CSound.GetPosition(_CurrentMusicStream) + CSettings.BackgroundMusicFadeTime, _CurrentPlaylistElement.VideoGap);
            }
            CSound.FadeAndPause(_CurrentMusicStream, 0f, CSettings.BackgroundMusicFadeTime);
            IsPlaying = false;
        }

        public static void Update()
        {
            if (_AllFileNames.Count > 0 && _CurrentMusicStream != -1)
            {
                float timeToPlay;
                if (Math.Abs(_CurrentPlaylistElement.Finish) < 0.001) //No End-Tag defined
                    timeToPlay = CSound.GetLength(_CurrentMusicStream) - CSound.GetPosition(_CurrentMusicStream);
                else //End-Tag found
                    timeToPlay = _CurrentPlaylistElement.Finish - CSound.GetPosition(_CurrentMusicStream);

                bool finished = CSound.IsFinished(_CurrentMusicStream);
                if (IsPlaying && (timeToPlay <= CSettings.BackgroundMusicFadeTime || finished))
                {
                    if (_RepeatSong)
                    {
                        //Seek to #Start-Tag, if found
                        if (_CurrentPlaylistElement.Start > 0.001 && CConfig.BackgroundMusicUseStart == EOffOn.TR_CONFIG_ON)
                            CSound.SetPosition(_CurrentMusicStream, _CurrentPlaylistElement.Start);
                        else
                            CSound.SetPosition(_CurrentMusicStream, 0);
                        if (_VideoEnabled && _Video != -1)
                        {
                            if (_CurrentPlaylistElement.Start > 0.001 && CConfig.BackgroundMusicUseStart == EOffOn.TR_CONFIG_ON)
                                CVideo.VdSkip(_Video, _CurrentPlaylistElement.Start, _CurrentPlaylistElement.VideoGap);
                            else
                                CVideo.VdSkip(_Video, 0f, _CurrentPlaylistElement.VideoGap);
                        }
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
                if (_PreviousMusicIndex == _PreviousFileNames.Count - 1 || _PreviousFileNames.Count == 0)
                {
                    //We are not currently in the previous list
                    Stop();
                    if (_NotPlayedFileNames.Count == 0)
                        _NotPlayedFileNames.AddRange(_AllFileNames);

                    _CurrentPlaylistElement = _NotPlayedFileNames[CGame.Rand.Next(_NotPlayedFileNames.Count)];
                    _NotPlayedFileNames.Remove(_CurrentPlaylistElement);

                    _PreviousFileNames.Add(_CurrentPlaylistElement);
                    _PreviousMusicIndex = _PreviousFileNames.Count - 1;
                }
                else if (_PreviousFileNames.Count > 0)
                {
                    //We are in the previous list
                    Stop();
                    _PreviousMusicIndex++;

                    _CurrentPlaylistElement = _PreviousFileNames[_PreviousMusicIndex];
                }
                _CurrentMusicStream = CSound.Load(_CurrentPlaylistElement.MusicFilePath);
                CSound.SetStreamVolumeMax(_CurrentMusicStream, CConfig.BackgroundMusicVolume);

                //Seek to #Start-Tag, if found
                if (_CurrentPlaylistElement.Start > 0.001 && CConfig.BackgroundMusicUseStart == EOffOn.TR_CONFIG_ON)
                    CSound.SetPosition(_CurrentMusicStream, _CurrentPlaylistElement.Start);

                if (_VideoEnabled)
                    _LoadVideo();
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
                if (CSound.GetPosition(_CurrentMusicStream) >= 1.5f)
                {
                    //Seek to #Start-Tag, if found
                    if (_CurrentPlaylistElement.Start > 0.001 && CConfig.BackgroundMusicUseStart == EOffOn.TR_CONFIG_ON)
                        CSound.SetPosition(_CurrentMusicStream, _CurrentPlaylistElement.Start);
                    else
                        CSound.SetPosition(_CurrentMusicStream, 0);
                    if (_VideoEnabled && _Video != -1)
                    {
                        if (_CurrentPlaylistElement.Start > 0.001 && CConfig.BackgroundMusicUseStart == EOffOn.TR_CONFIG_ON)
                            CVideo.VdSkip(_Video, _CurrentPlaylistElement.Start, _CurrentPlaylistElement.VideoGap);
                        else
                            CVideo.VdSkip(_Video, 0f, _CurrentPlaylistElement.VideoGap);
                    }
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
                        _LoadVideo();
                    CSound.SetStreamVolume(_CurrentMusicStream, 0f);
                    Play();
                }
            }
            else if (_CurrentMusicStream != -1)
            {
                //Seek to #Start-Tag, if found
                if (_CurrentPlaylistElement.Start > 0.001 && CConfig.BackgroundMusicUseStart == EOffOn.TR_CONFIG_ON)
                    CSound.SetPosition(_CurrentMusicStream, _CurrentPlaylistElement.Start);
                else
                    CSound.SetPosition(_CurrentMusicStream, 0);
                if (_VideoEnabled && _Video != -1)
                {
                    if (_CurrentPlaylistElement.Start > 0.001 && CConfig.BackgroundMusicUseStart == EOffOn.TR_CONFIG_ON)
                        CVideo.VdSkip(_Video, _CurrentPlaylistElement.Start, _CurrentPlaylistElement.VideoGap);
                    else
                        CVideo.VdSkip(_Video, 0f, _CurrentPlaylistElement.VideoGap);
                }
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
                    _AllFileNames.Add(new CPlaylistElement(song));
                    _NotPlayedFileNames.Add(new CPlaylistElement(song));
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

            if (IsPlaying && !_IsBackgroundFile(_CurrentPlaylistElement) || _AllFileNames.Count == 0)
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

                if (IsPlaying && _IsBackgroundFile(_CurrentPlaylistElement) || _AllFileNames.Count == 0)
                    Next();
            }
        }

        public static void CheckAndApplyConfig(EOffOn newOnOff, EBackgroundMusicSource newSource, float newVolume)
        {
            if (CConfig.BackgroundMusicSource != newSource || (newOnOff == EOffOn.TR_CONFIG_ON && !_OwnMusicAdded && !_BackgroundMusicAdded))
            {
                CConfig.BackgroundMusicSource = newSource;

                switch (newSource)
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

            if (CConfig.BackgroundMusic != newOnOff)
            {
                CConfig.BackgroundMusic = newOnOff;
                if (newOnOff == EOffOn.TR_CONFIG_ON)
                    Play();
                else
                    Pause();
            }

            if (Math.Abs(CConfig.BackgroundMusicVolume - newVolume) > 0.01)
            {
                CConfig.BackgroundMusicVolume = (int)newVolume;
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
                }
            }
            else
                _LoadVideo();
        }

        public static STexture GetVideoTexture()
        {
            if (_Video != -1)
            {
                float vtime;
                CVideo.VdGetFrame(_Video, ref _CurrentVideoTexture, CSound.GetPosition(_CurrentMusicStream), out vtime);
                if (_FadeTimer.ElapsedMilliseconds <= 3000L)
                    _CurrentVideoTexture.Color.A = _FadeTimer.ElapsedMilliseconds / 3000f;
                else
                {
                    _CurrentVideoTexture.Color.A = 1f;
                    _FadeTimer.Stop();
                }
                return _CurrentVideoTexture;
            }
            return new STexture(-1);
        }

        private static bool _IsBackgroundFile(CPlaylistElement element)
        {
            return _BGMusicFileNames.Any(elements => elements.MusicFilePath == element.MusicFilePath);
        }

        private static void _LoadVideo()
        {
            if (_Video == -1)
            {
                _Video = CVideo.VdLoad(_CurrentPlaylistElement.VideoFilePath);
                if (_CurrentPlaylistElement.Start > 0.001 && CConfig.BackgroundMusicUseStart == EOffOn.TR_CONFIG_ON)
                    CVideo.VdSkip(_Video, _CurrentPlaylistElement.Start, _CurrentPlaylistElement.VideoGap);
                else
                    CVideo.VdSkip(_Video, 0f, _CurrentPlaylistElement.VideoGap);
                _VideoEnabled = true;
                _FadeTimer.Reset();
                _FadeTimer.Start();
            }
        }
    }
}

class CPlaylistElement
{
    private readonly CSong _Song;

    private readonly int _SongID;
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

        set { _MusicFilePath = value; }
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

    public CPlaylistElement(CSong song)
    {
        MusicFilePath = string.Empty;
        _SongID = song.ID;

        _Song = song;
    }

    public CPlaylistElement(string filePath)
    {
        MusicFilePath = filePath;
        _SongID = -1;
    }

    public CPlaylistElement()
    {
        MusicFilePath = string.Empty;
        _SongID = -1;
    }
}