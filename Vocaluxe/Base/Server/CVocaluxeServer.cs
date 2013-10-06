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
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Linq;
using System.IO;
using System.Text;
using System.Windows.Forms;
using ClientServerLib;
using Vocaluxe.Lib.Input;
using VocaluxeLib;
using VocaluxeLib.Profile;
using VocaluxeLib.Songs;

namespace Vocaluxe.Base.Server
{
    static class CVocaluxeServer
    {
        private static CServer _Server;
        private static CDiscover _Discover;
        private static Dictionary<int, CClientHandler> _Clients;

        public static readonly CControllerFramework Controller = new CControllerFramework();

        public static void Init()
        {
            _Clients = new Dictionary<int, CClientHandler>();
            if (CConfig.ServerEncryption == EOffOn.TR_CONFIG_ON)
            {
                _Server = new CServer(CConfig.ServerPort);
                //_Server = new CServer(RequestHandler, CConfig.ServerPort, CConfig.ServerPassword);
            }
            else
            {
                _Server = new CServer(CConfig.ServerPort);
                //_Server = new CServer(RequestHandler, CConfig.ServerPort, String.Empty);
            }

            CServer.SendKeyEvent = sendKeyEvent;
            CServer.GetProfileData = getProfileData;
            CServer.SendProfileData = sendProfileData;
            CServer.GetProfileList = getProfileList;
            CServer.SendPhoto = sendPhoto;
            CServer.GetSiteFile = getSiteFile;
            CServer.GetSong = getSong;
            CServer.GetAllSongs = getAllSongs;
            CServer.GetCurrentSongId = getCurrentSongId;

            _Discover = new CDiscover(CConfig.ServerPort, CCommands.BroadcastKeyword);
            Controller.Init();
        }

        public static void Start()
        {
            if (CConfig.ServerActive == EOffOn.TR_CONFIG_ON)
            {
                _Server.Start();
                _Discover.StartBroadcasting();
            }
        }

        public static void Close()
        {
            _Server.Stop();
            _Discover.Stop();
            _Clients = new Dictionary<int, CClientHandler>();
        }

        private static bool sendKeyEvent(string key)
        {
            bool result = false;
            if (key != null && key != "")
            {
                switch (key.ToLower())
                {
                    case "up":
                        Controller.AddKeyEvent(new SKeyEvent(ESender.Keyboard, false, false, false, false, Char.MinValue, Keys.Up));
                        result = true;
                        break;
                    case "down":
                        Controller.AddKeyEvent(new SKeyEvent(ESender.Keyboard, false, false, false, false, Char.MinValue, Keys.Down));
                        result = true;
                        break;
                    case "left":
                        Controller.AddKeyEvent(new SKeyEvent(ESender.Keyboard, false, false, false, false, Char.MinValue, Keys.Left));
                        result = true;
                        break;
                    case "right":
                        Controller.AddKeyEvent(new SKeyEvent(ESender.Keyboard, false, false, false, false, Char.MinValue, Keys.Right));
                        result = true;
                        break;
                    case "escape":
                        Controller.AddKeyEvent(new SKeyEvent(ESender.Keyboard, false, false, false, false, Char.MinValue, Keys.Escape));
                        result = true;
                        break;
                    case "return":
                        Controller.AddKeyEvent(new SKeyEvent(ESender.Keyboard, false, false, false, false, Char.MinValue, Keys.Return));
                        result = true;
                        break;
                    case "tab":
                        Controller.AddKeyEvent(new SKeyEvent(ESender.Keyboard, false, false, false, false, Char.MinValue, Keys.Tab));
                        result = true;
                        break;
                    default:
                        foreach (char c in key.ToCharArray())
                        {
                            bool shift = true;
                            char keyChar;
                            if (char.ToUpper(c) != c)
                            {
                                keyChar = char.ToUpper(c);
                                shift = false;
                            }
                            else
                            {
                                keyChar = c;
                            }

                            if ((keyChar >= 0x30 && keyChar <= 0x39) || (keyChar >= 0x41 && keyChar <= 0x5A))
                            {
                                Keys keys = (Keys)keyChar;
                                if (shift)
                                {
                                    keys |= Keys.Shift;
                                }
                                Controller.AddKeyEvent(new SKeyEvent(ESender.Keyboard, false, false, false, false, Char.MinValue, keys));
                                result = true;
                            }
                        }
                        break;
                }
            }

            return result;

        }

        #region profile

        private static ProfileData getProfileData(int profileId)
        {
            CProfile profile = CProfiles.GetProfile(profileId);
            if (profile == null)
            {
                return new ProfileData();
            }
            return CreateProfileData(profile); ;
        }

        private static bool sendProfileData(ProfileData profile)
        {
            CProfile newProfile;
            CProfile existingProfile = CProfiles.GetProfile(profile.ProfileId);

            if (existingProfile != null)
            {
                newProfile = new CProfile
                {
                    Active = existingProfile.Active,
                    AvatarFileName = existingProfile.AvatarFileName,
                    Avatar = existingProfile.Avatar,
                    Difficulty = existingProfile.Difficulty,
                    GuestProfile = existingProfile.GuestProfile,
                    PlayerName = existingProfile.PlayerName
                };
            }
            else
            {
                newProfile = new CProfile
                {
                    Active = EOffOn.TR_CONFIG_ON,
                    GuestProfile = EOffOn.TR_CONFIG_OFF
                };
            }

            if (profile.Avatar != null)
            {
                newProfile.Avatar = _AddAvatar(profile.Avatar);

            }
            else if (newProfile.Avatar == null)
            {
                //TODO:standardAvatar                
            }

            if (profile.PlayerName != null && profile.PlayerName != "")
            {
                newProfile.PlayerName = profile.PlayerName;
            }
            else if (newProfile.PlayerName != null && newProfile.PlayerName != "")
            {
                newProfile.PlayerName = "DummyName";
            }

            if (profile.Difficulty >= 0 && profile.Difficulty <= 2)
            {
                newProfile.Difficulty = (EGameDifficulty)profile.Difficulty;
            }

            if (existingProfile != null)
            {
                CProfiles.EditProfile(newProfile);
            }
            else
            {
                CProfiles.AddProfile(newProfile);
            }

            return true;
        }

        private static ProfileData[] getProfileList()
        {
            List<ProfileData> result = new List<ProfileData>(CProfiles.NumProfiles);

            foreach (CProfile profile in CProfiles.GetProfiles())
            {
                result.Add(CreateProfileData(profile));
            }

            return result.ToArray();
        }

        private static ProfileData CreateProfileData(CProfile profile)
        {
            ProfileData profileData = new ProfileData();

            profileData.IsEditable = true; //TODO: set correctly 
            profileData.ProfileId = profile.ID;
            profileData.PlayerName = profile.PlayerName;
            profileData.Type = (int)profile.GuestProfile;
            profileData.Difficulty = (int)profile.Difficulty;
            CAvatar avatar = profile.Avatar;
            if (avatar != null)
            {
                if (File.Exists(avatar.FileName))
                {
                    Image avatarImage = Image.FromFile(avatar.FileName);
                    //TODO: Convert??? and Resize?
                    profileData.Avatar = new Base64Image(avatarImage, avatarImage.RawFormat);
                }
            }
            return profileData;
        }

        private static CAvatar _AddAvatar(Base64Image avatarData)
        {
            string result = String.Empty;
            try
            {
                string filename = _SaveImage(avatarData, "snapshot", CSettings.FolderProfiles);

                CAvatar avatar = new CAvatar(-1);
                if (avatar.LoadFromFile(filename))
                {
                    CProfiles.AddAvatar(avatar);
                    return avatar;
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region photo

        private static bool sendPhoto(PhotoData photoData)
        {
            if (photoData.Photo == null)
            {
                return false;
            }

            string name = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string filePath = _SaveImage(photoData.Photo, name, CSettings.FolderPhotos);

            return (filePath != null && filePath != "");
        }

        #endregion

        #region website

        private static byte[] getSiteFile(string filename)
        {
            string path = "Website/" + filename;
            path = path.Replace("..", "");

            if (!File.Exists(path))
            {
                return null;
            }

            /*string content = File.ReadAllText(path);

            content = content.Replace("%SERVER%", System.Net.Dns.GetHostName() + ":" + CConfig.ServerPort);


            return Encoding.UTF8.GetBytes(content);*/

            return File.ReadAllBytes(path);
        }

        #endregion

        #region songs

        private static SongInfo getSong(int songId)
        {
            CSong song = CSongs.GetSong(songId);
            return getSongInfo(song, true);
        }

        private static SongInfo[] getAllSongs()
        {
            var songs = CSongs.Songs;            
            return (from s in songs
                    select getSongInfo(s, false)).ToArray<SongInfo>();
        }

        private static int getCurrentSongId()
        {
            CSong song = CGame.GetSong();
            if (song == null)
            {
                return -1;
            }
            return song.ID;
        }

        private static SongInfo getSongInfo(CSong song, bool includeCover)
        {
            SongInfo result = new SongInfo();
            if (song != null)
            {
                result.Title = song.Title;
                result.Artist = song.Artist;
                result.Genre = song.Genre.FirstOrDefault();
                result.Language = song.Language.FirstOrDefault();
                result.Year = song.Year;
                result.IsDuet = song.IsDuet;
                result.SongId = song.ID;
                if (includeCover)
                {
                    Image cover = Image.FromFile(song.Folder + "\\" + song.CoverFileName);
                    result.Cover = new Base64Image(cover, cover.RawFormat);
                }
            }
            return result;
        }

        #endregion

        private static string _SaveImage(Base64Image imageDate, string name, string folder)
        {
            Image avatarImage = imageDate.getImage();
            string extension = imageDate.getImageType();

            string filename = Path.Combine(folder, name);
            if (File.Exists(filename + "." + extension))
            {
                int i = 0;
                while (File.Exists(filename + "_" + i + "." + extension))
                {
                    i++;
                }
                filename = filename + "_" + i + "." + extension;
            }
            else
            {
                filename = filename + "." + extension;
            }

            avatarImage.Save(filename);
            return filename;
        }
    }
}