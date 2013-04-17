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
        private CText _Warning;

        public override void LoadTheme(string xmlPath)
        {
            _Warning = GetNewText();
            _Warning.Height = 100f;
            _Warning.X = 150;
            _Warning.Y = 300;
            _Warning.Font = "Normal";
            _Warning.Style = EStyle.Normal;
            _Warning.Color = new SColorF(1f, 0f, 0f, 1f);
            _Warning.SelColor = new SColorF(1f, 0f, 0f, 1f);
            _Warning.Text = "SOMETHING WENT WRONG!";
            AddText(_Warning);
        }

        public override void ReloadTheme(string xmlPath) {}

        public override void ReloadTextures() {}

        public override void SaveTheme() {}

        public override void UnloadTextures() {}

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
                        _FadeTo(EScreens.ScreenParty);
                        break;
                }
            }
            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);

            if (mouseEvent.LB && IsMouseOver(mouseEvent)) {}

            if (mouseEvent.RB)
                _FadeTo(EScreens.ScreenParty);

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