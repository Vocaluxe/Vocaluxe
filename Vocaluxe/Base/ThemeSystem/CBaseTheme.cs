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

using System.Diagnostics;
using System.IO;
using System.Linq;
using Vocaluxe.Base.Fonts;
using VocaluxeLib.Log;

namespace Vocaluxe.Base.ThemeSystem
{
    class CBaseTheme : CTheme
    {
        public SThemeCursor CursorTheme
        {
            get
            {
                Debug.Assert(_Data.Cursor != null, "Check in _Load!");
                return _Data.Cursor.Value;
            }
        }

        public CBaseTheme(string filePath) : base(filePath, -1) {}

        protected override CSkin _GetNewSkin(string path, string file)
        {
            return new CBaseSkin(path, file, this);
        }

        public override void Unload()
        {
            base.Unload();
            CFonts.UnloadThemeFonts(Name);
        }

        protected override bool _Load()
        {
            if (!_Data.Cursor.HasValue)
                return false;

            return CFonts.LoadThemeFonts(_Data.Fonts, Path.Combine(_Folder, Name, CSettings.FolderNameThemeFonts), Name, -1);
        }

        protected override bool _LoadSkin()
        {
            CSkin skin;
            if (!_Skins.TryGetValue(CConfig.Config.Theme.Skin, out skin))
                skin = _Skins.Values.FirstOrDefault();
            while (skin != null)
            {
                if (skin.Load())
                    break;
                skin.Unload();
                CLog.Error("Failed to load skin " + skin + "! Removing...", true);
                _Skins.Remove(skin.Name);
                skin = _Skins.Values.FirstOrDefault();
            }
            if (skin == null)
                return false;
            CurrentSkin = skin;
            return true;
        }
    }
}