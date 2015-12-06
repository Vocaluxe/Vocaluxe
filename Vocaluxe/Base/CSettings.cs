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
using System.IO;
using System.Reflection;
using System.Windows.Forms;
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

    /// <summary>
    ///     This class contains settings for the program
    ///     These are not editable, everything that can be changed by the user is in CConfig
    /// </summary>
    static class CSettings
    {
#if ARCH_X86
        private const String _Arch = "x86";
#endif

#if ARCH_X64
        private const String _Arch = "x64";
#endif

        //TODO: This should not be here as it can change
        //State of the program
        public static EProgramState ProgramState = EProgramState.Start;

        //Adjusting of programName and version now in the assembly config.
        //I'd use the major and minor for Main releases, build number for every public release and revision for every bugfix version without any features
        private const string _ProgramCodeName = "Shining Heaven";

        public const ERevision VersionRevision = ERevision.Beta;

        public const int DatabaseHighscoreVersion = 3;
        public const int DatabaseCoverVersion = 1;
        public const int DatabaseCreditsRessourcesVersion = 1;

        public const int RenderW = 1280;
        public const int RenderH = 720;
        public static readonly SRectF RenderRect = new SRectF(0, 0, RenderW, RenderH, 0);

        public const int ZNear = -100;
        public const int ZFar = 100;

        public const int VertexBufferElements = 10000;

        public static readonly string ProgramFolder = AppDomain.CurrentDomain.BaseDirectory;

#if INSTALLER
        public static readonly string DataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Vocaluxe");
#elif LINUX
        public static readonly string DataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vocaluxe");
#else
        public static readonly string DataFolder = ProgramFolder;
#endif

        public const string FallbackLanguage = "English";

        public const string FileNameIcon = "Vocaluxe.ico";
        public const string FileNameLogo = "Logo.png";
        public const string FileNameFonts = "Fonts.xml";

        public const string FileNameOldHighscoreDB = "Ultrastar.db";
        public const string FileNameCoverDB = "CoverDB.sqlite";
        public const string FileNameCreditsRessourcesDB = "CreditsRessourcesDB.sqlite";

        public const string FileNamePerformanceLog = "Performance.log";
        public const string FileNameErrorLog = "Error.log";
        public const string FileNameBenchmarkLog = "Benchmark.log";
        public const string FileNameDebugLog = "Debug.log";
        public const string FileNameSongInfoLog = "SongInformation.log";

        public const string FileNameRequiredSkinElements = "RequiredSkinElements.xml";

        public const string FileNameSoundT440 = "440Hz.mp3";

        public const string FolderNameSongs = "Songs";
        public const string FolderNameProfiles = "Profiles";
        public const string FolderNameCover = "Cover";
        public const string FolderNameGraphics = "Graphics";
        public const string FolderNameFonts = "Fonts";
        public const string FolderNameThemes = "Themes";
        public const string FolderNameThemeFonts = "Fonts";
        public const string FolderNameScreens = "Screens";
        public const string FolderNamePhotos = "Photos";
        public const string FolderNameSounds = "Sounds";
        public const string FolderNameLanguages = "Languages";
        public const string FolderNameScreenshots = "Screenshots";
        public const string FolderNameBackgroundMusic = "BackgroundMusic";

        public const string FolderNamePartyModes = "PartyModes";
        public const string FolderNamePartyModeCode = "Code";
        public const string FolderNamePartyModeLanguages = "Languages";
        public const string FolderNamePartyModeFonts = "Fonts";

        public const string LinkAndroidApp = "https://build.phonegap.com/apps/639714/download/android/?qr_key=uY98ymvTr6K144RyTdhs";
        public const string LinkSymbianApp = "https://build.phonegap.com/apps/639714/download/symbian/?qr_key=uY98ymvTr6K144RyTdhs";
        public const string LinkWebOSApp = "https://build.phonegap.com/apps/639714/download/webos/?qr_key=uY98ymvTr6K144RyTdhs";
        public const string LinkWindowsPhoneApp = "https://build.phonegap.com/apps/639714/download/winphone/?qr_key=uY98ymvTr6K144RyTdhs";

        /// <summary>
        ///     Default name for themes, skins...
        /// </summary>
        public const string DefaultName = "Default";

        //public const String[] ToneStrings = new String[]{ "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        public const int ToneMin = -36;
        public const int ToneMax = 89;

        public const int NumNoteLines = 11;

        public const int MouseMoveOffTime = 3000; //in ms
        public const int MouseMoveDiffMinActive = 3;
        public const int MouseMoveDiffMinInactive = 15;

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
        public const float PauseResetTime = 1f;

        public const float SoundPlayerFadeTime = 0.5f;

        public const float SlideShowImageTime = 3500f;
        public const float SlideShowFadeTime = 500f;

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
            {
                // ReSharper restore ConditionIsAlwaysTrueOrFalse
                string gitversion = Application.ProductVersion;
                version += " " + _GetVersionStatus() + String.Format(" ({0})", gitversion);
            }

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

        public static void CreateFolders()
        {
            var folders = new List<string>
                {
                    Path.Combine(ProgramFolder, FolderNameCover),
                    Path.Combine(ProgramFolder, FolderNameFonts),
                    Path.Combine(DataFolder, FolderNameScreenshots),
                    Path.Combine(ProgramFolder, FolderNameBackgroundMusic),
                    Path.Combine(ProgramFolder, FolderNameSounds),
                    Path.Combine(DataFolder, CConfig.FolderPlaylists)
                };
            folders.AddRange(CConfig.ProfileFolders);
            folders.AddRange(CConfig.SongFolders);

            foreach (string folder in folders)
                _CreateFolder(folder);
        }

        private static void _CreateFolder(string path)
        {
            if (path != "" && !Directory.Exists(path))
            {
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch (Exception e)
                {
                    CLog.LogError("Cannot create directory \"" + path + "\": " + e.Message);
                }
            }
        }
    }
}