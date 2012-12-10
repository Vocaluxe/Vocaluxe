using System;
using System.Collections.Generic;
using System.Text;

using Vocaluxe.Menu;

namespace Vocaluxe.PartyModes
{
    abstract class CPartyMode : IPartyMode
    {
        #region Implementation
        public virtual void Init()
        {
        }

        public virtual EScreens GetStartScreen()
        {
            return EScreens.ScreenSong;
        }

        public virtual EScreens GetMainScreen()
        {
            return EScreens.ScreenSong;
        }

        public virtual ScreenSongOptions GetScreenSongOptions()
        {
            return new ScreenSongOptions();
        }

        public virtual void SetSearchString(string SearchString, bool Visible)
        {
        }
        #endregion Implementation
    }
}
