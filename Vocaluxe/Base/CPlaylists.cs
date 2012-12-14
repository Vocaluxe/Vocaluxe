using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;

using Vocaluxe.Lib.Playlist;
using Vocaluxe.GameModes;
using Vocaluxe.Menu;
using Vocaluxe.Menu.SongMenu;

namespace Vocaluxe.Base
{

    static class CPlaylists
    {
        private static List<CPlaylistFile> _Playlists;
        private static CHelper Helper = new CHelper();

        public static CPlaylistFile[] Playlists
        {
            get { return _Playlists.ToArray(); }
        }

        public static string[] PlaylistNames
        {
            get 
            {
                List<string> names = new List<string>();
                for (int i = 0; i < _Playlists.Count; i++) 
                {
                    names.Add(_Playlists[i].PlaylistName);
                }
                return names.ToArray();
            }
        }

        public static int NumPlaylists
        {
            get { return _Playlists.Count; }
        }

        public static void Init()
        {
            LoadPlaylists();
        }

        public static void LoadPlaylists()
        {
            _Playlists = new List<CPlaylistFile>();
            List<string> files = new List<string>();
            files.AddRange(Helper.ListFiles(CSettings.sFolderPlaylists, "*.xml", true, true));

            foreach (string file in files)
            {
                CPlaylistFile playlist = new CPlaylistFile(file);
                _Playlists.Add(playlist);
            }

            SortPlaylistsByName();
        }

        public static string GetPlaylistName(int PlaylistID)
        {
            if (PlaylistID >= _Playlists.Count || PlaylistID < 0)
                return "Error: Can't find Playlist";

            return _Playlists[PlaylistID].PlaylistName;
        }

        public static void SetPlaylistName(int PlaylistID, string Name)
        {
            if (PlaylistID >= _Playlists.Count || PlaylistID < 0)
                return;

            _Playlists[PlaylistID].PlaylistName = Name;
        }

        public static void DeletePlaylist(int PlaylistID)
        {
            if (PlaylistID < 0 || PlaylistID >= _Playlists.Count)
                return;
            if (_Playlists[PlaylistID].PlaylistFile != String.Empty)
            {
                try
                {
                    File.Delete(_Playlists[PlaylistID].PlaylistFile);
                }
                catch (Exception)
                {
                    CLog.LogError("Can't delete Playlist File " + _Playlists[PlaylistID].PlaylistFile + ".xml");
                }
            }
            _Playlists.RemoveAt(PlaylistID);
        }

        public static void SavePlaylist(int PlaylistID)
        {
            if (PlaylistID < 0 || PlaylistID >= _Playlists.Count)
                return;
            _Playlists[PlaylistID].SavePlaylist();
        }

        public static int NewPlaylist()
        {
            CPlaylistFile pl = new CPlaylistFile();
            pl.PlaylistName = "New Playlist";
            _Playlists.Add(pl);
            return (_Playlists.Count - 1);
        }



        public static void AddPlaylistSong(int PlaylistID, int SongID)
        {
            if (PlaylistID >= _Playlists.Count || PlaylistID < 0)
                return;

            _Playlists[PlaylistID].AddSong(SongID);
        }

        public static void AddPlaylistSong(int PlaylistID, int SongID, EGameMode GameMode)
        {
            if (PlaylistID >= _Playlists.Count || PlaylistID < 0)
                return;

            _Playlists[PlaylistID].AddSong(SongID, GameMode);
        }

        public static void InsertPlaylistSong(int PlaylistID, int PositionIndex, int SongID, EGameMode GameMode)
        {
            if (PlaylistID >= _Playlists.Count || PlaylistID < 0)
                return;

            _Playlists[PlaylistID].SongInsert(PositionIndex, SongID, GameMode);
        }

        public static void MovePlaylistSong(int PlaylistID, int SourceIndex, int DestIndex)
        {
            if (PlaylistID >= _Playlists.Count || PlaylistID < 0)
                return;

            _Playlists[PlaylistID].SongMove(SourceIndex, DestIndex);
        }

        public static void MovePlaylistSongDown(int PlaylistID, int SongIndex)
        {
            if (PlaylistID >= _Playlists.Count || PlaylistID < 0)
                return;

            _Playlists[PlaylistID].SongDown(SongIndex);
        }

        public static void MovePlaylistSongUp(int PlaylistID, int SongIndex)
        {
            if (PlaylistID >= _Playlists.Count || PlaylistID < 0)
                return;

            _Playlists[PlaylistID].SongUp(SongIndex);
        }

        public static void DeletePlaylistSong(int PlaylistID, int SongIndex)
        {
            if (PlaylistID >= _Playlists.Count || PlaylistID < 0)
                return;

            _Playlists[PlaylistID].DeleteSong(SongIndex);
        }

        public static int GetPlaylistSongCount(int PlaylistID)
        {
            if (PlaylistID >= _Playlists.Count || PlaylistID < 0)
                return 0;

            return _Playlists[PlaylistID].Songs.Count;
        }

        public static CPlaylistSong GetPlaylistSong(int PlaylistID, int SongIndex)
        {
            if (PlaylistID >= _Playlists.Count || PlaylistID < 0)
                return null;

            if (SongIndex >= _Playlists[PlaylistID].Songs.Count)
                return null;

            return _Playlists[PlaylistID].Songs[SongIndex];
        }

        #region private methods
        private static void SortPlaylistsByName()
        {
            _Playlists.Sort(CompareByPlaylistName);
        }

        private static int CompareByPlaylistName(CPlaylistFile a, CPlaylistFile b)
        {
            return String.Compare(a.PlaylistName, b.PlaylistName);
        }
        #endregion
    }
}
