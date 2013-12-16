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

namespace Vocaluxe.Base.Server
{
    public struct SLoginData
    {
        public byte[] Sha256;
    }

    public struct SPicture
    {
        public int Width;
        public int Height;
        public byte[] Data;
    }

    public struct SProfile
    {
        public SPicture Avatar;
        public string PlayerName;
        public int Difficulty;
    }
}