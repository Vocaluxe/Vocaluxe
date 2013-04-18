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
        ParticleEffect
    }

    class CInteraction
    {
        private readonly int _Num;
        private readonly EType _Type;

        public int Num
        {
            get { return _Num; }
        }

        public EType Type
        {
            get { return _Type; }
        }

        public bool ThemeEditorOnly
        {
            get
            {
                return _Type == EType.Background ||
                       _Type == EType.NameSelection ||
                       _Type == EType.Text ||
                       _Type == EType.Static ||
                       _Type == EType.SongMenu ||
                       _Type == EType.Lyric ||
                       _Type == EType.SingNote ||
                       _Type == EType.Equalizer ||
                       _Type == EType.Playlist;
            }
        }

        public CInteraction(int num, EType type)
        {
            _Num = num;
            _Type = type;
        }
    }
}