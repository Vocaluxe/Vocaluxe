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
using System.Diagnostics;
using System.IO;
using System.Linq;
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

    abstract class CTheme
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

        public readonly int PartyModeID;

        protected readonly string _Folder;
        private readonly string _FileName;

        public CSkin CurrentSkin { get; protected set; }

        protected readonly Dictionary<String, CSkin> _Skins = new Dictionary<string, CSkin>();

        private bool _IsLoaded;

        protected CTheme(string filePath, int partyModeID)
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
                CSkin skin = _GetNewSkin(path, file);
                if (skin.Init())
                {
                    _Skins.Add(skin.Name, skin);
                    ok = true;
                }
            }
            return ok;
        }

        protected abstract CSkin _GetNewSkin(string path, string file);
        protected abstract bool _Load(CXMLReader xmlReader);
        protected abstract bool _LoadSkin();

        public bool Load()
        {
            if (_IsLoaded)
                return true;
            CXMLReader xmlReader = CXMLReader.OpenFile(Path.Combine(_Folder, _FileName));
            if (xmlReader == null)
                return false;

            if (!_LoadSkin())
                return false;
            bool ok = _Load(xmlReader);

            _IsLoaded = ok;
            return ok;
        }

        public virtual void Unload()
        {
            if (CurrentSkin != null)
            {
                CurrentSkin.Unload();
                CurrentSkin = null;
            }
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