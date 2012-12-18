using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Menu;

namespace Vocaluxe.PartyModes
{
    struct TableRow
    {
        public CText Pos;
        public CText Name;
        public CText Won;
        public CText Drawn;
        public CText Lost;
        public CText SingPoints;
        public CText GamePoints;
    }


    public class PartyScreenChallengeMain : CMenuParty
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        const int ScreenVersion = 1;

        const string TextPosition = "TextPosition";
        const string TextPlayerName = "TextPlayerName";
        const string TextWon = "TextWon";
        const string TextDrawn = "TextDrawn";
        const string TextLost = "TextLost";
        const string TextSingPoints = "TextSingPoints";
        const string TextGamePoints = "TextGamePoints";

        const string ButtonNextRound = "ButtonNextRound";


        private DataFromScreen Data;
        private DataToScreenMain GameState;
        private List<TableRow> Table;

        public PartyScreenChallengeMain()
        {
            Data = new DataFromScreen();
            Data.ScreenMain = new FromScreenMain();
        }

        protected override void Init()
        {
            base.Init();

            _ThemeName = "PartyScreenChallengeMain";
            _ThemeTexts = new string[] { TextPosition, TextPlayerName, TextWon, TextDrawn, TextLost, TextSingPoints, TextGamePoints };
            _ThemeButtons = new string[] { "ButtonNextRound" };
            _ScreenVersion = ScreenVersion;
        }

        public override void LoadTheme(string XmlPath)
        {
			base.LoadTheme(XmlPath);

            GameState = new DataToScreenMain();
            BuildTable();
        }

        public override void DataToScreen(object ReceivedData)
        {
            DataToScreenMain data = new DataToScreenMain();

            try
            {
                data = (DataToScreenMain)ReceivedData;
                GameState = data;
            }
            catch (Exception e)
            {
                _Base.Log.LogError("Error in party mode screen challenge main. Can't cast received data from game mode " + _ThemeName + ". " + e.Message); ;
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
                        EndParty();
                        break;

                    case Keys.Enter:
                        if (Buttons[htButtons(ButtonNextRound)].Selected)
                            NextRound();
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
                if (Buttons[htButtons(ButtonNextRound)].Selected)
                    NextRound();
            }

            if (MouseEvent.RB)
            {
                EndParty();
            }

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            Updatetable();
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

        private void NextRound()
        {
            Data.ScreenMain.FadeToSongSelection = true;
            _PartyMode.DataFromScreen(_ThemeName, Data);
        }

        private void EndParty()
        {
            //TODO
        }

        private void BuildTable()
        {
            Table = new List<TableRow>();
            float delta = Texts[htTexts(TextPosition)].Height * 1.2f;

            for (int i = 0; i < _PartyMode.GetMaxPlayer(); i++)
            {
                TableRow row = new TableRow();

                row.Pos = GetNewText(Texts[htTexts(TextPosition)]);
                row.Name = GetNewText(Texts[htTexts(TextPlayerName)]);
                row.Won = GetNewText(Texts[htTexts(TextWon)]);
                row.Drawn = GetNewText(Texts[htTexts(TextDrawn)]);
                row.Lost = GetNewText(Texts[htTexts(TextLost)]);
                row.SingPoints = GetNewText(Texts[htTexts(TextSingPoints)]);
                row.GamePoints = GetNewText(Texts[htTexts(TextGamePoints)]);

                row.Pos.Y += delta * (i + 1);
                row.Name.Y += delta * (i + 1);
                row.Won.Y += delta * (i + 1);
                row.Drawn.Y += delta * (i + 1);
                row.Lost.Y += delta * (i + 1);
                row.SingPoints.Y += delta * (i + 1);
                row.GamePoints.Y += delta * (i + 1);

                row.Pos.Text = (i + 1).ToString() + ")";

                row.Pos.Visible = false;
                row.Name.Visible = false;
                row.Won.Visible = false;
                row.Drawn.Visible = false;
                row.Lost.Visible = false;
                row.SingPoints.Visible = false;
                row.GamePoints.Visible = false;

                AddText(row.Pos);
                AddText(row.Name);
                AddText(row.Won);
                AddText(row.Drawn);
                AddText(row.Lost);
                AddText(row.SingPoints);
                AddText(row.GamePoints);

                Table.Add(row);
            }
        }

        private void Updatetable()
        {
            SProfile[] profiles = _Base.Profiles.GetProfiles();

            for (int i = 0; i < Table.Count; i++)
            {
                if (i < GameState.ResultTable.Count)
                {
                    TableRow row = Table[i];

                    row.Pos.Visible = true;
                    row.Name.Visible = true;
                    row.Won.Visible = true;
                    row.Drawn.Visible = true;
                    row.Lost.Visible = true;
                    row.SingPoints.Visible = true;
                    row.GamePoints.Visible = true;

                    row.Name.Text = profiles[GameState.ResultTable[i].PlayerID].PlayerName;
                    row.Won.Text = GameState.ResultTable[i].NumWon.ToString();
                    row.Drawn.Text = GameState.ResultTable[i].NumDrawn.ToString();
                    row.Lost.Text = GameState.ResultTable[i].NumLost.ToString();
                    row.SingPoints.Text = GameState.ResultTable[i].SumSingPoints.ToString();
                    row.GamePoints.Text = GameState.ResultTable[i].NumGamePoints.ToString();
                }
            }
        }
    }
}
