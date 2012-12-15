using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.XPath;

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
        public string Author;
        public string Folder;
        public string PartyModeFile;
        public List<string> ScreenFiles;
        public int PartyModeVersionMajor;
        public int PartyModeVersionMinor;
        public bool NoErrors;
        public IPartyMode PartyMode;
    }
    #endregion Structs


    static class CParty
    {
        const int PartyModeSystemVersion = 1;

        private static CHelper Helper;
        private static Dictionary<int, SPartyMode> _PartyModes;
        private static Queue<int> _IDs;

        private static int _NormalGameModeID;
        private static SPartyMode _CurrentPartyMode;

        public static int CurrentPartyModeID
        {
            get { return _CurrentPartyMode.PartyModeID; }
        }

        #region public stuff
        public static int NumModes
        {
            get { return 0 ; }   //first mode is the dummy normal game mode
        }

        public static void Init()
        {
            Helper = new CHelper();
            _PartyModes = new Dictionary<int, SPartyMode>();
            _IDs = new Queue<int>(1000);

            for (int i = 0; i < 1000; i++)
                _IDs.Enqueue(i);

            //add dummy normal game mode and set it as default
            SPartyMode pm = new SPartyMode();
            pm.PartyMode = new CPartyModeNone();
            pm.ScreenFiles = new List<string>();
            pm.PartyMode.Initialize(CMain.Base);
            pm.PartyModeID = _IDs.Dequeue();
            _NormalGameModeID = pm.PartyModeID;
            _PartyModes.Add(pm.PartyModeID, pm);
            _CurrentPartyMode = pm;

            //load other party modes
            LoadPartyModes();
        }
                
        public static void SetNormalGameMode()
        {
            SetPartyMode(_NormalGameModeID);
        }

        public static void SetPartyMode(int PartyModeID)
        {
            if (!_PartyModes.TryGetValue(PartyModeID, out _CurrentPartyMode))
                CLog.LogError("CParty: Can't find party mode ID: " + PartyModeID.ToString());
        }

        public static CMenu GetNextPartyScreen()
        {
            CMenu NextScreen = _CurrentPartyMode.PartyMode.GetNextPartyScreen();
            if (NextScreen != null)
                return NextScreen;

            NextScreen = new CScreenPartyDummy();
            NextScreen.Initialize(CMain.Base);
            NextScreen.LoadTheme();
            return NextScreen;
        }
        #endregion public stuff

        #region Interface
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

        public static void SetSearchString(string SearchString, bool Visible)
        {
            _CurrentPartyMode.PartyMode.SetSearchString(SearchString, Visible);
        }

        public static void JokerUsed(int TeamNr)
        {
            _CurrentPartyMode.PartyMode.JokerUsed(TeamNr);
        }
        #endregion Interface

        #region private stuff
        private static void LoadPartyModes()
        {
            List<string> files = new List<string>();
            files.AddRange(Helper.ListFiles(CSettings.sFolderPartyModes, "*.xml", false, true));

            foreach (string file in files)
            {
                SPartyMode pm = LoadPartyMode(file);
                pm.PartyModeID = _IDs.Dequeue();
                _PartyModes.Add(pm.PartyModeID, pm);
            }
        }

        private static SPartyMode LoadPartyMode(string file)
        {
            SPartyMode pm =  new SPartyMode();
            pm.ScreenFiles = new List<string>();
            pm.NoErrors = false;

            bool loaded = false;
            XPathDocument xPathDoc = null;
            XPathNavigator navigator = null;

            try
            {
                xPathDoc = new XPathDocument(file);
                navigator = xPathDoc.CreateNavigator();
                loaded = true;
            }
            catch (Exception e)
            {
                loaded = false;
                if (navigator != null)
                    navigator = null;

                if (xPathDoc != null)
                    xPathDoc = null;

                CLog.LogError("Error opening party mode file " + file + ": " + e.Message);
            }

            if (loaded)
            {
                loaded &= CHelper.TryGetIntValueFromXML("//root/PartyModeSystemVersion", navigator, ref pm.PartyModeSystemVersion);                
                loaded &= CHelper.GetValueFromXML("//root/Info/Name", navigator, ref pm.Name, "ERROR Name");
                loaded &= CHelper.GetValueFromXML("//root/Info/Author", navigator, ref pm.Author, "ERROR Author");
                loaded &= CHelper.GetValueFromXML("//root/Info/Folder", navigator, ref pm.Folder, "ERROR Folder");
                loaded &= CHelper.GetValueFromXML("//root/Info/PartyModeFile", navigator, ref pm.PartyModeFile, "ERROR PartyModeFile");
                loaded &= CHelper.GetInnerValuesFromXML("PartyScreens", navigator, ref pm.ScreenFiles);
                loaded &= CHelper.TryGetIntValueFromXML("//root/Info/PartyModeVersionMajor", navigator, ref pm.PartyModeVersionMajor);
                loaded &= CHelper.TryGetIntValueFromXML("//root/Info/PartyModeVersionMinor", navigator, ref pm.PartyModeVersionMinor);

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
            }

            if (!loaded)
            {
                CLog.LogError("Error loading PartyMode file: " + file);
                return pm;
            }

            string PathToCode = Path.Combine(Path.Combine(CSettings.sFolderPartyModes, pm.Folder), CSettings.sFolderPartyModeCode);

            List<string> FilesToCompile = new List<string>();
            FilesToCompile.AddRange(Helper.ListFiles(PathToCode, "*.cs", false, true));
            
            Assembly Output = CompileFiles(FilesToCompile.ToArray());
            if (Output == null)
                return pm;

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
            pm.PartyMode.Initialize(CMain.Base);

            foreach (string screenfile in pm.ScreenFiles)
            {
                CMenuParty Screen = GetPartyScreenInstance(Output, screenfile);
                if (Screen != null)
                    pm.PartyMode.AddScreen(Screen, screenfile);
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

        private static CMenuParty GetPartyScreenInstance(Assembly Assembly, string ScreenName)
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
            Screen.Initialize(CMain.Base);
            Screen.LoadTheme();
            return Screen;
        }
        #endregion private stuff
    }
}
