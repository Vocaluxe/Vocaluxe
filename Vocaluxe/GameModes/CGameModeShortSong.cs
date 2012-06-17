using System;
using System.Collections.Generic;
using System.Text;

using Vocaluxe.Base;
using Vocaluxe.Lib.Song;

namespace Vocaluxe.GameModes
{
    class CGameModeShortSong : CGameMode
    {
        public override void Init()
        {
            base.Init();

            _GameMode = EGameMode.TR_GAMEMODE_SHORTSONG;
            _Initialized = true;
        }

        protected override void SongManipulation(int SongIndex)
        {
            CSong song = _Songs[SongIndex];

            song.Finish = CGame.GetTimeFromBeats(song.ShortEnd, song.BPM) + CSettings.DefaultMedleyFadeOutTime + song.Gap;

            // set lines to medley mode
            song.Notes.SetMedley(song.Notes.GetLines(0).Line[0].FirstNoteBeat, song.ShortEnd);
        }
    }
}
