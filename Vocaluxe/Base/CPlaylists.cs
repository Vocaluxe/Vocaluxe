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
using System.IO;
using System.Linq;
using System.Text;
using Vocaluxe.Lib.Playlist;
using VocaluxeLib;
using VocaluxeLib.Log;
using VocaluxeLib.Menu;

namespace Vocaluxe.Base
{
    static class CPlaylists
    {
        private static List<CPlaylistFile> _Playlists;

        public static IEnumerable<CPlaylistFile> Playlists
        {
            get { return _Playlists.AsReadOnly(); }
        }

        public static List<string> Names
        {
            get { return _Playlists.Select(t => t.Name).ToList(); }
        }

        public static List<int> Ids
        {
            get { return _Playlists.Select(t => t.Id).ToList(); }
        }

        public static int NumPlaylists
        {
            get { return _Playlists.Count; }
        }

        public static void Init()
        {
            Load();
            _ConvertUSDXPlaylists();

            _SortByName();
        }

        private static void _ConvertUSDXPlaylists()
        {
            var files = new List<string>();
            files.AddRange(CHelper.ListFiles(Path.Combine(CSettings.DataFolder, CConfig.FolderPlaylists), "*.upl", true, true));

            foreach (var file in files)
            {
                var playlist = _ConvertUSDXPlaylist(file);
                playlist.Save();
                _Playlists.Add(playlist);
            }
        }

        public static void Load()
        {
            _Playlists = new List<CPlaylistFile>();

            var files = new List<string>();
            files.AddRange(CHelper.ListFiles(Path.Combine(CSettings.DataFolder, CConfig.FolderPlaylists), "*.xml", true, true));

            foreach (var file in files)
            {
                var playlist = new CPlaylistFile();
                if (playlist._Load(file))
                {
                    _Playlists.Add(playlist);
                }
            }
        }

        public static CPlaylistFile Get(int playlistId)
        {
            return _Playlists.FirstOrDefault(pl => pl.Id == playlistId);
        }

        public static string GetName(int playlistId)
        {
            var pl = Get(playlistId);
            return pl == null ? "Error: Can't find Playlist" : pl.Name;
        }

        public static void SetName(int playlistId, string name)
        {
            var pl = Get(playlistId);

            if (pl != null)
            {
                pl.Name = name;
            }
        }

        public static void Delete(int playlistId)
        {
            var pl = Get(playlistId);
            if (pl == null)
            {
                return;
            }

            if (pl.File != "" && File.Exists(pl.File))
            {
                try
                {
                    File.Delete(pl.File);
                }
                catch (Exception)
                {
                    CLog.Error("Can't delete Playlist File " + _Playlists[playlistId].File + ".xml");
                }
            }

            _Playlists.Remove(pl);
        }

        public static void Save(int playlistId)
        {
            var pl = Get(playlistId);
            if (pl != null)
            {
                pl.Save();
            }
        }

        public static int NewPlaylist(string name = "New Playlist")
        {
            var pl = new CPlaylistFile { Name = name };
            _Playlists.Add(pl);
            return pl.Id;
        }

        public static void AddSong(int playlistId, int songId)
        {
            var pl = Get(playlistId);
            if (pl != null)
            {
                pl.AddSong(songId);
            }
        }

        public static void AddSong(int playlistId, int songId, EGameMode gameMode)
        {
            var pl = Get(playlistId);
            if (pl != null)
            {
                pl.AddSong(songId, gameMode);
            }
        }

        public static void InsertSong(int playlistId, int positionIndex, int songId, EGameMode gameMode)
        {
            var pl = Get(playlistId);
            if (pl != null)
            {
                pl.InsertSong(positionIndex, songId, gameMode);
            }
        }

        public static void MoveSong(int playlistId, int sourceIndex, int destIndex)
        {
            var pl = Get(playlistId);
            if (pl != null)
            {
                pl.MoveSong(sourceIndex, destIndex);
            }
        }

        public static void MovePSongDown(int playlistId, int songIndex)
        {
            var pl = Get(playlistId);
            if (pl != null)
            {
                pl.MoveSongDown(songIndex);
            }
        }

        public static void MoveSongUp(int playlistId, int songIndex)
        {
            var pl = Get(playlistId);
            if (pl != null)
            {
                pl.MoveSongUp(songIndex);
            }
        }

        public static void DeleteSong(int playlistId, int songIndex)
        {
            var pl = Get(playlistId);
            if (pl != null)
            {
                pl.DeleteSong(songIndex);
            }
        }

        public static int GetSongCount(int playlistId)
        {
            var pl = Get(playlistId);
            return pl != null ? pl.Songs.Count : -1;
        }

        public static CPlaylistSong GetSong(int playlistId, int songIndex)
        {
            var pl = Get(playlistId);
            if (pl == null || songIndex >= pl.Songs.Count)
            {
                return null;
            }

            return pl.Songs[songIndex];
        }

        public static bool ContainsSong(int playlistId, int songId)
        {
            var pl = Get(playlistId);
            return pl != null && pl.Songs.Find((x) => x.SongId == songId) != null;
        }

        #region private methods
        private static void _SortByName()
        {
            _Playlists.Sort(_CompareByName);
        }

        private static int _CompareByName(CPlaylistFile a, CPlaylistFile b)
        {
            return String.CompareOrdinal(a.Name, b.Name);
        }

        private static CPlaylistFile _ConvertUSDXPlaylist(string file)
        {
            var pl = new CPlaylistFile();
            var allSongs = CSongs.AllSongs;

            if (!File.Exists(file))
            {
                return null;
            }

            try
            {
                StreamReader sr;
                using (sr = new StreamReader(file, Encoding.Default, true))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        var pos = line.IndexOf(":", StringComparison.Ordinal);
                        if (pos <= 0)
                        {
                            continue;
                        }

                        if (line[0] == '#')
                        {
                            //Name or comment
                            var identifier = line.Substring(1, pos - 1).Trim();
                            var value = line.Substring(pos + 1, line.Length - pos - 1).Trim();
                            if (identifier.ToUpper() == "NAME")
                            {
                                pl.Name = value;
                            }
                        }
                        else
                        {
                            //Song
                            var artist = line.Substring(0, pos - 1).Trim();
                            var title = line.Substring(pos + 1, line.Length - pos - 1).Trim();
                            var plSong = allSongs.FirstOrDefault(song => song.Artist == artist && song.Title == title);
                            if (plSong != null)
                            {
                                pl.AddSong(plSong.Id);
                            }
                            else
                            {
                                CLog.Error("Can't find song '" + title + "' from '" + artist + "' in playlist file: " + file);
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