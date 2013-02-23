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
        const string TextTargetAudience = "TextTargetAudience";
        const string TextNumTeams = "TextNumTeams";
        const string TextNumPlayers = "TextNumPlayers";
        const string TextAuthor = "TextAuthor";
        const string TextVersion = "TextVersion";
        const string TextError = "TextError";
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
            _ThemeTexts = new string[] { TextDescription, TextTargetAudience, TextNumTeams, TextNumPlayers, TextAuthor, TextVersion, TextError };
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

            SetInteractionToSelectSlide(SelectSlides[htSelectSlides(SelectSlideModes)]);
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

            //Description    
            Texts[htTexts(TextDescription)].Text = _PartyModeInfos[index].Description;
            Texts[htTexts(TextDescription)].TranslationID = _PartyModeInfos[index].PartyModeID;

            //TargetAudience
            Texts[htTexts(TextTargetAudience)].TranslationID = _PartyModeInfos[index].PartyModeID;
            Texts[htTexts(TextTargetAudience)].Text = _PartyModeInfos[index].TargetAudience;

            //NumTeams
            if (_PartyModeInfos[index].MaxTeams == 0)
                Texts[htTexts(TextNumTeams)].Text = "TR_SCREENPARTY_NOTEAMS";
            else if (_PartyModeInfos[index].MaxTeams == _PartyModeInfos[index].MinTeams)
                Texts[htTexts(TextNumTeams)].Text = _PartyModeInfos[index].MaxTeams.ToString();
            else if (_PartyModeInfos[index].MaxTeams > _PartyModeInfos[index].MinTeams)
                Texts[htTexts(TextNumTeams)].Text = _PartyModeInfos[index].MinTeams + " - " + _PartyModeInfos[index].MaxTeams;

            //NumPlayers
            if (_PartyModeInfos[index].MaxPlayers == _PartyModeInfos[index].MinPlayers)
                Texts[htTexts(TextNumPlayers)].Text = _PartyModeInfos[index].MaxTeams.ToString();
            else if (_PartyModeInfos[index].MaxPlayers > _PartyModeInfos[index].MinPlayers)
                Texts[htTexts(TextNumPlayers)].Text = _PartyModeInfos[index].MinPlayers + " - " + _PartyModeInfos[index].MaxPlayers;

            //Author
            Texts[htTexts(TextAuthor)].Text = _PartyModeInfos[index].Author;
            Texts[htTexts(TextAuthor)].TranslationID = _PartyModeInfos[index].PartyModeID;

            //Version
            Texts[htTexts(TextVersion)].Text = _PartyModeInfos[index].VersionMajor + "." + _PartyModeInfos[index].VersionMinor;
            Texts[htTexts(TextVersion)].TranslationID = _PartyModeInfos[index].PartyModeID;

            if (!_PartyModeInfos[index].Playable)
            {
                Buttons[htButtons(ButtonStart)].Visible = false;
                Texts[htTexts(TextError)].Visible = true;
            }
            else
            {
                Buttons[htButtons(ButtonStart)].Visible = true;
                Texts[htTexts(TextError)].Visible = false;
            }
        }

        private void StartPartyMode()
        {
            if (_PartyModeInfos.Count == 0)
                return;

            int index = SelectSlides[htSelectSlides(SelectSlideModes)].Selection;
            if (index >= _PartyModeInfos.Count)
                return;

            if (CMain.Config.GetMaxNumMics() == 0)
                return; //TODO: Add message!

            if (_PartyModeInfos[index].Playable)
            {
                CParty.SetPartyMode(_PartyModeInfos[index].PartyModeID);
                CGraphics.FadeTo(EScreens.ScreenPartyDummy);
            }
            //TODO: else Message!
        }
    }
}
