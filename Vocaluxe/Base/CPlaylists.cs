using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Vocaluxe.Lib.Playlist;
using VocaluxeLib.Menu;
using VocaluxeLib.Menu.SongMenu;

namespace Vocaluxe.Base
{
    static class CPlaylists
    {
        private static List<CPlaylistFile> _Playlists;

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
                    names.Add(_Playlists[i].PlaylistName);
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
            ConvertUSDXPlaylists();

            _SortPlaylistsByName();
        }

        public static void ConvertUSDXPlaylists()
        {
            List<string> files = new List<string>();
            files.AddRange(CHelper.ListFiles(CSettings.FolderPlaylists, "*.upl", true, true));

            foreach (string file in files)
            {
                CPlaylistFile playlist = _ConvertUSDXPlaylist(file);
                playlist.SavePlaylist();
                _Playlists.Add(playlist);
            }
        }

        public static void LoadPlaylists()
        {
            _Playlists = new List<CPlaylistFile>();
            List<string> files = new List<string>();
            files.AddRange(CHelper.ListFiles(CSettings.FolderPlaylists, "*.xml", true, true));

            foreach (string file in files)
            {
                CPlaylistFile playlist = new CPlaylistFile(file);
                _Playlists.Add(playlist);
            }
        }

        public static string GetPlaylistName(int playlistID)
        {
            if (playlistID >= _Playlists.Count || playlistID < 0)
                return "Error: Can't find Playlist";

            return _Playlists[playlistID].PlaylistName;
        }

        public static string[] GetPlaylistNames()
        {
            List<string> result = new List<string>();

            foreach (CPlaylistFile playlist in _Playlists)
                result.Add(playlist.PlaylistName);

            return result.ToArray();
        }

        public static void SetPlaylistName(int playlistID, string name)
        {
            if (playlistID >= _Playlists.Count || playlistID < 0)
                return;

            _Playlists[playlistID].PlaylistName = name;
        }

        public static void DeletePlaylist(int playlistID)
        {
            if (playlistID < 0 || playlistID >= _Playlists.Count)
                return;
            if (_Playlists[playlistID].PlaylistFile.Length > 0)
            {
                try
                {
                    File.Delete(_Playlists[playlistID].PlaylistFile);
                }
                catch (Exception)
                {
                    CLog.LogError("Can't delete Playlist File " + _Playlists[playlistID].PlaylistFile + ".xml");
                }
            }
            _Playlists.RemoveAt(playlistID);
        }

        public static void SavePlaylist(int playlistID)
        {
            if (playlistID < 0 || playlistID >= _Playlists.Count)
                return;
            _Playlists[playlistID].SavePlaylist();
        }

        public static int NewPlaylist()
        {
            CPlaylistFile pl = new CPlaylistFile();
            pl.PlaylistName = "New Playlist";
            _Playlists.Add(pl);
            return _Playlists.Count - 1;
        }

        public static void AddPlaylistSong(int playlistID, int songID)
        {
            if (playlistID >= _Playlists.Count || playlistID < 0)
                return;

            _Playlists[playlistID].AddSong(songID);
        }

        public static void AddPlaylistSong(int playlistID, int songID, EGameMode gameMode)
        {
            if (playlistID >= _Playlists.Count || playlistID < 0)
                return;

            _Playlists[playlistID].AddSong(songID, gameMode);
        }

        public static void InsertPlaylistSong(int playlistID, int positionIndex, int songID, EGameMode gameMode)
        {
            if (playlistID >= _Playlists.Count || playlistID < 0)
                return;

            _Playlists[playlistID].SongInsert(positionIndex, songID, gameMode);
        }

        public static void MovePlaylistSong(int playlistID, int sourceIndex, int destIndex)
        {
            if (playlistID >= _Playlists.Count || playlistID < 0)
                return;

            _Playlists[playlistID].SongMove(sourceIndex, destIndex);
        }

        public static void MovePlaylistSongDown(int playlistID, int songIndex)
        {
            if (playlistID >= _Playlists.Count || playlistID < 0)
                return;

            _Playlists[playlistID].SongDown(songIndex);
        }

        public static void MovePlaylistSongUp(int playlistID, int songIndex)
        {
            if (playlistID >= _Playlists.Count || playlistID < 0)
                return;

            _Playlists[playlistID].SongUp(songIndex);
        }

        public static void DeletePlaylistSong(int playlistID, int songIndex)
        {
            if (playlistID >= _Playlists.Count || playlistID < 0)
                return;

            _Playlists[playlistID].DeleteSong(songIndex);
        }

        public static int GetPlaylistSongCount(int playlistID)
        {
            if (playlistID >= _Playlists.Count || playlistID < 0)
                return 0;

            return _Playlists[playlistID].Songs.Count;
        }

        public static CPlaylistSong GetPlaylistSong(int playlistID, int songIndex)
        {
            if (playlistID >= _Playlists.Count || playlistID < 0)
                return null;

            if (songIndex >= _Playlists[playlistID].Songs.Count)
                return null;

            return _Playlists[playlistID].Songs[songIndex];
        }

        #region private methods
        private static void _SortPlaylistsByName()
        {
            _Playlists.Sort(_CompareByPlaylistName);
        }

        private static int _CompareByPlaylistName(CPlaylistFile a, CPlaylistFile b)
        {
            return String.Compare(a.PlaylistName, b.PlaylistName);
        }

        private static CPlaylistFile _ConvertUSDXPlaylist(string file)
        {
            CPlaylistFile pl = new CPlaylistFile();
            CSong[] allSongs = CSongs.AllSongs;

            if (!File.Exists(file))
                return null;
            StreamReader sr;
            try
            {
                using (sr = new StreamReader(file, Encoding.Default, true))
                {
                    int pos = -1;
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        pos = line.IndexOf(":");
                        if (pos > 0)
                        {
                            //Name or comment
                            if (line[0] == '#')
                            {
                                string identifier = line.Substring(1, pos - 1).Trim();
                                string value = line.Substring(pos + 1, line.Length - pos - 1).Trim();
                                if (identifier.ToUpper() == "NAME")
                                    pl.PlaylistName = value;
                            }
                                //Song
                            else
                            {
                                string artist = line.Substring(0, pos - 1).Trim();
                                string title = line.Substring(pos + 1, line.Length - pos - 1).Trim();
                                bool found = false;
                                for (int s = 0; s < allSongs.Length; s++)
                                {
                                    if (allSongs[s].Artist == artist && allSongs[s].Title == title)
                                    {
                                        pl.AddSong(allSongs[s].ID);
                                        found = true;
                                        break;
                                    }
                                }
                                if (!found)
                                    CLog.LogError("Can't find song '" + title + "' from '" + artist + "' in playlist file: " + file);
                            }
                        }
                    }
                }
                File.Delete(file);
            }
            catch
            {
                return null;
            }

            return pl;
        }
        #endregion
    }
}