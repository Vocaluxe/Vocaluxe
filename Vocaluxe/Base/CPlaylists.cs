using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;

using Vocaluxe.Lib.Song;
using Vocaluxe.Lib.Playlist;

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

        #region private methods

        private static void SortPlaylistsByName()
        {
            _Playlists.Sort(CompareByPlaylistName);
        }

        private static int CompareByPlaylistName(CPlaylistFile a, CPlaylistFile b)
        {
            return String.Compare(a.PlaylistName, b.PlaylistName);
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
                    CLog.LogError("Can't delete Profile File " + _Playlists[PlaylistID].PlaylistFile + ".xml");
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

        #endregion
    }
}
