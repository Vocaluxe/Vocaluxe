using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.Lib.Draw;
using Vocaluxe.Lib.Song;

namespace Vocaluxe.Menu.SongMenu
{
    class CSongMenuDreidel : CSongMenuFramework
    {
        private SRectF _ScrollRect;

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
    }
}
