using System.Collections.Generic;
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    class CScreenParty : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private const string _TextDescription = "TextDescription";
        private const string _TextTargetAudience = "TextTargetAudience";
        private const string _TextNumTeams = "TextNumTeams";
        private const string _TextNumPlayers = "TextNumPlayers";
        private const string _TextAuthor = "TextAuthor";
        private const string _TextVersion = "TextVersion";
        private const string _TextError = "TextError";
        private const string _ButtonStart = "ButtonStart";
        private const string _ButtonExit = "ButtonExit";
        private const string _SelectSlideModes = "SelectSlideModes";

        private List<SPartyModeInfos> _PartyModeInfos;

        public override void Init()
        {
            base.Init();

            _ThemeTexts = new string[] {_TextDescription, _TextTargetAudience, _TextNumTeams, _TextNumPlayers, _TextAuthor, _TextVersion, _TextError};
            _ThemeButtons = new string[] {_ButtonStart, _ButtonExit};
            _ThemeSelectSlides = new string[] {_SelectSlideModes};
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            base.HandleInput(keyEvent);

            if (keyEvent.KeyPressed) {}
            else
            {
                switch (keyEvent.Key)
                {
                    case Keys.Back:
                    case Keys.Escape:
                        CGraphics.FadeTo(EScreens.ScreenMain);
                        break;

                    case Keys.Enter:
                        if (Buttons[_ButtonStart].Selected)
                            _StartPartyMode();

                        if (Buttons[_ButtonExit].Selected)
                            CGraphics.FadeTo(EScreens.ScreenMain);
                        break;

                    case Keys.Left:
                    case Keys.Right:
                        if (SelectSlides[_SelectSlideModes].Selected)
                            _UpdateSelection();
                        break;
                }
            }
            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);

            if (mouseEvent.LB && IsMouseOver(mouseEvent))
            {
                if (Buttons[_ButtonStart].Selected)
                    _StartPartyMode();

                if (Buttons[_ButtonExit].Selected)
                    CGraphics.FadeTo(EScreens.ScreenMain);

                if (SelectSlides[_SelectSlideModes].Selected)
                    _UpdateSelection();
            }

            if (mouseEvent.RB)
                CGraphics.FadeTo(EScreens.ScreenMain);

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            _PartyModeInfos = CParty.GetPartyModeInfos();

            SelectSlides[_SelectSlideModes].Clear();
            foreach (SPartyModeInfos info in _PartyModeInfos)
                SelectSlides[_SelectSlideModes].AddValue(info.Name, info.PartyModeID);
            SelectSlides[_SelectSlideModes].Selection = 0;
            _UpdateSelection();

            SetInteractionToSelectSlide(SelectSlides[_SelectSlideModes]);
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

        private void _UpdateSelection()
        {
            if (_PartyModeInfos.Count == 0)
                return;

            int index = SelectSlides[_SelectSlideModes].Selection;
            if (index >= _PartyModeInfos.Count)
                return;

            //Description    
            Texts[_TextDescription].Text = _PartyModeInfos[index].Description;
            Texts[_TextDescription].TranslationID = _PartyModeInfos[index].PartyModeID;

            //TargetAudience
            Texts[_TextTargetAudience].TranslationID = _PartyModeInfos[index].PartyModeID;
            Texts[_TextTargetAudience].Text = _PartyModeInfos[index].TargetAudience;

            //NumTeams
            if (_PartyModeInfos[index].MaxTeams == 0)
                Texts[_TextNumTeams].Text = "TR_SCREENPARTY_NOTEAMS";
            else if (_PartyModeInfos[index].MaxTeams == _PartyModeInfos[index].MinTeams)
                Texts[_TextNumTeams].Text = _PartyModeInfos[index].MaxTeams.ToString();
            else if (_PartyModeInfos[index].MaxTeams > _PartyModeInfos[index].MinTeams)
                Texts[_TextNumTeams].Text = _PartyModeInfos[index].MinTeams + " - " + _PartyModeInfos[index].MaxTeams;

            //NumPlayers
            if (_PartyModeInfos[index].MaxPlayers == _PartyModeInfos[index].MinPlayers)
                Texts[_TextNumPlayers].Text = _PartyModeInfos[index].MaxTeams.ToString();
            else if (_PartyModeInfos[index].MaxPlayers > _PartyModeInfos[index].MinPlayers)
                Texts[_TextNumPlayers].Text = _PartyModeInfos[index].MinPlayers + " - " + _PartyModeInfos[index].MaxPlayers;

            //Author
            Texts[_TextAuthor].Text = _PartyModeInfos[index].Author;
            Texts[_TextAuthor].TranslationID = _PartyModeInfos[index].PartyModeID;

            //Version
            Texts[_TextVersion].Text = _PartyModeInfos[index].VersionMajor + "." + _PartyModeInfos[index].VersionMinor;
            Texts[_TextVersion].TranslationID = _PartyModeInfos[index].PartyModeID;

            if (!_PartyModeInfos[index].Playable)
            {
                Buttons[_ButtonStart].Visible = false;
                Texts[_TextError].Visible = true;
            }
            else
            {
                Buttons[_ButtonStart].Visible = true;
                Texts[_TextError].Visible = false;
            }
        }

        private void _StartPartyMode()
        {
            if (_PartyModeInfos.Count == 0)
                return;

            int index = SelectSlides[_SelectSlideModes].Selection;
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