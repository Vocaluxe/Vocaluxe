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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using Vocaluxe.Lib.Sound;
using Vocaluxe.Lib.Webcam;
using VocaluxeLib;
using VocaluxeLib.Profile;

namespace Vocaluxe.Base
{
    static class CConfig
    {
        /// <summary>
        /// Uniform settings for writing XML files. ALWAYS use this!
        /// </summary>
        public static readonly XmlWriterSettings XMLSettings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.UTF8,
                ConformanceLevel = ConformanceLevel.Document
            };

        // Debug
        public static EDebugLevel DebugLevel = EDebugLevel.TR_CONFIG_OFF;

        // Graphics
#if WIN
        public static ERenderer Renderer = ERenderer.TR_CONFIG_DIRECT3D;
#else
        public static ERenderer Renderer = ERenderer.TR_CONFIG_OPENGL;
#endif

        public static ETextureQuality TextureQuality = ETextureQuality.TR_CONFIG_TEXTURE_MEDIUM;
        public static float MaxFPS = 60f;

        public static int ScreenW = 1024;
        public static int ScreenH = 576;

        public static EAntiAliasingModes AAMode = EAntiAliasingModes.X0;

        public static EOffOn VSync = EOffOn.TR_CONFIG_ON;
        public static EOffOn FullScreen = EOffOn.TR_CONFIG_ON;

        public static float FadeTime = 0.4f;
        public static int CoverSize = 128;

        // Theme
        public static string Theme = "Ambient";
        public static string Skin = "Blue";
        public static string CoverTheme = "Blue";
        public static EOffOn DrawNoteLines = EOffOn.TR_CONFIG_ON;
        public static EOffOn DrawToneHelper = EOffOn.TR_CONFIG_ON;
        public static ETimerLook TimerLook = ETimerLook.TR_CONFIG_TIMERLOOK_EXPANDED;
        public static EPlayerInfo PlayerInfo = EPlayerInfo.TR_CONFIG_PLAYERINFO_BOTH;
        public static EFadePlayerInfo FadePlayerInfo = EFadePlayerInfo.TR_CONFIG_FADEPLAYERINFO_OFF;
        public static ECoverLoading CoverLoading = ECoverLoading.TR_CONFIG_COVERLOADING_DYNAMIC;
        public static ELyricStyle LyricStyle = ELyricStyle.Slide;

        // Sound
        public static EPlaybackLib PlayBackLib = EPlaybackLib.Gstreamer;
        public static ERecordLib RecordLib = ERecordLib.PortAudio;
        public static EBufferSize AudioBufferSize = EBufferSize.B2048;
        public static int AudioLatency;
        private static int _BackgroundMusicVolume = 30;
        public static EOffOn BackgroundMusic = EOffOn.TR_CONFIG_ON;
        public static EBackgroundMusicSource BackgroundMusicSource = EBackgroundMusicSource.TR_CONFIG_NO_OWN_MUSIC;
        public static EOffOn BackgroundMusicUseStart = EOffOn.TR_CONFIG_ON;
        public static int PreviewMusicVolume = 50;
        public static int GameMusicVolume = 80;

        // Game
        public static readonly List<string> SongFolder = new List<string>();
        public static ESongMenu SongMenu = ESongMenu.TR_CONFIG_TILE_BOARD;
        public static ESongSorting SongSorting = ESongSorting.TR_CONFIG_ARTIST;
        public static EOffOn IgnoreArticles = EOffOn.TR_CONFIG_ON;
        public static float ScoreAnimationTime = 10;
        public static ETimerMode TimerMode = ETimerMode.TR_CONFIG_TIMERMODE_REMAINING;
        public static int NumPlayer = 2;
        public static EOffOn Tabs = EOffOn.TR_CONFIG_OFF;
        public static string Language = "English";
        public static ELyricsPosition LyricsPosition = ELyricsPosition.TR_CONFIG_LYRICSPOSITION_BOTTOM;
        public static readonly string[] Players = new string[CSettings.MaxNumPlayer];

        public static float MinLineBreakTime = 0.1f; //Minimum time to show the text before it is (to be) sung (if possible)

        // Video
        public static EVideoDecoder VideoDecoder = EVideoDecoder.FFmpeg;
        public static EOffOn VideoBackgrounds = EOffOn.TR_CONFIG_ON;
        public static EOffOn VideoPreview = EOffOn.TR_CONFIG_ON;
        public static EOffOn VideosInSongs = EOffOn.TR_CONFIG_ON;
        public static EOffOn VideosToBackground = EOffOn.TR_CONFIG_OFF;

        // Record
        public static SMicConfig[] MicConfig;
        public static int MicDelay = 300; //[ms]
        public static EWebcamLib WebcamLib = EWebcamLib.OpenCV;
        public static SWebcamConfig WebcamConfig;

        // Server
        public static EOffOn ServerActive = EOffOn.TR_CONFIG_OFF;
        public static EOffOn ServerEncryption = EOffOn.TR_CONFIG_OFF;
        public static int ServerPort = 3000;
        public static string ServerPassword = "vocaluxe";

        //Lists to save parameters and values
        private static readonly List<string> _Params = new List<string>();
        private static readonly List<string> _Values = new List<string>();

        //Variables to save old values for commandline-parameters
        private static readonly List<string> _SongFolderOld = new List<string>();
        public static int BackgroundMusicVolume
        {
            get { return _BackgroundMusicVolume; }
            set
            {
                if (value < 0)
                    _BackgroundMusicVolume = 0;
                else if (value > 100)
                    _BackgroundMusicVolume = 100;
                else
                    _BackgroundMusicVolume = value;
                SaveConfig();
            }
        }

        public static void Init()
        {
            SongFolder.Add(Path.Combine(Directory.GetCurrentDirectory(), CSettings.FolderSongs));

#if INSTALLER
            SongFolder.Add(Path.Combine(CSettings.DataPath, CSettings.FolderSongs));            
#endif

            foreach (string folder in SongFolder)
                CSettings.CreateFolder(folder);

            MicConfig = new SMicConfig[CSettings.MaxNumPlayer];

            // Init config file
            if (!File.Exists(Path.Combine(CSettings.DataPath,CSettings.FileConfig)))
                SaveConfig();

            _LoadConfig();
        }

        private static void _LoadConfig()
        {
            CXMLReader xmlReader = CXMLReader.OpenFile(Path.Combine(CSettings.DataPath, CSettings.FileConfig));
            if (xmlReader == null)
                return;

            xmlReader.TryGetEnumValue("//root/Debug/DebugLevel", ref DebugLevel);

            #region Graphics
            xmlReader.TryGetEnumValue("//root/Graphics/Renderer", ref Renderer);
            xmlReader.TryGetEnumValue("//root/Graphics/TextureQuality", ref TextureQuality);
            xmlReader.TryGetIntValueRange("//root/Graphics/CoverSize", ref CoverSize, 32, 1024);

            xmlReader.TryGetIntValue("//root/Graphics/ScreenW", ref ScreenW);
            xmlReader.TryGetIntValue("//root/Graphics/ScreenH", ref ScreenH);
            xmlReader.TryGetEnumValue("//root/Graphics/AAMode", ref AAMode);
            xmlReader.TryGetFloatValue("//root/Graphics/MaxFPS", ref MaxFPS);
            xmlReader.TryGetEnumValue("//root/Graphics/VSync", ref VSync);
            xmlReader.TryGetEnumValue("//root/Graphics/FullScreen", ref FullScreen);
            xmlReader.TryGetFloatValue("//root/Graphics/FadeTime", ref FadeTime);
            #endregion Graphics

            #region Theme
            xmlReader.GetValue("//root/Theme/Name", out Theme, Theme);
            xmlReader.GetValue("//root/Theme/Skin", out Skin, Skin);
            xmlReader.GetValue("//root/Theme/Cover", out CoverTheme, CoverTheme);
            xmlReader.TryGetEnumValue("//root/Theme/DrawNoteLines", ref DrawNoteLines);
            xmlReader.TryGetEnumValue("//root/Theme/DrawToneHelper", ref DrawToneHelper);
            xmlReader.TryGetEnumValue("//root/Theme/TimerLook", ref TimerLook);
            xmlReader.TryGetEnumValue("//root/Theme/PlayerInfo", ref PlayerInfo);
            xmlReader.TryGetEnumValue("//root/Theme/FadePlayerInfo", ref FadePlayerInfo);
            xmlReader.TryGetEnumValue("//root/Theme/CoverLoading", ref CoverLoading);
            xmlReader.TryGetEnumValue("//root/Theme/LyricStyle", ref LyricStyle);
            #endregion Theme

            #region Sound
            xmlReader.TryGetEnumValue("//root/Sound/PlayBackLib", ref PlayBackLib);
            xmlReader.TryGetEnumValue("//root/Sound/RecordLib", ref RecordLib);
            xmlReader.TryGetEnumValue("//root/Sound/AudioBufferSize", ref AudioBufferSize);

            xmlReader.TryGetIntValueRange("//root/Sound/AudioLatency", ref AudioLatency, -500, 500);

            xmlReader.TryGetEnumValue("//root/Sound/BackgroundMusic", ref BackgroundMusic);
            xmlReader.TryGetIntValueRange("//root/Sound/BackgroundMusicVolume", ref _BackgroundMusicVolume);
            xmlReader.TryGetEnumValue("//root/Sound/BackgroundMusicSource", ref BackgroundMusicSource);
            xmlReader.TryGetEnumValue("//root/Sound/BackgroundMusicUseStart", ref BackgroundMusicUseStart);
            xmlReader.TryGetIntValueRange("//root/Sound/PreviewMusicVolume", ref PreviewMusicVolume);
            xmlReader.TryGetIntValueRange("//root/Sound/GameMusicVolume", ref GameMusicVolume);
            #endregion Sound

            #region Game
            // Songfolder
            string value = string.Empty;
            int i = 1;
            while (xmlReader.GetValue("//root/Game/SongFolder" + i, out value, value))
            {
                if (i == 1)
                    SongFolder.Clear();

                SongFolder.Add(value);
                value = string.Empty;
                i++;
            }

            xmlReader.TryGetEnumValue("//root/Game/SongMenu", ref SongMenu);
            xmlReader.TryGetEnumValue("//root/Game/SongSorting", ref SongSorting);
            xmlReader.TryGetEnumValue("//root/Game/IgnoreArticles", ref IgnoreArticles);
            xmlReader.TryGetFloatValue("//root/Game/ScoreAnimationTime", ref ScoreAnimationTime);
            xmlReader.TryGetEnumValue("//root/Game/TimerMode", ref TimerMode);
            xmlReader.TryGetIntValue("//root/Game/NumPlayer", ref NumPlayer);
            xmlReader.TryGetEnumValue("//root/Game/Tabs", ref Tabs);
            xmlReader.GetValue("//root/Game/Language", out Language, Language);
            xmlReader.TryGetEnumValue("//root/Game/LyricsPosition", ref LyricsPosition);
            xmlReader.TryGetFloatValue("//root/Game/MinLineBreakTime", ref MinLineBreakTime);

            if ((ScoreAnimationTime > 0 && ScoreAnimationTime < 1) || ScoreAnimationTime < 0)
                ScoreAnimationTime = 1;

            if (MinLineBreakTime < 0)
                MinLineBreakTime = 0.1f;

            if (NumPlayer < 1 || NumPlayer > CSettings.MaxNumPlayer)
                NumPlayer = 2;

            bool langExists = CLanguage.SetLanguage(Language);

            //TODO: What should we do, if English not exists?
            if (langExists == false)
                Language = "English";

            //Read players from config
            for (i = 1; i <= CSettings.MaxNumPlayer; i++)
                xmlReader.GetValue("//root/Game/Players/Player" + i, out Players[i - 1], string.Empty);
            #endregion Game

            #region Video
            xmlReader.TryGetEnumValue("//root/Video/VideoDecoder", ref VideoDecoder);
            xmlReader.TryGetEnumValue("//root/Video/VideoBackgrounds", ref VideoBackgrounds);
            xmlReader.TryGetEnumValue("//root/Video/VideoPreview", ref VideoPreview);
            xmlReader.TryGetEnumValue("//root/Video/VideosInSongs", ref VideosInSongs);
            xmlReader.TryGetEnumValue("//root/Video/VideosToBackground", ref VideosToBackground);

            xmlReader.TryGetEnumValue("//root/Video/WebcamLib", ref WebcamLib);
            WebcamConfig = new SWebcamConfig();
            xmlReader.GetValue("//root/Video/WebcamConfig/MonikerString", out WebcamConfig.MonikerString, String.Empty);
            xmlReader.TryGetIntValue("//root/Video/WebcamConfig/Framerate", ref WebcamConfig.Framerate);
            xmlReader.TryGetIntValue("//root/Video/WebcamConfig/Width", ref WebcamConfig.Width);
            xmlReader.TryGetIntValue("//root/Video/WebcamConfig/Height", ref WebcamConfig.Height);
            #endregion Video

            #region Record
            MicConfig = new SMicConfig[CSettings.MaxNumPlayer];
            for (int p = 1; p <= CSettings.MaxNumPlayer; p++)
            {
                MicConfig[p - 1] = new SMicConfig(0);
                xmlReader.GetValue("//root/Record/MicConfig" + p + "/DeviceName", out MicConfig[p - 1].DeviceName, String.Empty);
                xmlReader.GetValue("//root/Record/MicConfig" + p + "/DeviceDriver", out MicConfig[p - 1].DeviceDriver, String.Empty);
                xmlReader.TryGetIntValue("//root/Record/MicConfig" + p + "/Channel", ref MicConfig[p - 1].Channel);
            }

            xmlReader.TryGetIntValueRange("//root/Record/MicDelay", ref MicDelay, 0, 500);
            MicDelay = (int)(20 * Math.Round(MicDelay / 20.0));
            #endregion Record

            #region Server
            xmlReader.TryGetEnumValue("//root/Server/ServerActive", ref ServerActive);
            xmlReader.TryGetEnumValue("//root/Server/ServerEncryption", ref ServerEncryption);
            xmlReader.TryGetIntValue("//root/Server/ServerPort", ref ServerPort);
            if (ServerPort < 1 || ServerPort > 65535)
                ServerPort = 3000;

            xmlReader.GetValue("//root/Server/ServerPassword", out ServerPassword, ServerPassword);
            #endregion Server
        }

        public static bool SaveConfig()
        {
            XmlWriter writer = null;
            try
            {
                writer = XmlWriter.Create(Path.Combine(CSettings.DataPath, CSettings.FileConfig), XMLSettings);
                writer.WriteStartDocument();
                writer.WriteStartElement("root");


                writer.WriteStartElement("Info");

                writer.WriteElementString("Version", CSettings.GetFullVersionText());
                writer.WriteElementString("Time", DateTime.Now.ToString());
                writer.WriteElementString("Platform", Environment.OSVersion.Platform.ToString());
                writer.WriteElementString("OSVersion", Environment.OSVersion.ToString());
                writer.WriteElementString("ProcessorCount", Environment.ProcessorCount.ToString());

                writer.WriteElementString("Screens", Screen.AllScreens.Length.ToString());
                writer.WriteElementString("PrimaryScreenResolution", Screen.PrimaryScreen.Bounds.Size.ToString());

                writer.WriteElementString("Directory", Environment.CurrentDirectory);

                writer.WriteEndElement();

                #region Debug
                writer.WriteStartElement("Debug");

                writer.WriteComment("DebugLevel: " + CHelper.ListStrings(Enum.GetNames(typeof(EDebugLevel))));
                writer.WriteElementString("DebugLevel", Enum.GetName(typeof(EDebugLevel), DebugLevel));

                writer.WriteEndElement();
                #endregion Debug

                #region Graphics
                writer.WriteStartElement("Graphics");

                writer.WriteComment("Renderer: " + CHelper.ListStrings(Enum.GetNames(typeof(ERenderer))));
                writer.WriteElementString("Renderer", Enum.GetName(typeof(ERenderer), Renderer));

                writer.WriteComment("TextureQuality: " + CHelper.ListStrings(Enum.GetNames(typeof(ETextureQuality))));
                writer.WriteElementString("TextureQuality", Enum.GetName(typeof(ETextureQuality), TextureQuality));

                writer.WriteComment("CoverSize (pixels): 32, 64, 128, 256, 512, 1024 (default: 128)");
                writer.WriteElementString("CoverSize", CoverSize.ToString());

                writer.WriteComment("Screen width and height (pixels)");
                writer.WriteElementString("ScreenW", ScreenW.ToString());
                writer.WriteElementString("ScreenH", ScreenH.ToString());

                writer.WriteComment("AAMode: " + CHelper.ListStrings(Enum.GetNames(typeof(EAntiAliasingModes))));
                writer.WriteElementString("AAMode", Enum.GetName(typeof(EAntiAliasingModes), AAMode));

                writer.WriteComment("MaxFPS should be between 1..200");
                writer.WriteElementString("MaxFPS", MaxFPS.ToString("#"));

                writer.WriteComment("VSync: " + CHelper.ListStrings(Enum.GetNames(typeof(EOffOn))));
                writer.WriteElementString("VSync", Enum.GetName(typeof(EOffOn), VSync));

                writer.WriteComment("FullScreen: " + CHelper.ListStrings(Enum.GetNames(typeof(EOffOn))));
                writer.WriteElementString("FullScreen", Enum.GetName(typeof(EOffOn), FullScreen));

                writer.WriteComment("FadeTime should be between 0..3 Seconds");
                writer.WriteElementString("FadeTime", FadeTime.ToString("#0.00"));

                writer.WriteEndElement();
                #endregion Graphics

                #region Theme
                writer.WriteStartElement("Theme");

                writer.WriteComment("Theme name");
                writer.WriteElementString("Name", Theme);

                writer.WriteComment("Skin name");
                writer.WriteElementString("Skin", Skin);

                writer.WriteComment("Name of cover-theme");
                writer.WriteElementString("Cover", CoverTheme);

                writer.WriteComment("Draw note-lines: " + CHelper.ListStrings(Enum.GetNames(typeof(EOffOn))));
                writer.WriteElementString("DrawNoteLines", Enum.GetName(typeof(EOffOn), DrawNoteLines));

                writer.WriteComment("Draw tone-helper: " + CHelper.ListStrings(Enum.GetNames(typeof(EOffOn))));
                writer.WriteElementString("DrawToneHelper", Enum.GetName(typeof(EOffOn), DrawToneHelper));

                writer.WriteComment("Look of timer: " + CHelper.ListStrings(Enum.GetNames(typeof(ETimerLook))));
                writer.WriteElementString("TimerLook", Enum.GetName(typeof(ETimerLook), TimerLook));

                writer.WriteComment("Information about players on SingScreen: " + CHelper.ListStrings(Enum.GetNames(typeof(EPlayerInfo))));
                writer.WriteElementString("PlayerInfo", Enum.GetName(typeof(EPlayerInfo), PlayerInfo));

                writer.WriteComment("Fade player-information with lyrics and notebars: " + CHelper.ListStrings(Enum.GetNames(typeof(EFadePlayerInfo))));
                writer.WriteElementString("FadePlayerInfo", Enum.GetName(typeof(EFadePlayerInfo), FadePlayerInfo));

                writer.WriteComment("Cover Loading: " + CHelper.ListStrings(Enum.GetNames(typeof(ECoverLoading))));
                writer.WriteElementString("CoverLoading", Enum.GetName(typeof(ECoverLoading), CoverLoading));

                writer.WriteComment("Lyric Style: " + CHelper.ListStrings(Enum.GetNames(typeof(ELyricStyle))));
                writer.WriteElementString("LyricStyle", Enum.GetName(typeof(ELyricStyle), LyricStyle));

                writer.WriteEndElement();
                #endregion Theme

                #region Sound
                writer.WriteStartElement("Sound");

                writer.WriteComment("PlayBackLib: " + CHelper.ListStrings(Enum.GetNames(typeof(EPlaybackLib))));
                writer.WriteElementString("PlayBackLib", Enum.GetName(typeof(EPlaybackLib), PlayBackLib));

                writer.WriteComment("RecordLib: " + CHelper.ListStrings(Enum.GetNames(typeof(ERecordLib))));
                writer.WriteElementString("RecordLib", Enum.GetName(typeof(ERecordLib), RecordLib));

                writer.WriteComment("AudioBufferSize: " + CHelper.ListStrings(Enum.GetNames(typeof(EBufferSize))));
                writer.WriteElementString("AudioBufferSize", Enum.GetName(typeof(EBufferSize), AudioBufferSize));

                writer.WriteComment("AudioLatency from -500 to 500 ms");
                writer.WriteElementString("AudioLatency", AudioLatency.ToString());

                writer.WriteComment("Background Music: " + CHelper.ListStrings(Enum.GetNames(typeof(EOffOn))));
                writer.WriteElementString("BackgroundMusic", Enum.GetName(typeof(EOffOn), BackgroundMusic));

                writer.WriteComment("Background Music Volume from 0 to 100");
                writer.WriteElementString("BackgroundMusicVolume", BackgroundMusicVolume.ToString());

                writer.WriteComment("Background Music Source: " + CHelper.ListStrings(Enum.GetNames(typeof(EBackgroundMusicSource))));
                writer.WriteElementString("BackgroundMusicSource", Enum.GetName(typeof(EBackgroundMusicSource), BackgroundMusicSource));

                writer.WriteComment("Background Music use start-tag of songs: " + CHelper.ListStrings(Enum.GetNames(typeof(EOffOn))));
                writer.WriteElementString("BackgroundMusicUseStart", Enum.GetName(typeof(EOffOn), BackgroundMusicUseStart));

                writer.WriteComment("Preview Volume from 0 to 100");
                writer.WriteElementString("PreviewMusicVolume", PreviewMusicVolume.ToString());

                writer.WriteComment("Game Volume from 0 to 100");
                writer.WriteElementString("GameMusicVolume", GameMusicVolume.ToString());

                writer.WriteEndElement();
                #endregion Sound

                #region Game
                writer.WriteStartElement("Game");

                writer.WriteComment("Game-Language:");
                writer.WriteElementString("Language", Language);

                // SongFolder
                // Check, if program was started with songfolder-parameters
                if (_SongFolderOld.Count > 0)
                {
                    //Write "old" song-folders to config.
                    writer.WriteComment("SongFolder: SongFolder1, SongFolder2, SongFolder3, ...");
                    for (int i = 0; i < _SongFolderOld.Count; i++)
                        writer.WriteElementString("SongFolder" + (i + 1), _SongFolderOld[i]);
                }
                else
                {
                    //Write "normal" song-folders to config.
                    writer.WriteComment("SongFolder: SongFolder1, SongFolder2, SongFolder3, ...");
                    for (int i = 0; i < SongFolder.Count; i++)
                        writer.WriteElementString("SongFolder" + (i + 1), SongFolder[i]);
                }

                writer.WriteComment("SongMenu: " + CHelper.ListStrings(Enum.GetNames(typeof(ESongMenu))));
                writer.WriteElementString("SongMenu", Enum.GetName(typeof(ESongMenu), SongMenu));

                writer.WriteComment("SongSorting: " + CHelper.ListStrings(Enum.GetNames(typeof(ESongSorting))));
                writer.WriteElementString("SongSorting", Enum.GetName(typeof(ESongSorting), SongSorting));

                writer.WriteComment("Ignore articles on song-sorting: " + CHelper.ListStrings(Enum.GetNames(typeof(EOffOn))));
                writer.WriteElementString("IgnoreArticles", Enum.GetName(typeof(EOffOn), IgnoreArticles));

                writer.WriteComment("ScoreAnimationTime: Values >= 1 or 0 for no animation. Time is in seconds.");
                writer.WriteElementString("ScoreAnimationTime", ScoreAnimationTime.ToString());

                writer.WriteComment("TimerMode: " + CHelper.ListStrings(Enum.GetNames(typeof(ETimerMode))));
                writer.WriteElementString("TimerMode", Enum.GetName(typeof(ETimerMode), TimerMode));

                writer.WriteComment("NumPlayer: 1.." + CSettings.MaxNumPlayer);
                writer.WriteElementString("NumPlayer", NumPlayer.ToString());

                writer.WriteComment("Order songs in tabs: " + CHelper.ListStrings(Enum.GetNames(typeof(EOffOn))));
                writer.WriteElementString("Tabs", Enum.GetName(typeof(EOffOn), Tabs));

                writer.WriteComment("Position if lyrics on screen: " + CHelper.ListStrings(Enum.GetNames(typeof(ELyricsPosition))));
                writer.WriteElementString("LyricsPosition", Enum.GetName(typeof(ELyricsPosition), LyricsPosition));

                writer.WriteComment("MinLineBreakTime: Value >= 0 in s. Minimum time the text is shown before it is to be sung");
                writer.WriteElementString("MinLineBreakTime", MinLineBreakTime.ToString());

                writer.WriteComment("Default profile for players 1..." + CSettings.MaxNumPlayer + ":");
                writer.WriteStartElement("Players");
                for (int i = 1; i <= CSettings.MaxNumPlayer; i++)
                    writer.WriteElementString("Player" + i, Path.GetFileName(Players[i - 1]));
                writer.WriteEndElement();

                writer.WriteEndElement();
                #endregion Game

                #region Video
                writer.WriteStartElement("Video");

                writer.WriteComment("VideoDecoder: " + CHelper.ListStrings(Enum.GetNames(typeof(EVideoDecoder))));
                writer.WriteElementString("VideoDecoder", Enum.GetName(typeof(EVideoDecoder), VideoDecoder));

                writer.WriteComment("VideoBackgrounds: " + CHelper.ListStrings(Enum.GetNames(typeof(EOffOn))));
                writer.WriteElementString("VideoBackgrounds", Enum.GetName(typeof(EOffOn), VideoBackgrounds));

                writer.WriteComment("VideoPreview: " + CHelper.ListStrings(Enum.GetNames(typeof(EOffOn))));
                writer.WriteElementString("VideoPreview", Enum.GetName(typeof(EOffOn), VideoPreview));

                writer.WriteComment("Show Videos while singing: " + CHelper.ListStrings(Enum.GetNames(typeof(EOffOn))));
                writer.WriteElementString("VideosInSongs", Enum.GetName(typeof(EOffOn), VideosInSongs));

                writer.WriteComment("Show backgroundmusic videos as background: " + CHelper.ListStrings(Enum.GetNames(typeof(EOffOn))));
                writer.WriteElementString("VideosToBackground", Enum.GetName(typeof(EOffOn), VideosToBackground));

                writer.WriteComment("WebcamLib: " + CHelper.ListStrings(Enum.GetNames(typeof(EWebcamLib))));
                writer.WriteElementString("WebcamLib", Enum.GetName(typeof(EWebcamLib), WebcamLib));

                writer.WriteStartElement("WebcamConfig");

                writer.WriteElementString("MonikerString", WebcamConfig.MonikerString);
                writer.WriteElementString("Framerate", WebcamConfig.Framerate.ToString());
                writer.WriteElementString("Width", WebcamConfig.Width.ToString());
                writer.WriteElementString("Height", WebcamConfig.Height.ToString());

                writer.WriteEndElement();

                writer.WriteEndElement();
                #endregion Video

                #region Record
                writer.WriteStartElement("Record");

                for (int p = 1; p <= CSettings.MaxNumPlayer; p++)
                {
                    if (MicConfig[p - 1].DeviceName != "" && MicConfig[p - 1].Channel > 0)
                    {
                        writer.WriteStartElement("MicConfig" + p);

                        writer.WriteElementString("DeviceName", MicConfig[p - 1].DeviceName);
                        writer.WriteElementString("DeviceDriver", MicConfig[p - 1].DeviceDriver);
                        writer.WriteElementString("Channel", MicConfig[p - 1].Channel.ToString());

                        writer.WriteEndElement();
                    }
                }

                writer.WriteComment("Mic delay in ms. 0, 20, 40, 60 ... 500");
                writer.WriteElementString("MicDelay", MicDelay.ToString());

                writer.WriteEndElement();
                #endregion Record

                #region Server
                writer.WriteStartElement("Server");

                writer.WriteComment("Server On/Off: " + CHelper.ListStrings(Enum.GetNames(typeof(EOffOn))));
                writer.WriteElementString("ServerActive", Enum.GetName(typeof(EOffOn), ServerActive));

                writer.WriteComment("Server Encryption On/Off: " + CHelper.ListStrings(Enum.GetNames(typeof(EOffOn))));
                writer.WriteElementString("ServerEncryption", Enum.GetName(typeof(EOffOn), ServerEncryption));

                writer.WriteComment("Server Port (default: 3000) [1..65535]");
                writer.WriteElementString("ServerPort", ServerPort.ToString());

                writer.WriteComment("Server Password (default: vocaluxe)");
                writer.WriteElementString("ServerPassword", ServerPassword);

                writer.WriteEndElement();
                #endregion Server

                // End of File
                writer.WriteEndElement(); //end of root
                writer.WriteEndDocument();
                writer.Flush();
                writer.Close();
                writer = null;
            }
            catch (Exception)
            {
                if (writer != null)
                    writer.Close();
                return false;
            }
            return true;
        }

        /// <summary>
        ///     Calculates the maximum cycle time to reach the MaxFPS value.
        /// </summary>
        public static float CalcCycleTime()
        {
            return (1f / MaxFPS) * 1000f;
        }

        /// <summary>
        ///     Checks, if there is a mic-configuration
        /// </summary>
        /// <returns></returns>
        public static bool IsMicConfig()
        {
            ReadOnlyCollection<CRecordDevice> devices = CSound.RecordGetDevices();
            if (devices == null)
                return false;

            return devices.Any(t => t.PlayerChannel1 != 0 || t.PlayerChannel2 != 0);
        }

        /// <summary>
        ///     Checks, if there is a mic-configuration
        /// </summary>
        /// <param name="player">Player-Number</param>
        /// <returns></returns>
        public static bool IsMicConfig(int player)
        {
            ReadOnlyCollection<CRecordDevice> devices = CSound.RecordGetDevices();
            if (devices == null)
                return false;

            return devices.Any(t => t.PlayerChannel1 == player || t.PlayerChannel2 == player);
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

        /// <summary>
        ///     Try to assign automatically player 1 and 2 to usb-mics.
        /// </summary>
        public static bool AutoAssignMics()
        {
            //Look for (usb-)mic
            //SRecordDevice[] Devices = new SRecordDevice[CSound.RecordGetDevices().Length];
            ReadOnlyCollection<CRecordDevice> devices = CSound.RecordGetDevices();
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
                        MicConfig[0].DeviceName = device.Name;
                        MicConfig[0].DeviceDriver = device.Driver;
                        MicConfig[0].Channel = 1;
                        //Set this device to player 2
                        MicConfig[1].DeviceName = device.Name;
                        MicConfig[1].DeviceDriver = device.Driver;
                        MicConfig[1].Channel = 2;

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
                        MicConfig[0].DeviceName = device.Name;
                        MicConfig[0].DeviceDriver = device.Driver;
                        MicConfig[0].Channel = 1;

                        if (device.Channels >= 2)
                        {
                            //Set this device to player 2
                            MicConfig[1].DeviceName = device.Name;
                            MicConfig[1].DeviceDriver = device.Driver;
                            MicConfig[1].Channel = 2;

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///     Load command-line-parameters and their values to lists.
        /// </summary>
        /// <param name="args">Parameters</param>
        public static void LoadCommandLineParams(string[] args)
        {
            Regex spliterParam = new Regex(@"-{1,2}|\/", RegexOptions.IgnoreCase);

            //Complete argument string
            string arguments = args.Aggregate(string.Empty, (current, arg) => current + (arg + " "));

            args = spliterParam.Split(arguments);

            foreach (string text in args)
            {
                Regex spliterVal = new Regex(@"\s", RegexOptions.IgnoreCase);

                //split arg with Spilter-Regex and save in parts
                string[] parts = spliterVal.Split(text, 2);

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
                //Switch parameter as lower case
                string param = _Params[i];

                param = param.ToLower();

                string value = _Values[i];

                switch (param)
                {
                    case "configfile":
                        //Check if value is valid                      
                        if (_CheckFile(value))
                            CSettings.FileConfig = value;
                        break;

                    case "scorefile":
                        //Check if value is valid
                        if (_CheckFile(value))
                            CSettings.FileHighscoreDB = value;
                        break;

                    case "playlistfolder":
                        CSettings.CreateFolder(value);
                        CSettings.FolderPlaylists = value;
                        break;

                    case "profilefolder":
                        CSettings.CreateFolder(value);
                        CSettings.FoldersProfiles.Clear();
                        CSettings.FoldersProfiles.Add(value);
                        break;
                }
            }
        }

        /// <summary>
        ///     Apply command-line-parameters (after reading config.xml)
        /// </summary>
        public static void UseCommandLineParamsAfter()
        {
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
                        //Check if SongFolders from config.xml are saved
                        if (_SongFolderOld.Count == 0)
                        {
                            _SongFolderOld.Clear();
                            _SongFolderOld.AddRange(SongFolder);
                            SongFolder.Clear();
                        }
                        //Add parameter-value to SongFolder
                        SongFolder.Add(value);
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
                CGame.Players[j].ProfileID = -1;
                if (Players[j] == "" || profiles == null)
                    continue;

                foreach (CProfile profile in profiles)
                {
                    if (Path.GetFileName(profile.FileName) == Players[j] && profile.Active == EOffOn.TR_CONFIG_ON)
                    {
                        //Update Game-infos with player
                        CGame.Players[j].ProfileID = profile.ID;
                    }
                }
            }
        }
    }
}