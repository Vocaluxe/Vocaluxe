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

using System.Collections.Generic;
using VocaluxeLib.Draw;

namespace VocaluxeLib.Menu.SongMenu
{
    public class CCategory
    {
        public readonly string Name;
        private CTexture _CoverTextureSmall = new CTexture(-1);
        private CTexture _CoverTextureBig = new CTexture(-1);
        private bool _CoverBigLoaded;
        public readonly List<CSongPointer> Songs = new List<CSongPointer>();

        public CCategory(string name)
        {
            Name = name;
        }

        public CTexture CoverTextureSmall
        {
            get { return _CoverTextureSmall; }

            set { _CoverTextureSmall = value; }
        }

        public CTexture CoverTextureBig
        {
            get { return _CoverBigLoaded ? _CoverTextureBig : _CoverTextureSmall; }
            set
            {
                if (value.Index == -1)
                    return;
                _CoverTextureBig = value;
                _CoverBigLoaded = true;
            }
        }

        public CCategory(string name, CTexture coverSmall, CTexture coverBig)
        {
            Name = name;
            CoverTextureSmall = coverSmall;
            CoverTextureBig = coverBig;
        }

        public CCategory(CCategory cat)
        {
            Name = cat.Name;
            CoverTextureSmall = cat.CoverTextureSmall;
            CoverTextureBig = cat.CoverTextureBig;
        }
    }
}