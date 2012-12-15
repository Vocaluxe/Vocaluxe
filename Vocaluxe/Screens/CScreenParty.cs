using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.Menu;

namespace Vocaluxe.Screens
{
    class CScreenParty : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        const int ScreenVersion = 1;

        const string TextDescription = "TextDescription";
        const string ButtonStart = "ButtonStart";
        const string ButtonExit = "ButtonExit";
        const string SelectSlideModes = "SelectSlideModes";

        private List<SPartyModeInfos> _PartyModeInfos;

        public CScreenParty()
        {
        }

        protected override void Init()
        {
            base.Init();

            _ThemeName = "ScreenParty";
            _ScreenVersion = ScreenVersion;
            _ThemeTexts = new string[] { TextDescription };
            _ThemeButtons = new string[] { ButtonStart, ButtonExit };
            _ThemeSelectSlides = new string[] { SelectSlideModes };
        }

        public override void LoadTheme(string XmlPath)
        {
            base.LoadTheme(XmlPath);   
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

                    case Keys.Enter:
                        if (Buttons[htButtons(ButtonStart)].Selected)
                            StartPartyMode();

                        if (Buttons[htButtons(ButtonExit)].Selected)
                            CGraphics.FadeTo(EScreens.ScreenMain);
                        break;

                    case Keys.Left:
                    case Keys.Right:
                        if (SelectSlides[htSelectSlides(SelectSlideModes)].Selected)
                            UpdateSelection();
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
                if (Buttons[htButtons(ButtonStart)].Selected)
                    StartPartyMode();

                if (Buttons[htButtons(ButtonExit)].Selected)
                    CGraphics.FadeTo(EScreens.ScreenMain);

                if (SelectSlides[htSelectSlides(SelectSlideModes)].Selected)
                    UpdateSelection();
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

            _PartyModeInfos = CParty.GetPartyModeInfos();

            SelectSlides[htSelectSlides(SelectSlideModes)].Clear();
            foreach (SPartyModeInfos info in _PartyModeInfos)
            {
                SelectSlides[htSelectSlides(SelectSlideModes)].AddValue(info.Name, info.PartyModeID);
            }
            SelectSlides[htSelectSlides(SelectSlideModes)].Selection = 0;
            UpdateSelection();
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

        private void UpdateSelection()
        {
            if (_PartyModeInfos.Count == 0)
                return;

            int index = SelectSlides[htSelectSlides(SelectSlideModes)].Selection;
            if (index >= _PartyModeInfos.Count)
                return;

            Texts[htTexts(TextDescription)].PartyModeID = _PartyModeInfos[index].PartyModeID;
            Texts[htTexts(TextDescription)].Text = _PartyModeInfos[index].Description;
        }

        private void StartPartyMode()
        {
            if (_PartyModeInfos.Count == 0)
                return;

            int index = SelectSlides[htSelectSlides(SelectSlideModes)].Selection;
            if (index >= _PartyModeInfos.Count)
                return;

            CParty.SetPartyMode(_PartyModeInfos[index].PartyModeID);
            CGraphics.FadeTo(EScreens.ScreenPartyDummy);
        }
    }
}
