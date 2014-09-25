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
using VocaluxeLib.Draw;

namespace VocaluxeLib.Profile
{
    public class CAvatar
    {
        public int ID;
        private readonly string _FileName = "";
        private CTextureRef _Texture;
        private const int _MaxNameLen = 12;

        public CTextureRef Texture
        {
            get { return _Texture; }
        }
        public string FileName
        {
            get { return _FileName; }
        }

        public static CAvatar GetAvatar(string fileName)
        {
            CTextureRef texture = CBase.Drawing.AddTexture(fileName);
            return texture == null ? null : new CAvatar(texture, fileName);
        }

        private CAvatar(CTextureRef texture, string fileName, int id = -1)
        {
            _Texture = texture;
            _FileName = fileName;
            ID = id;
        }

        public bool Reload()
        {
            Unload();
            _Texture = CBase.Drawing.AddTexture(_FileName);

            return _Texture != null;
        }

        public void Unload()
        {
            CBase.Drawing.RemoveTexture(ref _Texture);
        }

        public string GetDisplayName()
        {
            string name = Path.GetFileNameWithoutExtension(_FileName);
            Debug.Assert(name != null);
            if (name.Length > _MaxNameLen)
                name = name.Substring(0, _MaxNameLen);
            return name;
        }
    }
}