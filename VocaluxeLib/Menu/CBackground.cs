﻿#region license
// This file is part of Vocaluxe.
// 
// Vocaluxe is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Vocaluxe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Drawing;
using System.Xml;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Collections.Generic;
using VocaluxeLib.Draw;

namespace VocaluxeLib.Menu
{
    public enum EBackgroundTypes
    {
        None,
        Color,
        Texture,
        SlideShow,
        Video
    }

    [XmlType("Background")]
    public struct SThemeBackground
    {
        [XmlAttributeAttribute(AttributeName = "Name")]
        public string Name;
        [XmlElement("Type")]
        public EBackgroundTypes Type;
        [XmlArray]
        public List<string> SlideShowTextures;
        [XmlElement("Video")]
        public string VideoName;
        [XmlElement("Skin")]
        public string TextureName;
        public SThemeColor Color;
    }

    public class CBackground : IMenuElement
    {
        private readonly int _PartyModeID;
        private SThemeBackground _Theme;
        private bool _ThemeLoaded;

        private int _SlideShowCurrent;
        private readonly Stopwatch _SlideShowTimer = new Stopwatch();
        private readonly List<CTexture> _SlideShowTextures = new List<CTexture>();

        public SColorF Color;
        public bool ThemeLoaded
        {
            get { return _ThemeLoaded; }
        }

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        #region Constructors
        public CBackground(int partyModeID)
        {
            _PartyModeID = partyModeID;
            _ThemeLoaded = false;
            _Theme = new SThemeBackground {SlideShowTextures = new List<string>()};

            Color = new SColorF(0f, 0f, 0f, 1f);
        }

        public CBackground(SThemeBackground theme, int partyModeID)
        {
            _Theme = theme;
            _PartyModeID = partyModeID;

            LoadTextures();
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

            if (xmlReader.GetValue(item + "/Color", out _Theme.Color.Name, String.Empty))
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.Color.Name, skinIndex, out Color);
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

            _Theme.Color.Color = new SColorF(Color);

            int i = 1;

            while (xmlReader.ItemExists(item + "/SlideShow" + i))
            {
                string slideShow;
                xmlReader.GetValue(item + "/SlideShow" + i, out slideShow, String.Empty);
                if (slideShow != "")
                    _Theme.SlideShowTextures.Add(slideShow);
                i++;
            }

            if (_ThemeLoaded)
            {
                _Theme.Name = elementName;
                LoadTextures();
            }
            return _ThemeLoaded;
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


            if (_Theme.Type == EBackgroundTypes.SlideShow && _Theme.SlideShowTextures.Count > 0)
                ok = _DrawSlideShow();

            if (!String.IsNullOrEmpty(_Theme.TextureName) &&
                (_Theme.Type == EBackgroundTypes.Texture ||
                 (_Theme.Type == EBackgroundTypes.Video && CBase.Config.GetVideoBackgrounds() == EOffOn.TR_CONFIG_OFF)))
                ok = _DrawTexture();

            if (_Theme.Type == EBackgroundTypes.Color || _Theme.Type == EBackgroundTypes.Texture && !ok ||
                (_Theme.Type == EBackgroundTypes.Video && CBase.Config.GetVideoBackgrounds() == EOffOn.TR_CONFIG_OFF && !ok))
                _DrawColor();

            return true;
        }

        public void UnloadTextures()
        {
            _SlideShowTextures.Clear();
        }

        public void LoadTextures()
        {
            if (!String.IsNullOrEmpty(_Theme.Color.Name))
                Color = CBase.Theme.GetColor(_Theme.Color.Name, _PartyModeID);

            foreach (string s in _Theme.SlideShowTextures)
                _SlideShowTextures.Add(CBase.Theme.GetSkinTexture(s, _PartyModeID));
        }

        public void AddSlideShowTexture(string image)
        {
            _Theme.Type = EBackgroundTypes.SlideShow;
            if (!String.IsNullOrEmpty(image))
            {
                CTexture texture = CBase.Drawing.AddTexture(image);
                if (texture != null)
                    _SlideShowTextures.Add(texture);
            }
        }

        public void RemoveSlideShowTextures()
        {
            foreach (CTexture tex in _SlideShowTextures)
            {
                CTexture texture = tex;
                CBase.Drawing.RemoveTexture(ref texture);
            }
            _SlideShowTextures.Clear();
        }

        public void ReloadTextures()
        {
            UnloadTextures();
            LoadTextures();
        }

        public SThemeBackground GetTheme()
        {
            return _Theme;
        }
        #endregion public

        #region internal
        private void _DrawColor()
        {
            var bounds = new SRectF(0f, 0f, CBase.Settings.GetRenderW(), CBase.Settings.GetRenderH(), CBase.Settings.GetZFar() / 4);

            CBase.Drawing.DrawColor(Color, bounds);
        }

        private bool _DrawTexture()
        {
            CTexture texture = CBase.Theme.GetSkinTexture(_Theme.TextureName, _PartyModeID);
            if (texture != null)
            {
                var bounds = new RectangleF(0f, 0f, CBase.Settings.GetRenderW(), CBase.Settings.GetRenderH());
                RectangleF rect;
                CHelper.SetRect(bounds, out rect, texture.OrigAspect, EAspect.Crop);

                CBase.Drawing.DrawTexture(texture, new SRectF(rect.X, rect.Y, rect.Width, rect.Height, CBase.Settings.GetZFar() / 4));
                return true;
            }
            return false;
        }

        private bool _DrawSlideShow()
        {
            if (_SlideShowTextures.Count > 0)
            {
                if (!_SlideShowTimer.IsRunning)
                {
                    _SlideShowTimer.Start();
                    _SlideShowCurrent = 0;
                }

                if (_SlideShowTimer.ElapsedMilliseconds >= (CBase.Settings.GetSlideShowFadeTime() + CBase.Settings.GetSlideShowImageTime()))
                {
                    _SlideShowTimer.Restart();
                    if (_SlideShowCurrent + 1 < _SlideShowTextures.Count)
                        _SlideShowCurrent++;
                    else if (_SlideShowCurrent != 0)
                        _SlideShowCurrent = 0;
                }

                CTexture texture = _SlideShowTextures[_SlideShowCurrent];

                if (texture == null)
                    return false;

                var bounds = new RectangleF(0f, 0f, CBase.Settings.GetRenderW(), CBase.Settings.GetRenderH());
                RectangleF rect;
                CHelper.SetRect(bounds, out rect, texture.OrigAspect, EAspect.Crop);

                CBase.Drawing.DrawTexture(texture, new SRectF(rect.X, rect.Y, rect.Width, rect.Height, CBase.Settings.GetZFar() / 4));

                if (_SlideShowTimer.ElapsedMilliseconds >= CBase.Settings.GetSlideShowImageTime())
                {
                    if (_SlideShowCurrent + 1 < _SlideShowTextures.Count)
                        texture = _SlideShowTextures[_SlideShowCurrent + 1];
                    else if (_SlideShowCurrent != 0)
                        texture = _SlideShowTextures[0];
                    else
                        texture = null;

                    if (texture != null)
                    {
                        float alpha = (_SlideShowTimer.ElapsedMilliseconds - CBase.Settings.GetSlideShowImageTime()) / CBase.Settings.GetSlideShowFadeTime();
                        CHelper.SetRect(bounds, out rect, texture.OrigAspect, EAspect.Crop);
                        CBase.Drawing.DrawTexture(texture, new SRectF(rect.X, rect.Y, rect.Width, rect.Height, (CBase.Settings.GetZFar() / 4) - 1), new SColorF(1, 1, 1, alpha));
                    }
                }

                return true;
            }
            return false;
        }

        private bool _DrawVideo()
        {
            CTexture videoTexture = CBase.Theme.GetSkinVideoTexture(_Theme.VideoName, _PartyModeID);
            if (videoTexture != null)
            {
                var bounds = new RectangleF(0f, 0f, CBase.Settings.GetRenderW(), CBase.Settings.GetRenderH());
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
                var bounds = new RectangleF(0f, 0f, CBase.Settings.GetRenderW(), CBase.Settings.GetRenderH());
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