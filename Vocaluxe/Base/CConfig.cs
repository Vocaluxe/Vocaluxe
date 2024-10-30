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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Serialization;
using Vocaluxe.Lib.Sound.Record;
using Vocaluxe.Lib.Webcam;
using VocaluxeLib;
using VocaluxeLib.Log;
using VocaluxeLib.Profile;
using VocaluxeLib.Xml;

namespace Vocaluxe.Base
{
    static class CConfig
    {
        private static bool _Initialized;

        // Base file and folder names (formerly from CSettings but they can be changed)
        public static string FolderPlaylists = Path.Combine(CSettings.DataFolder, "Playlists");
        public static string FileHighscoreDB = Path.Combine(CSettings.DataFolder, "HighscoreDB.sqlite");
        private static string _FileConfig = Path.Combine(CSettings.DataFolder, "Config.xml");

        // ReSharper disable UnassignedField.Global
#pragma warning disable 649
        // ReSharper disable MemberCanBePrivate.Global
        public struct SConfigInfo
        // ReSharper restore MemberCanBePrivate.Global
        {
            // ReSharper disable NotAccessedField.Global
            // ReSharper disable NotAccessedField.Local
            public string Version;
            public string Time;
            public string Platform;
            public string OSVersion;
            public int ProcessorCount;
            public int Screens;
            public string PrimaryScreenResolution;
            public string Directory;
            // ReSharper restore NotAccessedField.Local
            // ReSharper restore NotAccessedField.Global
        }

        public struct SConfigDebug
        {
            [DefaultValue(EDebugLevel.TR_CONFIG_OFF)]
            public EDebugLevel DebugLevel;
            [DefaultValue(EOffOn.TR_CONFIG_OFF)]
            public EOffOn SaveModifiedSongs;
        }

        public struct SConfigGraphics
        {
#if WIN
            [DefaultValue(ERenderer.TR_CONFIG_DIRECT3D)]
            public ERenderer Renderer;
#else
        [DefaultValue(ERenderer.TR_CONFIG_OPENGL)] public ERenderer Renderer;
#endif

            [DefaultValue(ETextureQuality.TR_CONFIG_TEXTURE_HIGH)]
            public ETextureQuality TextureQuality;
            [XmlRanged(32, 1024), DefaultValue(512)]
            public int CoverSize;

            [XmlRanged(1, 6), DefaultValue(1)]
            public int NumScreens;
            [DefaultValue(1920)]
            public int ScreenW;
            [DefaultValue(1080)]
            public int ScreenH;
            [DefaultValue(EGeneralAlignment.Middle)]
            public EGeneralAlignment ScreenAlignment;
            [DefaultValue(0)]
            public int BorderLeft;
            [DefaultValue(0)]
            public int BorderRight;
            [DefaultValue(0)]
            public int BorderTop;
            [DefaultValue(0)]
            public int BorderBottom;
            [DefaultValue(EAntiAliasingModes.X0)]
            public EAntiAliasingModes AAMode;
            [DefaultValue(60f)]
            public float MaxFPS;
            [DefaultValue(EOffOn.TR_CONFIG_ON)]
            public EOffOn VSync;
            [DefaultValue(EOffOn.TR_CONFIG_ON)]
            public EOffOn FullScreen;
            [DefaultValue(EOffOn.TR_CONFIG_OFF)]
            public EOffOn Stretch;
            [DefaultValue(0.4f), XmlRanged(0, 3)]
            public float FadeTime;
        }

        public struct SConfigTheme
        {
            [XmlElement("Name"), DefaultValue("Genius")] public string Theme;
            [DefaultValue("Standard")] public string Skin;
            [XmlElement("Cover"), DefaultValue("Genius Standard")] public string CoverTheme;
            [DefaultValue(EOffOn.TR_CONFIG_ON)] public EOffOn DrawNoteLines;
            [DefaultValue(EOffOn.TR_CONFIG_ON)] public EOffOn DrawToneHelper;
            [DefaultValue(ETimerLook.TR_CONFIG_TIMERLOOK_EXPANDED)] public ETimerLook TimerLook;
            [DefaultValue(EPlayerInfo.TR_CONFIG_PLAYERINFO_BOTH)] public EPlayerInfo PlayerInfo;
            [DefaultValue(EFadePlayerInfo.TR_CONFIG_FADEPLAYERINFO_OFF)] public EFadePlayerInfo FadePlayerInfo;
            [DefaultValue(ECoverLoading.TR_CONFIG_COVERLOADING_DYNAMIC)] public ECoverLoading CoverLoading;
            [DefaultValue(ELyricStyle.TR_CONFIG_LYRICSTYLE_SLIDE)] public ELyricStyle LyricStyle;
        }

        public struct SConfigSound
        {
            [DefaultValue(EPlaybackLib.GstreamerSharp)]
            public EPlaybackLib PlayBackLib;
            [DefaultValue(ERecordLib.PortAudio)]
            public ERecordLib RecordLib;
            [DefaultValue(EBufferSize.B2048)]
            public EBufferSize AudioBufferSize;
            [XmlRanged(-500, 500)]
            public int AudioLatency;
            // ReSharper disable MemberHidesStaticFromOuterClass
            [DefaultValue(EBackgroundMusicOffOn.TR_CONFIG_ON)]
            public EBackgroundMusicOffOn BackgroundMusic;
            [XmlRanged(0, 100), DefaultValue(30)]
            public int BackgroundMusicVolume;
            [DefaultValue(EBackgroundMusicSource.TR_CONFIG_NO_OWN_MUSIC)]
            public EBackgroundMusicSource BackgroundMusicSource;
            [DefaultValue(EOffOn.TR_CONFIG_ON)]
            public EOffOn BackgroundMusicUseStart;
            [XmlRanged(0, 100), DefaultValue(50)]
            public int PreviewMusicVolume;
            [XmlRanged(0, 100), DefaultValue(80)]
            public int GameMusicVolume;
            [DefaultValue(EOffOn.TR_CONFIG_OFF)]
            public EOffOn KaraokeEffect;
            [XmlRanged(0, 1), DefaultValue(1.0f)]
            public float KaraokeEffectLevel;
            // ReSharper restore MemberHidesStaticFromOuterClass
        }

        public struct SConfigGame
        {
            [DefaultValue(CSettings.FallbackLanguage)]
            public string Language;
            public string[] SongFolder;
            // ReSharper disable MemberHidesStaticFromOuterClass
            [DefaultValue(ESongMenu.TR_CONFIG_TILE_BOARD)]
            public ESongMenu SongMenu;
            // ReSharper restore MemberHidesStaticFromOuterClass
            [DefaultValue(ESongSorting.TR_CONFIG_ARTIST)] public ESongSorting SongSorting;
            [DefaultValue(EOffOn.TR_CONFIG_ON)] public EOffOn IgnoreArticles;
            [DefaultValue(ETimerMode.TR_CONFIG_TIMERMODE_REMAINING)] public ETimerMode TimerMode;
            [XmlAltName("NumPlayer"), DefaultValue(2)] public int NumPlayers;
            [DefaultValue(EOffOn.TR_CONFIG_OFF)] public EOffOn Tabs;
            [DefaultValue(EOffOn.TR_CONFIG_OFF)] public EOffOn AutoplayPreviews;
            [XmlAltName("AutoplayPreviewDelay"), DefaultValue(500)] public int AutoplayPreviewDelay;
            [DefaultValue(ELyricsPosition.TR_CONFIG_LYRICSPOSITION_BOTTOM)] public ELyricsPosition LyricsPosition;
            [DefaultValue(0.1f)] public float MinLineBreakTime; //Minimum time to show the text before it is (to be) sung (if possible)
            [XmlArrayItem("Player"), XmlArray] public string[] Players;
            [DefaultValue(EHighscoreStyle.TR_CONFIG_HIGHSCORE_LIST_BEST)] public EHighscoreStyle HighscoreStyle;
        }

        public struct SConfigVideo
        {
            [DefaultValue(EVideoDecoder.FFmpeg)]
            public EVideoDecoder VideoDecoder;
            [DefaultValue(EOffOn.TR_CONFIG_ON)]
            public EOffOn VideoBackgrounds;
            [DefaultValue(EOffOn.TR_CONFIG_ON)]
            public EOffOn VideoPreview;
            [DefaultValue(EOffOn.TR_CONFIG_ON)]
            public EOffOn VideosInSongs;
            [DefaultValue(EOffOn.TR_CONFIG_OFF)]
            public EOffOn VideosToBackground;
            [DefaultValue(EWebcamLib.AForgeNet)]
            public EWebcamLib WebcamLib;
            public SWebcamConfig? WebcamConfig;
        }

        public struct SConfigRecord
        {
            public SMicConfig[] MicConfig;
            [DefaultValue(200)]
            public int MicDelay; //[ms]
        }

        public struct SConfigServer
        {
            [DefaultValue(EOffOn.TR_CONFIG_OFF)]
            public EOffOn ServerActive;
            [DefaultValue(EOffOn.TR_CONFIG_OFF)]
            public EOffOn ServerEncryption;
            [DefaultValue(3000)]
            public int ServerPort;
            [DefaultValue(70)]
            public int SongCountCoverThreshold;
        }
#pragma warning restore 649
        // ReSharper restore UnassignedField.Global

        public struct SConfig
        {
            // ReSharper disable NotAccessedField.Global
            public SConfigInfo? Info;
            // ReSharper restore NotAccessedField.Global
            public SConfigDebug Debug;
            public SConfigGraphics Graphics;
            public SConfigTheme Theme;
            public SConfigSound Sound;
            public SConfigGame Game;
            public SConfigVideo Video;
            public SConfigRecord Record;
            public SConfigServer Server;
        }

        public static SConfig Config;
        public static event OnSongMenuChanged SongMenuChanged;

        //Folders
        //We need full path for folders for matching folder path with textfile path when loading
        public static readonly List<string> SongFolders = new List<string>
            {
#if INSTALLER
            Path.Combine(CSettings.DataFolder, CSettings.FolderNameSongs),
            Path.Combine(CSettings.ProgramFolder, FolderNameSongs)
#elif WIN
            Path.Combine(CSettings.ProgramFolder, CSettings.FolderNameSongs)
#elif LINUX
            Path.Combine(CSettings.DataFolder, CSettings.FolderNameSongs)
#endif
            };
        /// <summary>
        ///     Folders with profiles
        ///     First one is used for new profiles
        /// </summary>
        public static readonly List<string> ProfileFolders = new List<string>
            {
#if INSTALLER
            Path.Combine(CSettings.DataFolder, CSettings.FolderNameProfiles),
            CSettings.FolderNameProfiles
#elif WIN
            CSettings.FolderNameProfiles
#elif LINUX
            Path.Combine(CSettings.DataFolder, CSettings.FolderNameProfiles)
#endif
        };

        //Lists to save parameters and values
        private static readonly List<string> _Params = new List<string>();
        private static readonly List<string> _Values = new List<string>();

        public static int GetVolumeByType(EMusicType type)
        {
            switch (type)
            {
                case EMusicType.Preview:
                    return PreviewMusicVolume;
                case EMusicType.Game:
                    return GameMusicVolume;
                case EMusicType.BackgroundPreview:
                case EMusicType.Background:
                    return BackgroundMusicVolume;
                case EMusicType.None:
                    return 0;
                default:
                    throw new ArgumentException("Invalid type: " + type);
            }
        }

        public static void SetVolumeByType(EMusicType type, int volume)
        {
            switch (type)
            {
                case EMusicType.Preview:
                    PreviewMusicVolume = volume;
                    break;
                case EMusicType.Game:
                    GameMusicVolume = volume;
                    break;
                case EMusicType.BackgroundPreview:
                case EMusicType.Background:
                    BackgroundMusicVolume = volume;
                    break;
                default:
                    throw new ArgumentException("Invalid type: " + type);
            }
        }

        public static int BackgroundMusicVolume
        {
            get { return Config.Sound.BackgroundMusicVolume; }
            set
            {
                Config.Sound.BackgroundMusicVolume = value.Clamp(0, 100);
                SaveConfig();
            }
        }

        public static int GameMusicVolume
        {
            get { return Config.Sound.GameMusicVolume; }
            set
            {
                Config.Sound.GameMusicVolume = value.Clamp(0, 100);
                SaveConfig();
            }
        }

        public static int PreviewMusicVolume
        {
            get { return Config.Sound.PreviewMusicVolume; }
            set
            {
                Config.Sound.PreviewMusicVolume = value.Clamp(0, 100);
                SaveConfig();
            }
        }

        public static ESongMenu SongMenu
        {
            get { return Config.Game.SongMenu; }
            set
            {
                if (Config.Game.SongMenu == value)
                    return;
                Config.Game.SongMenu = value;
                if (SongMenuChanged != null)
                    SongMenuChanged();
            }
        }

        public static void Init()
        {
            if (_Initialized)
                return;

            // Init config file
            _LoadConfig();
            if (!File.Exists(_FileConfig))
                SaveConfig();

            _Initialized = true;
        }

        private static bool _XmlErrorsOccured;

        private static void _HandleXmlError(CXmlException e)
        {
            _XmlErrorsOccured = true;
        }

        private static void _LoadConfig()
        {
            _XmlErrorsOccured = false;
            var xml = new CXmlDeserializer(new CXmlErrorHandler(_HandleXmlError));
            if (File.Exists(_FileConfig))
            {
                Config = xml.Deserialize<SConfig>(_FileConfig);
                if (_XmlErrorsOccured)
                    CLog.Error("There were some warnings or errors loading the config file. Some values might have been reset to their defaults.");
            }
            else
                Config = xml.DeserializeString<SConfig>("<root />");

            NormalizeSongPaths();

            if (Config.Game.MinLineBreakTime < 0)
                Config.Game.MinLineBreakTime = 0.1f;

            if (!Config.Game.NumPlayers.IsInRange(1, CSettings.MaxNumPlayer))
                Config.Game.NumPlayers = 2;
            Array.Resize(ref Config.Game.Players, CSettings.MaxNumPlayer);

            if (!Config.Graphics.NumScreens.IsInRange(1, CSettings.MaxNumScreens))
                Config.Graphics.NumScreens = 1;

            bool langExists = CLanguage.SetLanguage(Config.Game.Language);

            if (langExists == false)
            {
                Config.Game.Language = CSettings.FallbackLanguage;
                CLanguage.SetLanguage(Config.Game.Language);
            }

            Array.Resize(ref Config.Record.MicConfig, CSettings.MaxNumPlayer);
            Config.Record.MicDelay = (int)(20 * Math.Round(Config.Record.MicDelay / 20.0));

            if (!Config.Server.ServerPort.IsInRange(1, 65535))
                Config.Server.ServerPort = 3000;

            Config.Info = new SConfigInfo
            {
                Version = CSettings.GetFullVersionText(),
                Time = DateTime.Now.ToString(),
                Platform = Environment.OSVersion.Platform.ToString(),
                OSVersion = Environment.OSVersion.ToString(),
                ProcessorCount = Environment.ProcessorCount,
                Screens = Screen.AllScreens.Length,
                PrimaryScreenResolution = Screen.PrimaryScreen.Bounds.Size.ToString(),
                Directory = CSettings.ProgramFolder
            };
        }

        private static readonly List<string> _CommentsGot = new List<string>();

        private static string _GetComment(string elName)
        {
            // Avoid multiple comments (e.g. for array entries)
            if (_CommentsGot.Contains(elName))
                return null;
            _CommentsGot.Add(elName);
            switch (elName)
            {
                case "DebugLevel":
                    return "DebugLevel: " + CHelper.ListStrings(Enum.GetNames(typeof(EDebugLevel)));
                case "SaveModifiedSongs":
                    return "SaveModifiedSongs: " + CHelper.ListStrings(Enum.GetNames(typeof(EOffOn)));
                case "Renderer":
                    return "Renderer: " + CHelper.ListStrings(Enum.GetNames(typeof(ERenderer)));
                case "TextureQuality":
                    return "TextureQuality: " + CHelper.ListStrings(Enum.GetNames(typeof(ETextureQuality)));
                case "CoverSize":
                    return "CoverSize (pixels): 32, 64, 128, 256, 512, 1024 (default: 128)";
                case "NumScreens":
                    return "Number of screens to use. 1 - 6 (default: 1)";
                case "ScreenW":
                    return "Screen width and height (pixels)";
                case "BorderLeft":
                    return "Screen borders (pixels)";
                case "AAMode":
                    return "AAMode: " + CHelper.ListStrings(Enum.GetNames(typeof(EAntiAliasingModes)));
                case "MaxFPS":
                    return "MaxFPS should be between 1..200";
                case "VSync":
                    return "VSync: " + CHelper.ListStrings(Enum.GetNames(typeof(EOffOn)));
                case "FullScreen":
                    return "FullScreen: " + CHelper.ListStrings(Enum.GetNames(typeof(EOffOn)));
                case "FadeTime":
                    return "FadeTime should be between 0..3 Seconds";
                case "Theme":
                    return "Theme name";
                case "Skin":
                    return "Skin name";
                case "Cover":
                    return "Name of cover-theme";
                case "DrawNoteLines":
                    return "Draw note-lines: " + CHelper.ListStrings(Enum.GetNames(typeof(EOffOn)));
                case "DrawToneHelper":
                    return "Draw tone-helper: " + CHelper.ListStrings(Enum.GetNames(typeof(EOffOn)));
                case "TimerLook":
                    return "Look of timer: " + CHelper.ListStrings(Enum.GetNames(typeof(ETimerLook)));
                case "PlayerInfo":
                    return "Information about players on SingScreen: " + CHelper.ListStrings(Enum.GetNames(typeof(EPlayerInfo)));
                case "FadePlayerInfo":
                    return "Fade player-information with lyrics and notebars: " + CHelper.ListStrings(Enum.GetNames(typeof(EFadePlayerInfo)));
                case "CoverLoading":
                    return "Cover Loading: " + CHelper.ListStrings(Enum.GetNames(typeof(ECoverLoading)));
                case "LyricStyle":
                    return "Lyric Style: " + CHelper.ListStrings(Enum.GetNames(typeof(ELyricStyle)));
                case "PlayBackLib":
                    return "PlayBackLib: " + CHelper.ListStrings(Enum.GetNames(typeof(EPlaybackLib)));
                case "RecordLib":
                    return "RecordLib: " + CHelper.ListStrings(Enum.GetNames(typeof(ERecordLib)));
                case "AudioBufferSize":
                    return "AudioBufferSize: " + CHelper.ListStrings(Enum.GetNames(typeof(EBufferSize)));
                case "AudioLatency":
                    return "AudioLatency from -500 to 500 ms";
                case "BackgroundMusic":
                    return "Background Music: " + CHelper.ListStrings(Enum.GetNames(typeof(EBackgroundMusicOffOn)));
                case "BackgroundMusicVolume":
                    return "Background Music Volume from 0 to 100";
                case "BackgroundMusicSource":
                    return "Background Music Source: " + CHelper.ListStrings(Enum.GetNames(typeof(EBackgroundMusicSource)));
                case "BackgroundMusicUseStart":
                    return "Background Music use start-tag of songs " + CHelper.ListStrings(Enum.GetNames(typeof(EOffOn)));
                case "PreviewMusicVolume":
                    return "Preview Volume from 0 to 100";
                case "GameMusicVolume":
                    return "Game Volume from 0 to 100";
                case "KaraokeEffect":
                    return "Apply a karaoke effect to song playback (EPlaybackLib.GstreamerSharp only)";
                case "KaraokeEffectLevel":
                    return "The level of the karaoke effect [0-1] (EPlaybackLib.GstreamerSharp only)";
                case "Language":
                    return "Game-Language";
                case "SongFolder":
                    return "SongFolder: Add each song folder in a seperate SongFolder entry";
                case "SongMenu":
                    return "SongMenu: " + CHelper.ListStrings(Enum.GetNames(typeof(ESongMenu)));
                case "SongSorting":
                    return "SongSorting: " + CHelper.ListStrings(Enum.GetNames(typeof(ESongSorting)));
                case "IgnoreArticles":
                    return "Ignore articles on song-sorting: " + CHelper.ListStrings(Enum.GetNames(typeof(EOffOn)));
                case "TimerMode":
                    return "TimerMode: " + CHelper.ListStrings(Enum.GetNames(typeof(ETimerMode)));
                case "NumPlayers":
                    return "NumPlayers: 1.." + CSettings.MaxNumPlayer;
                case "Tabs":
                    return "Order songs in tabs: " + CHelper.ListStrings(Enum.GetNames(typeof(EOffOn)));
                case "AutoplayPreviews":
                    return "Automatically play song preview when selected: " + CHelper.ListStrings(Enum.GetNames(typeof(EOffOn)));
                case "AutoplayPreviewDelay":
                    return "Delay before playback of autoplay previews is started in milliseconds";
                case "LyricsPosition":
                    return "Position if lyrics on screen: " + CHelper.ListStrings(Enum.GetNames(typeof(ELyricsPosition)));
                case "MinLineBreakTime":
                    return "MinLineBreakTime: Value >= 0 in s. Minimum time the text is shown before it is to be sung";
                case "Players":
                    return "Default profile for players 1..." + CSettings.MaxNumPlayer + ":";
                case "VideoDecoder":
                    return "VideoDecoder: " + CHelper.ListStrings(Enum.GetNames(typeof(EVideoDecoder)));
                case "VideoBackgrounds":
                    return "VideoBackgrounds: " + CHelper.ListStrings(Enum.GetNames(typeof(EOffOn)));
                case "VideoPreview":
                    return "VideoPreview: " + CHelper.ListStrings(Enum.GetNames(typeof(EOffOn)));
                case "VideosInSongs":
                    return "Show Videos while singing: " + CHelper.ListStrings(Enum.GetNames(typeof(EOffOn)));
                case "VideosToBackground":
                    return "Show backgroundmusic videos as background: " + CHelper.ListStrings(Enum.GetNames(typeof(EOffOn)));
                case "WebcamLib":
                    return "WebcamLib: " + CHelper.ListStrings(Enum.GetNames(typeof(EWebcamLib)));
                case "MicDelay":
                    return "Mic delay in ms. 0, 20, 40, 60 ... 500";
                case "ServerActive":
                    return "Server On/Off: " + CHelper.ListStrings(Enum.GetNames(typeof(EOffOn)));
                case "ServerEncryption":
                    return "Server Encryption On/Off: " + CHelper.ListStrings(Enum.GetNames(typeof(EOffOn)));
                case "ServerPort":
                    return "Server Port (default: 3000) [1..65535]";
                case "SongCountCoverThreshold":
                    return "Threshold of songs for that covers will not longer be included in get-all-songs-requests (e.g. song list) (default: 70) [-1..65535] -1 => always deliver covers";
                case "Stretch":
                    return "Stretch view to full window size: " + CHelper.ListStrings(Enum.GetNames(typeof(EOffOn)));
                default:
                    return null;
            }
        }

        public static void SaveConfig()
        {
            _CommentsGot.Clear();
            var xml = new CXmlSerializer(true, _GetComment);
            xml.Serialize(_FileConfig, Config);
        }

        /// <summary>
        ///     Calculates the maximum cycle time to reach the MaxFPS value.
        /// </summary>
        public static float CalcCycleTime()
        {
            return (1f / Config.Graphics.MaxFPS) * 1000f;
        }

        /// <summary>
        ///     Checks, if there is a mic-configuration
        /// </summary>
        /// <param name="player">Player-Number</param>
        /// <returns></returns>
        public static bool IsMicConfig(int player = 0)
        {
            ReadOnlyCollection<CRecordDevice> devices = CRecord.GetDevices();
            if (devices == null)
                return false;

            if (player > 0)
                return devices.Any(t => t.PlayerChannel.Contains(player));

            for (int p = 0; p < CSettings.MaxNumPlayer; ++p)
                if (devices.Any(t => t.PlayerChannel.Contains(p + 1)))
                {
                    return true;
                }

            return false;
        }

        public static int GetMaxNumMics()
        {
            int max = 0;
            for (int i = 1; i <= CSettings.MaxNumPlayer; i++)
            {
                if (IsMicConfig(i))
                    max = i;
                else
                    break;
            }
            return max;
        }

        public static int GetNumScreens()
        {
            return Config.Graphics.NumScreens;
        }

        /// <summary>
        ///     Try to assign automatically player 1 and 2 to usb-mics.
        /// </summary>
        public static bool AutoAssignMics()
        {
            //Look for (usb-)mic
            //SRecordDevice[] Devices = new SRecordDevice[CRecord.RecordGetDevices().Length];
            ReadOnlyCollection<CRecordDevice> devices = CRecord.GetDevices();
            if (devices == null)
                return false;

            foreach (CRecordDevice device in devices)
            {
                //Has Device some signal-names in name -> This could be a (usb-)mic
                if (Regex.IsMatch(device.Name, "Usb|Wireless", RegexOptions.IgnoreCase))
                {
                    //Check if there is one or more channels
                    if (device.Channels >= 2)
                    {
                        //Set this device to player 1
                        Config.Record.MicConfig[0].DeviceName = device.Name;
                        Config.Record.MicConfig[0].DeviceDriver = device.Driver;
                        Config.Record.MicConfig[0].Channel = 1;
                        //Set this device to player 2
                        Config.Record.MicConfig[1].DeviceName = device.Name;
                        Config.Record.MicConfig[1].DeviceDriver = device.Driver;
                        Config.Record.MicConfig[1].Channel = 2;

                        return true;
                    }
                }
            }
            //If no usb-mics found -> Look for Devices with "mic" or "mik" 
            foreach (CRecordDevice device in devices)
            {
                //Has Device some signal-names in name -> This could be a mic
                if (Regex.IsMatch(device.Name, "Mic|Mik", RegexOptions.IgnoreCase))
                {
                    //Check if there is one or more channels
                    if (device.Channels >= 1)
                    {
                        //Set this device to player 1
                        Config.Record.MicConfig[0].DeviceName = device.Name;
                        Config.Record.MicConfig[0].DeviceDriver = device.Driver;
                        Config.Record.MicConfig[0].Channel = 1;

                        if (device.Channels >= 2)
                        {
                            //Set this device to player 2
                            Config.Record.MicConfig[1].DeviceName = device.Name;
                            Config.Record.MicConfig[1].DeviceDriver = device.Driver;
                            Config.Record.MicConfig[1].Channel = 2;

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///     Set song path if none is find in config and check if every path exists
        /// </summary>
        public static void NormalizeSongPaths()
        {
            //Check if there are configured songfolders
            if (Config.Game.SongFolder.Length > 0 && Config.Game.SongFolder[0] != "")
            {
                SongFolders.Clear();

                foreach (string folder in Config.Game.SongFolder)
                {
                    //Check if folder exists
                    if (Directory.Exists(folder))
                        SongFolders.Add(Path.GetFullPath(folder));
                }
            }
            
            //Test if songfolders are still empty or now empty
            if(Config.Game.SongFolder.Length == 0)
                Config.Game.SongFolder = SongFolders.ToArray();
        }

        /// <summary>
        ///     Load command-line-parameters and their values to lists.
        /// </summary>
        /// <param name="args">Parameters</param>
        public static void LoadCommandLineParams(string[] args)
        {
            Regex spliterParam = new Regex(@"-{1,2}|\/", RegexOptions.IgnoreCase);

            //Complete argument string
            string arguments = args.Aggregate(String.Empty, (current, arg) => current + arg + " ");

            args = spliterParam.Split(arguments);

            foreach (string text in args)
            {
                Regex spliterVal = new Regex(@"\s", RegexOptions.IgnoreCase);

                //split arg with Spilter-Regex and save in parts
                string[] parts = spliterVal.Split(text.Trim(), 2);

                switch (parts.Length)
                {
                    //Only found a parameter
                    case 1:
                        if (parts[0] != "")
                        {
                            //Add parameter
                            _Params.Add(parts[0]);

                            //Add value
                            _Values.Add("");
                        }
                        break;


                    //Found parameter and value
                    case 2:
                        if (parts[0] != "")
                        {
                            //Add parameter
                            _Params.Add(parts[0]);

                            //Add value
                            _Values.Add(parts[1]);
                        }
                        break;
                }
            }
        }

        /// <summary>
        ///     Apply command-line-parameters (before reading config.xml)
        /// </summary>
        public static void UseCommandLineParamsBefore()
        {
            //Check each parameter
            for (int i = 0; i < _Params.Count; i++)
            {
                //Switch parameter to lower case
                string param = _Params[i].ToLower();

                string value = _Values[i];

                switch (param)
                {
                    case "configfile":
                        //Check if value is valid                      
                        if (_CheckFile(value))
                            _FileConfig = value;
                        break;

                    case "scorefile":
                        //Check if value is valid
                        if (_CheckFile(value))
                            FileHighscoreDB = value;
                        break;

                    case "playlistfolder":
                        FolderPlaylists = value;
                        break;

                    case "profilefolder":
                        ProfileFolders.Clear();
                        ProfileFolders.Add(value);
                        break;
                }
            }
        }

        /// <summary>
        ///     Apply command-line-parameters (after reading config.xml)
        /// </summary>
        public static void UseCommandLineParamsAfter()
        {
            bool songFoldersOverwritten = false;
            //Check each parameter
            for (int i = 0; i < _Params.Count; i++)
            {
                //Switch parameter as lower case
                string param = _Params[i];

                param = param.ToLower();

                string value = _Values[i];

                switch (param)
                {
                    case "songfolder":
                    case "songpath":
                        if (!songFoldersOverwritten)
                        {
                            SongFolders.Clear();
                            songFoldersOverwritten = true;
                        }
                        if (!SongFolders.Contains(value))
                            SongFolders.Add(value);
                        break;
                }
            }
        }

        private static bool _CheckFile(string value)
        {
            char[] chars = Path.GetInvalidPathChars();
            for (int i = 0; i < chars.Length; i++)
            {
                if (value.Contains(chars[i].ToString()))
                    return false;
            }
            return !String.IsNullOrEmpty(Path.GetFileName(value)) && Path.HasExtension(value);
        }

        /// <summary>
        ///     Use saved players from config now for games
        /// </summary>
        public static void UsePlayers()
        {
            CProfile[] profiles = CProfiles.GetProfiles();

            for (int j = 0; j < CSettings.MaxNumPlayer; j++)
            {
                CGame.Players[j].ProfileID = Guid.Empty;
                if (string.IsNullOrEmpty(Config.Game.Players[j]))
                    continue;

                foreach (CProfile profile in profiles)
                {
                    if (Path.GetFileName(profile.FilePath) == Config.Game.Players[j] && profile.Active == EOffOn.TR_CONFIG_ON)
                    {
                        //Update Game-infos with player
                        CGame.Players[j].ProfileID = profile.ID;
                    }
                }
            }
        }
    }
}
