using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VocaluxeLib.Draw;

namespace VocaluxeLib.Menu
{
    [XmlType("RatingPopup")]
    public struct SThemeRatingPopup
    {
        [XmlAttribute(AttributeName = "Name")] public string Name;
        public string SkinRatingPopup;
        public SRectF Rect;
        public SThemeText Text;
    }

    public sealed class CRatingPopup : CMenuElementBase, IMenuElement, IThemeable
    {
        private readonly int _PartyModeID;

        private SThemeRatingPopup _Theme;
        public bool Selectable => false;

        public bool ThemeLoaded { get; private set; }

        private CTextureRef _TextureRatingPopup;
        public CTextureRef TextureRatingPopup
        {
            get { return _TextureRatingPopup ?? CBase.Themes.GetSkinTexture(_Theme.SkinRatingPopup, _PartyModeID); }

            set { _TextureRatingPopup = value; }
        }


        public CText Text;

        public float Alpha = 1;



        public CRatingPopup(int partyModeID)
        {
            _PartyModeID = partyModeID;
            Text = new CText(partyModeID);
            Text.AllMonitors = false;
        }

        public CRatingPopup(CRatingPopup rp)
        {
            _PartyModeID = rp._PartyModeID;
            _TextureRatingPopup = rp._TextureRatingPopup;

            Text = new CText(rp.Text);
            Text.AllMonitors = false;

            MaxRect = rp.MaxRect;

        }

        public CRatingPopup(SThemeRatingPopup theme, int partyModeID)
        {
            _Theme = theme;
            _PartyModeID = partyModeID;

            Text = new CText(theme.Text, partyModeID);
            Text.AllMonitors = false;

            ThemeLoaded = true;
        }


        public void Draw()
        {
            // TODO player color and alpha from class properties
            SColorF color = new SColorF(1, 0, 0, 1f);
            if (_TextureRatingPopup != null)
            {
                CBase.Drawing.DrawTexture(_TextureRatingPopup  , Rect, color, false);
            }

            //Text.Color = new SColorF(1, 1, 1, 1);
            Text.DrawRelative(Rect.X, Rect.Y);
            //Text.X = 500;
            //Text.Z = -100;
            //Text.Draw();

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
                return;

            TextureRatingPopup = CBase.Themes.GetSkinTexture(_Theme.SkinRatingPopup, _PartyModeID);
            Text = new CText(_Theme.Text, _PartyModeID);
            Text.LoadSkin();

            MaxRect = _Theme.Rect;
        }

        public void MoveElement(int stepX, int stepY)
        {
            X += stepX;
            Y += stepY;

            _Theme.Rect.X += stepX;
            _Theme.Rect.Y += stepY;
        }

        public void ReloadSkin()
        {
            throw new NotImplementedException();
        }

        public void ResizeElement(int stepW, int stepH)
        {
            W += stepW;
            if (W <= 0)
                W = 1;

            H += stepH;
            if (H <= 0)
                H = 1;

            _Theme.Rect.W = Rect.W;
            _Theme.Rect.H = Rect.H;
        }

        public void UnloadSkin()
        {
            throw new NotImplementedException();
        }
    }
}
