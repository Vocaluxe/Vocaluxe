using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;

using Vocaluxe.Lib.Song;
using Vocaluxe.Base;


namespace Vocaluxe.Lib.Playlist
{
    public class CPlaylistSong
    {
        public int SongID;
        public GameModes.EGameMode gm;

        public CPlaylistSong(int SongID, GameModes.EGameMode gm)
        {
            this.SongID = SongID;
            this.gm = gm;
        }

        public CPlaylistSong()
        {
            gm = GameModes.EGameMode.TR_GAMEMODE_NORMAL;
        }
    }

    public class CPlaylist
    {
        private static XmlWriterSettings _settings = new XmlWriterSettings();
        private static CHelper Helper = new CHelper();

        public string PlaylistName;
        public string PlaylistFile;
        public List<CPlaylistSong> Songs;

        public CPlaylist() 
        {
            Init();
        }

        public CPlaylist(string file)
        {
            Init();
            PlaylistName = file;
            LoadPlaylist();
        }

        private void Init()
        {
            _settings.Indent = true;
            _settings.Encoding = System.Text.Encoding.UTF8;
            _settings.ConformanceLevel = ConformanceLevel.Document;
        }


        private void SavePlaylist()
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
            for (i = 0; i < Songs.Count; i++)
            {
                CSong[] songs = CSongs.AllSongs;
                CSong song = new CSong();
                for (int s = 0; i <= songs.Length; s++)
                {
                    if (songs[s].ID == Songs[i].SongID)
                    {
                        song = songs[s];
                        break;
                    }
                }
                writer.WriteStartElement("Song" + (i + 1).ToString());
                writer.WriteElementString("Artist", song.Artist);
                writer.WriteElementString("Title", song.Title);
                writer.WriteElementString("GameMode", Enum.GetName(typeof(GameModes.EGameMode), Songs[i].gm));
                writer.WriteEndElement();
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
                    GameModes.EGameMode gm = GameModes.EGameMode.TR_GAMEMODE_NORMAL;

                    for (int i = 0; i < songs.Count; i++)
                    {
                        CHelper.GetValueFromXML("//root/Songs/" + songs[i] + "/Artist", navigator, ref artist, String.Empty);
                        CHelper.GetValueFromXML("//root/Songs/" + songs[i] + "/Title", navigator, ref title, String.Empty);
                        CHelper.TryGetEnumValueFromXML<GameModes.EGameMode>("//root/Songs/" + songs[i] + "/GameMode", navigator, ref gm);

                        CPlaylistSong song = new CPlaylistSong();
                        CSong[] AllSongs = CSongs.AllSongs;

                        for (int s = 0; s < AllSongs.Length; s++)
                        {
                            if (AllSongs[s].Artist == artist && AllSongs[s].Title == title)
                            {
                                song.SongID = AllSongs[s].ID;
                                break;
                            }
                        }

                        song.gm = gm;

                        Songs.Add(song);
                    }
                }
                else
                {
                    CLog.LogError("Can't find PlaylistName in Playlist File: " + PlaylistFile);
                }
            }
        }
    }
}
