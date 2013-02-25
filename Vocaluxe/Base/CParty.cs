using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

using Vocaluxe.Menu;
using Vocaluxe.PartyModes;

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
        const int PartyModeSystemVersion = 1;

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
            SPartyMode pm = new SPartyMode();
            pm.PartyMode = new CPartyModeNone();
            pm.ScreenFiles = new List<string>();
            pm.PartyMode.Initialize();
            pm.PartyModeID = _IDs.Dequeue();
            _NormalGameModeID = pm.PartyModeID;
            _PartyModes.Add(pm.PartyModeID, pm);
            _CurrentPartyMode = pm;

            //load other party modes
            LoadPartyModes();
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

            int[] UsedKeys  = new int[_PartyModes.Count];
            _PartyModes.Keys.CopyTo(UsedKeys, 0);

            for (int i = 0; i < UsedKeys.Length; i++)
            {
                if (UsedKeys[i] != _NormalGameModeID)
                {
                    SPartyMode mode = new SPartyMode();
                    _PartyModes.TryGetValue(UsedKeys[i], out mode);

                    if (mode.PartyMode != null)
                    {
                        SPartyModeInfos info = new SPartyModeInfos();

                        info.Author = mode.Author;
                        info.Description = mode.Description;
                        info.Name = mode.Name;
                        info.PartyModeID = mode.PartyModeID;
                        info.Playable = mode.NoErrors;
                        info.VersionMajor = mode.PartyModeVersionMajor;
                        info.VersionMinor = mode.PartyModeVersionMinor;

                        info.TargetAudience = mode.TargetAudience;
                        info.MaxPlayers = mode.PartyMode.GetMaxPlayer();
                        info.MinPlayers = mode.PartyMode.GetMinPlayer();
                        info.MaxTeams = mode.PartyMode.GetMaxTeams();
                        info.MinTeams = mode.PartyMode.GetMinTeams();

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

        public static void SetPartyMode(int PartyModeID)
        {
            if (!_PartyModes.TryGetValue(PartyModeID, out _CurrentPartyMode))
                CLog.LogError("CParty: Can't find party mode ID: " + PartyModeID.ToString());

            _CurrentPartyMode.PartyMode.Init();
        }

        public static CMenu GetNextPartyScreen(out EScreens AlternativeScreen)
        {
            return _CurrentPartyMode.PartyMode.GetNextPartyScreen(out AlternativeScreen);
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

        public static ScreenSongOptions GetSongSelectionOptions()
        {
            return _CurrentPartyMode.PartyMode.GetScreenSongOptions();
        }

        public static void OnSongChange(int SongIndex, ref ScreenSongOptions ScreenSongOptions)
        {
            _CurrentPartyMode.PartyMode.OnSongChange(SongIndex, ref ScreenSongOptions);
        }

        public static void OnCategoryChange(int CategoryIndex, ref ScreenSongOptions ScreenSongOptions)
        {
            _CurrentPartyMode.PartyMode.OnCategoryChange(CategoryIndex,  ref ScreenSongOptions);
        }

        public static void SetSearchString(string SearchString, bool Visible)
        {
            _CurrentPartyMode.PartyMode.SetSearchString(SearchString, Visible);
        }

        public static void JokerUsed(int TeamNr)
        {
            _CurrentPartyMode.PartyMode.JokerUsed(TeamNr);
        }

        public static void SongSelected(int SongID)
        {
            _CurrentPartyMode.PartyMode.SongSelected(SongID);
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
        private static void LoadPartyModes()
        {
            List<string> files = new List<string>();
            files.AddRange(CHelper.ListFiles(CSettings.sFolderPartyModes, "*.xml", false, true));

            foreach (string file in files)
            {
                SPartyMode pm = LoadPartyMode(file);
                _PartyModes.Add(pm.PartyModeID, pm);
            }
        }

        private static SPartyMode LoadPartyMode(string file)
        {
            SPartyMode pm =  new SPartyMode();
            pm.PartyModeID = _IDs.Dequeue();
            pm.ScreenFiles = new List<string>();
            pm.NoErrors = false;

            CXMLReader xmlReader = CXMLReader.OpenFile(file);

            //Error...
            if (xmlReader == null)
                return pm;

            bool loaded = true;

            loaded &= xmlReader.TryGetIntValue("//root/PartyModeSystemVersion", ref pm.PartyModeSystemVersion);                
            loaded &= xmlReader.GetValue("//root/Info/Name", ref pm.Name, "ERROR Name");
            loaded &= xmlReader.GetValue("//root/Info/Description", ref pm.Description, "ERROR Description");
            loaded &= xmlReader.GetValue("//root/Info/Author", ref pm.Author, "ERROR Author");
            loaded &= xmlReader.GetValue("//root/Info/Folder", ref pm.Folder, "ERROR Folder");
            loaded &= xmlReader.GetValue("//root/Info/PartyModeFile", ref pm.PartyModeFile, "ERROR PartyModeFile");
            loaded &= xmlReader.GetInnerValues("PartyScreens", ref pm.ScreenFiles);
            loaded &= xmlReader.TryGetIntValue("//root/Info/PartyModeVersionMajor", ref pm.PartyModeVersionMajor);
            loaded &= xmlReader.TryGetIntValue("//root/Info/PartyModeVersionMinor", ref pm.PartyModeVersionMinor);
            loaded &= xmlReader.GetValue("//root/Info/TargetAudience", ref pm.TargetAudience, "ERROR TargetAudience");

            if (!loaded)
            {
                CLog.LogError("Error loading PartyMode file: " + file);
                return pm;
            }

            if (pm.PartyModeSystemVersion != PartyModeSystemVersion)
            {
                CLog.LogError("Error loading PartyMode file (wrong PartyModeSystemVersion): " + file);
                return pm;
            }

            if (pm.ScreenFiles.Count == 0)
            {
                CLog.LogError("Error loading PartyMode file (no ScreenFiles found): " + file);
                return pm;
            }

            string PathToCode = Path.Combine(Path.Combine(CSettings.sFolderPartyModes, pm.Folder), CSettings.sFolderPartyModeCode);

            List<string> FilesToCompile = new List<string>();
            FilesToCompile.AddRange(CHelper.ListFiles(PathToCode, "*.cs", false, true));
            
            Assembly Output = CompileFiles(FilesToCompile.ToArray());
            if (Output == null)
                return pm;

            if (!CLanguage.LoadPartyLanguageFiles(pm.PartyModeID, Path.Combine(Path.Combine(CSettings.sFolderPartyModes, pm.Folder), CSettings.sFolderPartyModeLanguages)))
            {
                CLog.LogError("Error loading language files for PartyMode: " + file);
                return pm;
            }

            object Instance = Output.CreateInstance("Vocaluxe.PartyModes." + pm.PartyModeFile);
            if (Instance == null)
            {
                CLog.LogError("Error creating Instance of PartyMode file: " + file);
                return pm;
            }

            try
            {
                pm.PartyMode = (IPartyMode)Instance;
            }
            catch (Exception e)
            {
                CLog.LogError("Error casting PartyMode file: " + file + "; " + e.Message);
                return pm;
            }
            pm.PartyMode.Initialize();

            if (!CTheme.AddTheme(Path.Combine(Directory.GetCurrentDirectory(), Path.Combine(Path.Combine(CSettings.sFolderPartyModes, pm.Folder), "Theme.xml")), pm.PartyModeID))
                return pm;

            int ThemeIndex = CTheme.GetThemeIndex(pm.PartyModeID);
            CTheme.ListSkins(ThemeIndex);
            int SkinIndex = CTheme.GetSkinIndex(pm.PartyModeID);

            if (!CTheme.LoadSkin(SkinIndex))
                return pm;

            if (!CTheme.LoadTheme(ThemeIndex))
                return pm;

            foreach (string screenfile in pm.ScreenFiles)
            {
                string XmlPath = Path.Combine(Path.Combine(CSettings.sFolderPartyModes, pm.Folder), CSettings.sFolderPartyModeScreens);
                CMenuParty Screen = GetPartyScreenInstance(
                    Output,
                    screenfile,
                    Path.Combine(Path.Combine(CSettings.sFolderPartyModes, pm.Folder), CSettings.sFolderPartyModeScreens)
                    );

                if (Screen != null)
                {
                    Screen.Initialize();
                    Screen.AssingPartyMode(pm.PartyMode);
                    Screen.SetPartyModeID(pm.PartyModeID);
                    Screen.LoadTheme(XmlPath);
                    pm.PartyMode.AddScreen(Screen, screenfile);
                }
                else
                    return pm;
            }

            pm.NoErrors = true;
            return pm;
        }

        private static Assembly CompileFiles(string[] files)
        {
            if (files == null)
                return null;

            if (files.Length == 0)
                return null;

            CompilerParameters Params = new CompilerParameters();
            Params.ReferencedAssemblies.Add("System.Windows.Forms.dll");
            Params.ReferencedAssemblies.Add("VocaluxeLib.dll");
            Params.GenerateInMemory = true;
#if DEBUG
            Params.IncludeDebugInformation = true;
#endif

            CodeDomProvider CDP = CodeDomProvider.CreateProvider("CSharp");
            CompilerResults CompileResult = null;

            try
            {
                CompileResult = CDP.CompileAssemblyFromFile(Params, files);
            }
            catch (Exception e)
            {
                CLog.LogError("Error Compiling Source (" + CHelper.ListStrings(files) + "): " + e.Message);
                return null;
            }
            
            if (CompileResult.Errors.Count > 0)
            {
                for (int i = 0; i < CompileResult.Errors.Count; i++)
                {
                    CLog.LogError("Error Compiling Source (" + CHelper.ListStrings(files) + "): " + CompileResult.Errors[i].ErrorText);
                }               
                return null;
            }
            return CompileResult.CompiledAssembly;
        }

        private static CMenuParty GetPartyScreenInstance(Assembly Assembly, string ScreenName, string XmlPath)
        {
            if (Assembly == null)
                return null;

            object Instance = Assembly.CreateInstance("Vocaluxe.PartyModes." + ScreenName);
            if (Instance == null)
            {
                CLog.LogError("Error creating Instance of PartyScreen: " + ScreenName);
                return null;
            }

            CMenuParty Screen = null;
            try
            {
                Screen = (CMenuParty)Instance;
            }
            catch (Exception e)
            {
                CLog.LogError("Error casting PartyScreen: " + ScreenName + "; " + e.Message);
                return null;
            }
            return Screen;
        }
        #endregion private stuff
    }
}
