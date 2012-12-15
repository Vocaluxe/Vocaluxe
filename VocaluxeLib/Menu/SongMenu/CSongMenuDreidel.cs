using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Vocaluxe.Menu.SongMenu
{
    class CSongMenuDreidel : CSongMenuFramework
    {
        new private Basic _Base;
        /*
        private SRectF _ScrollRect;
        private CStatic _CoverBig;
        private CStatic _TextBG;

        private CStatic _DuetIcon;
        private CStatic _VideoIcon;

        private CText _Artist;
        private CText _Title;
        private CText _SongLength;

        private int _actualSelection = -1;

        public override int GetActualSelection()
        {
            return _actualSelection;
        }
        */

        public CSongMenuDreidel(Basic Base, int PartyModeID)
            : base(Base, PartyModeID)
        {
            _Base = Base;
        }

        public override void Init()
        {
            base.Init();
            /**
            _CoverBig = _Theme.songMenuDreidel.StaticCoverBig;
            _TextBG = _Theme.songMenuDreidel.StaticTextBG;
            _DuetIcon = _Theme.songMenuDreidel.StaticDuetIcon;
            _VideoIcon = _Theme.songMenuDreidel.StaticVideoIcon;

            _Artist = _Theme.songMenuDreidel.TextArtist;
            _Title = _Theme.songMenuDreidel.TextTitle;
            _SongLength = _Theme.songMenuDreidel.TextSongLength;
             **/
        }
    }
}
