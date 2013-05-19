#region license
// /*
//     This file is part of Vocaluxe.
// 
//     Vocaluxe is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     Vocaluxe is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
//  */
#endregion

using VocaluxeLib.Draw;

namespace VocaluxeLib.Profile
{
    public class CAvatar
    {
        public int ID;
        public string FileName = "";
        public CTexture Texture;

        public CAvatar(int id)
        {
            ID = id;
        }

        public bool LoadFromFile(string fileName)
        {
            FileName = fileName;
            return Reload();
        }

        public bool Reload()
        {
            CBase.Drawing.RemoveTexture(ref Texture);
            Texture = CBase.Drawing.AddTexture(FileName);

            return Texture != null;
        }

        public void Unload()
        {
            CBase.Drawing.RemoveTexture(ref Texture);
        }
    }
}