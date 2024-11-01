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
using VocaluxeLib.Log;
using VocaluxeLib.Xml;

namespace Vocaluxe.Base.ThemeSystem
{
    abstract class CTheme
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        private const int _ThemeSystemVersion = 7;

        protected STheme _Data;
        public String Name
        {
            get { return _Data.Info.Name; }
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
            try
            {
                var xml = new CXmlDeserializer();
                _Data = xml.Deserialize<STheme>(Path.Combine(_Folder, _FileName));
                if (_Data.ThemeSystemVersion != _ThemeSystemVersion)
                {
                    string errorMsg = _Data.ThemeSystemVersion < _ThemeSystemVersion ? "the file ist outdated!" : "the file is for newer program versions!";
                    errorMsg += " Current Version is " + _ThemeSystemVersion;
                    throw new Exception(errorMsg);
                }
            }
            catch (Exception e)
            {
                CLog.Error(e, "Can't load theme \"" + _FileName + "\". Invalid file!");
                return false;
            }

            string path = Path.Combine(_Folder, Name);
            IEnumerable<string> files = CHelper.ListFiles(path, "*.xml");

            // Load skins, succeed if at least 1 skin was loaded
            bool ok = false;
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
        protected abstract bool _Load();
        protected abstract bool _LoadSkin();

        public bool Load()
        {
            if (_IsLoaded)
                return true;

            if (!_LoadSkin())
                return false;
            bool ok = _Load();

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
