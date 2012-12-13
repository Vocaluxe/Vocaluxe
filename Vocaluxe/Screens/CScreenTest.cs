using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.Lib.Draw;
using Vocaluxe.Menu;

namespace Vocaluxe.Screens
{
    class CScreenTest : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        const int ScreenVersion = 1;

        private int _TestMusic = -1;

        public CScreenTest()
        {
        }

        protected override void Init()
        {
            base.Init();

            _ThemeName = "ScreenTest";
            _ScreenVersion = ScreenVersion;
        }

        public override bool HandleInput(KeyEvent KeyEvent)
        {
            if (KeyEvent.KeyPressed && !Char.IsControl(KeyEvent.Unicode))
            {
                
            }
            else
            {
                switch (KeyEvent.Key)
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

        public override bool HandleMouse(MouseEvent MouseEvent)
        {
            if (MouseEvent.LB && IsMouseOver(MouseEvent))
            {
                
            }

            if (MouseEvent.LB)
            {
                CGraphics.FadeTo(EScreens.ScreenMain);
            }

            if (MouseEvent.RB)
            {
                CGraphics.FadeTo(EScreens.ScreenMain);
            }
            return true;
        }

        public override bool UpdateGame()
        {

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

        }

        public override bool Draw()
        {
            return base.Draw();
        }

        private void PlayFile()
        {
            if (_TestMusic == -1)
                _TestMusic = CSound.Load(Path.Combine(Environment.CurrentDirectory, "Test.mp3"));

            CSound.Play(_TestMusic);
            CSound.Fade(_TestMusic, 100f, 2f);
        }

        private void PauseFile()
        {
            CSound.Pause(_TestMusic);
        }

        private void FadeAndPause()
        {
            CSound.FadeAndPause(_TestMusic, 0f, 2f);
        }
    }
}
