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
using System.IO;
using System.Linq;
using Vocaluxe.Base.Fonts;
using VocaluxeLib.Log;

namespace Vocaluxe.Base.ThemeSystem
{
    class CPartyTheme : CTheme
    {
        public CPartyTheme(string filePath, int partyModeID) : base(filePath, partyModeID) {}

        public override void Unload()
        {
            base.Unload();
            CFonts.UnloadPartyModeFonts(PartyModeID);
        }

        protected override CSkin _GetNewSkin(string path, string file)
        {
            return new CPartySkin(path, file, this);
        }

        protected override bool _Load()
        {
            return CFonts.LoadThemeFonts(_Data.Fonts, Path.Combine(_Folder, "..", CSettings.FolderNamePartyModeFonts), Name, PartyModeID);
        }

        private CSkin _GetSkinToLoad(int fallbackNum)
        {
            CSkin skin;
            switch (fallbackNum)
            {
                case 0:
                    _Skins.TryGetValue(CConfig.Config.Theme.Skin, out skin);
                    break;
                case 1:
                    _Skins.TryGetValue(CSettings.DefaultName, out skin);
                    break;
                case 2:
                    skin = _Skins.Values.FirstOrDefault();
                    break;
                default:
                    throw new ArgumentException();
            }
            return skin;
        }

        protected override bool _LoadSkin()
        {
            CSkin skin = null;
            for (int i = 0; i < 3; i++)
            {
                skin = _GetSkinToLoad(i);
                if (skin == null)
                    continue;
                if (skin.Load())
                    break;
                skin.Unload();
                CLog.Error("Failed to load skin " + skin + "! Removing...", true);
                _Skins.Remove(skin.Name);
            }
            if (skin == null)
                return false;
            CurrentSkin = skin;
            return true;
        }
    }
}