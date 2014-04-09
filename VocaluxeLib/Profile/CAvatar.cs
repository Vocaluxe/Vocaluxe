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

using VocaluxeLib.Draw;

namespace VocaluxeLib.Profile
{
    public class CAvatar
    {
        public int ID;
        private string _FileName = "";
        private CTexture _Texture;

        public CTexture Texture
        {
            get { return _Texture; }
        }
        public string FileName
        {
            get { return _FileName; }
        }

        public static CAvatar GetAvatar(string fileName)
        {
            CTexture texture = CBase.Drawing.AddTexture(fileName);
            return texture == null ? null : new CAvatar(texture, fileName);
        }

        private CAvatar(CTexture texture, string fileName, int id = -1)
        {
            ID = id;
        }

        public bool LoadFromFile(string fileName)
        {
            _FileName = fileName;
            return Reload();
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
    }
}