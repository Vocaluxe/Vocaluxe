using System;
using System.IO;
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    class CScreenTest : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private int _TestMusic = -1;

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            if (keyEvent.KeyPressed && !Char.IsControl(keyEvent.Unicode)) {}
            else
            {
                switch (keyEvent.Key)
                {
                    case Keys.Escape:
                    case Keys.Back:
                        CGraphics.FadeTo(EScreens.ScreenMain);
                        break;

                    case Keys.Enter:
                        CGraphics.FadeTo(EScreens.ScreenMain);
                        break;

                    case Keys.F:
                        //FadeAndPause();
                        break;

                    case Keys.S:
                        //PlayFile();
                        break;

                    case Keys.P:
                        //PauseFile();
                        break;
                }
            }

            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            if (mouseEvent.LB && IsMouseOver(mouseEvent)) {}

            if (mouseEvent.LB)
                CGraphics.FadeTo(EScreens.ScreenMain);

            if (mouseEvent.RB)
                CGraphics.FadeTo(EScreens.ScreenMain);
            return true;
        }

        public override bool UpdateGame()
        {
            return true;
        }

        private void _PlayFile()
        {
            if (_TestMusic == -1)
                _TestMusic = CSound.Load(Path.Combine(Environment.CurrentDirectory, "Test.mp3"));

            CSound.Play(_TestMusic);
            CSound.Fade(_TestMusic, 100f, 2f);
        }

        private void _PauseFile()
        {
            CSound.Pause(_TestMusic);
        }

        private void _FadeAndPause()
        {
            CSound.FadeAndPause(_TestMusic, 0f, 2f);
        }
    }
}