using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using Vocaluxe.Lib.Playlist;
using Vocaluxe.GameModes;
using Vocaluxe.Menu;
using Vocaluxe.Menu.SongMenu;

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
            ConvertUSDXPlaylists();

            SortPlaylistsByName();
        }

        public static void ConvertUSDXPlaylists()
        {
            List<string> files = new List<string>();
            files.AddRange(CHelper.ListFiles(CSettings.sFolderPlaylists, "*.upl", true, true));

            foreach (string file in files)
            {
                CPlaylistFile playlist = ConvertUSDXPlaylist(file);
                playlist.SavePlaylist();
                _Playlists.Add(playlist);
            }
        }

        public static void LoadPlaylists()
        {
            _Playlists = new List<CPlaylistFile>();
            List<string> files = new List<string>();
            files.AddRange(CHelper.ListFiles(CSettings.sFolderPlaylists, "*.xml", true, true));

            foreach (string file in files)
            {
                CPlaylistFile playlist = new CPlaylistFile(file);
                _Playlists.Add(playlist);
            }
        }

        public static string GetPlaylistName(int PlaylistID)
        {
            if (PlaylistID >= _Playlists.Count || PlaylistID < 0)
                return "Error: Can't find Playlist";

            return _Playlists[PlaylistID].PlaylistName;
        }

        public static string[] GetPlaylistNames()
        {
            List<string> result = new List<string>();

            foreach (CPlaylistFile playlist in _Playlists)
	        {
		        result.Add(playlist.PlaylistName);
	        }

            return result.ToArray();
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

        private static CPlaylistFile ConvertUSDXPlaylist(string file)
        {
            CPlaylistFile pl = new CPlaylistFile();
            CSong[] AllSongs = CSongs.AllSongs;

            if (!File.Exists(file))
                return null;
            StreamReader sr;
            try
            {

                sr = new StreamReader(file, Encoding.Default, true);

                int pos = -1;
                string line;
                while((line = sr.ReadLine()) != null)
                {
                    pos = line.IndexOf(":");
                    if (pos > 0)
                    {
                        //Name or comment
                        if (line[0] == '#')
                        {
                            string Identifier = line.Substring(1, pos - 1).Trim();
                            string Value = line.Substring(pos + 1, line.Length - pos - 1).Trim();
                            if (Identifier.ToUpper() == "NAME")
                            {
                                pl.PlaylistName = Value;
                            }
                        }
                        //Song
                        else
                        {
                            string Artist = line.Substring(0, pos - 1).Trim();
                            string Title = line.Substring(pos + 1, line.Length - pos - 1).Trim();
                            bool found = false;
                            for (int s = 0; s < AllSongs.Length; s++)
                            {
                                if (AllSongs[s].Artist == Artist && AllSongs[s].Title == Title)
                                {
                                    pl.AddSong(AllSongs[s].ID);
                                    found = true;
                                    break;
                                }
                            }
                            if (!found)
                                CLog.LogError("Can't find song '" + Title + "' from '" + Artist + "' in playlist file: " + file);
                        }
                    }
                }
                sr.Close();
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
