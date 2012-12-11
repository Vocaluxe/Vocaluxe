using System;
using System.Collections.Generic;
using System.Text;

using Vocaluxe.Menu;
using Vocaluxe.PartyModes;

namespace Vocaluxe.Base
{
    static class CParty
    {
        private static List<IPartyMode> _PartyModes;
        private static int _CurrentModeNr;

        public static int NumModes
        {
            get { return _PartyModes.Count - 1; }   //first mode is the dummy normal game mode
        }

        public static void Init()
        {
            _PartyModes = new List<IPartyMode>();

            //add dummy normal game mode
            _PartyModes.Add(new CPartyModeNone());

            _CurrentModeNr = 0;
        }

        public static void SetNormalGameMode()
        {
            _CurrentModeNr = 0;
        }

        public static EScreens GetStartScreen()
        {
            return _PartyModes[_CurrentModeNr].GetStartScreen();
        }

        public static EScreens GetMainScreen()
        {
            return _PartyModes[_CurrentModeNr].GetMainScreen();
        }

        public static ScreenSongOptions GetSongSelectionOptions()
        {
            return _PartyModes[_CurrentModeNr].GetScreenSongOptions();
        }

        public static void SetSearchString(string SearchString, bool Visible)
        {
            _PartyModes[_CurrentModeNr].SetSearchString(SearchString, Visible);
        }

        public static void JokerUsed(int TeamNr)
        {
            _PartyModes[_CurrentModeNr].JokerUsed(TeamNr);
        }
    }
}
