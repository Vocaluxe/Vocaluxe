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
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Songs;

namespace Vocaluxe.GameModes
{
    static class CGameModes
    {
        private static Dictionary<EGameMode, IGameMode> _GameModes;

        public static void Init()
        {
            _GameModes = new Dictionary<EGameMode, IGameMode>
                {
                    {EGameMode.TR_GAMEMODE_NORMAL, new CGameModeNormal()},
                    {EGameMode.TR_GAMEMODE_DUET, new CGameModeDuet()},
                    {EGameMode.TR_GAMEMODE_SHORTSONG, new CGameModeShort()},
                    {EGameMode.TR_GAMEMODE_MEDLEY, new CGameModeMedley()}
                };
        }

        public static IGameMode Get(EGameMode gameMode)
        {
            if (_GameModes == null)
                return null;
            IGameMode result;
            if (!_GameModes.TryGetValue(gameMode, out result))
                result = _GameModes[EGameMode.TR_GAMEMODE_NORMAL];
            return result;
        }
    }

    class CGameModeNormal : CGameMode
    {
        protected override CSong _PrepareSong(CSong song)
        {
            return song;
        }
    }

    class CGameModeDuet : CGameMode
    {
        protected override CSong _PrepareSong(CSong song)
        {
            return (song.IsDuet) ? song : null;
        }
    }

    class CGameModeShort : CGameMode
    {
        protected override CSong _PrepareSong(CSong song)
        {
            CSong newSong = new CSong(song) {Finish = CGame.GetTimeFromBeats(song.ShortEnd, song.BPM) + CSettings.DefaultMedleyFadeOutTime + song.Gap};
            // set lines to short mode
            newSong.Notes.SetMedley(song.Notes.GetVoice(0).Lines[0].FirstNoteBeat, song.ShortEnd);

            return newSong;
        }
    }

    class CGameModeMedley : CGameMode
    {
        protected override CSong _PrepareSong(CSong song)
        {
            CSong newSong = new CSong(song) {Start = CGame.GetTimeFromBeats(song.Medley.StartBeat, song.BPM) - song.Medley.FadeInTime + song.Gap};
            if (newSong.Start < 0f)
                newSong.Start = 0f;

            newSong.Finish = CGame.GetTimeFromBeats(song.Medley.EndBeat, song.BPM) + song.Medley.FadeOutTime + song.Gap;

            // set lines to medley mode
            newSong.Notes.SetMedley(song.Medley.StartBeat, song.Medley.EndBeat);

            return newSong;
        }
    }
}