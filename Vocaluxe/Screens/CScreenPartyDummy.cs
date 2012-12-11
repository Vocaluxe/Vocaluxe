using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.Menu;

namespace Vocaluxe.Screens
{
    class CScreenPartyDummy : CMenu
    {// Version number for theme files. Increment it, if you've changed something on the theme files!
        const int ScreenVersion = 1;

        public CScreenPartyDummy()
        {
            Init();
        }

        protected override void Init()
        {
            base.Init();

            _ThemeName = "ScreenPartyDummy";
            _ScreenVersion = ScreenVersion;
        }

        public override void LoadTheme()
        {
        }

        public override void ReloadTheme()
        {
        }

        public override void ReloadTextures()
        {
        }

        public override void SaveTheme()
        {
        }

        public override void UnloadTextures()
        {
        }

        public override bool HandleInput(KeyEvent KeyEvent)
        {
            base.HandleInput(KeyEvent);

            if (KeyEvent.KeyPressed)
            {

            }
            else
            {
                switch (KeyEvent.Key)
                {
                    case Keys.Back:
                    case Keys.Escape:
                        CGraphics.FadeTo(EScreens.ScreenParty);
                        break;
                }
            }
            return true;
        }

        public override bool HandleMouse(MouseEvent MouseEvent)
        {
            base.HandleMouse(MouseEvent);

            if (MouseEvent.LB && IsMouseOver(MouseEvent))
            {

            }

            if (MouseEvent.RB)
            {
                CGraphics.FadeTo(EScreens.ScreenParty);
            }

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();
        }

        public override bool UpdateGame()
        {
            return true;
        }

        public override bool Draw()
        {
            base.Draw();

            CFonts.SetFont("Normal");
            CFonts.Style = EStyle.Normal;
            CDraw.DrawText("SOMETHING WENT WRONG!", 150, 300, 80);

            return true;
        }

        public override void OnClose()
        {
            base.OnClose();
        }
    }
}
