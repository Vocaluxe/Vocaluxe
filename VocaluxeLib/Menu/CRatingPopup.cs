using System;
using System.Xml.Serialization;
using VocaluxeLib.Draw;

namespace VocaluxeLib.Menu
{
    [XmlType("RatingPopup")]
    public struct SThemeRatingPopup
    {
        [XmlAttribute(AttributeName = "Name")]
        public string Name;
        public string SkinRatingPopup;
        public SRectF Rect;
        public SThemeText Text;
    }

    public sealed class CRatingPopup : CMenuElementBase, IMenuElement, IThemeable
    {
        private readonly int _PartyModeId;
        private SThemeRatingPopup _Theme;
        private CTextureRef _TextureRatingPopup;
        public CTextureRef TextureRatingPopup
        {
            get { return _TextureRatingPopup ?? CBase.Themes.GetSkinTexture(_Theme.SkinRatingPopup, _PartyModeId); }

            set { _TextureRatingPopup = value; }
        }

        public bool Selectable => false;
        public bool ThemeLoaded { get; private set; }
        public CText Text;
        public float Alpha = 1;
        public float Scale = 1;
        public float OffsetY;
        public SColorF Color;

        public CRatingPopup(int partyModeId)
        {
            _PartyModeId = partyModeId;
            Text = new CText(partyModeId);
            Text.AllMonitors = false;
            Visible = false;
        }

        public CRatingPopup(CRatingPopup rp)
        {
            _PartyModeId = rp._PartyModeId;
            _TextureRatingPopup = rp._TextureRatingPopup;
            Text = new CText(rp.Text);
            Text.AllMonitors = false;
            Visible = false;
            MaxRect = rp.MaxRect;
        }

        public CRatingPopup(SThemeRatingPopup theme, int partyModeId)
        {
            _Theme = theme;
            _PartyModeId = partyModeId;
            Text = new CText(theme.Text, partyModeId);
            Text.AllMonitors = false;
            Visible = false;
            ThemeLoaded = true;
        }

        public void Draw()
        {
            var color = Color;
            color.A = Alpha;
            Text.Color.A = Alpha;

            var w = Rect.W * Scale;
            var h = Rect.H * Scale;
            var x = Rect.X + (Rect.W - w) / 2f;
            var y = Rect.Y + (Rect.H - h) / 2f + OffsetY;

            if (_TextureRatingPopup != null)
            {
                CBase.Drawing.DrawTexture(_TextureRatingPopup, new SRectF(x, y, w, h, Rect.Z), color, false);
            }

            if (Math.Abs(Scale - 1f) < 0.001f)
            {
                Text.DrawRelative(x, y);
                return;
            }

            var textX = Text.X;
            var textY = Text.Y;
            var textW = Text.W;
            var fontHeight = Text.Font.Height;

            Text.X = textX * Scale;
            Text.Y = textY * Scale;
            Text.W = textW * Scale;
            Text.Font.Height = fontHeight * Scale;
            Text.DrawRelative(x, y);

            Text.X = textX;
            Text.Y = textY;
            Text.W = textW;
            Text.Font.Height = fontHeight;
        }

        public object GetTheme()
        {
            _Theme.Text = (SThemeText)Text.GetTheme();
            return _Theme;
        }

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        public void LoadSkin()
        {
            if (!ThemeLoaded)
            {
                return;
            }

            TextureRatingPopup = CBase.Themes.GetSkinTexture(_Theme.SkinRatingPopup, _PartyModeId);
            Text = new CText(_Theme.Text, _PartyModeId);
            Text.LoadSkin();
            MaxRect = _Theme.Rect;
        }

        public void UnloadSkin() { }

        public void ReloadSkin()
        {
            UnloadSkin();
            LoadSkin();
        }

        public void MoveElement(int stepX, int stepY)
        {
            X += stepX;
            Y += stepY;

            _Theme.Rect.X += stepX;
            _Theme.Rect.Y += stepY;
        }

        public void ResizeElement(int stepW, int stepH)
        {
            W += stepW;
            if (W <= 0)
            {
                W = 1;
            }

            H += stepH;
            if (H <= 0)
            {
                H = 1;
            }

            _Theme.Rect.W = Rect.W;
            _Theme.Rect.H = Rect.H;
        }
    }
}