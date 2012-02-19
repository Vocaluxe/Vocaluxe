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

            _GameMode = EGameMode.Normal;
            _Initialized = true;
        }
    }
}
