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
using VocaluxeLib;
using VocaluxeLib.Songs;
using VocaluxeLib.Draw;
using VocaluxeLib.Utils.Player;

namespace Vocaluxe.Base
{
    static class CBackgroundMusic
    {
        private static bool _Initialized;

        private static readonly CSongPlayer _BGPlayer = new CSongPlayer();
        private static readonly CSongPlayer _PreviewPlayer = new CSongPlayer(true);
        private static CSongPlayer _CurPlayer;

        private static bool _OwnSongsAvailable;
        private static EBackgroundMusicSource _MusicSource;

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
            get { return _CurPlayer.VideoEnabled; }
            set { _CurPlayer.VideoEnabled = value; }
        }

        public static bool CanSing
        {
            get
            {
                if (IsPlayingPreview)
                    return false;
                return !_BGMusicFiles.Contains(_CurrentPlaylistElement);
            }
        }

        public static bool Disabled
        {
            get { return _Disabled; }
            set
            {
                if (Disabled == value)
                    return;
                _Disabled = value;
                if (_Disabled)
                    Pause();
                else
                    Play();
            }
        }

        /// <summary>
        ///     Repeat the background song (preview songs are always repeating!)
        /// </summary>
        public static bool RepeatSong
        {
            get { return _BGPlayer.Loop; }
            set { _BGPlayer.Loop = value; }
        }

        public static int SongID
        {
            get { return _CurPlayer.SongID; }
        }

        public static bool SongHasVideo
        {
            get { return _CurPlayer.SongHasVideo; }
        }

        public static bool IsPlaying
        {
            get { return _CurPlayer.IsPlaying; }
        }

        public static bool IsPlayingPreview
        {
            get { return _CurPlayer == _PreviewPlayer; }
            set
            {
                if (IsPlayingPreview == value)
                    return;
                Pause();
                _CurPlayer = value ? _PreviewPlayer : _BGPlayer;
                Play();
            }
        }

        public static string ArtistAndTitle
        {
            get { return _CurPlayer.ArtistAndTitle; }
        }

        public static float Length
        {
            get { return _CurPlayer.Length; }
        }

        public static CTextureRef Cover
        {
            get { return _CurPlayer.Cover; }
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

            List<string> soundFiles = CHelper.ListSoundFiles(CSettings.FolderNameBackgroundMusic, true, true);

            foreach (string path in soundFiles)
                _BGMusicFiles.Add(new CPlaylistElement(path));
            _CurPlayer = _BGPlayer;
            //Set a default to have a consistent starting point, use SetMusicSource afterwards
            _MusicSource = EBackgroundMusicSource.TR_CONFIG_NO_OWN_MUSIC;
            _AddBackgroundMusic();

            SetMusicSource(CConfig.Config.Sound.BackgroundMusicSource);
            _Initialized = true;
        }

        public static void Close()
        {
            _BGPlayer.Close();
            _PreviewPlayer.Close();

            _BGMusicFiles.Clear();
            _NotPlayedFiles.Clear();
            _PreviousFiles.Clear();

            _Initialized = false;
        }

        public static void Play()
        {
            if (!IsPlayingPreview && CConfig.Config.Sound.BackgroundMusic != EBackgroundMusicOffOn.TR_CONFIG_ON)
                return;

            if (IsPlayingPreview || _BGPlayer.SoundLoaded)
                _CurPlayer.Play();
            else
                Next();
        }

        public static void Stop()
        {
            _CurPlayer.Stop();
            if (!IsPlayingPreview)
                _CurrentPlaylistElement = null;
        }

        public static void Pause()
        {
            _CurPlayer.Pause();
        }

        public static void Next()
        {
            if (IsPlayingPreview)
                return;

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
            if (IsPlayingPreview)
                return;
            if (_PreviousMusicIndex < 0)
                return;

            if (_CurrentPlaylistElement == null || (_BGPlayer.Position <= 1.5f && _PreviousMusicIndex > 0))
            {
                Stop(); //stop last song
                _PreviousMusicIndex--;

                _CurrentPlaylistElement = _PreviousFiles[_PreviousMusicIndex];
            }
            _StartSong();
        }

        public static void Update()
        {
            if (!IsPlaying)
                return;

            _CurPlayer.Update();
            if (!IsPlayingPreview && _BGPlayer.IsFinished)
                Next();
        }

        public static CTextureRef GetVideoTexture()
        {
            return _CurPlayer.GetVideoTexture();
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

        public static void LoadPreview(CSong song, float start = -1f)
        {
            if (song == null)
                throw new ArgumentNullException("song");

            if (!IsPlayingPreview)
            {
                Pause();
                CSound.SetGlobalVolume(CConfig.PreviewMusicVolume);
            }

            bool songChanged = _CurPlayer.SongID != song.ID;

            _PreviewPlayer.Load(song);

            //Change song position only if song is changed or near to end
            if (songChanged || _CurPlayer.Position + 30 < _CurPlayer.Length)
            {
                float length = _PreviewPlayer.Length;
                if (length < 1)
                    length = 30; // If length is unknow or invalid assume a length of 30s

                if (start < 0)
                    start = (song.Preview.Source == EDataSource.None) ? length / 4f : song.Preview.StartTime;
                if (start > length - 5f)
                    start = Math.Max(0f, Math.Min(length / 4f, length - 5f));
                if (start >= 0.5f)
                    start -= 0.5f;

                _PreviewPlayer.Position = start;
            }
            else
            {
                _PreviewPlayer.Position = _CurPlayer.Position;
            }

            _CurPlayer = _PreviewPlayer;

            Play();
        }

        public static void StopPreview()
        {
            if (!IsPlayingPreview)
                return;
            Stop();
            _CurPlayer = _BGPlayer;
            if (_MusicSource != EBackgroundMusicSource.TR_CONFIG_NO_OWN_MUSIC)
            {
                _CurPlayer.Load(CSongs.GetSong(_PreviewPlayer.SongID));
                _CurPlayer.Position = _PreviewPlayer.Position;
            }
            CSound.SetGlobalVolume(CConfig.BackgroundMusicVolume);
        }

        /// <summary>
        ///     (Re-)Starts the _CurrentPlaylistElement
        /// </summary>
        private static void _StartSong()
        {
            Debug.Assert(_CurrentPlaylistElement != null);

            //If current song same as loaded restart only
            if (_BGPlayer.SoundLoaded && _CurrentPlaylistElement.MusicFilePath == _BGPlayer.FilePath)
            {
                _BGPlayer.Stop();
                _BGPlayer.Play();
                return;
            }

            //otherwhise load
            if (!_CurrentPlaylistElement.HasMetaData)
                _BGPlayer.Load(_CurrentPlaylistElement.MusicFilePath, 0f, true);
            else
            {
                //Seek to #Start-Tag, if found
                float start = 0f;
                if (_CurrentPlaylistElement.Start > 0.001 && CConfig.Config.Sound.BackgroundMusicUseStart == EOffOn.TR_CONFIG_ON)
                    start = _CurrentPlaylistElement.Start;
                _BGPlayer.Load(_CurrentPlaylistElement.Song, start, true);
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
    }
}