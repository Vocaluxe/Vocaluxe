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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using VocaluxeLib;
using VocaluxeLib.Songs;
using VocaluxeLib.Draw;

namespace Vocaluxe.Base
{
    static class CBackgroundMusic
    {
        private static bool _Initialized;

        private static bool _OwnSongsAvailable;
        private static EBackgroundMusicSource _MusicSource;
        private static float _Volume;
        private static bool _VideoEnabled;

        private static CPlaylistElement _CurrentPlaylistElement; //Currently played music
        private static int _CurrentMusicStream = -1; //Current active music stream
        private static int _PreviousMusicIndex; //Index of _CurrentPlaylistElement in _PreviousFiles (kept for performance)

        private static readonly List<CPlaylistElement> _BGMusicFiles = new List<CPlaylistElement>(); //Background music files
        private static readonly List<CPlaylistElement> _NotPlayedFiles = new List<CPlaylistElement>(); //Not played files
        private static readonly List<CPlaylistElement> _PreviousFiles = new List<CPlaylistElement>(); //Played files

        private static int _Video = -1;
        private static CTexture _CurrentVideoTexture;
        private static CFading _VideoFading;

        private static bool _OwnMusicAdded;
        private static bool _BackgroundMusicAdded;
        private static bool _Disabled;

        public static bool VideoEnabled
        {
            get { return _VideoEnabled; }
            set
            {
                if (_VideoEnabled == value)
                    return;
                _VideoEnabled = value;
                if (_VideoEnabled)
                    _LoadVideo();
                else if (_Video != -1)
                {
                    CVideo.Close(_Video);
                    CDraw.RemoveTexture(ref _CurrentVideoTexture);
                    _Video = -1;
                }
            }
        }

        public static bool CanSing { get; private set; }

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

        public static bool RepeatSong { get; set; }

        public static int SongID
        {
            get { return _CurrentPlaylistElement == null ? -1 : _CurrentPlaylistElement.SongID; }
        }

        public static bool SongHasVideo
        {
            get { return _CurrentPlaylistElement != null && File.Exists(_CurrentPlaylistElement.VideoFilePath); }
        }

        public static bool IsPlaying { get; private set; }

        public static string ArtistAndTitle
        {
            get
            {
                if (_CurrentPlaylistElement == null)
                    return "";
                if (_CurrentPlaylistElement.Artist != "" && _CurrentPlaylistElement.Title != "")
                    return _CurrentPlaylistElement.Artist + " - " + _CurrentPlaylistElement.Title;
                return Path.GetFileNameWithoutExtension(_CurrentPlaylistElement.MusicFilePath);
            }
        }

        public static CTexture Cover
        {
            get { return _CurrentPlaylistElement == null ? CCover.NoCover : _CurrentPlaylistElement.Cover; }
        }

        //Use this to set whether own songs are available for access
        public static bool OwnSongsAvailable
        {
            get { return _OwnSongsAvailable; }
            set
            {
                _OwnSongsAvailable = value;
                if (_OwnSongsAvailable && _MusicSource != EBackgroundMusicSource.TR_CONFIG_NO_OWN_MUSIC)
                    _AddOwnMusic();
            }
        }

        /// <summary>
        /// Initializes the background music with values from config
        /// Has to be called before any other method is used
        /// </summary>
        public static void Init()
        {
            if (_Initialized)
                return;

            _Volume = CConfig.BackgroundMusicVolume;
            _VideoEnabled = (CConfig.VideoBackgrounds == EOffOn.TR_CONFIG_ON && CConfig.VideosToBackground == EOffOn.TR_CONFIG_ON);
            SetMusicSource(CConfig.BackgroundMusicSource);

            List<string> soundFiles = CHelper.ListSoundFiles(CSettings.FolderBackgroundMusic, true, true);

            foreach (string path in soundFiles)
                _BGMusicFiles.Add(new CPlaylistElement(path));

            IsPlaying = false;
            _Initialized = true;
        }

        public static void Play()
        {
            if (IsPlaying || CConfig.BackgroundMusic == EOffOn.TR_CONFIG_OFF)
                return;

            if (_CurrentMusicStream != -1)
            {
                //Resume
                CSound.SetStreamVolume(_CurrentMusicStream, 0f);
                CSound.Fade(_CurrentMusicStream, _Volume, CSettings.BackgroundMusicFadeTime);
                CSound.Play(_CurrentMusicStream);
                if (_VideoEnabled && _Video != -1)
                    CVideo.Resume(_Video);
                IsPlaying = true;
            }
            else
                Next();
        }

        public static void Stop()
        {
            if (!IsPlaying)
                return;

            if (_Video != -1)
            {
                CVideo.Close(_Video);
                CDraw.RemoveTexture(ref _CurrentVideoTexture);
                _Video = -1;
            }
            CSound.FadeAndClose(_CurrentMusicStream, 0f, CSettings.BackgroundMusicFadeTime);
            _CurrentMusicStream = -1;

            _CurrentPlaylistElement = null;
            IsPlaying = false;
        }

        public static void Pause()
        {
            if (!IsPlaying)
                return;

            if (_Video != -1)
            {
                CVideo.Pause(_Video);
                CVideo.Skip(_Video, CSound.GetPosition(_CurrentMusicStream) + CSettings.BackgroundMusicFadeTime, _CurrentPlaylistElement.VideoGap);
            }
            CSound.FadeAndPause(_CurrentMusicStream, 0f, CSettings.BackgroundMusicFadeTime);
            IsPlaying = false;
        }

        public static void Next()
        {
            Stop(); //stop last song if any
            if (_PreviousMusicIndex < _PreviousFiles.Count - 2)
            {
                //We are in the previous list and next element exists
                _PreviousMusicIndex++;
                _CurrentPlaylistElement = _PreviousFiles[_PreviousMusicIndex];
            }
            else
            {
                //We are not in the previous list (anymore)
                if (_NotPlayedFiles.Count == 0)
                {
                    if (_PreviousFiles.Count == 0)
                        return; //No songs to play
                    _NotPlayedFiles.AddRange(_PreviousFiles);
                }

                _CurrentPlaylistElement = _NotPlayedFiles[CGame.Rand.Next(_NotPlayedFiles.Count)];
                _NotPlayedFiles.Remove(_CurrentPlaylistElement);

                _PreviousFiles.Add(_CurrentPlaylistElement);
                _PreviousMusicIndex = _PreviousFiles.Count - 1;
            }
            _StartSong();
            CanSing = !_IsBackgroundFile(_CurrentPlaylistElement);
        }

        public static void Previous()
        {
            if (_PreviousMusicIndex < 0)
                return;
            Debug.Assert(_CurrentMusicStream != -1 && _CurrentPlaylistElement != null);
            if (CSound.GetPosition(_CurrentMusicStream) <= 1.5f && _PreviousMusicIndex > 0)
            {
                Stop(); //stop last song
                _PreviousMusicIndex--;

                _CurrentPlaylistElement = _PreviousFiles[_PreviousMusicIndex];
                CanSing = !_IsBackgroundFile(_CurrentPlaylistElement);
            }
            _StartSong();
        }

        public static void Update()
        {
            if (!IsPlaying)
                return;
            Debug.Assert(_CurrentMusicStream != -1 && _CurrentPlaylistElement != null);

            float timeToPlay;
            if (Math.Abs(_CurrentPlaylistElement.Finish) < 0.001) //No End-Tag defined
                timeToPlay = CSound.GetLength(_CurrentMusicStream) - CSound.GetPosition(_CurrentMusicStream);
            else //End-Tag found
                timeToPlay = _CurrentPlaylistElement.Finish - CSound.GetPosition(_CurrentMusicStream);

            bool finished = CSound.IsFinished(_CurrentMusicStream);
            if (timeToPlay <= CSettings.BackgroundMusicFadeTime || finished)
            {
                if (RepeatSong)
                    _StartSong();
                else
                    Next();
            }
        }

        public static CTexture GetVideoTexture()
        {
            if (_Video == -1)
                return null;
            float vtime;
            if (CVideo.GetFrame(_Video, ref _CurrentVideoTexture, CSound.GetPosition(_CurrentMusicStream), out vtime))
            {
                if (_VideoFading != null)
                {
                    bool finished;
                    _CurrentVideoTexture.Color.A = _VideoFading.GetValue(out finished);
                    if (finished)
                        _VideoFading = null;
                }
                return _CurrentVideoTexture;
            }
            return null;
        }

        public static void SetMusicSource(EBackgroundMusicSource source)
        {
            if (_MusicSource == source)
                return;
            _MusicSource = source;
            switch (_MusicSource)
            {
                case EBackgroundMusicSource.TR_CONFIG_NO_OWN_MUSIC:
                    _AddBackgroundMusic();
                    _RemoveOwnMusic();
                    break;

                case EBackgroundMusicSource.TR_CONFIG_ONLY_OWN_MUSIC:
                    _AddOwnMusic();
                    _RemoveBackgroundMusic();
                    break;
                case EBackgroundMusicSource.TR_CONFIG_OWN_MUSIC:
                    _AddBackgroundMusic();
                    _AddOwnMusic();
                    break;
            }
        }

        public static void SetVolume(float volume)
        {
            _Volume = volume;
            if (_CurrentMusicStream >= 0)
                CSound.SetStreamVolume(_CurrentMusicStream, _Volume);
        }

        /// <summary>
        /// (Re-)Starts the _CurrentPlaylistElement
        /// </summary>
        private static void _StartSong()
        {
            Debug.Assert(_CurrentPlaylistElement != null);
            //If we have an active stream, reuse it
            if (_CurrentMusicStream < 0)
            {
                //otherwhise load
                _CurrentMusicStream = CSound.Load(_CurrentPlaylistElement.MusicFilePath);
                if (_CurrentMusicStream < 0)
                    return;
                CSound.SetStreamVolume(_CurrentMusicStream, 0f);
                CSound.Fade(_CurrentMusicStream, _Volume, CSettings.BackgroundMusicFadeTime);
            }
            //Seek to #Start-Tag, if found
            if (_CurrentPlaylistElement.Start > 0.001 && CConfig.BackgroundMusicUseStart == EOffOn.TR_CONFIG_ON)
                CSound.SetPosition(_CurrentMusicStream, _CurrentPlaylistElement.Start);

            if (_VideoEnabled)
            {
                if (_Video != -1)
                {
                    if (_CurrentPlaylistElement.Start > 0.001 && CConfig.BackgroundMusicUseStart == EOffOn.TR_CONFIG_ON)
                        CVideo.Skip(_Video, 0f, _CurrentPlaylistElement.VideoGap + _CurrentPlaylistElement.Start);
                    else
                        CVideo.Skip(_Video, 0f, _CurrentPlaylistElement.VideoGap);
                }
                else
                    _LoadVideo();
            }
        }

        private static void _AddOwnMusic()
        {
            if (_OwnMusicAdded || !_OwnSongsAvailable)
                return;
            foreach (CSong song in CSongs.AllSongs)
                _NotPlayedFiles.Add(new CPlaylistElement(song));
            _OwnMusicAdded = true;
        }

        private static void _RemoveOwnMusic()
        {
            if (!_OwnMusicAdded)
                return;
            _NotPlayedFiles.RemoveAll(el => el.SongID >= 0);
            _PreviousFiles.RemoveAll(el => el.SongID >= 0);
            _PreviousMusicIndex = _PreviousFiles.IndexOf(_CurrentPlaylistElement);

            if (IsPlaying && !_IsBackgroundFile(_CurrentPlaylistElement))
                Next();

            _OwnMusicAdded = false;
        }

        private static void _AddBackgroundMusic()
        {
            if (_BackgroundMusicAdded)
                return;
            _NotPlayedFiles.AddRange(_BGMusicFiles);
            _BackgroundMusicAdded = true;
        }

        private static void _RemoveBackgroundMusic()
        {
            if (!_BackgroundMusicAdded)
                return;
            _NotPlayedFiles.RemoveAll(el => el.SongID < 0);
            _PreviousFiles.RemoveAll(el => el.SongID < 0);
            _PreviousMusicIndex = _PreviousFiles.IndexOf(_CurrentPlaylistElement);

            if (IsPlaying && _IsBackgroundFile(_CurrentPlaylistElement))
                Next();

            _BackgroundMusicAdded = false;
        }

        private static bool _IsBackgroundFile(CPlaylistElement element)
        {
            return _BGMusicFiles.Contains(element);
        }

        private static void _LoadVideo()
        {
            if (_Video != -1 || String.IsNullOrEmpty(_CurrentPlaylistElement.VideoFilePath))
                return;
            _Video = CVideo.Load(_CurrentPlaylistElement.VideoFilePath);
            if (CConfig.BackgroundMusicUseStart == EOffOn.TR_CONFIG_ON)
                CVideo.Skip(_Video, 0f, _CurrentPlaylistElement.VideoGap + _CurrentPlaylistElement.Start); //Use gap, otherwhise we will overwrite position in GetVideoTexture
            else
                CVideo.Skip(_Video, 0f, _CurrentPlaylistElement.VideoGap);
            _VideoFading = new CFading(0f, 1f, 3f);
        }
    }
}