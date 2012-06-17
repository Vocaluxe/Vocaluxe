using System;
using System.Collections.Generic;
using System.Text;

namespace Vocaluxe.GameModes
{
    class CGameModeDuet : CGameMode
    {
        public override void Init()
        {
            base.Init();

            _GameMode = EGameMode.TR_GAMEMODE_DUET;
            _Initialized = true;
        }
    }
}
