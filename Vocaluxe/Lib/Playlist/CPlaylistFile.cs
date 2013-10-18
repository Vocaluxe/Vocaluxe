#region license
// /*
//     This file is part of Vocaluxe.
// 
//     Vocaluxe is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     Vocaluxe is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
//  */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Menu;
using VocaluxeLib.Songs;

namespace Vocaluxe.Lib.Playlist
{
    public class CPlaylistFile
    {
        public string PlaylistName;
        public string PlaylistFile;
        public List<CPlaylistSong> Songs = new List<CPlaylistSong>();

        public CPlaylistFile()
        {
            PlaylistName = string.Empty;
            PlaylistFile = string.Empty;
        }

        public CPlaylistFile(string file)
        {
            PlaylistFile = file;
            _LoadPlaylist();
        }

        public void SavePlaylist()
        {
            if (PlaylistFile == "")
            {
                string filename = string.Empty;
                // ReSharper disable LoopCanBeConvertedToQuery
                foreach (char chr in PlaylistName)
                    // ReSharper restore LoopCanBeConvertedToQuery
                {
                    if (char.IsLetter(chr))
                        filename += chr.ToString();
                }

                if (filename == "")
                    filename = "1";

                int i = 0;
                while (File.Exists(Path.Combine(CSettings.DataPath, CSettings.FolderPlaylists, filename + ".xml")))
                {
                    i++;
                    if (!File.Exists(Path.Combine(CSettings.DataPath, CSettings.FolderPlaylists, filename + i + ".xml")))
                        filename += i;
                }

                PlaylistFile = Path.Combine(CSettings.DataPath, CSettings.FolderPlaylists, filename + ".xml");
            }

            XmlWriter writer;
            try
            {
                writer = XmlWriter.Create(PlaylistFile, CConfig.XMLSettings);
            }
            catch (Exception e)
            {
                CLog.LogError("Error creating/opening Playlist File " + PlaylistFile + ": " + e.Message);
                return;
            }

            try
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("root");

                writer.WriteStartElement("Info");
                writer.WriteElementString("PlaylistName", PlaylistName);
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

        private void _LoadPlaylist()
        {
            CXMLReader xmlReader = CXMLReader.OpenFile(PlaylistFile);
            if (xmlReader == null)
                return;

            string value = String.Empty;
            if (xmlReader.GetValue("//root/Info/PlaylistName", out value, value))
            {
                PlaylistName = value;

                Songs = new List<CPlaylistSong>();

                List<string> songs = xmlReader.GetValues("Songs");
                var gm = EGameMode.TR_GAMEMODE_NORMAL;

                foreach (string song in songs)
                {
                    string artist;
                    string title;
                    xmlReader.GetValue("//root/Songs/" + song + "/Artist", out artist, String.Empty);
                    xmlReader.GetValue("//root/Songs/" + song + "/Title", out title, String.Empty);
                    xmlReader.TryGetEnumValue("//root/Songs/" + song + "/GameMode", ref gm);

                    var playlistSong = new CPlaylistSong {SongID = -1};

                    foreach (CSong curSong in CSongs.AllSongs)
                    {
                        if (curSong.Artist != artist || curSong.Title != title)
                            continue;
                        playlistSong.SongID = curSong.ID;
                        break;
                    }

                    if (playlistSong.SongID != -1)
                    {
                        playlistSong.GameMode = gm;
                        Songs.Add(playlistSong);
                    }
                    else
                        CLog.LogError("Can't find song '" + title + "' from '" + artist + "' in playlist file: " + PlaylistFile);
                }
            }
            else
                CLog.LogError("Can't find PlaylistName in Playlist File: " + PlaylistFile);
        }

        public void AddSong(int songID)
        {
            var song = new CPlaylistSong {SongID = songID, GameMode = CSongs.GetSong(songID).IsDuet ? EGameMode.TR_GAMEMODE_DUET : EGameMode.TR_GAMEMODE_NORMAL};

            Songs.Add(song);
        }

        public void AddSong(int songID, EGameMode gm)
        {
            var song = new CPlaylistSong {SongID = songID, GameMode = gm};

            Songs.Add(song);
        }

        public void DeleteSong(int songNr)
        {
            Songs.RemoveAt(songNr);
        }

        public void SongUp(int songNr)
        {
            if (songNr < Songs.Count - 1 && songNr > 0)
                Songs.Reverse(songNr - 1, 2);
        }

        public void SongDown(int songNr)
        {
            if (songNr < Songs.Count - 1 && songNr >= 0)
                Songs.Reverse(songNr, 2);
        }

        public void SongMove(int sourceNr, int destNr)
        {
            if (sourceNr < 0 || destNr < 0 || sourceNr == destNr || sourceNr > Songs.Count - 1 || destNr > Songs.Count - 1)
                return;

            var ps = new CPlaylistSong(Songs[sourceNr]);
            Songs.RemoveAt(sourceNr);
            Songs.Insert(destNr, ps);
        }

        public void SongInsert(int destNr, int songID, EGameMode gm)
        {
            if (destNr < 0 || destNr > Songs.Count - 1)
                return;

            var ps = new CPlaylistSong(songID, gm);
            Songs.Insert(destNr, ps);
        }
    }
}