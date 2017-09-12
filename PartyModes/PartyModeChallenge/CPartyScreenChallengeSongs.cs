using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VocaluxeLib.Menu;

namespace VocaluxeLib.PartyModes.Challenge
{
    class CPartyScreenChallengeSongs : CMenuPartySongSelection
    {
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private new CPartyModeChallenge _PartyMode;

        public override void Init()
        {
            base.Init();
            _PartyMode = (CPartyModeChallenge)base._PartyMode;
        }

        public override void OnShow()
        {
            Source = _PartyMode.GameData.SongSource;
            Sorting = _PartyMode.GameData.Sorting;
            Category = _PartyMode.GameData.CategoryIndex;
            Playlist = _PartyMode.GameData.PlaylistID;
            SongMode = _PartyMode.GameData.GameMode;
            NumMedleySongs = _PartyMode.GameData.NumMedleySongs;

            base.OnShow();
        }

        public override void Back()
        {
            _SaveConfig();
            _PartyMode.Back();
        }

        public override void Next()
        {
            _SaveConfig();
            _PartyMode.Next();
        }

        public override bool UpdateGame()
        {
            return true;
        }

        protected override void _SetAllowedOptions()
        {
            base._SetAllowedOptions();

            AllowedSongModes = new EGameMode[] { EGameMode.TR_GAMEMODE_NORMAL, EGameMode.TR_GAMEMODE_SHORTSONG, EGameMode.TR_GAMEMODE_MEDLEY };
        }

        private void _SaveConfig()
        {
            _PartyMode.GameData.SongSource = Source;
            _PartyMode.GameData.Sorting = Sorting;
            _PartyMode.GameData.CategoryIndex = Category;
            _PartyMode.GameData.PlaylistID = Playlist;
            _PartyMode.GameData.GameMode = SongMode;
            _PartyMode.GameData.NumMedleySongs = NumMedleySongs;
        }
    }
}
