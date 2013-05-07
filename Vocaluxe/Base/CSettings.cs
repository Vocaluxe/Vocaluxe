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
using System.IO;
using System.Reflection;
using VocaluxeLib;

namespace Vocaluxe.Base
{
    enum ERevision
    {
        // ReSharper disable UnusedMember.Global
        Alpha,
        Beta,
        RC,
        Release
        // ReSharper restore UnusedMember.Global
    }

    static class CSettings
    {
#if ARCH_X86
        private const String _Arch = "x86";
#endif

#if ARCH_X64
        private const String _Arch = "x64";
#endif

        public static EGameState GameState = EGameState.Start;

        //Adjusting of programName and version now in the assembly config.
        //I'd use the major and minor for Main releases, build number for every public release and revision for every bugfix version without any features
        //As it is different than before, this is open for discussion.
        //TODO: Remove this when this is decided
        private const string _ProgramCodeName = "Shining Heaven";

        public const ERevision VersionRevision = ERevision.Alpha;

        public const int DatabaseHighscoreVersion = 2;
        public const int DatabaseCoverVersion = 1;
        public const int DatabaseCreditsRessourcesVersion = 1;

        public const int RenderW = 1280;
        public const int RenderH = 720;

        public const int ZNear = -100;
        public const int ZFar = 100;

        public static bool IsFullScreen;
        public const int VertexBufferElements = 10000;

        public const string Icon = "Vocaluxe.ico";
        public const string Logo = "Logo.png";
        public const string FallbackLanguage = "English";
        public static string FileConfig = "Config.xml";
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

        public const bool TabNavigation = false;

        public const float BackgroundMusicFadeTime = 0.5f;

        public static readonly List<string> MusicFileTypes = new List<string>
            {
                "*.mp3",
                "*.wma",
                "*.ogg",
                "*.wav"
            };

        private static AssemblyName _Assembly
        {
            get { return Assembly.GetExecutingAssembly().GetName(); }
        }

        public static string ProgramName
        {
            get { return _Assembly.Name; }
        }
        private static string _Version
        {
            get
            {
                Version v = _Assembly.Version;
                return "v" + v.Major + "." + v.Minor + "." + v.Build;
            }
        }

        private static string _GetVersionText()
        {
            string version = _Version + " (" + _Arch + ")";

            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            if (VersionRevision != ERevision.Release)
                // ReSharper restore ConditionIsAlwaysTrueOrFalse
                version += " " + _GetVersionStatus() + String.Format(" ({0:0000)}", _Assembly.Version.Revision);

            return version;
        }

        public static string GetFullVersionText()
        {
            string version = ProgramName;

            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            if (_ProgramCodeName != "")
                // ReSharper restore ConditionIsAlwaysTrueOrFalse
                version += " \"" + _ProgramCodeName + "\"";

            return version + " " + _GetVersionText();
        }

        private static string _GetVersionStatus()
        {
            string result;

            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            if (VersionRevision != ERevision.Release)
                // ReSharper restore ConditionIsAlwaysTrueOrFalse
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
            List<string> folders = new List<string> {FolderCover, FolderFonts, FolderProfiles, FolderSongs, FolderScreenshots, FolderBackgroundMusic, FolderSounds, FolderPlaylists};

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