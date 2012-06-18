using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;

using Vocaluxe.Lib.Song;

namespace Vocaluxe.Base
{
    class CPlaylistSong
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

    struct SPlaylist
    {
        public string PlaylistName;
        public string PlaylistFile;
        public List<CPlaylistSong> Songs;
    }

    static class CPlaylists
    {
        private static XmlWriterSettings _settings = new XmlWriterSettings();
        private static CHelper Helper = new CHelper();
        private static List<SPlaylist> _Playlists;

        public static SPlaylist[] Playlists
        {
            get { return _Playlists.ToArray(); }
        }

        public static int NumPlaylists
        {
            get { return _Playlists.Count; }
        }

        public static void Init()
        {
            _settings.Indent = true;
            _settings.Encoding = System.Text.Encoding.UTF8;
            _settings.ConformanceLevel = ConformanceLevel.Document;

            LoadPlaylists();
        }

        public static void LoadPlaylists()
        {
            _Playlists = new List<SPlaylist>();
            List<string> files = new List<string>();
            files.AddRange(Helper.ListFiles(CSettings.sFolderPlaylists, "*.xml", true, true));

            foreach (string file in files)
            {
                LoadPlaylist(file);
            }

            SortPlaylistsByName();
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

        #region private methods

        private static void SortPlaylistsByName()
        {
            _Playlists.Sort(CompareByPlaylistName);
        }

        private static int CompareByPlaylistName(SPlaylist a, SPlaylist b)
        {
            return String.Compare(a.PlaylistName, b.PlaylistName);
        }

        private static void SavePlaylist(int PlaylistID)
        {
            if (PlaylistID < 0 || PlaylistID >= _Playlists.Count)
                return;

            string filename = string.Empty;
            foreach (char chr in _Playlists[PlaylistID].PlaylistName)
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

            SPlaylist playlist = _Playlists[PlaylistID];
            playlist.PlaylistFile = Path.Combine(CSettings.sFolderPlaylists, filename + ".xml");
            _Playlists[PlaylistID] = playlist;

            XmlWriter writer;
            try
            {
                writer = XmlWriter.Create(_Playlists[PlaylistID].PlaylistFile, _settings);
            }
            catch (Exception e)
            {
                CLog.LogError("Error creating/opening Playlist File " + _Playlists[PlaylistID].PlaylistFile + ": " + e.Message);
                return;
            }

            if (writer == null)
            {
                CLog.LogError("Error creating/opening Playlist File " + _Playlists[PlaylistID].PlaylistFile);
                return;
            }

            writer.WriteStartDocument();
            writer.WriteStartElement("root");

            writer.WriteStartElement("Info");
            writer.WriteElementString("PlaylistName", _Playlists[PlaylistID].PlaylistName);
            writer.WriteEndElement();

            writer.WriteStartElement("Songs");
            for (i = 0; i < playlist.Songs.Count; i++)
            {
                CSong[] songs = CSongs.AllSongs;
                CSong song = new CSong();
                for (int s = 0; i <= songs.Length; s++)
                {
                    if (songs[s].ID == playlist.Songs[i].SongID)
                    {
                        song = songs[s];
                        break;
                    }
                }
                writer.WriteStartElement("Song" + (i+1).ToString());
                writer.WriteElementString("Artist", song.Artist);
                writer.WriteElementString("Title", song.Title);
                writer.WriteElementString("GameMode", Enum.GetName(typeof(GameModes.EGameMode), playlist.Songs[i].gm));
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteEndElement(); //end of root
            writer.WriteEndDocument();

            writer.Flush();
            writer.Close();
        }

        private static void LoadPlaylist(string FileName)
        {
            bool loaded = false;
            XPathDocument xPathDoc = null;
            XPathNavigator navigator = null;

            SPlaylist playlist = new SPlaylist();
            playlist.PlaylistFile = Path.Combine(CSettings.sFolderPlaylists, FileName);

            try
            {
                xPathDoc = new XPathDocument(playlist.PlaylistFile);
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

                CLog.LogError("Error opening Playlist File " + FileName + ": " + e.Message);
            }

            if (loaded)
            {
                string value = String.Empty;
                if (CHelper.GetValueFromXML("//root/Info/PlaylistName", navigator, ref value, value))
                {
                    playlist.PlaylistName = value;

                    playlist.Songs = new List<CPlaylistSong>();

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

                        playlist.Songs.Add(song);
                    }

                    _Playlists.Add(playlist);
                }
                else
                {
                    CLog.LogError("Can't find PlaylistName in Playlist File: " + FileName);
                }
            }
        }

        #endregion
    }
}
