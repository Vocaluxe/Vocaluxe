using System.Collections.Generic;
using System.Windows.Forms;

using Vocaluxe.Base;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    class CScreenParty : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion { get { return 1; } }

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

        public override void Init()
        {
            base.Init();

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
                        if (Buttons[ButtonStart].Selected)
                            StartPartyMode();

                        if (Buttons[ButtonExit].Selected)
                            CGraphics.FadeTo(EScreens.ScreenMain);
                        break;

                    case Keys.Left:
                    case Keys.Right:
                        if (SelectSlides[SelectSlideModes].Selected)
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
                if (Buttons[ButtonStart].Selected)
                    StartPartyMode();

                if (Buttons[ButtonExit].Selected)
                    CGraphics.FadeTo(EScreens.ScreenMain);

                if (SelectSlides[SelectSlideModes].Selected)
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

            SelectSlides[SelectSlideModes].Clear();
            foreach (SPartyModeInfos info in _PartyModeInfos)
            {
                SelectSlides[SelectSlideModes].AddValue(info.Name, info.PartyModeID);
            }
            SelectSlides[SelectSlideModes].Selection = 0;
            UpdateSelection();

            SetInteractionToSelectSlide(SelectSlides[SelectSlideModes]);
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

        private void UpdateSelection()
        {
            if (_PartyModeInfos.Count == 0)
                return;

            int index = SelectSlides[SelectSlideModes].Selection;
            if (index >= _PartyModeInfos.Count)
                return;

            //Description    
            Texts[TextDescription].Text = _PartyModeInfos[index].Description;
            Texts[TextDescription].TranslationID = _PartyModeInfos[index].PartyModeID;

            //TargetAudience
            Texts[TextTargetAudience].TranslationID = _PartyModeInfos[index].PartyModeID;
            Texts[TextTargetAudience].Text = _PartyModeInfos[index].TargetAudience;

            //NumTeams
            if (_PartyModeInfos[index].MaxTeams == 0)
                Texts[TextNumTeams].Text = "TR_SCREENPARTY_NOTEAMS";
            else if (_PartyModeInfos[index].MaxTeams == _PartyModeInfos[index].MinTeams)
                Texts[TextNumTeams].Text = _PartyModeInfos[index].MaxTeams.ToString();
            else if (_PartyModeInfos[index].MaxTeams > _PartyModeInfos[index].MinTeams)
                Texts[TextNumTeams].Text = _PartyModeInfos[index].MinTeams + " - " + _PartyModeInfos[index].MaxTeams;

            //NumPlayers
            if (_PartyModeInfos[index].MaxPlayers == _PartyModeInfos[index].MinPlayers)
                Texts[TextNumPlayers].Text = _PartyModeInfos[index].MaxTeams.ToString();
            else if (_PartyModeInfos[index].MaxPlayers > _PartyModeInfos[index].MinPlayers)
                Texts[TextNumPlayers].Text = _PartyModeInfos[index].MinPlayers + " - " + _PartyModeInfos[index].MaxPlayers;

            //Author
            Texts[TextAuthor].Text = _PartyModeInfos[index].Author;
            Texts[TextAuthor].TranslationID = _PartyModeInfos[index].PartyModeID;

            //Version
            Texts[TextVersion].Text = _PartyModeInfos[index].VersionMajor + "." + _PartyModeInfos[index].VersionMinor;
            Texts[TextVersion].TranslationID = _PartyModeInfos[index].PartyModeID;

            if (!_PartyModeInfos[index].Playable)
            {
                Buttons[ButtonStart].Visible = false;
                Texts[TextError].Visible = true;
            }
            else
            {
                Buttons[ButtonStart].Visible = true;
                Texts[TextError].Visible = false;
            }
        }

        private void StartPartyMode()
        {
            if (_PartyModeInfos.Count == 0)
                return;

            int index = SelectSlides[SelectSlideModes].Selection;
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
