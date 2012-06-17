using System;
using System.Collections.Generic;
using System.Text;

namespace Vocaluxe.GameModes
{
    class CGameModeNormal : CGameMode
    {
        public override void Init()
        {
            base.Init();

            _GameMode = EGameMode.TR_GAMEMODE_NORMAL;
            _Initialized = true;
        }
    }
}
