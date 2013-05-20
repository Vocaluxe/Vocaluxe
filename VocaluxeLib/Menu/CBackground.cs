#region license
// /*
//     This file is part of Vocaluxe.
// 
//     Vocaluxe is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     Vocaluxe is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
//  */
#endregion

using System;
using System.Drawing;
using System.Xml;
using VocaluxeLib.Draw;

namespace VocaluxeLib.Menu
{
    public enum EBackgroundTypes
    {
        None,
        Color,
        Texture,
        Video
    }

    struct SThemeBackground
    {
        public string Name;

        public EBackgroundTypes Type;

        public string VideoName;
        public string TextureName;

        public string ColorName;
    }

    public class CBackground : IMenuElement
    {
        private readonly int _PartyModeID;
        private SThemeBackground _Theme;
        private bool _ThemeLoaded;

        public SColorF Color;

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        #region Constructors
        public CBackground(int partyModeID)
        {
            _PartyModeID = partyModeID;
            _ThemeLoaded = false;
            _Theme = new SThemeBackground();

            Color = new SColorF(0f, 0f, 0f, 1f);
        }
        #endregion Constructors

        #region public
        public bool LoadTheme(string xmlPath, string elementName, CXMLReader xmlReader, int skinIndex)
        {
            string item = xmlPath + "/" + elementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= xmlReader.TryGetEnumValue(item + "/Type", ref _Theme.Type);

            bool vid = xmlReader.GetValue(item + "/Video", out _Theme.VideoName, String.Empty);
            bool tex = xmlReader.GetValue(item + "/Skin", out _Theme.TextureName, String.Empty);
            _ThemeLoaded &= vid || tex || _Theme.Type == EBackgroundTypes.None;

            if (xmlReader.GetValue(item + "/Color", out _Theme.ColorName, String.Empty))
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.ColorName, skinIndex, out Color);
            else
            {
                bool success = true;
                success &= xmlReader.TryGetFloatValue(item + "/R", ref Color.R);
                success &= xmlReader.TryGetFloatValue(item + "/G", ref Color.G);
                success &= xmlReader.TryGetFloatValue(item + "/B", ref Color.B);
                success &= xmlReader.TryGetFloatValue(item + "/A", ref Color.A);

                if (_Theme.Type != EBackgroundTypes.None)
                    _ThemeLoaded &= success;
            }

            if (_ThemeLoaded)
            {
                _Theme.Name = elementName;
                LoadTextures();
            }
            return _ThemeLoaded;
        }

        public bool SaveTheme(XmlWriter writer)
        {
            if (_ThemeLoaded)
            {
                writer.WriteStartElement(_Theme.Name);

                writer.WriteComment("<Type>: Background type: " + CHelper.ListStrings(Enum.GetNames(typeof(EBackgroundTypes))));
                writer.WriteElementString("Type", Enum.GetName(typeof(EBackgroundTypes), _Theme.Type));

                writer.WriteComment("<Video>: Background video name");
                writer.WriteElementString("Video", _Theme.VideoName);

                writer.WriteComment("<Skin>: Background Texture name");
                writer.WriteElementString("Skin", _Theme.TextureName);

                writer.WriteComment("<Color>: Background color for type \"Color\" from ColorScheme (high priority)");
                writer.WriteComment("or <R>, <G>, <B>, <A> (lower priority)");
                if (!String.IsNullOrEmpty(_Theme.ColorName))
                    writer.WriteElementString("Color", _Theme.ColorName);
                else
                {
                    if (_Theme.Type != EBackgroundTypes.None)
                    {
                        writer.WriteElementString("R", Color.R.ToString("#0.00"));
                        writer.WriteElementString("G", Color.G.ToString("#0.00"));
                        writer.WriteElementString("B", Color.B.ToString("#0.00"));
                        writer.WriteElementString("A", Color.A.ToString("#0.00"));
                    }
                }

                writer.WriteEndElement();
                return true;
            }
            return false;
        }

        public void Resume()
        {
            if (_Theme.Type == EBackgroundTypes.Video && !String.IsNullOrEmpty(_Theme.VideoName) && CBase.Config.GetVideoBackgrounds() == EOffOn.TR_CONFIG_ON)
                CBase.Theme.SkinVideoResume(_Theme.VideoName, _PartyModeID);
        }

        public void Pause()
        {
            if (!String.IsNullOrEmpty(_Theme.VideoName))
                CBase.Theme.SkinVideoPause(_Theme.VideoName, _PartyModeID);
        }

        public bool Draw()
        {
            bool ok = false;
            if (_Theme.Type == EBackgroundTypes.Video && CBase.Config.GetVideoBackgrounds() == EOffOn.TR_CONFIG_ON &&
                CBase.Config.GetVideosToBackground() == EOffOn.TR_CONFIG_ON && CBase.BackgroundMusic.IsPlaying() && CBase.BackgroundMusic.SongHasVideo() &&
                CBase.BackgroundMusic.VideoEnabled() && !CBase.BackgroundMusic.IsDisabled())
            {
                Pause();
                ok = _DrawBackgroundMusicVideo();
            }
            else if (_Theme.Type == EBackgroundTypes.Video && CBase.Config.GetVideoBackgrounds() == EOffOn.TR_CONFIG_ON)
            {
                Resume();
                ok = _DrawVideo();
            }

            if (!String.IsNullOrEmpty(_Theme.TextureName) &&
                (_Theme.Type == EBackgroundTypes.Texture ||
                 (_Theme.Type == EBackgroundTypes.Video && (CBase.Config.GetVideoBackgrounds() == EOffOn.TR_CONFIG_OFF || !ok))))
                ok = _DrawTexture();

            if (_Theme.Type == EBackgroundTypes.Color || _Theme.Type == EBackgroundTypes.Texture && !ok ||
                (_Theme.Type == EBackgroundTypes.Video && CBase.Config.GetVideoBackgrounds() == EOffOn.TR_CONFIG_OFF && !ok))
                _DrawColor();

            return true;
        }

        public void UnloadTextures() {}

        public void LoadTextures()
        {
            if (!String.IsNullOrEmpty(_Theme.ColorName))
                Color = CBase.Theme.GetColor(_Theme.ColorName, _PartyModeID);
        }

        public void ReloadTextures()
        {
            UnloadTextures();
            LoadTextures();
        }
        #endregion public

        #region internal
        private void _DrawColor()
        {
            SRectF bounds = new SRectF(0f, 0f, CBase.Settings.GetRenderW(), CBase.Settings.GetRenderH(), CBase.Settings.GetZFar() / 4);

            CBase.Drawing.DrawColor(Color, bounds);
        }

        private bool _DrawTexture()
        {
            CTexture texture = CBase.Theme.GetSkinTexture(_Theme.TextureName, _PartyModeID);
            if (texture != null)
            {
                RectangleF bounds = new RectangleF(0f, 0f, CBase.Settings.GetRenderW(), CBase.Settings.GetRenderH());
                RectangleF rect;
                CHelper.SetRect(bounds, out rect, texture.OrigAspect, EAspect.Crop);

                CBase.Drawing.DrawTexture(texture, new SRectF(rect.X, rect.Y, rect.Width, rect.Height, CBase.Settings.GetZFar() / 4));
                return true;
            }
            return false;
        }

        private bool _DrawVideo()
        {
            CTexture videoTexture = CBase.Theme.GetSkinVideoTexture(_Theme.VideoName, _PartyModeID);
            if (videoTexture != null)
            {
                RectangleF bounds = new RectangleF(0f, 0f, CBase.Settings.GetRenderW(), CBase.Settings.GetRenderH());
                RectangleF rect;
                CHelper.SetRect(bounds, out rect, videoTexture.OrigAspect, EAspect.Crop);

                CBase.Drawing.DrawTexture(videoTexture, new SRectF(rect.X, rect.Y, rect.Width, rect.Height, CBase.Settings.GetZFar() / 4));
                return true;
            }
            return false;
        }

        private bool _DrawBackgroundMusicVideo()
        {
            CTexture videoTexture = CBase.BackgroundMusic.GetVideoTexture();
            if (videoTexture != null)
            {
                RectangleF bounds = new RectangleF(0f, 0f, CBase.Settings.GetRenderW(), CBase.Settings.GetRenderH());
                RectangleF rect;
                CHelper.SetRect(bounds, out rect, videoTexture.OrigAspect, EAspect.Crop);

                CBase.Drawing.DrawTexture(videoTexture, new SRectF(rect.X, rect.Y, rect.Width, rect.Height, CBase.Settings.GetZFar() / 4));
                return true;
            }
            return false;
        }
        #endregion internal

        #region ThemeEdit
        public void MoveElement(int stepX, int stepY) {}

        public void ResizeElement(int stepW, int stepH) {}
        #endregion ThemeEdit
    }
}