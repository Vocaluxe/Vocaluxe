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

            if (_TextureRatingPopup != null)
            {
                CBase.Drawing.DrawTexture(_TextureRatingPopup, Rect, color, false);
            }

            Text.DrawRelative(Rect.X, Rect.Y);
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