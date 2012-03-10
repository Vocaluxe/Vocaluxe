﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Vocaluxe.Base
{
    public enum EGameState
    {
        Start,
        Normal,
        EditTheme
    }

    enum ERevision
    {
        Alpha,
        Beta,
        RC,
        Release
    }

    enum EArch
    {
        x86,
        x64
    }

    static class CSettings
    {
#if ARCH_X86
        public const EArch ARCH = EArch.x86;
#endif

#if ARCH_X64
        public const EArch ARCH = EArch.x64;
#endif


        public static EGameState GameState = EGameState.Start;

        public const string sProgramName = "Vocaluxe";
        public const string sProgramCodeName = "Rising Star";

        public const int iVersionMajor = 0;
        public const int iVersionMinor = 1;      // milestones
        public const int iVersionSub = 0;        // steps
        public const ERevision VersionRevision = ERevision.Beta;

        public const int iBuild = 54;             // Increase on every published version! Never Reset!

        public const int iDatabaseHighscoreVersion = 1;
        public const int iDatabaseCoverVersion = 1;
        public const int iDatabaseCreditsRessourcesVersion = 1;
        

        public static int iRenderW = 1280;
        public static int iRenderH = 720;

        public static int zNear = -100;
        public static int zFar = 100;

        public static bool bFullScreen = false;

        public const string sIcon = "Vocaluxe.ico";
        public const string sLogo = "Logo.png";
        public const string FallbackLanguage = "English";
        public static string sBassRegistration = "Registration.xml";
        public static string sFileConfig = "Config.xml";
        public const string sFileCover = "Cover.xml";
        public const string sFileFonts = "Fonts.xml";

        public const string sFileOldHighscoreDB = "Ultrastar.db";
        public static string sFileHighscoreDB = "HighscoreDB.sqlite";
        public const string sFileCoverDB = "CoverDB.sqlite";
        public const string sFileCreditsRessourcesDB = "CreditsRessourcesDB.sqlite";
        public const string sFilePerformanceLog = "Performance.log";
        public const string sFileErrorLog = "Error.log";
        public const string sFileBenchmarkLog = "Benchmark.log";

        public const string sSoundT440 = "440Hz.mp3";

        public const string sFolderCover = "Cover";
        public const string sFolderGraphics = "Graphics";
        public const string sFolderFonts = "Fonts";
        public const string sFolderThemes = "Themes";
        public const string sFolderSkins = "Skins";
        public const string sFolderThemeFonts = "Fonts";
        public const string sFolderScreens = "Screens";
        public const string sFolderProfiles = "Profiles";
        public const string sFolderSongs = "Songs";
        public const string sFolderSounds = "Sounds";
        public const string sFolderLanguages = "Languages";
        public const string sFolderScreenshots = "Screenshots";
        public const string sFolderBackgroundMusic = "BackgroundMusic";

        //public const String[] ToneStrings = new String[]{ "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        public const int ToneMin = -36;
        public const int ToneMax = 89;

        public const int NumNoteLines = 11;
        public static int MouseMoveDiffMin = 2;
        public const float MouseMoveOffTime = 3f;
        
        public const int MaxNumPlayer = 3;
        public const int MaxScore = 10000;
        public const int LinebonusScore = 1000;
        public const int MinScoreForDB = 100;

        public const float LyricHelperEnableTime = 5f;
        public const float LyricHelperMoveTime = 1.5f;
        public const float LyricHelperMinTime = 0.2f;

        public static bool TabNavigation = false;

        public const float BackgroundMusicFadeTime = 0.5f;
        
        public static string GetVersionText()
        {
            string sVersion = "v" + iVersionMajor.ToString() + "." +
                iVersionMinor.ToString() + "." +
                iVersionSub.ToString() + " (" + Enum.GetName(typeof(EArch), ARCH) + ")";
            
            if (VersionRevision != ERevision.Release)
                sVersion += " " + GetVersionStatus() + String.Format(" ({0:0000)}", iBuild);

            return sVersion;
        }

        public static string GetFullVersionText()
        {
            string sVersion = sProgramName;

            if (sProgramCodeName != String.Empty)
                sVersion += " \"" + sProgramCodeName + "\"";

            return sVersion += " " + GetVersionText();
        }

        public static string GetVersionStatus()
        {
            string _Result = String.Empty;

            if (VersionRevision != ERevision.Release)
                _Result = Enum.GetName(typeof(ERevision), VersionRevision);

            return _Result;
        }

        public static float GetRenderAspect()
        {
            return (float)iRenderW / (float)iRenderH;
        }

        public static void MouseInacive()
        {
            MouseMoveDiffMin = 15;
        }

        public static void MouseActive()
        {
            MouseMoveDiffMin = 3;
        }

        public static void CreateFolders()
        {
            List<string> Folders = new List<string>();

            Folders.Add(sFolderCover);
            Folders.Add(sFolderFonts);
            Folders.Add(sFolderProfiles);
            Folders.Add(sFolderSongs);
            Folders.Add(sFolderScreenshots);
            Folders.Add(sFolderBackgroundMusic);
            Folders.Add(sFolderSounds);

            foreach (string folder in Folders)
            {
                string path = Path.Combine(Environment.CurrentDirectory, folder);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);   
            }       
        }
    }
}
