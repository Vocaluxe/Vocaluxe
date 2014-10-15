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
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Vocaluxe.Base.ThemeSystem;
using VocaluxeLib;
using VocaluxeLib.Menu;
using VocaluxeLib.PartyModes;
using VocaluxeLib.Xml;

namespace Vocaluxe.Base
{

    #region Structs
    struct SPartyMode
    {
        public int PartyModeID;
        public int PartyModeSystemVersion;
        public string Name;
        public string Description;
        public string Author;
        public string Folder;
        public string PartyModeFile;
        public List<string> ScreenFiles;
        public int PartyModeVersionMajor;
        public int PartyModeVersionMinor;
        public string TargetAudience;
        public IPartyMode PartyMode;
    }
    #endregion Structs

    static class CParty
    {
        private const int _PartyModeSystemVersion = 1;

        private static Dictionary<int, SPartyMode> _PartyModes;
        private static int _NextID;

        private static int _NormalGameModeID;
        private static SPartyMode _CurrentPartyMode;

        #region public stuff
        public static void Init()
        {
            _PartyModes = new Dictionary<int, SPartyMode>();
            _NextID = 0;

            //add dummy normal game mode and set it as default
            var pm = new SPartyMode {PartyMode = new CPartyModeNone(null), ScreenFiles = new List<string>(), PartyModeID = _NextID++};
            _NormalGameModeID = pm.PartyModeID;
            _PartyModes.Add(pm.PartyModeID, pm);
            _CurrentPartyMode = pm;

            //load other party modes
            _LoadPartyModes();
        }

        public static int CurrentPartyModeID
        {
            get
            {
                if (_CurrentPartyMode.PartyModeID != _NormalGameModeID)
                    return _CurrentPartyMode.PartyModeID;

                return -1;
            }
        }

        public static int NumModes
        {
            get { return _PartyModes.Count; }
        }

        public static void SaveThemes()
        {
            foreach (KeyValuePair<int, SPartyMode> pair in _PartyModes)
                pair.Value.PartyMode.SaveScreens();
        }

        public static List<SPartyModeInfos> GetPartyModeInfos()
        {
            var infos = new List<SPartyModeInfos>();

            var pmIDs = new int[_PartyModes.Count];
            _PartyModes.Keys.CopyTo(pmIDs, 0);

            foreach (int pmID in pmIDs)
            {
                if (pmID != _NormalGameModeID)
                {
                    SPartyMode mode;
                    _PartyModes.TryGetValue(pmID, out mode);

                    if (mode.PartyMode != null)
                    {
                        var info = new SPartyModeInfos
                            {
                                Author = mode.Author,
                                Description = mode.Description,
                                Name = mode.Name,
                                PartyModeID = mode.PartyModeID,
                                VersionMajor = mode.PartyModeVersionMajor,
                                VersionMinor = mode.PartyModeVersionMinor,
                                TargetAudience = mode.TargetAudience,
                                MaxPlayers = mode.PartyMode.GetMaxPlayer(),
                                MinPlayers = mode.PartyMode.GetMinPlayer(),
                                MaxTeams = mode.PartyMode.GetMaxTeams(),
                                MinTeams = mode.PartyMode.GetMinTeams()
                            };

                        infos.Add(info);
                    }
                }
            }
            return infos;
        }

        public static void SetNormalGameMode()
        {
            CSongs.ResetPartySongSung();
            SetPartyMode(_NormalGameModeID);
        }

        public static void SetPartyMode(int partyModeID)
        {
            if (!_PartyModes.TryGetValue(partyModeID, out _CurrentPartyMode))
                CLog.LogError("CParty: Can't find party mode ID: " + partyModeID);

            _CurrentPartyMode.PartyMode.Init();
        }

        public static CMenu GetNextPartyScreen(out EScreens alternativeScreen)
        {
            return _CurrentPartyMode.PartyMode.GetNextPartyScreen(out alternativeScreen);
        }
        #endregion public stuff

        #region Interface
        public static void UpdateGame()
        {
            _CurrentPartyMode.PartyMode.UpdateGame();
        }

        public static EScreens GetStartScreen()
        {
            return _CurrentPartyMode.PartyMode.GetStartScreen();
        }

        public static EScreens GetMainScreen()
        {
            return _CurrentPartyMode.PartyMode.GetMainScreen();
        }

        public static SScreenSongOptions GetSongSelectionOptions()
        {
            return _CurrentPartyMode.PartyMode.GetScreenSongOptions();
        }

        public static void OnSongChange(int songIndex, ref SScreenSongOptions screenSongOptions)
        {
            _CurrentPartyMode.PartyMode.OnSongChange(songIndex, ref screenSongOptions);
        }

        public static void OnCategoryChange(int categoryIndex, ref SScreenSongOptions screenSongOptions)
        {
            _CurrentPartyMode.PartyMode.OnCategoryChange(categoryIndex, ref screenSongOptions);
        }

        public static void SetSearchString(string searchString, bool visible)
        {
            _CurrentPartyMode.PartyMode.SetSearchString(searchString, visible);
        }

        public static void JokerUsed(int teamNr)
        {
            _CurrentPartyMode.PartyMode.JokerUsed(teamNr);
        }

        public static void SongSelected(int songID)
        {
            _CurrentPartyMode.PartyMode.SongSelected(songID);
        }

        public static void FinishedSinging()
        {
            _CurrentPartyMode.PartyMode.FinishedSinging();
        }

        public static void LeavingScore()
        {
            _CurrentPartyMode.PartyMode.LeavingScore();
        }

        public static void LeavingHighscore()
        {
            _CurrentPartyMode.PartyMode.LeavingHighscore();
        }
        #endregion Interface

        #region private stuff
        private static void _LoadPartyModes()
        {
            var files = new List<string>();
            files.AddRange(CHelper.ListFiles(CSettings.FolderNamePartyModes, "*.xml", false, true));

            foreach (string file in files)
            {
                SPartyMode pm;
                if (_LoadPartyMode(file, out pm))
                    _PartyModes.Add(pm.PartyModeID, pm);
            }
        }

        private static bool _LoadPartyMode(string file, out SPartyMode pm)
        {
            pm = new SPartyMode {PartyModeID = _NextID++, ScreenFiles = new List<string>()};

            CXMLReader xmlReader = CXMLReader.OpenFile(file);

            //Error...
            if (xmlReader == null)
                return false;

            bool loaded = true;

            loaded &= xmlReader.TryGetIntValue("//root/PartyModeSystemVersion", ref pm.PartyModeSystemVersion);
            loaded &= xmlReader.GetValue("//root/Info/Name", out pm.Name);
            loaded &= xmlReader.GetValue("//root/Info/Description", out pm.Description);
            loaded &= xmlReader.GetValue("//root/Info/Author", out pm.Author);
            loaded &= xmlReader.GetValue("//root/Info/Folder", out pm.Folder);
            loaded &= xmlReader.GetValue("//root/Info/PartyModeFile", out pm.PartyModeFile);
            loaded &= xmlReader.TryGetIntValue("//root/Info/PartyModeVersionMajor", ref pm.PartyModeVersionMajor);
            loaded &= xmlReader.TryGetIntValue("//root/Info/PartyModeVersionMinor", ref pm.PartyModeVersionMinor);
            loaded &= xmlReader.GetValue("//root/Info/TargetAudience", out pm.TargetAudience);
            loaded &= xmlReader.GetValues("//root/PartyScreens/*", ref pm.ScreenFiles);

            if (!loaded)
            {
                CLog.LogError("Error loading PartyMode file: " + file);
                return false;
            }

            if (pm.PartyModeSystemVersion != _PartyModeSystemVersion)
            {
                CLog.LogError("Error loading PartyMode file (wrong PartyModeSystemVersion): " + file);
                return false;
            }

            if (pm.ScreenFiles.Count == 0)
            {
                CLog.LogError("Error loading PartyMode file (no ScreenFiles found): " + file);
                return false;
            }

            string pathToPm = Path.Combine(CSettings.ProgramFolder, CSettings.FolderNamePartyModes, pm.Folder);
            string pathToCode = Path.Combine(pathToPm, CSettings.FolderNamePartyModeCode);

            var filesToCompile = new List<string>();
            filesToCompile.AddRange(CHelper.ListFiles(pathToCode, "*.cs", false, true));

            Assembly output = _CompileFiles(filesToCompile.ToArray());
            if (output == null)
                return false;

            if (!CLanguage.LoadPartyLanguageFiles(pm.PartyModeID, Path.Combine(pathToPm, CSettings.FolderNamePartyModeLanguages)))
            {
                CLog.LogError("Error loading language files for PartyMode: " + file);
                return false;
            }

            object instance = output.CreateInstance(typeof(IPartyMode).Namespace + "." + pm.Folder + "." + pm.PartyModeFile, false,
                BindingFlags.Public | BindingFlags.Instance,null, new object[]{pathToPm}, null, null);
            if (instance == null)
            {
                CLog.LogError("Error creating Instance of PartyMode file: " + file);
                return false;
            }

            try
            {
                pm.PartyMode = (IPartyMode)instance;
            }
            catch (Exception e)
            {
                CLog.LogError("Error casting PartyMode file: " + file + "; " + e.Message);
                return false;
            }

            if (!CThemes.ReadThemesFromFolder(Path.Combine(pathToPm, CSettings.FolderNameThemes), pm.PartyModeID))
                return false;

            if (!CThemes.LoadPartymodeTheme(pm.PartyModeID))
                return false;

            foreach (string screenfile in pm.ScreenFiles)
            {
                string xmlPath = CThemes.GetThemeScreensPath(pm.PartyModeID);
                CMenuParty screen = _GetPartyScreenInstance(output, screenfile, pm.Folder);

                if (screen != null)
                {
                    screen.Init();
                    screen.AssignPartyMode(pm.PartyMode);
                    screen.SetPartyModeID(pm.PartyModeID);
                    screen.LoadTheme(xmlPath);
                    pm.PartyMode.AddScreen(screen, screenfile);
                }
                else
                    return false;
            }

            return true;
        }

        private static Assembly _CompileFiles(string[] files)
        {
            if (files == null || files.Length == 0)
                return null;

            var compilerParams = new CompilerParameters();
            compilerParams.ReferencedAssemblies.Add("System.Windows.Forms.dll");
            compilerParams.ReferencedAssemblies.Add("System.Core.dll");
            compilerParams.ReferencedAssemblies.Add("VocaluxeLib.dll");
            compilerParams.GenerateInMemory = true;
#if DEBUG
            compilerParams.IncludeDebugInformation = true;
#endif

            using (CodeDomProvider cdp = CodeDomProvider.CreateProvider("CSharp"))
            {
                CompilerResults compileResult;

                try
                {
                    compileResult = cdp.CompileAssemblyFromFile(compilerParams, files);
                }
                catch (Exception e)
                {
                    CLog.LogError("Error Compiling Source (" + CHelper.ListStrings(files) + "): " + e.Message);
                    return null;
                }

                if (compileResult.Errors.Count > 0)
                {
                    foreach (CompilerError e in compileResult.Errors)
                        CLog.LogError("Error Compiling Source (" + CHelper.ListStrings(files) + "): " + e.ErrorText + " in '" + e.FileName + "' (" + e.Line + ")");
                    return null;
                }
                return compileResult.CompiledAssembly;
            }
        }

        private static CMenuParty _GetPartyScreenInstance(Assembly assembly, string screenName, string partyModeName)
        {
            if (assembly == null)
                return null;

            object instance = assembly.CreateInstance(typeof(IPartyMode).Namespace + "." + partyModeName + "." + screenName);
            if (instance == null)
            {
                CLog.LogError("Error creating Instance of PartyScreen: " + screenName);
                return null;
            }

            CMenuParty screen;
            try
            {
                screen = (CMenuParty)instance;
            }
            catch (Exception e)
            {
                CLog.LogError("Error casting PartyScreen: " + screenName + "; " + e.Message);
                return null;
            }
            return screen;
        }
        #endregion private stuff
    }
}