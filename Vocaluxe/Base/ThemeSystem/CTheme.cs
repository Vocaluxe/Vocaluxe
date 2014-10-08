using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Vocaluxe.Base.Fonts;
using VocaluxeLib;
using VocaluxeLib.Xml;

namespace Vocaluxe.Base.ThemeSystem
{
    struct SThemeCursor
    {
        public string SkinName;

        public float W;
        public float H;

        public SThemeColor Color;
    }

    class CTheme
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        private const int _ThemeSystemVersion = 5;

        private SInfo _Info;
        public String Name
        {
            get { return _Info.Name; }
        }

        public string[] SkinNames
        {
            get { return _Skins.Keys.ToArray(); }
        }

        private readonly string _Folder;
        private readonly string _FileName;
        public readonly int PartyModeID;

        public SThemeCursor CursorTheme;
        public CSkin CurrentSkin { get; private set; }

        private readonly Dictionary<String, CSkin> _Skins = new Dictionary<string, CSkin>();

        private bool _IsLoaded;

        public CTheme(string filePath, int partyModeID)
        {
            _Folder = Path.GetDirectoryName(filePath);
            _FileName = Path.GetFileName(filePath);
            PartyModeID = partyModeID;
            CurrentSkin = null;
        }

        public override string ToString()
        {
            return Name;
        }

        public bool Init()
        {
            CXMLReader xmlReader = CXMLReader.OpenFile(Path.Combine(_Folder, _FileName));
            if (xmlReader == null)
                return false;

            if (!xmlReader.CheckVersion("//root/ThemeSystemVersion", _ThemeSystemVersion))
                return false;

            bool ok = xmlReader.Read("//root/Info", out _Info);

            if (!ok)
            {
                CLog.LogError("Can't load theme \"" + _FileName + "\". Invalid file!");
                return false;
            }

            string path = Path.Combine(_Folder, Name);
            List<string> files = CHelper.ListFiles(path, "*.xml");

            // Load skins, succeed if at least 1 skin was loaded
            ok = false;
            foreach (string file in files)
            {
                CSkin skin = new CSkin(path, file, PartyModeID);
                if (skin.Init())
                {
                    _Skins.Add(skin.Name, skin);
                    ok = true;
                }
            }
            return ok;
        }

        public bool Load()
        {
            if (_IsLoaded)
                return true;
            CXMLReader xmlReader = CXMLReader.OpenFile(Path.Combine(_Folder, _FileName));
            if (xmlReader == null)
                return false;

            if (!_LoadSkin())
                return false;
            bool ok;
            if (PartyModeID == -1)
            {
                ok = _LoadCursor(xmlReader);
                ok &= CFonts.LoadThemeFonts(Name, Path.Combine(_Folder, Name, CSettings.FolderNameThemeFonts), xmlReader);
            }
            else
                ok = CFonts.LoadPartyModeFonts(PartyModeID, Path.Combine(_Folder, "..", CSettings.FolderNamePartyModeFonts), xmlReader);
            _IsLoaded = ok;
            return ok;
        }

        private bool _LoadSkin()
        {
            CSkin skin;
            if (!_Skins.TryGetValue(CConfig.Skin, out skin))
            {
                if (PartyModeID < 0)
                    skin = _Skins.Values.FirstOrDefault();
                else if (Name == CSettings.DefaultName)
                {
                    if (!_Skins.TryGetValue(CSettings.DefaultName, out skin))
                        skin = _Skins.Values.FirstOrDefault();
                }
            }
            while (skin != null)
            {
                if (skin.Load())
                    break;
                skin.Unload();
                CLog.LogError("Failed to load skin " + Name + ":" + skin + "! Removing...", true);
                _Skins.Remove(skin.Name);
                skin = (PartyModeID == -1 || Name == CSettings.DefaultName) ? _Skins.Values.FirstOrDefault() : null;
            }
            if (skin == null)
                return false;
            CurrentSkin = skin;
            return true;
        }

        private bool _LoadCursor(CXMLReader xmlReader)
        {
            bool ok = xmlReader.GetValue("//root/Cursor/Skin", out CursorTheme.SkinName);

            ok &= xmlReader.TryGetFloatValue("//root/Cursor/W", ref CursorTheme.W);
            ok &= xmlReader.TryGetFloatValue("//root/Cursor/H", ref CursorTheme.H);

            if (!xmlReader.GetValue("//root/Cursor/Color/Name", out CursorTheme.Color.Name))
                ok &= xmlReader.TryGetColorFromRGBA("//root/Cursor/Color/Color", out CursorTheme.Color.Color);

            return ok;
        }

        public void Unload()
        {
            if (CurrentSkin != null)
            {
                CurrentSkin.Unload();
                CurrentSkin = null;
            }
            if (PartyModeID == -1)
                CFonts.UnloadThemeFonts(Name);
            else
                CFonts.UnloadPartyModeFonts(PartyModeID);
            _IsLoaded = false;
        }

        public string GetScreenPath()
        {
            return Path.Combine(_Folder, Name, CSettings.FolderNameScreens);
        }

        public void ReloadSkin()
        {
            if (CurrentSkin != null)
            {
                CurrentSkin.Unload();
                CurrentSkin = null;
            }
            bool ok = _LoadSkin();
            Debug.Assert(ok);
        }
    }
}