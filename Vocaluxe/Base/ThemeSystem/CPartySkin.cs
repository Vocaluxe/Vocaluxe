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

using VocaluxeLib;
using VocaluxeLib.Draw;

namespace Vocaluxe.Base.ThemeSystem
{
    class CPartySkin : CSkin
    {
        private CSkin _BaseSkin;

        public CPartySkin(string folder, string file, CTheme parent) : base(folder, file, parent) {}

        public override bool Load()
        {
            _BaseSkin = CThemes.CurrentThemes[-1].CurrentSkin;
            if (!base.Load())
                return false;
            for (int i = 1; i <= CSettings.MaxNumPlayer; i++)
            {
                if (!_Data.Colors.ContainsKey("Player" + i))
                    continue;
                CLog.LogDebug("Party themes cannot contain player colors. They will be ignored!");
                break;
            }
            return true;
        }

        public override bool GetColor(string name, out SColorF color)
        {
            if (_Parent.Name == "Default" && !_Required.Colors.Contains(name))
                CLog.LogDebug("Non-Default color: " + name);
            return base.GetColor(name, out color) || _BaseSkin.GetColor(name, out color);
        }

        public override CVideoStream GetVideo(string name, bool loop)
        {
            if (_Parent.Name == "Default" && !_Required.Videos.Contains(name))
                CLog.LogDebug("Non-Default color: " + name);
            return base.GetVideo(name, loop) ?? _BaseSkin.GetVideo(name, loop);
        }

        public override CTextureRef GetTexture(string name)
        {
            if (_Parent.Name == "Default" && !_Required.Textures.Contains(name))
                CLog.LogDebug("Non-Default color: " + name);
            return base.GetTexture(name) ?? _BaseSkin.GetTexture(name);
        }
    }
}