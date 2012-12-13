using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.Menu;

namespace Vocaluxe.Screens
{
    class CScreenParty : CMenu
    {// Version number for theme files. Increment it, if you've changed something on the theme files!
        const int ScreenVersion = 1;

        const string ButtonExit = "ButtonExit";

        public CScreenParty()
        {
        }

        protected override void Init()
        {
            base.Init();

            _ThemeName = "ScreenParty";
            _ScreenVersion = ScreenVersion;
            _ThemeButtons = new string[] { ButtonExit };
        }

        public override void LoadTheme()
        {
            base.LoadTheme();            
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
                        CGraphics.FadeTo(EScreens.ScreenMain);
                        break;
                    
                    case Keys.C:
                        //CGraphics.FadeTo(EScreens.ScreenPartyDummy);
                        break;

                    case Keys.Enter:
                        if (Buttons[htButtons(ButtonExit)].Selected)
                            CGraphics.FadeTo(EScreens.ScreenMain);
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

                if (Buttons[htButtons(ButtonExit)].Selected)
                    CGraphics.FadeTo(EScreens.ScreenMain);
            }

            if (MouseEvent.RB)
            {
                CGraphics.FadeTo(EScreens.ScreenMain);
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
            base.DrawBG();
            base.DrawFG();

            return true;
        }

        public override void OnClose()
        {
            base.OnClose();
        }
    }
}
