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
        private static List<CPlaylist> _Playlists;
        private static CHelper Helper = new CHelper();

        public static CPlaylist[] Playlists
        {
            get { return _Playlists.ToArray(); }
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
            _Playlists = new List<CPlaylist>();
            List<string> files = new List<string>();
            files.AddRange(Helper.ListFiles(CSettings.sFolderPlaylists, "*.xml", true, true));

            foreach (string file in files)
            {
                CPlaylist playlist = new CPlaylist(file);
            }

            SortPlaylistsByName();
        }

        #region private methods

        private static void SortPlaylistsByName()
        {
            _Playlists.Sort(CompareByPlaylistName);
        }

        private static int CompareByPlaylistName(CPlaylist a, CPlaylist b)
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
                    _Playlists.RemoveAt(PlaylistID);
                }
                catch (Exception)
                {
                    CLog.LogError("Can't delete Profile File " + _Playlists[PlaylistID].PlaylistFile + ".xml");
                }
            }
        }

        #endregion
    }
}
