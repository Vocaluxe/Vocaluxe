using System;
using System.Collections.Generic;
using System.IO;
using VocaluxeLib.Menu;

namespace Vocaluxe.Base
{
    enum ERevision
    {
        Alpha,
        Beta,
        RC,
        Release
    }

    static class CSettings
    {
#if ARCH_X86
        public const String Arch = "x86";
#endif

#if ARCH_X64
        public const String Arch = "x64";
#endif

        public static EGameState GameState = EGameState.Start;

        public const string ProgramName = "Vocaluxe";
        public const string ProgramCodeName = "Shining Heaven";

        public const int VersionMajor = 0;
        public const int VersionMinor = 3; // milestones
        public const int VersionSub = 0; // patches
        public const ERevision VersionRevision = ERevision.Alpha;

        public const int Build = 77; // Increase on every published version! Never Reset!

        public const int DatabaseHighscoreVersion = 2;
        public const int DatabaseCoverVersion = 1;
        public const int DatabaseCreditsRessourcesVersion = 1;

        public static int RenderW = 1280;
        public static int RenderH = 720;

        public const int ZNear = -100;
        public const int ZFar = 100;

        public static bool IsFullScreen = false;
        public static int VertexBufferElements = 10000;

        public const string Icon = "Vocaluxe.ico";
        public const string Logo = "Logo.png";
        public const string FallbackLanguage = "English";
        public const string BassRegistration = "Registration.xml";
        public static string FileConfig = "Config.xml";
        public const string FileCover = "Cover.xml";
        public const string FileFonts = "Fonts.xml";

        public const string FileOldHighscoreDB = "Ultrastar.db";
        public static string FileHighscoreDB = "HighscoreDB.sqlite";
        public const string FileCoverDB = "CoverDB.sqlite";
        public const string FileCreditsRessourcesDB = "CreditsRessourcesDB.sqlite";
        public const string FilePerformanceLog = "Performance.log";
        public const string FileErrorLog = "Error.log";
        public const string FileBenchmarkLog = "Benchmark.log";

        public const string SoundT440 = "440Hz.mp3";

        public const string FolderCover = "Cover";
        public const string FolderGraphics = "Graphics";
        public const string FolderFonts = "Fonts";
        public const string FolderThemes = "Themes";
        public const string FolderSkins = "Skins";
        public const string FolderThemeFonts = "Fonts";
        public const string FolderScreens = "Screens";
        public static string FolderProfiles = "Profiles";
        public const string FolderSongs = "Songs";
        public const string FolderSounds = "Sounds";
        public const string FolderLanguages = "Languages";
        public const string FolderScreenshots = "Screenshots";
        public const string FolderBackgroundMusic = "BackgroundMusic";
        public static string FolderPlaylists = "Playlists";

        public const string FolderPartyModes = "PartyModes";
        public const string FolderPartyModeCode = "Code";
        public const string FolderPartyModeScreens = "Screens";
        public const string FolderPartyModeLanguages = "Languages";
        public const string FolderPartyModeFonts = "Fonts";

        //public const String[] ToneStrings = new String[]{ "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        public const int ToneMin = -36;
        public const int ToneMax = 89;

        public const int NumNoteLines = 11;
        public static int MouseMoveDiffMin = 2;
        public const float MouseMoveOffTime = 3f;

        public const int MaxNumPlayer = 6;
        public const int MaxScore = 10000;
        public const int LinebonusScore = 1000;
        public const int MinScoreForDB = 100;

        public const float LyricHelperEnableTime = 5f;
        public const float LyricHelperMoveTime = 1.5f;
        public const float LyricHelperMinTime = 0.2f;

        public const float DefaultMedleyFadeInTime = 8f;
        public const float DefaultMedleyFadeOutTime = 2f;
        public const int MedleyMinSeriesLength = 3;
        public const float MedleyMinDuration = 40f;

        public static bool TabNavigation = false;

        public const float BackgroundMusicFadeTime = 0.5f;

        public static List<string> MusicFileTypes = new List<string>
            {
                "*.mp3",
                "*.wma",
                "*.ogg",
                "*.wav"
            };

        public static string GetVersionText()
        {
            string version = "v" + VersionMajor.ToString() + "." +
                              VersionMinor.ToString() + "." +
                              VersionSub.ToString() + " (" + Arch + ")";

            if (VersionRevision != ERevision.Release)
                version += " " + GetVersionStatus() + String.Format(" ({0:0000)}", Build);

            return version;
        }

        public static string GetFullVersionText()
        {
            string version = ProgramName;

            if (ProgramCodeName.Length > 0)
                version += " \"" + ProgramCodeName + "\"";

            return version + " " + GetVersionText();
        }

        public static string GetVersionStatus()
        {
            string result = String.Empty;

            if (VersionRevision != ERevision.Release)
                result = Enum.GetName(typeof(ERevision), VersionRevision);

            return result;
        }

        public static float GetRenderAspect()
        {
            return RenderW / (float)RenderH;
        }

        public static void MouseInactive()
        {
            MouseMoveDiffMin = 15;
        }

        public static void MouseActive()
        {
            MouseMoveDiffMin = 3;
        }

        public static void CreateFolders()
        {
            List<string> folders = new List<string>();

            folders.Add(FolderCover);
            folders.Add(FolderFonts);
            folders.Add(FolderProfiles);
            folders.Add(FolderSongs);
            folders.Add(FolderScreenshots);
            folders.Add(FolderBackgroundMusic);
            folders.Add(FolderSounds);
            folders.Add(FolderPlaylists);

            foreach (string folder in folders)
            {
                string path = Path.Combine(Environment.CurrentDirectory, folder);
                CreateFolder(path);
            }
        }

        public static void CreateFolder(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }
}