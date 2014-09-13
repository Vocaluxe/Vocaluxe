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
using System.Xml;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Menu;
using VocaluxeLib.Songs;

namespace Vocaluxe.Lib.Playlist
{
    public class CPlaylistFile
    {
        public string Name = "";
        public string File;
        public readonly int Id = -1;
        public List<CPlaylistSong> Songs = new List<CPlaylistSong>();

        public CPlaylistFile(int id, string file = "")
        {
            Id = id;
            File = file;
            if (File != "")
                _Load();
        }

        public void Save()
        {
            if (File == "")
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

            XmlWriter writer;
            try
            {
                writer = XmlWriter.Create(File, CConfig.XMLSettings);
            }
            catch (Exception e)
            {
                CLog.LogError("Error creating/opening Playlist File " + File + ": " + e.Message);
                return;
            }

            try
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("root");

                writer.WriteStartElement("Info");
                writer.WriteElementString("PlaylistName", Name);
                writer.WriteEndElement();

                writer.WriteStartElement("Songs");
                for (int i = 0; i < Songs.Count; i++)
                {
                    CSong song = CSongs.GetSong(Songs[i].SongID);
                    if (song != null)
                    {
                        writer.WriteStartElement("Song" + (i + 1));
                        writer.WriteElementString("Artist", song.Artist);
                        writer.WriteElementString("Title", song.Title);
                        writer.WriteElementString("GameMode", Enum.GetName(typeof(EGameMode), Songs[i].GameMode));
                        writer.WriteEndElement();
                    }
                    else
                        CLog.LogError("Playlist.SavePlaylist(): Can't find Song. This should never happen!");
                }
                writer.WriteEndElement();

                writer.WriteEndElement(); //end of root
                writer.WriteEndDocument();

                writer.Flush();
            }
            finally
            {
                writer.Close();
            }
        }

        private void _Load()
        {
            CXMLReader xmlReader = CXMLReader.OpenFile(File);
            if (xmlReader == null)
                return;

            string value;
            if (xmlReader.GetValue("//root/Info/PlaylistName", out value, ""))
            {
                Name = value;

                Songs = new List<CPlaylistSong>();

                List<string> songs = xmlReader.GetNames("//root/Songs/*");

                foreach (string songEntry in songs)
                {
                    string artist;
                    string title;
                    xmlReader.GetValue("//root/Songs/" + songEntry + "/Artist", out artist, String.Empty);
                    xmlReader.GetValue("//root/Songs/" + songEntry + "/Title", out title, String.Empty);
                    EGameMode gm = EGameMode.TR_GAMEMODE_NORMAL;
                    xmlReader.TryGetEnumValue("//root/Songs/" + songEntry + "/GameMode", ref gm);

                    CSong plSong = CSongs.AllSongs.FirstOrDefault(song => song.Artist == artist && song.Title == title);
                    if (plSong != null)
                    {
                        var playlistSong = new CPlaylistSong(plSong.ID, gm);
                        Songs.Add(playlistSong);
                    }
                    else
                        CLog.LogError("Can't find song '" + title + "' from '" + artist + "' in playlist file: " + File);
                }
            }
            else
                CLog.LogError("Can't find PlaylistName in Playlist File: " + File);
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