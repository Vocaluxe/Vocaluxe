﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Vocaluxe.GameModes
{
    // Nomal singing
    class CGameModeNormal : CGameMode
    {
        public override void Init()
        {
            base.Init();

            _Initialized = true;
        }
    }
}
