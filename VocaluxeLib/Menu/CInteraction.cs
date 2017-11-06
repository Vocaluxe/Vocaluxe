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

namespace VocaluxeLib.Menu
{
    public enum EType
    {
        Background,
        Button,
        SelectSlide,
        Text,
        Static,
        SongMenu,
        Lyric,
        SingNote,
        NameSelection,
        Equalizer,
        Playlist,
        ParticleEffect,
        ProgressBar
    }

    public class CInteraction
    {
        public readonly int Num;
        public readonly EType Type;

        public CInteraction(int num, EType type)
        {
            Num = num;
            Type = type;
        }
    }
}