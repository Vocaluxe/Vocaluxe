using System.Windows.Forms;
using VocaluxeLib.Menu;

namespace VocaluxeLib.PartyModes
{
    public class CScreenPartyDummy : CMenuParty
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }
        private CText Warning;

        public override void LoadTheme(string XmlPath)
        {
            Warning = GetNewText();
            Warning.Height = 100f;
            Warning.X = 150;
            Warning.Y = 300;
            Warning.Font = "Normal";
            Warning.Style = EStyle.Normal;
            Warning.Color = new SColorF(1f, 0f, 0f, 1f);
            Warning.SelColor = new SColorF(1f, 0f, 0f, 1f);
            Warning.Text = "SOMETHING WENT WRONG!";
            AddText(Warning);
        }

        public override void ReloadTheme(string XmlPath) {}

        public override void ReloadTextures() {}

        public override void SaveTheme() {}

        public override void UnloadTextures() {}

        public override bool HandleInput(KeyEvent keyEvent)
        {
            base.HandleInput(keyEvent);

            if (keyEvent.KeyPressed) {}
            else
            {
                switch (keyEvent.Key)
                {
                    case Keys.Back:
                    case Keys.Escape:
                        FadeTo(EScreens.ScreenParty);
                        break;
                }
            }
            return true;
        }

        public override bool HandleMouse(MouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);

            if (mouseEvent.LB && IsMouseOver(mouseEvent)) {}

            if (mouseEvent.RB)
                FadeTo(EScreens.ScreenParty);

            return true;
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
    }
}