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
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using VocaluxeLib.Menu;
using VocaluxeLib.PartyModes;

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
        public bool NoErrors;
        public IPartyMode PartyMode;
    }
    #endregion Structs

    static class CParty
    {
        private const int _PartyModeSystemVersion = 1;

        private static Dictionary<int, SPartyMode> _PartyModes;
        private static Queue<int> _IDs;

        private static int _NormalGameModeID;
        private static SPartyMode _CurrentPartyMode;

        #region public stuff
        public static void Init()
        {
            _PartyModes = new Dictionary<int, SPartyMode>();
            _IDs = new Queue<int>(1000);

            for (int i = 0; i < 1000; i++)
                _IDs.Enqueue(i);

            //add dummy normal game mode and set it as default
            SPartyMode pm = new SPartyMode {PartyMode = new CPartyModeNone(), ScreenFiles = new List<string>()};
            pm.PartyMode.Initialize();
            pm.PartyModeID = _IDs.Dequeue();
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

        public static List<SPartyModeInfos> GetPartyModeInfos()
        {
            List<SPartyModeInfos> infos = new List<SPartyModeInfos>();

            int[] pmIDs = new int[_PartyModes.Count];
            _PartyModes.Keys.CopyTo(pmIDs, 0);

            foreach (int pmID in pmIDs)
            {
                if (pmID != _NormalGameModeID)
                {
                    SPartyMode mode;
                    _PartyModes.TryGetValue(pmID, out mode);

                    if (mode.PartyMode != null)
                    {
                        SPartyModeInfos info = new SPartyModeInfos
                            {
                                Author = mode.Author,
                                Description = mode.Description,
                                Name = mode.Name,
                                PartyModeID = mode.PartyModeID,
                                Playable = mode.NoErrors,
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
            List<string> files = new List<string>();
            files.AddRange(CHelper.ListFiles(CSettings.FolderPartyModes, "*.xml", false, true));

            foreach (string file in files)
            {
                SPartyMode pm = _LoadPartyMode(file);
                _PartyModes.Add(pm.PartyModeID, pm);
            }
        }

        private static SPartyMode _LoadPartyMode(string file)
        {
            SPartyMode pm = new SPartyMode {PartyModeID = _IDs.Dequeue(), ScreenFiles = new List<string>(), NoErrors = false};

            CXMLReader xmlReader = CXMLReader.OpenFile(file);

            //Error...
            if (xmlReader == null)
                return pm;

            bool loaded = true;

            loaded &= xmlReader.TryGetIntValue("//root/PartyModeSystemVersion", ref pm.PartyModeSystemVersion);
            loaded &= xmlReader.GetValue("//root/Info/Name", out pm.Name, "ERROR Name");
            loaded &= xmlReader.GetValue("//root/Info/Description", out pm.Description, "ERROR Description");
            loaded &= xmlReader.GetValue("//root/Info/Author", out pm.Author, "ERROR Author");
            loaded &= xmlReader.GetValue("//root/Info/Folder", out pm.Folder, "ERROR Folder");
            loaded &= xmlReader.GetValue("//root/Info/PartyModeFile", out pm.PartyModeFile, "ERROR PartyModeFile");
            loaded &= xmlReader.GetInnerValues("PartyScreens", ref pm.ScreenFiles);
            loaded &= xmlReader.TryGetIntValue("//root/Info/PartyModeVersionMajor", ref pm.PartyModeVersionMajor);
            loaded &= xmlReader.TryGetIntValue("//root/Info/PartyModeVersionMinor", ref pm.PartyModeVersionMinor);
            loaded &= xmlReader.GetValue("//root/Info/TargetAudience", out pm.TargetAudience, "ERROR TargetAudience");

            if (!loaded)
            {
                CLog.LogError("Error loading PartyMode file: " + file);
                return pm;
            }

            if (pm.PartyModeSystemVersion != _PartyModeSystemVersion)
            {
                CLog.LogError("Error loading PartyMode file (wrong PartyModeSystemVersion): " + file);
                return pm;
            }

            if (pm.ScreenFiles.Count == 0)
            {
                CLog.LogError("Error loading PartyMode file (no ScreenFiles found): " + file);
                return pm;
            }

            string pathToCode = Path.Combine(Path.Combine(CSettings.FolderPartyModes, pm.Folder), CSettings.FolderPartyModeCode);

            List<string> filesToCompile = new List<string>();
            filesToCompile.AddRange(CHelper.ListFiles(pathToCode, "*.cs", false, true));

            Assembly output = _CompileFiles(filesToCompile.ToArray());
            if (output == null)
                return pm;

            if (!CLanguage.LoadPartyLanguageFiles(pm.PartyModeID, Path.Combine(Path.Combine(CSettings.FolderPartyModes, pm.Folder), CSettings.FolderPartyModeLanguages)))
            {
                CLog.LogError("Error loading language files for PartyMode: " + file);
                return pm;
            }

            object instance = output.CreateInstance(typeof(IPartyMode).Namespace + "." + pm.Folder + "." + pm.PartyModeFile);
            if (instance == null)
            {
                CLog.LogError("Error creating Instance of PartyMode file: " + file);
                return pm;
            }

            try
            {
                pm.PartyMode = (IPartyMode)instance;
            }
            catch (Exception e)
            {
                CLog.LogError("Error casting PartyMode file: " + file + "; " + e.Message);
                return pm;
            }
            pm.PartyMode.Initialize();

            if (!CTheme.AddTheme(Path.Combine(Directory.GetCurrentDirectory(), Path.Combine(Path.Combine(CSettings.FolderPartyModes, pm.Folder), "Theme.xml")), pm.PartyModeID))
                return pm;

            int themeIndex = CTheme.GetThemeIndex(pm.PartyModeID);
            CTheme.ListSkins(themeIndex);
            int skinIndex = CTheme.GetSkinIndex(pm.PartyModeID);

            if (!CTheme.LoadSkin(skinIndex))
                return pm;

            if (!CTheme.LoadTheme(themeIndex))
                return pm;

            foreach (string screenfile in pm.ScreenFiles)
            {
                string xmlPath = Path.Combine(Path.Combine(CSettings.FolderPartyModes, pm.Folder), CSettings.FolderPartyModeScreens);
                CMenuParty screen = _GetPartyScreenInstance(output, screenfile, pm.Folder);

                if (screen != null)
                {
                    screen.Init();
                    screen.AssingPartyMode(pm.PartyMode);
                    screen.SetPartyModeID(pm.PartyModeID);
                    screen.LoadTheme(xmlPath);
                    pm.PartyMode.AddScreen(screen, screenfile);
                }
                else
                    return pm;
            }

            pm.NoErrors = true;
            return pm;
        }

        private static Assembly _CompileFiles(string[] files)
        {
            if (files == null || files.Length == 0)
                return null;

            CompilerParameters compilerParams = new CompilerParameters();
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