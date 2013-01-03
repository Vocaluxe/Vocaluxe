using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Menu;

namespace Vocaluxe.PartyModes
{
    public class CScreenPartyDummy : CMenuParty
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        const int ScreenVersion = 1;
        private CText Warning;

        public CScreenPartyDummy()
        {
        }

        protected override void Init()
        {
            base.Init();

            _ThemeName = "ScreenPartyDummy";
            _ScreenVersion = ScreenVersion;
        }

        public override void LoadTheme(string XmlPath)
        {
            Warning = GetNewText();
            Warning.Height = 100f;
            Warning.X = 150;
            Warning.Y = 300;
            Warning.Fon = "Normal";
            Warning.Style = EStyle.Normal;
            Warning.Color = new SColorF(1f, 0f, 0f, 1f);
            Warning.SColor = new SColorF(1f, 0f, 0f, 1f);
            Warning.Text = "SOMETHING WENT WRONG!";
            AddText(Warning);
        }

        public override void ReloadTheme(string XmlPath)
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
                        FadeTo(EScreens.ScreenParty);
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
                FadeTo(EScreens.ScreenParty);
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
            return true;
        }

        public override void OnClose()
        {
            base.OnClose();
        }
    }
}
