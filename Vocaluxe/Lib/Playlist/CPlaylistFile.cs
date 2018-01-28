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
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Log;
using VocaluxeLib.Menu;
using VocaluxeLib.Songs;
using VocaluxeLib.Xml;

namespace Vocaluxe.Lib.Playlist
{
    public class CPlaylistFile
    {
        public string Name = "";
        public string File;
        public readonly int Id;
        public List<CPlaylistSong> Songs = new List<CPlaylistSong>();

        private static int _NextID;

        public CPlaylistFile()
        {
            Id = _NextID++;
        }

        public void Save()
        {
            if (string.IsNullOrEmpty(File))
            {
                string filename = string.Empty;
                // ReSharper disable LoopCanBeConvertedToQuery
                foreach (char chr in Name)
                    // ReSharper restore LoopCanBeConvertedToQuery
                {
                    if (char.IsLetter(chr))
                        filename += chr.ToString();
                }

                if (filename == "")
                    filename = "1";

                File = CHelper.GetUniqueFileName(Path.Combine(CSettings.DataFolder, CConfig.FolderPlaylists), filename + ".xml");
            }

            SPlaylist data = new SPlaylist {Info = {Name = Name}, Songs = Songs.Select(plSong => plSong.ToStruct()).ToArray()};

            var xml = new CXmlSerializer();
            xml.Serialize(File, data);
        }

        public bool _Load(string file)
        {
            File = file;
            SPlaylist data;
            try
            {
                var xml = new CXmlDeserializer();
                data = xml.Deserialize<SPlaylist>(File);
            }
            catch (Exception e)
            {
                CLog.Error("Cannot load playlist from " + file + ": " + e.Message);
                return false;
            }
            Name = data.Info.Name;
            Songs = new List<CPlaylistSong>();
            foreach (SPlaylistSong songEntry in data.Songs)
            {
                CSong plSong = CSongs.AllSongs.FirstOrDefault(song => song.Artist == songEntry.Artist && song.Title == songEntry.Title);
                if (plSong == null)
                    CLog.Error("Can't find song '" + songEntry.Title + "' from '" + songEntry.Artist + "' in playlist file: " + File);
                else
                {
                    var playlistSong = new CPlaylistSong(plSong.ID, songEntry.GameMode);
                    Songs.Add(playlistSong);
                }
            }
            return true;
        }

        public void AddSong(int songID)
        {
            var song = new CPlaylistSong
                {
                    SongID = songID,
                    GameMode = CSongs.GetSong(songID).IsGameModeAvailable(EGameMode.TR_GAMEMODE_DUET) ? EGameMode.TR_GAMEMODE_DUET : EGameMode.TR_GAMEMODE_NORMAL
                };

            Songs.Add(song);
        }

        public void AddSong(int songID, EGameMode gm)
        {
            var song = new CPlaylistSong(songID, gm);

            Songs.Add(song);
        }

        public void DeleteSong(int songNr)
        {
            Songs.RemoveAt(songNr);
        }

        public void MoveSongUp(int songNr)
        {
            if (songNr < Songs.Count && songNr > 0)
                Songs.Reverse(songNr - 1, 2);
        }

        public void MoveSongDown(int songNr)
        {
            if (songNr < Songs.Count - 1 && songNr >= 0)
                Songs.Reverse(songNr, 2);
        }

        public void MoveSong(int sourceNr, int destNr)
        {
            if (sourceNr < 0 || destNr < 0 || sourceNr == destNr || sourceNr >= Songs.Count || destNr >= Songs.Count)
                return;

            CPlaylistSong song = Songs[sourceNr];
            Songs.RemoveAt(sourceNr);
            Songs.Insert(destNr, song);
        }

        public void InsertSong(int destNr, int songID, EGameMode gm)
        {
            if (destNr < 0 || destNr >= Songs.Count)
                return;

            CPlaylistSong ps = new CPlaylistSong(songID, gm);
            Songs.Insert(destNr, ps);
        }
    }
}