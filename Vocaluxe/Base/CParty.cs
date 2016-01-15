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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Vocaluxe.Base.ThemeSystem;
using VocaluxeLib;
using VocaluxeLib.Menu;
using VocaluxeLib.PartyModes;
using VocaluxeLib.Xml;

namespace Vocaluxe.Base
{
    static class CParty
    {
        private const int _PartyModeSystemVersion = 2;

        private static readonly Dictionary<int, SPartyMode> _PartyModes = new Dictionary<int, SPartyMode>();
        private static int _NextID;
        private static IPartyMode _CurrentPartyMode;

        #region public stuff
        public static bool Init()
        {
            if (_PartyModes.Count > 0)
                return false; //Already initialized
            SPartyMode pm = new SPartyMode
                {
                    Info = new SPartyModeInfos {Author = "Vocaluxe Team", Description = "Normal game", Name = "Normal", TargetAudience = "Just a normal game for everyone"},
                    PartyMode = new CPartyModeNormal(-1),
                    PartyModeSystemVersion = _PartyModeSystemVersion,
                    ScreenFiles = new List<string>()
                };
            _PartyModes.Add(-1, pm);
            _CurrentPartyMode = pm.PartyMode;
            Debug.Assert(_CurrentPartyMode != null && _CurrentPartyMode.ID == -1);

            //load other party modes
            _LoadPartyModes();
            return _CurrentPartyMode.Init();
        }

        public static int CurrentPartyModeID
        {
            get { return _CurrentPartyMode.ID; }
            set
            {
                if (_CurrentPartyMode.ID == value) {
                    _CurrentPartyMode.SetDefaults();
                    return;
                }
                if (value != -1 && !_PartyModes.ContainsKey(value))
                    throw new ArgumentException("Partymode with ID=" + value + " does not exist!");
                IPartyMode pm = _PartyModes[value].PartyMode;
                if (pm.Init())
                    _CurrentPartyMode = pm;
                else
                {
                    CLog.LogError("Could not init PartyMode \"" + _PartyModes[value].Info.Name + "\"! Removing...", true);
                    _PartyModes.Remove(value);
                }
            }
        }

        public static int Count
        {
            get { return _PartyModes.Count; }
        }

        public static void ReloadTheme()
        {
            foreach (SPartyMode pm in _PartyModes.Values)
                pm.PartyMode.ReloadTheme();
        }

        public static void ReloadSkin()
        {
            foreach (SPartyMode pm in _PartyModes.Values)
                pm.PartyMode.ReloadSkin();
        }

        public static void SaveThemes()
        {
            foreach (SPartyMode pm in _PartyModes.Values)
                pm.PartyMode.SaveScreens();
        }

        public static List<SPartyModeInfos> GetPartyModeInfos()
        {
            List<SPartyModeInfos> list = new List<SPartyModeInfos>();
            foreach (SPartyMode pm in _PartyModes.Values)
            {
                if (pm.PartyMode.ID >= 0)
                    list.Add(pm.Info);
            }
            return list;
        }

        public static void SetNormalGameMode()
        {
            CSongs.ResetPartySongSung();
            CurrentPartyModeID = -1;
        }

        public static void SetPartyMode(int partyModeID)
        {
            CurrentPartyModeID = partyModeID;
        }
        #endregion public stuff

        #region Interface
        public static void UpdateGame()
        {
            _CurrentPartyMode.UpdateGame();
        }

        public static IMenu GetStartScreen()
        {
            return _CurrentPartyMode.GetStartScreen();
        }

        public static SScreenSongOptions GetSongSelectionOptions()
        {
            return _CurrentPartyMode.GetScreenSongOptions();
        }

        public static void OnSongChange(int songIndex, ref SScreenSongOptions screenSongOptions)
        {
            _CurrentPartyMode.OnSongChange(songIndex, ref screenSongOptions);
        }

        public static void OnCategoryChange(int categoryIndex, ref SScreenSongOptions screenSongOptions)
        {
            _CurrentPartyMode.OnCategoryChange(categoryIndex, ref screenSongOptions);
        }

        public static void SetSearchString(string searchString, bool visible)
        {
            _CurrentPartyMode.SetSearchString(searchString, visible);
        }

        public static void JokerUsed(int teamNr)
        {
            _CurrentPartyMode.JokerUsed(teamNr);
        }

        public static void SongSelected(int songID)
        {
            _CurrentPartyMode.SongSelected(songID);
        }

        public static void FinishedSinging()
        {
            _CurrentPartyMode.FinishedSinging();
        }

        public static void LeavingScore()
        {
            _CurrentPartyMode.LeavingScore();
        }

        public static void LeavingHighscore()
        {
            _CurrentPartyMode.LeavingHighscore();
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
                    _PartyModes.Add(pm.PartyMode.ID, pm);
            }
        }

        private static bool _LoadPartyMode(string filePath, out SPartyMode pm)
        {
            CXmlDeserializer deser = new CXmlDeserializer();
            try
            {
                pm = deser.Deserialize<SPartyMode>(filePath);
                if (pm.PartyModeSystemVersion != _PartyModeSystemVersion)
                    throw new Exception("Wrong PartyModeSystemVersion " + pm.PartyModeSystemVersion + " expected: " + _PartyModeSystemVersion);

                if (pm.ScreenFiles.Count == 0)
                    throw new Exception("No ScreenFiles found");
            }
            catch (Exception e)
            {
                pm = new SPartyMode();
                CLog.LogError("Error loading PartyMode file " + filePath + ": " + e.Message);
                return false;
            }

            string pathToPm = Path.Combine(CSettings.ProgramFolder, CSettings.FolderNamePartyModes, pm.Info.Folder);
            string pathToCode = Path.Combine(pathToPm, CSettings.FolderNamePartyModeCode);

            var filesToCompile = new List<string>();
            filesToCompile.AddRange(CHelper.ListFiles(pathToCode, "*.cs", false, true));

            Assembly output = _CompileFiles(filesToCompile.ToArray());
            if (output == null)
                return false;

            object instance = output.CreateInstance(typeof(IPartyMode).Namespace + "." + pm.Info.Folder + "." + pm.Info.PartyModeFile, false,
                                                    BindingFlags.Public | BindingFlags.Instance, null, new object[] {_NextID++}, null, null);
            if (instance == null)
            {
                CLog.LogError("Error creating Instance of PartyMode file: " + filePath);
                return false;
            }

            try
            {
                pm.PartyMode = (IPartyMode)instance;
            }
            catch (Exception e)
            {
                CLog.LogError("Error casting PartyMode file: " + filePath + "; " + e.Message);
                return false;
            }

            if (!CLanguage.LoadPartyLanguageFiles(pm.PartyMode.ID, Path.Combine(pathToPm, CSettings.FolderNamePartyModeLanguages)))
            {
                CLog.LogError("Error loading language files for PartyMode: " + filePath);
                return false;
            }

            if (!CThemes.ReadThemesFromFolder(Path.Combine(pathToPm, CSettings.FolderNameThemes), pm.PartyMode.ID))
                return false;

            if (!CThemes.LoadPartymodeTheme(pm.PartyMode.ID))
                return false;

            foreach (string screenfile in pm.ScreenFiles)
            {
                CMenuParty screen = _GetPartyScreenInstance(output, screenfile, pm.Info.Folder);

                if (screen != null)
                {
                    screen.AssignPartyMode(pm.PartyMode);
                    pm.PartyMode.AddScreen(screen, screenfile);
                }
                else
                    return false;
            }
            pm.PartyMode.LoadTheme();
            pm.Info.ExtInfo = pm.PartyMode;
            return true;
        }

        private static Assembly _CompileFiles(string[] files)
        {
            if (files == null || files.Length == 0)
                return null;

            var compilerParams = new CompilerParameters();

            compilerParams.ReferencedAssemblies.Add("System.Windows.Forms.dll");
            compilerParams.ReferencedAssemblies.Add("System.dll");
            compilerParams.ReferencedAssemblies.Add("System.Core.dll");
            compilerParams.ReferencedAssemblies.Add("libs\\managed\\VocaluxeLib.dll");
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