using System;
using System.Collections.Generic;
using System.Text;

using Vocaluxe.Base;
using Vocaluxe.Lib.Song;

namespace Vocaluxe.GameModes
{
    class CGameModeMedley : CGameMode
    {
        public override void Init()
        {
            base.Init();

            _GameMode = EGameMode.TR_GAMEMODE_MEDLEY;
            _Initialized = true;
        }

        protected override void SongManipulation(int SongIndex)
        {
            CSong song = _Songs[SongIndex];

            // set medley mode timings
            song.Start = CGame.GetTimeFromBeats(song.Medley.StartBeat, song.BPM) - song.Medley.FadeInTime + song.Gap;
            if (song.Start < 0f)
                song.Start = 0f;

            song.Finish = CGame.GetTimeFromBeats(song.Medley.EndBeat, song.BPM) + song.Medley.FadeOutTime + song.Gap;
            
            // set lines to medley mode
            song.Notes.SetMedley(song.Medley.StartBeat, song.Medley.EndBeat);
        }
    }
}
