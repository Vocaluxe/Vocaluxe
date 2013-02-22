using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Menu;

namespace Vocaluxe.PartyModes
{
    public class Field
    {
        public Round Content;
        public CButton Button;
    }

    public enum EStatus
    {
        FieldChoosing,
        JokerRetry,
        FieldSelected,
        None
    }

    public class PartyScreenTicTacToeMain : CMenuParty
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        const int ScreenVersion = 1;

        const string TextPopupReallyExit = "TextPopupReallyExit";
        const string TextTeamChoosing = "TextTeamChoosing";
        const string TextFinishMessage = "TextFinishMessage";
        const string TextNextPlayerT1 = "TextNextPlayerT1";
        const string TextNextPlayerT2 = "TextNextPlayerT2";
        const string TextNextPlayerNameT1 = "TextNextPlayerNameT1";
        const string TextNextPlayerNameT2 = "TextNextPlayerNameT2";

        const string ButtonNextRound = "ButtonNextRound";
        const string ButtonBack = "ButtonBack";
        const string ButtonExit = "ButtonExit";
        const string ButtonPopupYes = "ButtonPopupYes";
        const string ButtonPopupNo = "ButtonPopupNo";
        const string ButtonField = "ButtonField";

        const string ButtonJokerRandomT1 = "ButtonJokerRandomT1";
        const string ButtonJokerRandomT2 = "ButtonJokerRandomT2";
        const string ButtonJokerRetryT1 = "ButtonJokerRetryT1";
        const string ButtonJokerRetryT2 = "ButtonJokerRetryT2";


        const string StaticPopupBG = "StaticPopupBG";
        const string StaticAvatarT1 = "StaticAvatarT1";
        const string StaticAvatarT2 = "StaticAvatarT2";

        private bool ExitPopupVisible = false;

        private DataFromScreen Data;
        private DataToScreenMain GameData;

        private List<Field> Fields;
        private float FieldFirstX = 25;
        private float FieldFirstY = 25;
        private int FieldSpace = 10;
        private float FieldSize = 100;

        private int PreviewStream = -1;
        private int SelectedField = -1;
        private int OldSelectedField = -1;

        private int[,] Possibilities;
        private EStatus Status;

        public PartyScreenTicTacToeMain()
        {
        }

        protected override void Init()
        {
            base.Init();

            _ThemeName = "PartyScreenTicTacToeMain";
            _ThemeTexts = new string[] { TextPopupReallyExit, TextTeamChoosing, TextFinishMessage, TextNextPlayerT1, TextNextPlayerT2, TextNextPlayerNameT1, TextNextPlayerNameT2 };
            _ThemeButtons = new string[] { ButtonNextRound, ButtonBack, ButtonExit, ButtonPopupYes, ButtonPopupNo, ButtonField, ButtonJokerRandomT1, ButtonJokerRandomT2, ButtonJokerRetryT1, ButtonJokerRetryT2 };
            _ThemeStatics = new string[] { StaticPopupBG, StaticAvatarT1, StaticAvatarT2 };
            _ScreenVersion = ScreenVersion;

            Data = new DataFromScreen();
            FromScreenMain config = new FromScreenMain();
            GameData = new DataToScreenMain();
            config.SingRoundNr = 1;
            config.Rounds = new List<Round>();
            config.Songs = new List<int>();
            config.PlayerTeam1 = new List<int>();
            config.PlayerTeam2 = new List<int>();
            config.FadeToNameSelection = false;
            config.FadeToSinging = false;
            Data.ScreenMain = config;

            Fields = new List<Field>();
        }

        public override void LoadTheme(string XmlPath)
        {
			base.LoadTheme(XmlPath);

            CreateFields();
            Buttons[htButtons(ButtonField)].Visible = false;
        }

        public override void DataToScreen(object ReceivedData)
        {
            DataToScreenMain config = new DataToScreenMain();

            try
            {
                config = (DataToScreenMain)ReceivedData;
                GameData = config;
            }
            catch (Exception e)
            {
                CBase.Log.LogError("Error in party mode screen TicTacToe main. Can't cast received data from game mode " + _ThemeName + ". " + e.Message); ;
            }

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
                        if (!ExitPopupVisible)
                            if (GameData.CurrentRoundNr == 1 && Status != EStatus.FieldSelected)
                                Back();
                            else if(Status == EStatus.None)
                                EndParty();
                            else
                                ShowPopup(true);
                        else
                            ShowPopup(false);
                        break;

                    case Keys.Enter:
                        if (!ExitPopupVisible)
                        {
                            if (Buttons[htButtons(ButtonNextRound)].Selected)
                                NextRound();
                            if (Buttons[htButtons(ButtonBack)].Selected && GameData.CurrentRoundNr == 1 && Status != EStatus.FieldSelected)
                                Back();
                            if (Buttons[htButtons(ButtonExit)].Selected && (GameData.CurrentRoundNr > 1 || Status == EStatus.FieldSelected) && Status != EStatus.None)
                                ShowPopup(true);
                            else if (Status == EStatus.None)
                                EndParty();
                            for (int i = 0; i < GameData.NumFields; i++)
                            {
                                switch (Status)
                                {
                                    case EStatus.FieldChoosing:
                                        if (Fields[i].Button.Selected)
                                        {
                                            SelectedField = i;
                                            FieldSelected();
                                        }
                                        break;

                                    case EStatus.JokerRetry:
                                        if (Fields[i].Button.Selected)
                                        {
                                            SelectedField = i;
                                            FieldSelectedAgain();
                                        }
                                        break;
                                }
                            }
                            if (Status == EStatus.FieldSelected)
                            {
                                if (Buttons[htButtons(ButtonJokerRandomT1)].Selected)
                                    UseJoker(0, 0);
                                if (Buttons[htButtons(ButtonJokerRandomT2)].Selected)
                                    UseJoker(1, 0);
                                if (Buttons[htButtons(ButtonJokerRetryT1)].Selected)
                                    UseJoker(0, 1);
                                if (Buttons[htButtons(ButtonJokerRetryT2)].Selected)
                                    UseJoker(1, 1);
                            }
                        }
                        else
                        {
                            if (Buttons[htButtons(ButtonPopupYes)].Selected)
                                EndParty();
                            if (Buttons[htButtons(ButtonPopupNo)].Selected)
                                ShowPopup(false);
                        }
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
                if (!ExitPopupVisible)
                {
                    if (Buttons[htButtons(ButtonNextRound)].Selected)
                        NextRound();
                    if (Buttons[htButtons(ButtonBack)].Selected)
                        Back();
                    if (Buttons[htButtons(ButtonExit)].Selected)
                        if (Status == EStatus.None)
                            EndParty();
                        else
                            ShowPopup(true);
                    for (int i = 0; i < GameData.NumFields; i++)
                    {
                        switch (Status)
                        {
                            case EStatus.FieldChoosing:
                                if (Fields[i].Button.Selected)
                                {
                                    SelectedField = i;
                                    FieldSelected();
                                }
                                break;

                            case EStatus.JokerRetry:
                                if (Fields[i].Button.Selected)
                                {
                                    SelectedField = i;
                                    FieldSelectedAgain();
                                }
                                break;
                        }
                    }
                    if(Status == EStatus.FieldSelected)
                    {
                        if (Buttons[htButtons(ButtonJokerRandomT1)].Selected)
                            UseJoker(0, 0);
                        if (Buttons[htButtons(ButtonJokerRandomT2)].Selected)
                            UseJoker(1, 0);
                        if (Buttons[htButtons(ButtonJokerRetryT1)].Selected)
                            UseJoker(0, 1);
                        if (Buttons[htButtons(ButtonJokerRetryT2)].Selected)
                            UseJoker(1, 1);

                    }
                }
                else
                {
                    if (Buttons[htButtons(ButtonPopupYes)].Selected)
                        EndParty();
                    if (Buttons[htButtons(ButtonPopupNo)].Selected)
                        ShowPopup(false);
                }
            }

            if (MouseEvent.RB)
            {
                if (!ExitPopupVisible)
                    if (GameData.CurrentRoundNr == 1 && Status != EStatus.FieldSelected)
                        Back();
                    else if(Status == EStatus.None)
                        EndParty();
                    else
                        ShowPopup(true);
                else
                    ShowPopup(false);
            }

            if (MouseEvent.Wheel != 0)
            {
            }

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            PreviewStream = -1;

            if (GameData.CurrentRoundNr == 1)
            {
                BuildWinnerPossibilities(); 
                SelectedField = -1;
                Buttons[htButtons(ButtonBack)].Visible = true;
                Buttons[htButtons(ButtonExit)].Visible = false;
                SetInteractionToButton(Buttons[htButtons(ButtonBack)]);
            }
            else
            {
                Buttons[htButtons(ButtonBack)].Visible = false;
                Buttons[htButtons(ButtonExit)].Visible = true;
                SetInteractionToButton(Buttons[htButtons(ButtonExit)]);
            }

            Status = EStatus.FieldChoosing;

            UpdateFields();
            UpdateFieldContents();

            int Winner = GetWinner();
            if (GameData.CurrentRoundNr <= GameData.NumFields && Winner == 0)
            {
                UpdateTeamChoosingMessage();
                Texts[htTexts(TextNextPlayerT1)].Visible = false;
                Texts[htTexts(TextNextPlayerT2)].Visible = false;
                Texts[htTexts(TextNextPlayerNameT1)].Visible = false;
                Texts[htTexts(TextNextPlayerNameT2)].Visible = false;
                Statics[htStatics(StaticAvatarT1)].Visible = false;
                Statics[htStatics(StaticAvatarT2)].Visible = false;
                Buttons[htButtons(ButtonJokerRandomT1)].Visible = false;
                Buttons[htButtons(ButtonJokerRandomT2)].Visible = false;
                Buttons[htButtons(ButtonJokerRetryT1)].Visible = false;
                Buttons[htButtons(ButtonJokerRetryT2)].Visible = false;
                Buttons[htButtons(ButtonNextRound)].Visible = false;
                Texts[htTexts(TextFinishMessage)].Visible = false;
            }
            else
            {
                Status = EStatus.None;
                Buttons[htButtons(ButtonNextRound)].Visible = false;
                Texts[htTexts(TextFinishMessage)].Visible = true;
                Texts[htTexts(TextTeamChoosing)].Visible = false;
                if (Winner > 0)
                {
                    Texts[htTexts(TextFinishMessage)].Color = CBase.Theme.GetPlayerColor(Winner);
                    Texts[htTexts(TextFinishMessage)].Text = CBase.Language.Translate("TR_SCREENMAIN_WINNER", _PartyModeID) + " " + CBase.Language.Translate("TR_TEAM", _PartyModeID) + " " + Winner;
                }
                else
                {
                    Texts[htTexts(TextFinishMessage)].Color = new SColorF(1, 1, 1, 1);
                    Texts[htTexts(TextFinishMessage)].Text = CBase.Language.Translate("TR_SCREENMAIN_NOWINNER", _PartyModeID);
                }
                SetInteractionToButton(Buttons[htButtons(ButtonExit)]);
            }

            ShowPopup(false);
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
            CBase.BackgroundMusic.SetStatus(false);
            CBase.Sound.FadeAndStop(PreviewStream, 0f, 0.5f);
        }

        private void CreateFields()
        {
            for (int i = 0; i < 25; i++)
            {
                Field f = new Field();
                f.Button = GetNewButton(Buttons[htButtons(ButtonField)]);
                f.Button.Visible = false;
                f.Content = new Round();
                AddButton(f.Button);
                Fields.Add(f);
            }
        }

        private void UpdateFields()
        {
            int NumOneRow = (int)Math.Sqrt((double)GameData.NumFields);
            float FieldSizeY = (CBase.Settings.GetRenderH() - 150 - NumOneRow * FieldSpace) / NumOneRow;
            float FieldSizeX = (CBase.Settings.GetRenderW() - 300 - NumOneRow * FieldSpace) / NumOneRow;
            if (FieldSizeX < FieldSizeY)
                FieldSize = FieldSizeX;
            else
                FieldSize = FieldSizeY;
            FieldFirstX = CBase.Settings.GetRenderW() / 2 - (NumOneRow * FieldSize + (NumOneRow - 1) * FieldSpace) / 2;
            FieldFirstY = 140 + (CBase.Settings.GetRenderH() - 140) / 2 - (NumOneRow * FieldSize + NumOneRow * FieldSpace) / 2;
            int row = 0;
            int column = 0;
            float x = FieldFirstX;
            float y = FieldFirstY;
            for (int i = 0; i < Fields.Count; i++)
            {
                if (i < GameData.NumFields)
                {
                    Fields[i].Button.Rect.W = FieldSize;
                    Fields[i].Button.Rect.H = FieldSize;
                    Fields[i].Button.Rect.X = x;
                    Fields[i].Button.Rect.Y = y;
                    Fields[i].Button.Visible = true;
                    Fields[i].Button.Enabled = true;
                    column++;
                    if ((i+1) >= NumOneRow * (row+1))
                    {
                        column = 0;
                        row++;
                        y = FieldFirstY + FieldSize * row + FieldSpace * row;
                        x = FieldFirstX;
                    }
                    else
                        x = FieldFirstX + FieldSize * column + FieldSpace * column;
                }
                else
                {
                    Fields[i].Button.Visible = false;
                    Fields[i].Button.Enabled = false;
                }
            }
        }

        private void UpdateFieldContents()
        {
            for (int i = 0; i < GameData.Rounds.Count; i++)
            {
                Fields[i].Button.Enabled = true;
                Fields[i].Button.Texture = Buttons[htButtons(ButtonField)].Texture;
                Fields[i].Button.Color = Buttons[htButtons(ButtonField)].Color;
                Fields[i].Button.SColor = Buttons[htButtons(ButtonField)].SColor;
                Fields[i].Content = GameData.Rounds[i];
                if (Fields[i].Content.Finished)
                {
                    Fields[i].Button.Enabled = false;
                    Fields[i].Button.Texture = CBase.Songs.GetSongByID(Fields[i].Content.SongID).CoverTextureBig;
                    Fields[i].Button.Color = CBase.Theme.GetPlayerColor(Fields[i].Content.Winner);
                    Fields[i].Button.SColor = CBase.Theme.GetPlayerColor(Fields[i].Content.Winner);
                }
                if (Status == EStatus.FieldSelected && SelectedField == i)
                {
                    Fields[i].Button.Texture = CBase.Songs.GetSongByID(Fields[i].Content.SongID).CoverTextureBig;
                    Fields[i].Button.Color = new SColorF(1, 1, 1, 1);
                    Fields[i].Button.SColor = new SColorF(1, 1, 1, 1);
                    Fields[i].Button.Enabled = false;
                }
                if (Status == EStatus.JokerRetry && Fields[i].Content.Finished)
                {
                    Fields[i].Button.SColor = CBase.Theme.GetPlayerColor(GameData.Team+1);
                    Fields[i].Button.Enabled = true;
                }
                if (Status == EStatus.JokerRetry && !Fields[i].Content.Finished)
                {
                    Fields[i].Button.Enabled = false;
                }
                if(Status == EStatus.FieldSelected)
                {
                    Fields[i].Button.Enabled = false;
                }
            }
        }

        private void FieldSelected()
        {
            int SongID = 0;
            int SingerTeam1 = 0;
            int SingerTeam2 = 0;
            if (GameData.Songs.Count > 0)
            {
                SongID = GameData.Songs[0];
                GameData.Songs.RemoveAt(0);
            }
            if (GameData.PlayerTeam1.Count > 0)
            {
                SingerTeam1 = GameData.PlayerTeam1[0];
                GameData.PlayerTeam1.RemoveAt(0);
            }

            if (GameData.PlayerTeam2.Count > 0)
            {
                SingerTeam2 = GameData.PlayerTeam2[0];
                GameData.PlayerTeam2.RemoveAt(0);
            }
            Menu.SongMenu.CSong Song = CBase.Songs.GetSongByID(SongID);

            CBase.BackgroundMusic.SetStatus(true);

            PreviewStream = CBase.Sound.Load(Song.GetMP3(), false);
            CBase.Sound.SetPosition(PreviewStream, Song.PreviewStart);
            CBase.Sound.SetStreamVolume(PreviewStream, 0f);
            CBase.Sound.Play(PreviewStream);
            CBase.Sound.Fade(PreviewStream, CBase.Config.GetBackgroundMusicVolume(), 1f);
            Status = EStatus.FieldSelected;
            Fields[SelectedField].Content.SongID = SongID;
            GameData.Rounds[SelectedField].SongID = SongID;
            GameData.Rounds[SelectedField].SingerTeam1 = SingerTeam1;
            GameData.Rounds[SelectedField].SingerTeam2 = SingerTeam2;
            UpdateFieldContents();

            Texts[htTexts(TextNextPlayerT1)].Visible = true;
            Texts[htTexts(TextNextPlayerT2)].Visible = true;
            Texts[htTexts(TextNextPlayerNameT1)].Visible = true;
            Texts[htTexts(TextNextPlayerNameT2)].Visible = true;
            SProfile[] profiles =  CBase.Profiles.GetProfiles();
            Texts[htTexts(TextNextPlayerNameT1)].Text = profiles[GameData.ProfileIDsTeam1[GameData.Rounds[SelectedField].SingerTeam1]].PlayerName;
            Texts[htTexts(TextNextPlayerNameT2)].Text = profiles[GameData.ProfileIDsTeam2[GameData.Rounds[SelectedField].SingerTeam2]].PlayerName;
            Statics[htStatics(StaticAvatarT1)].Visible = true;
            Statics[htStatics(StaticAvatarT2)].Visible = true;
            Statics[htStatics(StaticAvatarT1)].Texture = profiles[GameData.ProfileIDsTeam1[GameData.Rounds[SelectedField].SingerTeam1]].Avatar.Texture;
            Statics[htStatics(StaticAvatarT2)].Texture = profiles[GameData.ProfileIDsTeam2[GameData.Rounds[SelectedField].SingerTeam2]].Avatar.Texture;

            UpdateJokerButtons();

            Buttons[htButtons(ButtonNextRound)].Visible = true;
            Buttons[htButtons(ButtonExit)].Visible = true;
            Buttons[htButtons(ButtonBack)].Visible = false;

            SetInteractionToButton(Buttons[htButtons(ButtonNextRound)]);
        }

        private void FieldSelectedAgain()
        {
            int SongID = Fields[SelectedField].Content.SongID;
            Menu.SongMenu.CSong Song = CBase.Songs.GetSongByID(SongID);
            CBase.BackgroundMusic.SetStatus(true);

            PreviewStream = CBase.Sound.Load(Song.GetMP3(), false);
            CBase.Sound.SetPosition(PreviewStream, Song.PreviewStart);
            CBase.Sound.SetStreamVolume(PreviewStream, 0f);
            CBase.Sound.Play(PreviewStream);
            CBase.Sound.Fade(PreviewStream, CBase.Config.GetBackgroundMusicVolume(), 1f);
            Status = EStatus.FieldSelected;
            Fields[SelectedField].Content.SongID = SongID;
            GameData.Rounds[SelectedField].SongID = SongID;
            GameData.Rounds[SelectedField].SingerTeam1 = GameData.Rounds[OldSelectedField].SingerTeam1;
            GameData.Rounds[SelectedField].SingerTeam2 = GameData.Rounds[OldSelectedField].SingerTeam2;
            UpdateFieldContents();

            Texts[htTexts(TextNextPlayerT1)].Visible = true;
            Texts[htTexts(TextNextPlayerT2)].Visible = true;
            Texts[htTexts(TextNextPlayerNameT1)].Visible = true;
            Texts[htTexts(TextNextPlayerNameT2)].Visible = true;
            SProfile[] profiles = CBase.Profiles.GetProfiles();
            Texts[htTexts(TextNextPlayerNameT1)].Text = profiles[GameData.ProfileIDsTeam1[GameData.Rounds[SelectedField].SingerTeam1]].PlayerName;
            Texts[htTexts(TextNextPlayerNameT2)].Text = profiles[GameData.ProfileIDsTeam2[GameData.Rounds[SelectedField].SingerTeam2]].PlayerName;
            Statics[htStatics(StaticAvatarT1)].Visible = true;
            Statics[htStatics(StaticAvatarT2)].Visible = true;
            Statics[htStatics(StaticAvatarT1)].Texture = profiles[GameData.ProfileIDsTeam1[GameData.Rounds[SelectedField].SingerTeam1]].Avatar.Texture;
            Statics[htStatics(StaticAvatarT2)].Texture = profiles[GameData.ProfileIDsTeam2[GameData.Rounds[SelectedField].SingerTeam2]].Avatar.Texture;

            UpdateJokerButtons();

            Buttons[htButtons(ButtonNextRound)].Visible = true;
            Buttons[htButtons(ButtonExit)].Visible = true;
            Buttons[htButtons(ButtonBack)].Visible = false;

            SetInteractionToButton(Buttons[htButtons(ButtonNextRound)]);
        }

        private void UseJoker(int TeamNr, int JokerNum)
        {
            switch (JokerNum)
            {
                //Random-Joker
                case 0:
                    if (GameData.NumJokerRandom[TeamNr] > 0)
                    {
                        GameData.NumJokerRandom[TeamNr]--;
                        if (!CBase.Sound.IsFinished(PreviewStream))
                            CBase.Sound.FadeAndStop(PreviewStream, 0, 1);
                        FieldSelected();
                    }
                    break;

                //Retry-Joker
                case 1:
                    if (GameData.NumJokerRetry[TeamNr] > 0 && GameData.CurrentRoundNr > 1)
                    {
                        GameData.NumJokerRetry[TeamNr]--;
                        GameData.Team = TeamNr;
                        if (!CBase.Sound.IsFinished(PreviewStream))
                            CBase.Sound.FadeAndStop(PreviewStream, 0, 1);
                        Status = EStatus.JokerRetry;
                        OldSelectedField = SelectedField;
                        SelectedField = -1;
                        UpdateFieldContents();
                    }
                    break;
            }
            UpdateJokerButtons();
        }

        private void UpdateTeamChoosingMessage()
        {
            Texts[htTexts(TextTeamChoosing)].Color = CBase.Theme.GetPlayerColor(GameData.Team + 1);
            Texts[htTexts(TextTeamChoosing)].Text = CBase.Language.Translate("TR_TEAM", _PartyModeID) + " " + (GameData.Team + 1) + "! " + CBase.Language.Translate("TR_SCREENMAIN_TEAM_CHOOSE", _PartyModeID);
            if (Status == EStatus.JokerRetry || Status == EStatus.FieldChoosing)
                Texts[htTexts(TextTeamChoosing)].Visible = true;
            else
                Texts[htTexts(TextTeamChoosing)].Visible = false;
        }

        private void UpdateJokerButtons()
        {
            Buttons[htButtons(ButtonJokerRandomT1)].Visible = true;
            Buttons[htButtons(ButtonJokerRandomT2)].Visible = true;
            Buttons[htButtons(ButtonJokerRandomT1)].Text.Text = GameData.NumJokerRandom[0].ToString();
            Buttons[htButtons(ButtonJokerRandomT2)].Text.Text = GameData.NumJokerRandom[1].ToString();
            Buttons[htButtons(ButtonJokerRandomT1)].Enabled = GameData.NumJokerRandom[0] > 0;
            Buttons[htButtons(ButtonJokerRandomT2)].Enabled = GameData.NumJokerRandom[1] > 0;
            Buttons[htButtons(ButtonJokerRetryT1)].Visible = true;
            Buttons[htButtons(ButtonJokerRetryT2)].Visible = true;
            Buttons[htButtons(ButtonJokerRetryT1)].Text.Text = GameData.NumJokerRetry[0].ToString();
            Buttons[htButtons(ButtonJokerRetryT2)].Text.Text = GameData.NumJokerRetry[1].ToString();
            Buttons[htButtons(ButtonJokerRetryT1)].Enabled = GameData.NumJokerRetry[0] > 0;
            Buttons[htButtons(ButtonJokerRetryT2)].Enabled = GameData.NumJokerRetry[1] > 0;
        }

        private void NextRound()
        {
            CBase.Sound.FadeAndStop(PreviewStream, 0f, 0.5f);
            Data.ScreenMain.Rounds = GameData.Rounds;
            Data.ScreenMain.FadeToNameSelection = false;
            Data.ScreenMain.FadeToSinging = true;
            Data.ScreenMain.Songs = GameData.Songs;
            Data.ScreenMain.SingRoundNr = SelectedField;
            Data.ScreenMain.PlayerTeam1 = GameData.PlayerTeam1;
            Data.ScreenMain.PlayerTeam2 = GameData.PlayerTeam2;
            _PartyMode.DataFromScreen(_ThemeName, Data);
        }

        private void EndParty()
        {
            CBase.Sound.FadeAndStop(PreviewStream, 0f, 0.5f);
            FadeTo(EScreens.ScreenParty);
        }

        private void ShowPopup(bool Visible)
        {
            ExitPopupVisible = Visible;

            Statics[htStatics(StaticPopupBG)].Visible = ExitPopupVisible;
            Texts[htTexts(TextPopupReallyExit)].Visible = ExitPopupVisible;
            Buttons[htButtons(ButtonPopupYes)].Visible = ExitPopupVisible;
            Buttons[htButtons(ButtonPopupNo)].Visible = ExitPopupVisible;

            if (ExitPopupVisible)
                SetInteractionToButton(Buttons[htButtons(ButtonPopupNo)]);
        }

        private void Back()
        {
            CBase.Sound.FadeAndStop(PreviewStream, 0f, 0.5f);
            Data.ScreenMain.FadeToNameSelection = true;
            Data.ScreenMain.FadeToSinging = false;
            Data.ScreenMain.SingRoundNr = SelectedField;
            _PartyMode.DataFromScreen(_ThemeName, Data);
        }

        private int BuildWinnerPossibilities()
        {
            int NumOneRow = (int)Math.Sqrt((double)GameData.NumFields);
            Possibilities = new int[(NumOneRow * 2) + 2, NumOneRow];
            for (int i = 0; i < Possibilities.GetLength(0); i++)
            {
                if (i < NumOneRow)
                {
                    for(int c = 0; c < NumOneRow; c++){
                        Possibilities[i, c] = i * NumOneRow + c;
                    }
                }
                else if (i < NumOneRow * 2)
                {
                    for (int c = 0; c < NumOneRow; c++)
                    {
                        Possibilities[i, c] = (i-NumOneRow) + (c*NumOneRow);
                    }
                }
                else if( i == Possibilities.GetLength(0) - 2)
                {
                    for (int c = 0; c < NumOneRow;c++)
                    {
                        Possibilities[i, c] = (NumOneRow + 1) * c;
                    }
                }
                else if (i == Possibilities.GetLength(0) - 1)
                {
                    for (int c = 0; c < NumOneRow; c++)
                    {
                        Possibilities[i, c] = (NumOneRow - 1) * c + (NumOneRow - 1);
                    }
                }
            }
            return 0;
        }

        private int GetWinner()
        {
            for (int i = 0; i < Possibilities.GetLength(0); i++)
            {
                List<int> Check = new List<int>();
                for (int j = 0; j < Possibilities.GetLength(1); j++)
                {
                    if (Fields[Possibilities[i, j]].Content.Winner > 0)
                        Check.Add(Fields[Possibilities[i, j]].Content.Winner);
                }
                if (Check.Count == Possibilities.GetLength(1))
                {
                    //Check for winner
                    if (Check.Contains(1) && !Check.Contains(2))
                        return 1;
                    else if (Check.Contains(2) && !Check.Contains(1))
                        return 2;
                }
            }
            return 0;
        }
    }
}
