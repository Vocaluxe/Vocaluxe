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
using VocaluxeLib.Log;

namespace Vocaluxe.Base.ThemeSystem
{
    class CBaseSkin : CSkin
    {
        public CBaseSkin(string folder, string file, CTheme parent) : base(folder, file, parent) {}

        public override bool Load()
        {
            if (!base.Load())
                return false;

            if (!_CheckRequiredElements())
            {
                _IsLoaded = false;
                return false;
            }

            return true;
        }

        private bool _CheckRequiredElements()
        {
            List<string> missingTextures = _Required.Textures.FindAll(name => !_Textures.ContainsKey(name));
            List<string> missingVideos = _Required.Videos.FindAll(name => !_Videos.ContainsKey(name));
            List<string> missingColors = _Required.Colors.FindAll(name => !_Data.Colors.ContainsKey(name));
            if (missingTextures.Count + missingVideos.Count + missingColors.Count == 0)
                return true;
            string msg = "The skin \"" + this + "\" is missing the following elements: ";
            if (missingTextures.Count > 0)
                msg += Environment.NewLine + "Textures: " + String.Join(", ", missingTextures);
            if (missingVideos.Count > 0)
                msg += Environment.NewLine + "Videos: " + String.Join(", ", missingVideos);
            if (missingColors.Count > 0)
                msg += Environment.NewLine + "Colors: " + String.Join(", ", missingColors);
            CLog.Error(msg);
            return false;
        }
    }
}