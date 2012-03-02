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
        private const string Cover = "Cover";

        private const string ButtonPrevious = "ButtonPrevious";
        private const string ButtonPlay = "ButtonPlay";
        private const string ButtonPause = "ButtonPause";
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

            _ThemeStatics = new string[] { StaticBG, Cover };

            List<string> buttons = new List<string>();
            buttons.Add(ButtonPlay);
            buttons.Add(ButtonPause);
            buttons.Add(ButtonPrevious);
            buttons.Add(ButtonNext);
            _ThemeButtons = buttons.ToArray();
        }

        public override bool HandleInput(KeyEvent KeyEvent)
        {
            base.HandleInput(KeyEvent);
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
                        if (Buttons[htButtons(ButtonPrevious)].Selected)
                            CBackgroundMusic.Previous();
                        if (Buttons[htButtons(ButtonPlay)].Selected)
                            CBackgroundMusic.Play();
                        if (Buttons[htButtons(ButtonPause)].Selected)
                            CBackgroundMusic.Pause();
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
                if (Buttons[htButtons(ButtonNext)].Selected)
                    CBackgroundMusic.Next();
                if (Buttons[htButtons(ButtonPrevious)].Selected)
                    CBackgroundMusic.Previous();
                if (Buttons[htButtons(ButtonPlay)].Selected)
                    CBackgroundMusic.Play();
                if (Buttons[htButtons(ButtonPause)].Selected)
                    CBackgroundMusic.Pause();
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
            Buttons[htButtons(ButtonPause)].Visible = CBackgroundMusic.IsPlaying();
            Buttons[htButtons(ButtonPlay)].Visible = !CBackgroundMusic.IsPlaying();
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
