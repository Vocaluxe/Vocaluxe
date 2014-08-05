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
using VocaluxeLib.Utils.Player;

namespace Vocaluxe.Base
{
    static class CBackgroundMusic
    {
        private static bool _Initialized;

        private static CSongPlayer _SongPlayer;

        private static bool _OwnSongsAvailable;
        private static EBackgroundMusicSource _MusicSource;
        private static bool _VideoEnabled;

        private static CPlaylistElement _CurrentPlaylistElement; //Currently played music
        private static int _PreviousMusicIndex = -1;

        private static readonly List<CPlaylistElement> _BGMusicFiles = new List<CPlaylistElement>(); //Background music files
        private static readonly List<CPlaylistElement> _NotPlayedFiles = new List<CPlaylistElement>(); //Not played files
        private static readonly List<CPlaylistElement> _PreviousFiles = new List<CPlaylistElement>(); //Played files

        private static bool _OwnMusicAdded;
        private static bool _BackgroundMusicAdded;
        private static bool _Disabled;

        public static bool VideoEnabled
        {
            get { return _SongPlayer.VideoEnabled; }
            set { _SongPlayer.VideoEnabled = value; }
        }

        public static bool CanSing { get { return _BGMusicFiles.Contains(_CurrentPlaylistElement) ? false : _SongPlayer.CanSing; } }

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

        public static bool RepeatSong {
            get { return _SongPlayer.RepeatSong; }
            set { _SongPlayer.RepeatSong = value; }
        }

        public static int SongID
        {
            get { return _SongPlayer.SongID; }
        }

        public static bool SongHasVideo
        {
            get { return _SongPlayer.SongHasVideo; }
        }

        public static bool IsPlaying { get { return _SongPlayer.IsPlaying; } }

        public static string ArtistAndTitle
        {
            get { 
                return  _BGMusicFiles.Contains(_CurrentPlaylistElement) ? Path.GetFileNameWithoutExtension(_CurrentPlaylistElement.MusicFilePath) :_SongPlayer.ArtistAndTitle;
            }
        }

        public static CTexture Cover
        {
            get { return _SongPlayer.Cover; }
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
        ///     Initializes the background music with values from config
        ///     Has to be called before any other method is used
        /// </summary>
        public static void Init()
        {
            if (_Initialized)
                return;

            List<string> soundFiles = CHelper.ListSoundFiles(CSettings.FolderBackgroundMusic, true, true);

            foreach (string path in soundFiles)
                _BGMusicFiles.Add(new CPlaylistElement(path));
            //Set a default to have a consistent starting point, use SetMusicSource afterwards
            _MusicSource = EBackgroundMusicSource.TR_CONFIG_NO_OWN_MUSIC;
            _AddBackgroundMusic();

            _SongPlayer = new CSongPlayer();
            _SongPlayer.Volume = CConfig.BackgroundMusicVolume;

            _VideoEnabled = (CConfig.VideoBackgrounds == EOffOn.TR_CONFIG_ON && CConfig.VideosToBackground == EOffOn.TR_CONFIG_ON);
            SetMusicSource(CConfig.BackgroundMusicSource);
            _Initialized = true;
        }

        public static void Close()
        {
            _SongPlayer.Close();

            _BGMusicFiles.Clear();
            _NotPlayedFiles.Clear();
            _PreviousFiles.Clear();

            _Initialized = false;
        }

        public static void Play()
        {
            if (IsPlaying || CConfig.BackgroundMusic == EOffOn.TR_CONFIG_OFF)
                return;

            if (_SongPlayer.SongLoaded)
            {
                //Resume
                _SongPlayer.TogglePause();
            }
            else
                Next();
        }

        public static void Stop()
        {
            if (!IsPlaying)
                return;

            _SongPlayer.Stop();

            _CurrentPlaylistElement = null;
        }

        public static void Pause()
        {
            if (!IsPlaying)
                return;

            _SongPlayer.TogglePause();
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
        }

        public static void Previous()
        {
            if (_PreviousMusicIndex < 0)
                return;
            Debug.Assert(_CurrentPlaylistElement != null);
            if (_SongPlayer.Position <= 1.5f && _PreviousMusicIndex > 0)
            {
                Stop(); //stop last song
                _PreviousMusicIndex--;

                _CurrentPlaylistElement = _PreviousFiles[_PreviousMusicIndex];
            }
            _StartSong();
        }

        public static void Update()
        {
            if (!IsPlaying && !_SongPlayer.SongLoaded)
                return;

            _SongPlayer.Update();
            if (_SongPlayer.IsFinished)
                Next();
        }

        public static CTexture GetVideoTexture()
        {
            return _SongPlayer.GetVideoTexture();
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

        /// <summary>
        ///     (Re-)Starts the _CurrentPlaylistElement
        /// </summary>
        private static void _StartSong()
        {
            Debug.Assert(_CurrentPlaylistElement != null);

            //If current song same as loaded restart only
            if (_CurrentPlaylistElement.SongID == _SongPlayer.SongID)
                _SongPlayer.Play();

            //otherwhise load
            //Seek to #Start-Tag, if found
            float start = 0f;
            if (_CurrentPlaylistElement.Start > 0.001 && CConfig.BackgroundMusicUseStart == EOffOn.TR_CONFIG_ON)
                start = _CurrentPlaylistElement.Start;
            _SongPlayer.Load(CBase.Songs.GetSongByID(_CurrentPlaylistElement.SongID), start, true);
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

    }
}