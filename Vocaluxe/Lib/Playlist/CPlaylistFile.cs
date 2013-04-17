using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Vocaluxe.Base;
using VocaluxeLib.Menu;
using VocaluxeLib.Menu.SongMenu;

namespace Vocaluxe.Lib.Playlist
{
    public class CPlaylistFile
    {
        private static readonly XmlWriterSettings _Settings = new XmlWriterSettings();

        public string PlaylistName;
        public string PlaylistFile;
        public List<CPlaylistSong> Songs = new List<CPlaylistSong>();

        public CPlaylistFile()
        {
            _Init();
            PlaylistName = string.Empty;
            PlaylistFile = string.Empty;
        }

        public CPlaylistFile(string file)
        {
            _Init();
            PlaylistFile = file;
            _LoadPlaylist();
        }

        private void _Init()
        {
            _Settings.Indent = true;
            _Settings.Encoding = Encoding.UTF8;
            _Settings.ConformanceLevel = ConformanceLevel.Document;
        }

        public void SavePlaylist()
        {
            if (PlaylistFile.Length == 0)
            {
                string filename = string.Empty;
                foreach (char chr in PlaylistName)
                {
                    if (char.IsLetter(chr))
                        filename += chr.ToString();
                }

                if (filename.Length == 0)
                    filename = "1";

                int i = 0;
                while (File.Exists(Path.Combine(CSettings.FolderPlaylists, filename + ".xml")))
                {
                    i++;
                    if (!File.Exists(Path.Combine(CSettings.FolderPlaylists, filename + i + ".xml")))
                        filename += i;
                }

                PlaylistFile = Path.Combine(CSettings.FolderPlaylists, filename + ".xml");
            }

            XmlWriter writer;
            try
            {
                writer = XmlWriter.Create(PlaylistFile, _Settings);
            }
            catch (Exception e)
            {
                CLog.LogError("Error creating/opening Playlist File " + PlaylistFile + ": " + e.Message);
                return;
            }

            if (writer == null)
            {
                CLog.LogError("Error creating/opening Playlist File " + PlaylistFile);
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
                        writer.WriteStartElement("Song" + (i + 1).ToString());
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
            if (xmlReader.GetValue("//root/Info/PlaylistName", ref value, value))
            {
                PlaylistName = value;

                Songs = new List<CPlaylistSong>();

                List<string> songs = xmlReader.GetValues("Songs");
                string artist = String.Empty;
                string title = String.Empty;
                EGameMode gm = EGameMode.TR_GAMEMODE_NORMAL;

                for (int i = 0; i < songs.Count; i++)
                {
                    xmlReader.GetValue("//root/Songs/" + songs[i] + "/Artist", ref artist, String.Empty);
                    xmlReader.GetValue("//root/Songs/" + songs[i] + "/Title", ref title, String.Empty);
                    xmlReader.TryGetEnumValue("//root/Songs/" + songs[i] + "/GameMode", ref gm);

                    CPlaylistSong song = new CPlaylistSong();
                    song.SongID = -1;
                    CSong[] allSongs = CSongs.AllSongs;

                    for (int s = 0; s < allSongs.Length; s++)
                    {
                        if (allSongs[s].Artist == artist && allSongs[s].Title == title)
                        {
                            song.SongID = allSongs[s].ID;
                            break;
                        }
                    }

                    if (song.SongID != -1)
                    {
                        song.GameMode = gm;
                        Songs.Add(song);
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
            CPlaylistSong song = new CPlaylistSong();
            song.SongID = songID;
            if (CSongs.GetSong(songID).IsDuet)
                song.GameMode = EGameMode.TR_GAMEMODE_DUET;
            else
                song.GameMode = EGameMode.TR_GAMEMODE_NORMAL;

            Songs.Add(song);
        }

        public void AddSong(int songID, EGameMode gm)
        {
            CPlaylistSong song = new CPlaylistSong();
            song.SongID = songID;
            song.GameMode = gm;

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

            CPlaylistSong ps = new CPlaylistSong(Songs[sourceNr]);
            Songs.RemoveAt(sourceNr);
            Songs.Insert(destNr, ps);
        }

        public void SongInsert(int destNr, int songID, EGameMode gm)
        {
            if (destNr < 0 || destNr > Songs.Count - 1)
                return;

            CPlaylistSong ps = new CPlaylistSong(songID, gm);
            Songs.Insert(destNr, ps);
        }
    }
}