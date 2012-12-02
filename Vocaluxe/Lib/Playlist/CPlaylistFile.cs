using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;

using Vocaluxe.Base;
using Vocaluxe.GameModes;
using Vocaluxe.Lib.Song;


namespace Vocaluxe.Lib.Playlist
{
    public class CPlaylistSong
    {
        public int SongID;
        public EGameMode GameMode;

        public CPlaylistSong(int SongID, EGameMode gm)
        {
            this.SongID = SongID;
            this.GameMode = gm;
        }

        public CPlaylistSong()
        {
            GameMode = EGameMode.TR_GAMEMODE_NORMAL;
        }
    }

    public class CPlaylistFile
    {
        private static XmlWriterSettings _settings = new XmlWriterSettings();
        private static CHelper Helper = new CHelper();

        public string PlaylistName;
        public string PlaylistFile;
        public List<CPlaylistSong> Songs = new List<CPlaylistSong>();

        public CPlaylistFile() 
        {
            Init();
            PlaylistName = string.Empty;
            PlaylistFile = string.Empty;
        }

        public CPlaylistFile(string file)
        {
            Init();
            PlaylistFile = file;
            LoadPlaylist();
        }

        private void Init()
        {
            _settings.Indent = true;
            _settings.Encoding = System.Text.Encoding.UTF8;
            _settings.ConformanceLevel = ConformanceLevel.Document;
        }


        public void SavePlaylist()
        {
            if (PlaylistFile == string.Empty)
            {
                string filename = string.Empty;
                foreach (char chr in PlaylistName)
                {
                    if (char.IsLetter(chr))
                        filename += chr.ToString();
                }

                if (filename == String.Empty)
                    filename = "1";

                int i = 0;
                while (File.Exists(Path.Combine(CSettings.sFolderPlaylists, filename + ".xml")))
                {
                    i++;
                    if (!File.Exists(Path.Combine(CSettings.sFolderPlaylists, filename + i + ".xml")))
                    {
                        filename += i;
                    }
                }

                PlaylistFile = Path.Combine(CSettings.sFolderPlaylists, filename + ".xml");
            }

            XmlWriter writer;
            try
            {
                writer = XmlWriter.Create(PlaylistFile, _settings);
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
                {
                    CLog.LogError("Playlist.SavePlaylist(): Can't find Song. This should never happen!");
                }
            }
            writer.WriteEndElement();

            writer.WriteEndElement(); //end of root
            writer.WriteEndDocument();

            writer.Flush();
            writer.Close();
        }

        private void LoadPlaylist()
        {
            bool loaded = false;
            XPathDocument xPathDoc = null;
            XPathNavigator navigator = null;

            try
            {
                xPathDoc = new XPathDocument(PlaylistFile);
                navigator = xPathDoc.CreateNavigator();
                loaded = true;
            }
            catch (Exception e)
            {
                loaded = false;
                if (navigator != null)
                    navigator = null;

                if (xPathDoc != null)
                    xPathDoc = null;

                CLog.LogError("Error opening Playlist File " + PlaylistFile + ": " + e.Message);
            }

            if (loaded)
            {
                string value = String.Empty;
                if (CHelper.GetValueFromXML("//root/Info/PlaylistName", navigator, ref value, value))
                {
                    PlaylistName = value;

                    Songs = new List<CPlaylistSong>();

                    List<string> songs = CHelper.GetValuesFromXML("Songs", navigator);
                    string artist = String.Empty;
                    string title = String.Empty;
                    EGameMode gm = EGameMode.TR_GAMEMODE_NORMAL;

                    for (int i = 0; i < songs.Count; i++)
                    {
                        CHelper.GetValueFromXML("//root/Songs/" + songs[i] + "/Artist", navigator, ref artist, String.Empty);
                        CHelper.GetValueFromXML("//root/Songs/" + songs[i] + "/Title", navigator, ref title, String.Empty);
                        CHelper.TryGetEnumValueFromXML<EGameMode>("//root/Songs/" + songs[i] + "/GameMode", navigator, ref gm);

                        CPlaylistSong song = new CPlaylistSong();
                        song.SongID = -1;
                        CSong[] AllSongs = CSongs.AllSongs;

                        for (int s = 0; s < AllSongs.Length; s++)
                        {
                            if (AllSongs[s].Artist == artist && AllSongs[s].Title == title)
                            {
                                song.SongID = AllSongs[s].ID;
                                break;
                            }
                        }

                        if (song.SongID != -1)
                        {
                            song.GameMode = gm;
                            Songs.Add(song);
                        }
                        else
                        {
                            CLog.LogError("Can't find song '" + title + "' from '" + artist + "' in playlist file: " + PlaylistFile);
                        }
                    }
                }
                else
                {
                    CLog.LogError("Can't find PlaylistName in Playlist File: " + PlaylistFile);
                }
            }
        }

        public void AddSong(int SongID)
        {
            CPlaylistSong song = new CPlaylistSong();
            song.SongID = SongID;
            if (CSongs.GetSong(SongID).IsDuet)
                song.GameMode = EGameMode.TR_GAMEMODE_DUET;
            else
                song.GameMode = EGameMode.TR_GAMEMODE_NORMAL;

            Songs.Add(song);            
        }

        public void AddSong(int SongID, EGameMode gm)
        {
            CPlaylistSong song = new CPlaylistSong();
            song.SongID = SongID;
            song.GameMode = gm;

            Songs.Add(song);   
        }

        public void DeleteSong(int SongNr)
        {
            Songs.RemoveAt(SongNr);
        }

        public void SongDown(int SongNr)
        {
            if (SongNr != 0)
            {
                Songs.Reverse(SongNr - 1, 2);
            }
        }

        public void SongUp(int SongNr)
        {
            if (SongNr + 1 < Songs.Count)
            {
                Songs.Reverse(SongNr, 2);
            }
        }
    }
}
