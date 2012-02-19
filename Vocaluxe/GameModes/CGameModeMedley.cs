using System;
using System.Collections.Generic;
using System.Text;

namespace Vocaluxe.GameModes
{
    class CGameModeMedley : CGameMode
    {
        public override void Init()
        {
            base.Init();

            _GameMode = EGameMode.Medley;
            _Initialized = true;
        }
    }
}
