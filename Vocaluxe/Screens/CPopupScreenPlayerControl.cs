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
    class CPopupScreenPlayerControl : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        const int ScreenVersion = 1;

        private const string StaticBG = "StaticBG";

        private const string ButtonNext = "ButtonNext";

        public CPopupScreenPlayerControl()
        {
            Init();
        }

        protected override void Init()
        {
            base.Init();

            _ThemeName = "PopupScreenPlayerControl";
            _ScreenVersion = ScreenVersion;

            _ThemeStatics = new string[] { StaticBG };

            List<string> buttons = new List<string>();
            buttons.Add(ButtonNext);
            _ThemeButtons = buttons.ToArray();
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
                        CGraphics.HidePopup(EPopupScreens.PopupPlayerControl);
                        return false;

                    case Keys.Enter:
                        if (Buttons[htButtons(ButtonNext)].Selected)
                            CBackgroundMusic.Next();
                        break;
                }
            }

            return true;
        }

        public override bool HandleMouse(MouseEvent MouseEvent)
        {
            if (MouseEvent.LB && IsMouseOver(MouseEvent))
            {
                if (Buttons[htButtons(ButtonNext)].Selected)
                    CBackgroundMusic.Next();

            } else if (MouseEvent.LB)
            {
                CGraphics.HidePopup(EPopupScreens.PopupPlayerControl);
                return false;
            } else if (MouseEvent.RB)
            {
                CGraphics.HidePopup(EPopupScreens.PopupPlayerControl);
                return false;
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
            if (!_Active)
                return false;

            return base.Draw();
        }
    }
}
