using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;

using OpenTK.Platform;

using Vocaluxe.Lib.Sound;

namespace Vocaluxe.Base
{
    #region Enums
    public enum ERenderer
    {
        TR_CONFIG_SOFTWARE,
        TR_CONFIG_OPENGL,
        TR_CONFIG_DIRECT3D
    }

    public enum EAntiAliasingModes
    {
        x0 = 0,
        x2 = 2,
        x4 = 4,
        x8 = 8,
        x16 = 16,
        x32 = 32
    }

    public enum EColorDeep
    {
        Bit8 = 8,
        Bit16 = 16,
        Bit24 = 24,
        Bit32 = 32
    }

    public enum ETextureQuality
    {
        TR_CONFIG_TEXTURE_LOWEST,
        TR_CONFIG_TEXTURE_LOW,
        TR_CONFIG_TEXTURE_MEDIUM,
        TR_CONFIG_TEXTURE_HIGH,
        TR_CONFIG_TEXTURE_HIGHEST
    }

    public enum EOffOn
    {
        TR_CONFIG_OFF,
        TR_CONFIG_ON
    }

    public enum EDebugLevel
    {
        // don't change the order!
        TR_CONFIG_OFF,		    //no debug infos
        TR_CONFIG_ONLY_FPS,
        TR_CONFIG_LEVEL1,
        TR_CONFIG_LEVEL2,
        TR_CONFIG_LEVEL3,
        TR_CONFIG_LEVEL_MAX	    //all debug infos
    }

    public enum EBufferSize
    {
        b0 = 0,
        b512 = 512,
        b1024 = 1024,
        b1536 = 1536,
        b2048 = 2048,
        b2560 = 2560,
        b3072 = 3072,
        b3584 = 3584,
        b4096 = 4096
    }

    public enum EPlaybackLib
    {
        PortAudio,
        OpenAL
    }

    public enum ERecordLib
    {
        PortAudio
    }

    public enum EVideoDecoder
    {
        FFmpeg
    }

    public enum ESongMenu
    {
        //TR_CONFIG_LIST,		    //a simple list
        //TR_CONFIG_DREIDEL,	    //as in ultrastar deluxe
        TR_CONFIG_TILE_BOARD,	//chessboard like
        //TR_CONFIG_BOOK          //for playlists
    }

    public enum ESongSorting
    {
        TR_CONFIG_NONE,
        //TR_CONFIG_RANDOM,
        TR_CONFIG_FOLDER,
        TR_CONFIG_ARTIST,
        TR_CONFIG_ARTIST_LETTER,
        TR_CONFIG_TITLE_LETTER,
        TR_CONFIG_EDITION,
        TR_CONFIG_GENRE,
        TR_CONFIG_LANGUAGE,
        TR_CONFIG_YEAR,
        TR_CONFIG_DECADE
    }

    public enum ECoverLoading
    {
        TR_CONFIG_COVERLOADING_ONDEMAND,
        TR_CONFIG_COVERLOADING_ATSTART,
        TR_CONFIG_COVERLOADING_DYNAMIC
    }

    public enum EGameDifficulty
    {
        TR_CONFIG_EASY,
        TR_CONFIG_NORMAL,
        TR_CONFIG_HARD
    }

    public enum ETimerMode
    {
        TR_CONFIG_TIMERMODE_CURRENT,
        TR_CONFIG_TIMERMODE_REMAINING,
        TR_CONFIG_TIMERMODE_TOTAL
    }

    public enum ETimerLook
    {
        TR_CONFIG_TIMERLOOK_NORMAL,
        TR_CONFIG_TIMERLOOK_EXPANDED
    }

    public enum EBackgroundMusicSource
    {
        TR_CONFIG_NO_OWN_MUSIC,
        TR_CONFIG_OWN_MUSIC,
        TR_CONFIG_ONLY_OWN_MUSIC
    }

    #endregion Enums

    static class CConfig
    {
        private static XmlWriterSettings _settings = new XmlWriterSettings();
        
        // Debug
        public static EDebugLevel DebugLevel = EDebugLevel.TR_CONFIG_OFF;
        
        // Graphics
        public static ERenderer Renderer = ERenderer.TR_CONFIG_DIRECT3D;
        public static ETextureQuality TextureQuality = ETextureQuality.TR_CONFIG_TEXTURE_MEDIUM;
        public static float MaxFPS = 60f;

        public static int ScreenW = 1024;
        public static int ScreenH = 576;

        public static EAntiAliasingModes AAMode = EAntiAliasingModes.x0;
        public static EColorDeep Colors = EColorDeep.Bit32;

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
        public static ECoverLoading CoverLoading = ECoverLoading.TR_CONFIG_COVERLOADING_ATSTART;

        // Sound
        public static EPlaybackLib PlayBackLib = EPlaybackLib.PortAudio;
        public static ERecordLib RecordLib = ERecordLib.PortAudio;
        public static EBufferSize AudioBufferSize = EBufferSize.b2048;
        public static int AudioLatency = 0;
        public static int BackgroundMusicVolume = 50;
        public static EOffOn BackgroundMusic = EOffOn.TR_CONFIG_ON;
        public static EBackgroundMusicSource BackgroundMusicSource = EBackgroundMusicSource.TR_CONFIG_NO_OWN_MUSIC;

        // Game
        public static List<string> SongFolder = new List<string>();
        public static ESongMenu SongMenu = ESongMenu.TR_CONFIG_TILE_BOARD;
        public static ESongSorting SongSorting = ESongSorting.TR_CONFIG_ARTIST;
        public static float ScoreAnimationTime = 10;
        public static ETimerMode TimerMode = ETimerMode.TR_CONFIG_TIMERMODE_REMAINING;
        public static int NumPlayer = 2;
        public static EOffOn Tabs = EOffOn.TR_CONFIG_OFF;
        public static string Language = "English";
        public static EOffOn LyricsOnTop = EOffOn.TR_CONFIG_OFF;
        public static string[] Players = new string[CSettings.MaxNumPlayer];
        
        // Video
        public static EVideoDecoder VideoDecoder = EVideoDecoder.FFmpeg;
        public static EOffOn VideoBackgrounds = EOffOn.TR_CONFIG_ON;
        public static EOffOn VideoPreview = EOffOn.TR_CONFIG_ON;
        public static EOffOn VideosInSongs = EOffOn.TR_CONFIG_ON;

        // Record
        public static SMicConfig[] MicConfig;
        public static int MicDelay = 200;   //[ms]

        //Lists to save parameters and values
        private static List<string> _Params = new List<string>();
        private static List<string> _Values = new List<string>();

        //Variables to save old values for commandline-parameters
        private static List<string> SongFolderOld = new List<string>();

        public static void Init()
        {
            _settings.Indent = true;
            _settings.Encoding = Encoding.UTF8;
            _settings.ConformanceLevel = ConformanceLevel.Document;

            SongFolder.Add(Path.Combine(Directory.GetCurrentDirectory(), CSettings.sFolderSongs));

            MicConfig = new SMicConfig[CSettings.MaxNumPlayer];

            // Init config file
            if (!File.Exists(CSettings.sFileConfig))          
                SaveConfig();
            
            LoadConfig();
        }

        public static bool LoadConfig()
        {
            #region Inits
            bool loaded = false;
            XPathDocument xPathDoc = null;
            XPathNavigator navigator = null;

            try
            {
                xPathDoc = new XPathDocument(CSettings.sFileConfig);
                navigator = xPathDoc.CreateNavigator();
                loaded = true;
            }
            catch (Exception)
            {
                CLog.LogError("Error opening Config.xml (FileName: " + CSettings.sFileConfig);
                loaded = false;
                if (navigator != null)
                    navigator = null;

                if (xPathDoc != null)
                    xPathDoc = null;
            }
            #endregion Inits

            if (loaded)
            {
                string value = string.Empty;

                #region Debug
                CHelper.TryGetEnumValueFromXML<EDebugLevel>("//root/Debug/DebugLevel", navigator, ref DebugLevel);
                #endregion Debug

                #region Graphics
                CHelper.TryGetEnumValueFromXML<ERenderer>("//root/Graphics/Renderer", navigator, ref Renderer);
                CHelper.TryGetEnumValueFromXML<ETextureQuality>("//root/Graphics/TextureQuality", navigator, ref TextureQuality);
                CHelper.TryGetIntValueFromXML("//root/Graphics/CoverSize", navigator, ref CoverSize);
                if (CoverSize > 1024)
                    CoverSize = 1024;
                if (CoverSize < 32)
                    CoverSize = 32;

                CHelper.TryGetIntValueFromXML("//root/Graphics/ScreenW", navigator, ref ScreenW);
                CHelper.TryGetIntValueFromXML("//root/Graphics/ScreenH", navigator, ref ScreenH);
                CHelper.TryGetEnumValueFromXML<EAntiAliasingModes>("//root/Graphics/AAMode", navigator, ref AAMode);
                CHelper.TryGetEnumValueFromXML<EColorDeep>("//root/Graphics/Colors", navigator, ref Colors);
                CHelper.TryGetFloatValueFromXML("//root/Graphics/MaxFPS", navigator, ref MaxFPS);
                CHelper.TryGetEnumValueFromXML<EOffOn>("//root/Graphics/VSync", navigator, ref VSync);
                CHelper.TryGetEnumValueFromXML<EOffOn>("//root/Graphics/FullScreen", navigator, ref FullScreen);
                CHelper.TryGetFloatValueFromXML("//root/Graphics/FadeTime", navigator, ref FadeTime);
                #endregion Graphics

                #region Theme
                CHelper.GetValueFromXML("//root/Theme/Name", navigator, ref Theme, Theme);
                CHelper.GetValueFromXML("//root/Theme/Skin", navigator, ref Skin, Skin);
                CHelper.GetValueFromXML("//root/Theme/Cover", navigator, ref CoverTheme, CoverTheme);
                CHelper.TryGetEnumValueFromXML("//root/Theme/DrawNoteLines", navigator, ref DrawNoteLines);
                CHelper.TryGetEnumValueFromXML("//root/Theme/DrawToneHelper", navigator, ref DrawToneHelper);
                CHelper.TryGetEnumValueFromXML("//root/Theme/TimerLook", navigator, ref TimerLook);
                CHelper.TryGetEnumValueFromXML("//root/Theme/CoverLoading", navigator, ref CoverLoading);
                #endregion Theme

                #region Sound
                CHelper.TryGetEnumValueFromXML<EPlaybackLib>("//root/Sound/PlayBackLib", navigator, ref PlayBackLib);
                CHelper.TryGetEnumValueFromXML<ERecordLib>("//root/Sound/RecordLib", navigator, ref RecordLib);
                CHelper.TryGetEnumValueFromXML<EBufferSize>("//root/Sound/AudioBufferSize", navigator, ref AudioBufferSize);

                CHelper.TryGetIntValueFromXML("//root/Sound/AudioLatency", navigator, ref AudioLatency);
                if (AudioLatency < -500)
                    AudioLatency = -500;
                if (AudioLatency > 500)
                    AudioLatency = 500;

                CHelper.TryGetEnumValueFromXML("//root/Sound/BackgroundMusic", navigator, ref BackgroundMusic);
                CHelper.TryGetIntValueFromXML("//root/Sound/BackgroundMusicVolume", navigator, ref BackgroundMusicVolume);
                CHelper.TryGetEnumValueFromXML("//root/Sound/BackgroundMusicSource", navigator, ref BackgroundMusicSource);
                #endregion Sound

                #region Game
                // Songfolder
                value = string.Empty;
                int i = 1;
                while(CHelper.GetValueFromXML("//root/Game/SongFolder" + i.ToString(), navigator, ref value, value))
                {
                    if (i == 1)
                        SongFolder.Clear();

                    SongFolder.Add(value);
                    value = string.Empty;
                    i++;
                }

                CHelper.TryGetEnumValueFromXML<ESongMenu>("//root/Game/SongMenu", navigator, ref SongMenu);
                CHelper.TryGetEnumValueFromXML<ESongSorting>("//root/Game/SongSorting", navigator, ref SongSorting);
                CHelper.TryGetFloatValueFromXML("//root/Game/ScoreAnimationTime", navigator, ref ScoreAnimationTime);
                CHelper.TryGetEnumValueFromXML<ETimerMode>("//root/Game/TimerMode", navigator, ref TimerMode);
                CHelper.TryGetIntValueFromXML("//root/Game/NumPlayer", navigator, ref NumPlayer);
                CHelper.TryGetEnumValueFromXML("//root/Game/Tabs", navigator, ref Tabs);
                CHelper.GetValueFromXML("//root/Game/Language", navigator, ref Language, Language);
                CHelper.TryGetEnumValueFromXML<EOffOn>("//root/Game/LyricsOnTop", navigator, ref LyricsOnTop);

                if ((ScoreAnimationTime > 0 && ScoreAnimationTime < 1) || ScoreAnimationTime < 0)
                {
                    ScoreAnimationTime = 1;
                }

                if (NumPlayer < 1 || NumPlayer > CSettings.MaxNumPlayer)
                    NumPlayer = 2;

                List<string> _Languages = new List<string>();
                _Languages = CLanguage.GetLanguages();

                bool _LangExists = false;

                for (i = 0; i < _Languages.Count; i++)
                {
                    if (_Languages[i] == Language)
                    {
                        _LangExists = true;
                    }
                }

                //TODO: What should we do, if English not exists?
                if(_LangExists == false){
                    Language = "English";
                    
                }
                CLanguage.SetLanguage(Language);

                //Read players from config
                for (i = 1; i <= CSettings.MaxNumPlayer; i++)
                {
                    CHelper.GetValueFromXML("//root/Game/Players/Player" + i.ToString(), navigator, ref Players[i-1], string.Empty);
                }

                #endregion Game

                #region Video
                CHelper.TryGetEnumValueFromXML<EVideoDecoder>("//root/Video/VideoDecoder", navigator, ref VideoDecoder);
                CHelper.TryGetEnumValueFromXML<EOffOn>("//root/Video/VideoBackgrounds", navigator, ref VideoBackgrounds);
                CHelper.TryGetEnumValueFromXML<EOffOn>("//root/Video/VideoPreview", navigator, ref VideoPreview);
                CHelper.TryGetEnumValueFromXML<EOffOn>("//root/Video/VideosInSongs", navigator, ref VideosInSongs);
                #endregion Video

                #region Record
                MicConfig = new SMicConfig[CSettings.MaxNumPlayer];
                value = string.Empty;
                for (int p = 1; p <= CSettings.MaxNumPlayer; p++)
                {
                    MicConfig[p - 1] = new SMicConfig(0);
                    CHelper.GetValueFromXML("//root/Record/MicConfig" + p.ToString() + "/DeviceName", navigator, ref MicConfig[p - 1].DeviceName, String.Empty);
                    CHelper.GetValueFromXML("//root/Record/MicConfig" + p.ToString() + "/DeviceDriver", navigator, ref MicConfig[p - 1].DeviceDriver, String.Empty);
                    CHelper.GetValueFromXML("//root/Record/MicConfig" + p.ToString() + "/InputName", navigator, ref MicConfig[p - 1].InputName, String.Empty);
                    CHelper.TryGetIntValueFromXML("//root/Record/MicConfig" + p.ToString() + "/Channel", navigator, ref MicConfig[p - 1].Channel);
                }

                CHelper.TryGetIntValueFromXML("//root/Record/MicDelay", navigator, ref MicDelay);
                MicDelay = (int)(20 * Math.Round(MicDelay / 20.0));
                if (MicDelay < 0)
                    MicDelay = 0;
                if (MicDelay > 500)
                    MicDelay = 500;

                #endregion Record

                return true;
            }
            else return false;
        }

        public static bool SaveConfig()
        {
            
            XmlWriter writer = XmlWriter.Create(CSettings.sFileConfig, _settings);
            writer.WriteStartDocument();
            writer.WriteStartElement("root");

            #region Info
            writer.WriteStartElement("Info");

            writer.WriteElementString("Version", CSettings.GetFullVersionText());
            writer.WriteElementString("Time", System.DateTime.Now.ToString());
            writer.WriteElementString("Platform", System.Environment.OSVersion.Platform.ToString());
            writer.WriteElementString("OSVersion", System.Environment.OSVersion.ToString());
            writer.WriteElementString("ProcessorCount", System.Environment.ProcessorCount.ToString());

            writer.WriteElementString("Screens", Screen.AllScreens.Length.ToString());
            writer.WriteElementString("PrimaryScreenResolution", Screen.PrimaryScreen.Bounds.Size.ToString());

            writer.WriteElementString("Directory", System.Environment.CurrentDirectory.ToString());
            
            writer.WriteEndElement();
            #endregion Info

            #region Debug
            writer.WriteStartElement("Debug");

            writer.WriteComment("DebugLevel: " + ListStrings(Enum.GetNames(typeof(EDebugLevel))));
            writer.WriteElementString("DebugLevel", Enum.GetName(typeof(EDebugLevel), DebugLevel));

            writer.WriteEndElement();
            #endregion Debug

            #region Graphics
            writer.WriteStartElement("Graphics");

            writer.WriteComment("Renderer: " + ListStrings(Enum.GetNames(typeof(ERenderer))));
            writer.WriteElementString("Renderer", Enum.GetName(typeof(ERenderer), Renderer));

            writer.WriteComment("TextureQuality: " + ListStrings(Enum.GetNames(typeof(ETextureQuality))));
            writer.WriteElementString("TextureQuality", Enum.GetName(typeof(ETextureQuality), TextureQuality));

            writer.WriteComment("CoverSize (pixels): 32, 64, 128, 256, 512, 1024 (default: 128)");
            writer.WriteElementString("CoverSize", CoverSize.ToString());

            writer.WriteComment("Screen width and height (pixels)");
            writer.WriteElementString("ScreenW", ScreenW.ToString());
            writer.WriteElementString("ScreenH", ScreenH.ToString());

            writer.WriteComment("AAMode: " + ListStrings(Enum.GetNames(typeof(EAntiAliasingModes))));
            writer.WriteElementString("AAMode", Enum.GetName(typeof(EAntiAliasingModes), AAMode));

            writer.WriteComment("Colors: " + ListStrings(Enum.GetNames(typeof(EColorDeep))));
            writer.WriteElementString("Colors", Enum.GetName(typeof(EColorDeep), Colors));

            writer.WriteComment("MaxFPS should be between 1..200");
            writer.WriteElementString("MaxFPS", MaxFPS.ToString("#"));

            writer.WriteComment("VSync: " + ListStrings(Enum.GetNames(typeof(EOffOn))));
            writer.WriteElementString("VSync", Enum.GetName(typeof(EOffOn), VSync));

            writer.WriteComment("FullScreen: " + ListStrings(Enum.GetNames(typeof(EOffOn))));
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

            writer.WriteComment("Draw note-lines:" + ListStrings(Enum.GetNames(typeof(EOffOn))));
            writer.WriteElementString("DrawNoteLines", Enum.GetName(typeof(EOffOn), DrawNoteLines));

            writer.WriteComment("Draw tone-helper:" + ListStrings(Enum.GetNames(typeof(EOffOn))));
            writer.WriteElementString("DrawToneHelper", Enum.GetName(typeof(EOffOn), DrawToneHelper));

            writer.WriteComment("Look of timer:" + ListStrings(Enum.GetNames(typeof(ETimerLook))));
            writer.WriteElementString("TimerLook", Enum.GetName(typeof(ETimerLook), TimerLook));

            writer.WriteComment("Cover Loading:" + ListStrings(Enum.GetNames(typeof(ECoverLoading))));
            writer.WriteElementString("CoverLoading", Enum.GetName(typeof(ECoverLoading), CoverLoading));

            writer.WriteEndElement();
            #endregion Theme

            #region Sound
            writer.WriteStartElement("Sound");

            writer.WriteComment("PlayBackLib: " + ListStrings(Enum.GetNames(typeof(EPlaybackLib))));
            writer.WriteElementString("PlayBackLib", Enum.GetName(typeof(EPlaybackLib), PlayBackLib));

            writer.WriteComment("RecordLib: " + ListStrings(Enum.GetNames(typeof(ERecordLib))));
            writer.WriteElementString("RecordLib", Enum.GetName(typeof(ERecordLib), RecordLib));

            writer.WriteComment("AudioBufferSize: " + ListStrings(Enum.GetNames(typeof(EBufferSize))));
            writer.WriteElementString("AudioBufferSize", Enum.GetName(typeof(EBufferSize), AudioBufferSize));

            writer.WriteComment("AudioLatency from -500 to 500 ms");
            writer.WriteElementString("AudioLatency", AudioLatency.ToString());

            writer.WriteComment("Background Music");
            writer.WriteElementString("BackgroundMusic", Enum.GetName(typeof(EOffOn), BackgroundMusic));

            writer.WriteComment("Background Music Volume from 0 to 100");
            writer.WriteElementString("BackgroundMusicVolume", BackgroundMusicVolume.ToString());

            writer.WriteComment("Background Music Source");
            writer.WriteElementString("BackgroundMusicSource", Enum.GetName(typeof(EBackgroundMusicSource), BackgroundMusicSource));

            writer.WriteEndElement();
            #endregion Sound

            #region Game
            writer.WriteStartElement("Game");

            writer.WriteComment("Game-Language:");
            writer.WriteElementString("Language", Language);

            // SongFolder
            // Check, if program was started with songfolder-parameters
            if (SongFolderOld.Count > 0)
            {
                //Write "old" song-folders to config.
                writer.WriteComment("SongFolder: SongFolder1, SongFolder2, SongFolder3, ...");
                for (int i = 0; i < SongFolderOld.Count; i++)
                {
                    writer.WriteElementString("SongFolder" + (i + 1).ToString(), SongFolderOld[i]);
                }
            }
            else
            {
                //Write "normal" song-folders to config.
                writer.WriteComment("SongFolder: SongFolder1, SongFolder2, SongFolder3, ...");
                for (int i = 0; i < SongFolder.Count; i++)
                {
                    writer.WriteElementString("SongFolder" + (i + 1).ToString(), SongFolder[i]);
                }
            }

            writer.WriteComment("SongMenu: " + ListStrings(Enum.GetNames(typeof(ESongMenu))));
            writer.WriteElementString("SongMenu", Enum.GetName(typeof(ESongMenu), SongMenu));

            writer.WriteComment("SongSorting: " + ListStrings(Enum.GetNames(typeof(ESongSorting))));
            writer.WriteElementString("SongSorting", Enum.GetName(typeof(ESongSorting), SongSorting));

            writer.WriteComment("ScoreAnimationTime: Values >= 1 or 0 for no animation. Time is in seconds.");
            writer.WriteElementString("ScoreAnimationTime", ScoreAnimationTime.ToString());

            writer.WriteComment("TimerMode: " + ListStrings(Enum.GetNames(typeof(ETimerMode))));
            writer.WriteElementString("TimerMode", Enum.GetName(typeof(ETimerMode), TimerMode));

            writer.WriteComment("NumPlayer: 1.." + CSettings.MaxNumPlayer.ToString());
            writer.WriteElementString("NumPlayer", NumPlayer.ToString());

            writer.WriteComment("Order songs in tabs: " + ListStrings(Enum.GetNames(typeof(EOffOn))));
            writer.WriteElementString("Tabs", Enum.GetName(typeof(EOffOn), Tabs));

            writer.WriteComment("Lyrics also on Top of screen: " + ListStrings(Enum.GetNames(typeof(EOffOn))));
            writer.WriteElementString("LyricsOnTop", Enum.GetName(typeof(EOffOn), LyricsOnTop));

            writer.WriteComment("Default profile for players 1..." + CSettings.MaxNumPlayer.ToString() + ":");
            writer.WriteStartElement("Players");
            for (int i = 1; i <= CSettings.MaxNumPlayer; i++)
            {
                writer.WriteElementString("Player" + i.ToString(), Path.GetFileName(Players[i - 1]));
            }
            writer.WriteEndElement();

            writer.WriteEndElement();
            #endregion Game
            
            #region Video
            writer.WriteStartElement("Video");

            writer.WriteComment("VideoDecoder: " + ListStrings(Enum.GetNames(typeof(EVideoDecoder))));
            writer.WriteElementString("VideoDecoder", Enum.GetName(typeof(EVideoDecoder), VideoDecoder));

            writer.WriteComment("VideoBackgrounds: " + ListStrings(Enum.GetNames(typeof(EOffOn))));
            writer.WriteElementString("VideoBackgrounds", Enum.GetName(typeof(EOffOn), VideoBackgrounds));

            writer.WriteComment("VideoPreview: " + ListStrings(Enum.GetNames(typeof(EOffOn))));
            writer.WriteElementString("VideoPreview", Enum.GetName(typeof(EOffOn), VideoPreview));

            writer.WriteComment("Show Videos while singing: " + ListStrings(Enum.GetNames(typeof(EOffOn))));
            writer.WriteElementString("VideosInSongs", Enum.GetName(typeof(EOffOn), VideosInSongs));

            writer.WriteEndElement();
            #endregion Video

            #region Record
            writer.WriteStartElement("Record");

            for (int p = 1; p <= CSettings.MaxNumPlayer; p++)
            {
                if (MicConfig[p - 1].DeviceName != String.Empty && MicConfig[p - 1].InputName != String.Empty && MicConfig[p - 1].Channel > 0)
                {
                    writer.WriteStartElement("MicConfig" + p.ToString());

                    writer.WriteElementString("DeviceName", MicConfig[p - 1].DeviceName);
                    writer.WriteElementString("DeviceDriver", MicConfig[p - 1].DeviceDriver);
                    writer.WriteElementString("InputName", MicConfig[p - 1].InputName);
                    writer.WriteElementString("Channel", MicConfig[p - 1].Channel.ToString());

                    writer.WriteEndElement();
                }
            }

            writer.WriteComment("Mic delay in ms. 0, 20, 40, 60 ... 500");
            writer.WriteElementString("MicDelay", MicDelay.ToString());

            writer.WriteEndElement();
            #endregion Record

            // End of File
            writer.WriteEndElement(); //end of root
            writer.WriteEndDocument();

            writer.Flush();
            writer.Close();
            return true;
        }
        
		/// <summary>
		/// Calculates the maximum cycle time to reach the MaxFPS value.
		/// </summary>
        public static float CalcCycleTime()
        {
            return (1f / MaxFPS)*1000f;
        }

        /// <summary>
        /// Checks, if there is a mic-configuration
        /// </summary>
        /// <returns></returns>
        public static bool IsMicConfig()
        {
            SRecordDevice[] Devices = CSound.RecordGetDevices();
			if (Devices == null)
				return false;
			
            for (int dev = 0; dev < Devices.Length; dev++)
            {
                for (int inp = 0; inp < Devices[dev].Inputs.Count; inp++)
                {
                    if (Devices[dev].Inputs[inp].PlayerChannel1 != 0 || Devices[dev].Inputs[inp].PlayerChannel2 != 0)
                    {
                        return true;
                    }
                }
            }           
            return false;
        }

        /// <summary>
        /// Checks, if there is a mic-configuration
        /// </summary>
        /// <param name="player">Player-Number</param>
        /// <returns></returns>
        public static bool IsMicConfig(int player)
        {
            SRecordDevice[] Devices = CSound.RecordGetDevices();
			if (Devices == null)
				return false;
			
			if (Devices != null)
			{
	            for (int dev = 0; dev < Devices.Length; dev++)
	            {
	                for (int inp = 0; inp < Devices[dev].Inputs.Count; inp++)
	                {
	                    if (Devices[dev].Inputs[inp].PlayerChannel1 == player || Devices[dev].Inputs[inp].PlayerChannel2 == player)
	                    {
	                        return true;
	                    }
	                }
	            }
				return false;
			}
            return false;
        }

        /// <summary>
        /// Try to assign automatically player 1 and 2 to usb-mics.
        /// </summary>
        public static bool AutoAssignMics()
        {
            //Look for (usb-)mic
            //SRecordDevice[] Devices = new SRecordDevice[CSound.RecordGetDevices().Length];
            SRecordDevice[] Devices = CSound.RecordGetDevices();
			if (Devices == null)
				return false;
			
            for (int dev = 0; dev < Devices.Length; dev++)
            {
                //Has Device some signal-names in name -> This could be a (usb-)mic
                if (Regex.IsMatch(Devices[dev].Name, @"Usb|Wireless", RegexOptions.IgnoreCase))
                {
                    //Check if there are inputs.
                    if (Devices[dev].Inputs.Count >= 1)
                    {
                        //Check if there is one or more channels
                        if (Devices[dev].Inputs[0].Channels >= 2)
                        {
                            //Set this device to player 1
                            MicConfig[0].DeviceName = Devices[dev].Name;
                            MicConfig[0].DeviceDriver = Devices[dev].Driver;
                            MicConfig[0].InputName = Devices[dev].Inputs[0].Name;
                            MicConfig[0].Channel = 1;
                            //Set this device to player 2
                            MicConfig[1].DeviceName = Devices[dev].Name;
                            MicConfig[1].DeviceDriver = Devices[dev].Driver;
                            MicConfig[1].InputName = Devices[dev].Inputs[0].Name;
                            MicConfig[1].Channel = 2;

                            return true;
                        }
                    }
                }
            }
            //If no usb-mics found -> Look for Devices with "mic" or "mik"
            for (int dev = 0; dev < Devices.Length; dev++)
            {
                //Has Device some signal-names in name -> This could be a mic
                if (Regex.IsMatch(Devices[dev].Name, @"Mic|Mik", RegexOptions.IgnoreCase))
                {
                    //Check if there are inputs.
                    if (Devices[dev].Inputs.Count >= 1)
                    {
                        bool micfound = false;
                        //Check if there is one or more channels
                        if (Devices[dev].Inputs[0].Channels >= 1)
                        {
                            //Set this device to player 1
                            MicConfig[0].DeviceName = Devices[dev].Name;
                            MicConfig[0].DeviceDriver = Devices[dev].Driver;
                            MicConfig[0].InputName = Devices[dev].Inputs[0].Name;
                            MicConfig[0].Channel = 1;

                            micfound = true;
                        }
                        if (Devices[dev].Inputs[0].Channels >= 2)
                        {
                            //Set this device to player 2
                            MicConfig[1].DeviceName = Devices[dev].Name;
                            MicConfig[1].DeviceDriver = Devices[dev].Driver;
                            MicConfig[1].InputName = Devices[dev].Inputs[0].Name;
                            MicConfig[1].Channel = 2;

                            micfound = true;
                        }
                        if (micfound)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Load command-line-parameters and their values to lists.
        /// </summary>
        /// <param name="args">Parameters</param>
        public static void LoadCommandLineParams(string[] args)
        {
            Regex SpliterParam = new Regex(@"-{1,2}|\/", RegexOptions.IgnoreCase);

            //Complete argument string
            string arguments = string.Empty;
            foreach (string arg in args)
            {
                arguments += arg + " ";
            }

            args = SpliterParam.Split(arguments);

            foreach (string text in args)
            {
                Regex SpliterVal = new Regex(@"\s", RegexOptions.IgnoreCase);

                //Array for parts of an arg
                string[] parts;

                //split arg with Spilter-Regex and save in parts
                parts = SpliterVal.Split(text, 2);

                switch (parts.Length)
                {

                    //Only found a parameter
                    case 1:
                        if (!Regex.IsMatch(parts[0], @"\s") && parts[0] != string.Empty)
                        {
                            //Add parameter
                            _Params.Add(parts[0]);

                            //Add value
                            _Values.Add("");
                        }
                        break;

                    
                    //Found parameter and value
                    case 2:
                        if (!Regex.IsMatch(parts[0], @"\s") && parts[0] != string.Empty)
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
        /// Apply command-line-parameters (before reading config.xml)
        /// </summary>
        public static void UseCommandLineParamsBefore(){

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
                        if (CheckFile(value))
                        {
                            CSettings.sFileConfig = value;
                        }
                        break;

                    case "scorefile":
                        //Check if value is valid
                        if (CheckFile(value))
                        {
                            CSettings.sFileHighscoreDB = value;
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Apply command-line-parameters (after reading config.xml)
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
                        //Check if SongFolders from config.xml are saved
                        if (SongFolderOld.Count == 0)
                        {
                            SongFolderOld.Clear();
                            SongFolderOld.AddRange(SongFolder);
                            SongFolder.Clear();
                        }
                        //Add parameter-value to SongFolder
                        SongFolder.Add(value);
                        break;

                    case "songpath":
                        //Check if SongFolders from config.xml are saved
                        if (SongFolderOld.Count == 0)
                        {
                            SongFolderOld.Clear();
                            SongFolderOld.AddRange(SongFolder);
                            SongFolder.Clear();
                        }
                        //Add parameter-value to SongFolder
                        SongFolder.Add(value);
                        break;
                }
            }
        }

        private static bool CheckFile(string value)
        {
            char [] chars = Path.GetInvalidPathChars();
            for(int i=0; i<chars.Length; i++){
                if(value.Contains(chars[i].ToString())){
                    return false;
                }
            }
            if (Path.GetFileName(value) == string.Empty)
            {
                return false;
            }
            if (!Path.HasExtension(value))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Concat strings into one string with ", " as separator.
        /// </summary>
        public static string ListStrings(string[] str)
        {
            string _Result = string.Empty;

            for (int i = 0; i < str.Length; i++)
			{
                _Result += str[i];
			    if(i < str.Length - 1)
                    _Result += ", ";
			}
    
            return _Result;
        }

        /// <summary>
        /// Use saved players from config now for games
        /// </summary>
        public static void UsePlayers() 
        {
            for (int i = 0; i < CProfiles.Profiles.Length; i++)
            {
                for (int j = 0; j < CSettings.MaxNumPlayer; j++)
                {
                    if (Players[j] != string.Empty)
                    {
                        if (Path.GetFileName(CProfiles.Profiles[i].ProfileFile) == Players[j])
                        {
                            //Update Game-infos with player
                            CGame.Player[j].Name = CProfiles.Profiles[i].PlayerName;
                            CGame.Player[j].Difficulty = CProfiles.Profiles[i].Difficulty;
                            CGame.Player[j].ProfileID = i;
                        }
                    }
                }
            }
        }


        
    }
}
