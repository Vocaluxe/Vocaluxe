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
        public int PartyModeSystemVersion;
        public string Name;
        public string Author;
        public string Folder;
        public string PartyModeFile;
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
        private static List<SPartyMode> _PartyModes;

        private static int _CurrentModeNr;
        public static int CurrentPartyModeID
        {
            get
            {
                if (_CurrentModeNr == 0)
                    return -1;

                return _CurrentModeNr - 1;
            }
        }

        #region public stuff
        public static int NumModes
        {
            get { return _PartyModes.Count - 1; }   //first mode is the dummy normal game mode
        }

        public static void Init()
        {
            Helper = new CHelper();
            _PartyModes = new List<SPartyMode>();

            //add dummy normal game mode and set it as default
            SPartyMode pm = new SPartyMode();
            pm.PartyMode = new CPartyModeNone();
            pm.PartyMode.Initialize(CMain.Base);
            _PartyModes.Add(pm);
            _CurrentModeNr = 0;

            //load other party modes
            LoadPartyModes();
        }
                
        public static void SetNormalGameMode()
        {
            _CurrentModeNr = 0;
        }

        public static CMenu GetNextPartyScreen()
        {
            CMenu NextScreen = _PartyModes[_CurrentModeNr].PartyMode.GetNextPartyScreen();
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
            return _PartyModes[_CurrentModeNr].PartyMode.GetStartScreen();
        }

        public static EScreens GetMainScreen()
        {
            return _PartyModes[_CurrentModeNr].PartyMode.GetMainScreen();
        }

        public static ScreenSongOptions GetSongSelectionOptions()
        {
            return _PartyModes[_CurrentModeNr].PartyMode.GetScreenSongOptions();
        }

        public static void SetSearchString(string SearchString, bool Visible)
        {
            _PartyModes[_CurrentModeNr].PartyMode.SetSearchString(SearchString, Visible);
        }

        public static void JokerUsed(int TeamNr)
        {
            _PartyModes[_CurrentModeNr].PartyMode.JokerUsed(TeamNr);
        }
        #endregion Interface

        #region private stuff
        private static void LoadPartyModes()
        {
            List<string> files = new List<string>();
            files.AddRange(Helper.ListFiles(CSettings.sFolderPartyModes, "*.xml", false, true));

            foreach (string file in files)
            {
                _PartyModes.Add(LoadPartyMode(file));
            }
        }

        private static SPartyMode LoadPartyMode(string file)
        {
            SPartyMode pm =  new SPartyMode();
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
                loaded &= CHelper.TryGetIntValueFromXML("//root/Info/PartyModeVersionMajor", navigator, ref pm.PartyModeVersionMajor);
                loaded &= CHelper.TryGetIntValueFromXML("//root/Info/PartyModeVersionMinor", navigator, ref pm.PartyModeVersionMinor);

                if (pm.PartyModeSystemVersion != PartyModeSystemVersion)
                {
                    CLog.LogError("Error loading PartyMode file (wrong PartyModeSystemVersion): " + file);
                    return pm;
                }
            }

            if (!loaded)
            {
                CLog.LogError("Error loading PartyMode file: " + file);
                return pm;
            }


            Assembly Output = CompileFile(Path.Combine(Path.Combine(Path.Combine(CSettings.sFolderPartyModes, pm.Folder), CSettings.sFolderPartyModeCode), pm.PartyModeFile + ".cs"));
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
            pm.NoErrors = true;
            return pm;
        }

        private static Assembly CompileFile(string file)
        {
            CompilerParameters Params = new CompilerParameters();
            Params.ReferencedAssemblies.Add("VocaluxeLib.dll");
            Params.GenerateInMemory = true;
#if DEBUG
            Params.IncludeDebugInformation = true;
#endif

            CodeDomProvider CDP = CodeDomProvider.CreateProvider("CSharp");
            CompilerResults CompileResult = null;
            try
            {
                CompileResult = CDP.CompileAssemblyFromFile(Params, file);
            }
            catch (Exception e)
            {
                CLog.LogError("Error Compiling Source (" + file + "): " + e.Message);
                return null;
            }
            
            if (CompileResult.Errors.Count > 0)
            {
                for (int i = 0; i < CompileResult.Errors.Count; i++)
                {
                    CLog.LogError("Error Compiling Source (" + file + "): " + CompileResult.Errors[i].ErrorText);
                }               
                return null;
            }
            return CompileResult.CompiledAssembly;
        }

        #endregion private stuff
    }
}
