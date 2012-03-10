using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Xml;
using System.Xml.XPath;

using Vocaluxe.Base;
using Vocaluxe.Lib.Draw;

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
        private SThemeBackground _Theme;
        private bool _ThemeLoaded;
                       
        public SColorF Color;

        #region Constructors
        public CBackground()
        {
            _ThemeLoaded = false;
            _Theme = new SThemeBackground();
            
            Color = new SColorF(0f, 0f, 0f, 1f);
        }
        #endregion Constructors

        #region public
        public bool LoadTheme(string XmlPath, string ElementName, XPathNavigator navigator, int SkinIndex)
        {
            string item = XmlPath + "/" + ElementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= CHelper.TryGetEnumValueFromXML<EBackgroundTypes>(item + "/Type", navigator, ref _Theme.Type);
            
            bool vid = CHelper.GetValueFromXML(item + "/Video", navigator, ref _Theme.VideoName, String.Empty);
            bool tex = CHelper.GetValueFromXML(item + "/Skin", navigator, ref _Theme.TextureName, String.Empty);
            _ThemeLoaded &= vid || tex || _Theme.Type == EBackgroundTypes.None;
                
            if (CHelper.GetValueFromXML(item + "/Color", navigator, ref _Theme.ColorName, String.Empty))
            {
                _ThemeLoaded &= CTheme.GetColor(_Theme.ColorName, SkinIndex, ref Color);
            }
            else
            {
                bool success = true;
                success &= CHelper.TryGetFloatValueFromXML(item + "/R", navigator, ref Color.R);
                success &= CHelper.TryGetFloatValueFromXML(item + "/G", navigator, ref Color.G);
                success &= CHelper.TryGetFloatValueFromXML(item + "/B", navigator, ref Color.B);
                success &= CHelper.TryGetFloatValueFromXML(item + "/A", navigator, ref Color.A);

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

                writer.WriteComment("<Type>: Background type: " + CConfig.ListStrings(Enum.GetNames(typeof(EBackgroundTypes))));
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
            if (_Theme.Type == EBackgroundTypes.Video && _Theme.VideoName != String.Empty && CConfig.VideoBackgrounds == EOffOn.TR_CONFIG_ON)
                CTheme.SkinVideoResume(_Theme.VideoName);
        }

        public void Pause()
        {
            if (_Theme.VideoName != String.Empty)
                CTheme.SkinVideoPause(_Theme.VideoName);
        }

        public bool Draw()
        {
            bool ok = false;
            if (_Theme.Type == EBackgroundTypes.Video && CConfig.VideoBackgrounds == EOffOn.TR_CONFIG_ON && CConfig.VideosToBackground == EOffOn.TR_CONFIG_ON && CBackgroundMusic.IsPlaying() && CBackgroundMusic.HasVideo() && CBackgroundMusic.IsVideoEnabled() && CBackgroundMusic.IsEnabled())
            {
                Pause();
                ok = DrawBackgroundMusicVideo();
            }
            else if (_Theme.Type == EBackgroundTypes.Video && CConfig.VideoBackgrounds == EOffOn.TR_CONFIG_ON)
            {
                Resume();
                ok = DrawVideo();
            }

            if (_Theme.TextureName != String.Empty &&
                (_Theme.Type == EBackgroundTypes.Texture ||
                (_Theme.Type == EBackgroundTypes.Video && (CConfig.VideoBackgrounds == EOffOn.TR_CONFIG_OFF || !ok))))
                ok = DrawTexture();
            
            if (_Theme.Type == EBackgroundTypes.Color || _Theme.Type == EBackgroundTypes.Texture && !ok ||
                (_Theme.Type == EBackgroundTypes.Video && CConfig.VideoBackgrounds == EOffOn.TR_CONFIG_OFF && !ok))
                DrawColor();
            
            return true;
        }

        public void UnloadTextures()
        {
        }

        public void LoadTextures()
        {
            if (_Theme.ColorName != String.Empty)
                Color = CTheme.GetColor(_Theme.ColorName);
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
            SRectF bounds = new SRectF(0f, 0f, CSettings.iRenderW, CSettings.iRenderH, CSettings.zFar/4);

            CDraw.DrawColor(Color, bounds);
        }

        private bool DrawTexture()
        {
            STexture Texture = CTheme.GetSkinTexture(_Theme.TextureName);
            if (Texture.height > 0)
            {
                RectangleF bounds = new RectangleF(0f, 0f, CSettings.iRenderW, CSettings.iRenderH);
                RectangleF rect = new RectangleF(0f, 0f, Texture.width, Texture.height);
                CHelper.SetRect(bounds, ref rect, rect.Width / rect.Height, EAspect.Crop);

                CDraw.DrawTexture(Texture, new SRectF(rect.X, rect.Y, rect.Width, rect.Height, CSettings.zFar / 4));
                return true;
            }
            return false;
        }

        private bool DrawVideo()
        {
            STexture VideoTexture = CTheme.GetSkinVideoTexture(_Theme.VideoName);
            if (VideoTexture.height > 0)
            {
                RectangleF bounds = new RectangleF(0f, 0f, CSettings.iRenderW, CSettings.iRenderH);
                RectangleF rect = new RectangleF(0f, 0f, VideoTexture.width, VideoTexture.height);
                CHelper.SetRect(bounds, ref rect, rect.Width / rect.Height, EAspect.Crop);

                CDraw.DrawTexture(VideoTexture, new SRectF(rect.X, rect.Y, rect.Width, rect.Height, CSettings.zFar / 4));
                return true;
            }
            return false;
        }

        private bool DrawBackgroundMusicVideo()
        {
            STexture VideoTexture = CBackgroundMusic.GetVideoTexture();
            if (VideoTexture.height > 0)
            {
                RectangleF bounds = new RectangleF(0f, 0f, CSettings.iRenderW, CSettings.iRenderH);
                RectangleF rect = new RectangleF(0f, 0f, VideoTexture.width, VideoTexture.height);
                CHelper.SetRect(bounds, ref rect, rect.Width / rect.Height, EAspect.Crop);

                CDraw.DrawTexture(VideoTexture, new SRectF(rect.X, rect.Y, rect.Width, rect.Height, CSettings.zFar / 4));
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
