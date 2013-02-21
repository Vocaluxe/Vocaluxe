using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Xml;

namespace Vocaluxe.Menu
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
        private int _PartyModeID;
        private Basic _Base;
        private SThemeBackground _Theme;
        private bool _ThemeLoaded;
                       
        public SColorF Color;

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        #region Constructors
        public CBackground(Basic Base, int PartyModeID)
        {
            _PartyModeID = PartyModeID;
            _Base = Base;
            _ThemeLoaded = false;
            _Theme = new SThemeBackground();
            
            Color = new SColorF(0f, 0f, 0f, 1f);
        }
        #endregion Constructors

        #region public
        public bool LoadTheme(string XmlPath, string ElementName, CXMLReader xPathHelper, int SkinIndex)
        {
            string item = XmlPath + "/" + ElementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= xPathHelper.TryGetEnumValue<EBackgroundTypes>(item + "/Type", ref _Theme.Type);
            
            bool vid = xPathHelper.GetValue(item + "/Video", ref _Theme.VideoName, String.Empty);
            bool tex = xPathHelper.GetValue(item + "/Skin", ref _Theme.TextureName, String.Empty);
            _ThemeLoaded &= vid || tex || _Theme.Type == EBackgroundTypes.None;
                
            if (xPathHelper.GetValue(item + "/Color", ref _Theme.ColorName, String.Empty))
            {
                _ThemeLoaded &= _Base.Theme.GetColor(_Theme.ColorName, SkinIndex, ref Color);
            }
            else
            {
                bool success = true;
                success &= xPathHelper.TryGetFloatValue(item + "/R", ref Color.R);
                success &= xPathHelper.TryGetFloatValue(item + "/G", ref Color.G);
                success &= xPathHelper.TryGetFloatValue(item + "/B", ref Color.B);
                success &= xPathHelper.TryGetFloatValue(item + "/A", ref Color.A);

                if (_Theme.Type != EBackgroundTypes.None)
                    _ThemeLoaded &= success;
            }

            if (_ThemeLoaded)
            {
                _Theme.Name = ElementName;
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
                if (_Theme.ColorName != String.Empty)
                {
                    writer.WriteElementString("Color", _Theme.ColorName);
                }
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
            if (_Theme.Type == EBackgroundTypes.Video && _Theme.VideoName != String.Empty && _Base.Config.GetVideoBackgrounds() == EOffOn.TR_CONFIG_ON)
                _Base.Theme.SkinVideoResume(_Theme.VideoName, _PartyModeID);
        }

        public void Pause()
        {
            if (_Theme.VideoName != String.Empty)
                _Base.Theme.SkinVideoPause(_Theme.VideoName, _PartyModeID);
        }

        public bool Draw()
        {
            bool ok = false;
            if (_Theme.Type == EBackgroundTypes.Video && _Base.Config.GetVideoBackgrounds() == EOffOn.TR_CONFIG_ON &&
                _Base.Config.GetVideosToBackground() == EOffOn.TR_CONFIG_ON && _Base.BackgroundMusic.IsPlaying() && _Base.BackgroundMusic.SongHasVideo() &&
                _Base.BackgroundMusic.VideoEnabled() && !_Base.BackgroundMusic.IsDisabled())
            {
                Pause();
                ok = DrawBackgroundMusicVideo();
            }
            else if (_Theme.Type == EBackgroundTypes.Video && _Base.Config.GetVideoBackgrounds() == EOffOn.TR_CONFIG_ON)
            {
                Resume();
                ok = DrawVideo();
            }

            if (_Theme.TextureName != String.Empty &&
                (_Theme.Type == EBackgroundTypes.Texture ||
                (_Theme.Type == EBackgroundTypes.Video && (_Base.Config.GetVideoBackgrounds() == EOffOn.TR_CONFIG_OFF || !ok))))
                ok = DrawTexture();
            
            if (_Theme.Type == EBackgroundTypes.Color || _Theme.Type == EBackgroundTypes.Texture && !ok ||
                (_Theme.Type == EBackgroundTypes.Video && _Base.Config.GetVideoBackgrounds() == EOffOn.TR_CONFIG_OFF && !ok))
                DrawColor();
            
            return true;
        }

        public void UnloadTextures()
        {
        }

        public void LoadTextures()
        {
            if (_Theme.ColorName != String.Empty)
                Color = _Base.Theme.GetColor(_Theme.ColorName, _PartyModeID);
        }

        public void ReloadTextures()
        {
            UnloadTextures();
            LoadTextures();            
        }
        #endregion public

        #region internal
        private void DrawColor()
        {
            SRectF bounds = new SRectF(0f, 0f, _Base.Settings.GetRenderW(), _Base.Settings.GetRenderH(), _Base.Settings.GetZFar()/4);

            _Base.Drawing.DrawColor(Color, bounds);
        }

        private bool DrawTexture()
        {
            STexture Texture = _Base.Theme.GetSkinTexture(_Theme.TextureName, _PartyModeID);
            if (Texture.height > 0)
            {
                RectangleF bounds = new RectangleF(0f, 0f, _Base.Settings.GetRenderW(), _Base.Settings.GetRenderH());
                RectangleF rect = new RectangleF(0f, 0f, Texture.width, Texture.height);
                CHelper.SetRect(bounds, ref rect, rect.Width / rect.Height, EAspect.Crop);

                _Base.Drawing.DrawTexture(Texture, new SRectF(rect.X, rect.Y, rect.Width, rect.Height, _Base.Settings.GetZFar() / 4));
                return true;
            }
            return false;
        }

        private bool DrawVideo()
        {
            STexture VideoTexture = _Base.Theme.GetSkinVideoTexture(_Theme.VideoName, _PartyModeID);
            if (VideoTexture.height > 0)
            {
                RectangleF bounds = new RectangleF(0f, 0f, _Base.Settings.GetRenderW(), _Base.Settings.GetRenderH());
                RectangleF rect = new RectangleF(0f, 0f, VideoTexture.width, VideoTexture.height);
                CHelper.SetRect(bounds, ref rect, rect.Width / rect.Height, EAspect.Crop);

                _Base.Drawing.DrawTexture(VideoTexture, new SRectF(rect.X, rect.Y, rect.Width, rect.Height, _Base.Settings.GetZFar() / 4));
                return true;
            }
            return false;
        }

        private bool DrawBackgroundMusicVideo()
        {
            STexture VideoTexture = _Base.BackgroundMusic.GetVideoTexture();
            if (VideoTexture.height > 0)
            {
                RectangleF bounds = new RectangleF(0f, 0f, _Base.Settings.GetRenderW(), _Base.Settings.GetRenderH());
                RectangleF rect = new RectangleF(0f, 0f, VideoTexture.width, VideoTexture.height);
                CHelper.SetRect(bounds, ref rect, rect.Width / rect.Height, EAspect.Crop);

                _Base.Drawing.DrawTexture(VideoTexture, new SRectF(rect.X, rect.Y, rect.Width, rect.Height, _Base.Settings.GetZFar() / 4));
                return true;
            }
            return false;
        }
        #endregion internal

        #region ThemeEdit
        public void MoveElement(int stepX, int stepY)
        {
        }

        public void ResizeElement(int stepW, int stepH)
        {
        }
        #endregion ThemeEdit
    }
}
