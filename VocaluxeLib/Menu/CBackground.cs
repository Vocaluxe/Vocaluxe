#region license
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
using System.Xml.Serialization;
using System.Diagnostics;
using System.Collections.Generic;
using VocaluxeLib.Draw;
using VocaluxeLib.Xml;

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
        [XmlAttribute(AttributeName = "Name")] public string Name;
        public EBackgroundTypes Type;
        [XmlArray] public List<string> SlideShowTextures;
        [XmlElement("Video")] public string VideoName;
        public string Skin;
        public SThemeColor Color;
    }

    public class CBackground : CMenuElementBase, IMenuElement, IThemeable
    {
        private readonly int _PartyModeID;
        private SThemeBackground _Theme;

        private int _SlideShowCurrent;
        private readonly Stopwatch _SlideShowTimer = new Stopwatch();
        private readonly List<CTextureRef> _SlideShowTextures = new List<CTextureRef>();

        private CVideoStream _VideoStream;

        public bool Selectable
        {
            get { return false; }
        }

        public SColorF Color;
        public bool ThemeLoaded { get; private set; }

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        #region Constructors
        public CBackground(int partyModeID)
        {
            _PartyModeID = partyModeID;
            ThemeLoaded = false;
            _Theme = new SThemeBackground {SlideShowTextures = new List<string>()};

            Color = new SColorF(0f, 0f, 0f, 1f);
        }

        public CBackground(SThemeBackground theme, int partyModeID)
        {
            _Theme = theme;
            _PartyModeID = partyModeID;

            ThemeLoaded = true;
        }
        #endregion Constructors

        #region public
        public bool LoadTheme(string xmlPath, string elementName, CXmlReader xmlReader)
        {
            string item = xmlPath + "/" + elementName;
            ThemeLoaded = true;

            ThemeLoaded &= xmlReader.TryGetEnumValue(item + "/Type", ref _Theme.Type);

            bool vid = xmlReader.GetValue(item + "/Video", out _Theme.VideoName, String.Empty);
            bool tex = xmlReader.GetValue(item + "/Skin", out _Theme.Skin, String.Empty);
            ThemeLoaded &= vid || tex || _Theme.Type == EBackgroundTypes.None;

            if (xmlReader.GetValue(item + "/Color", out _Theme.Color.Name, String.Empty))
                ThemeLoaded &= _Theme.Color.Get(_PartyModeID, out Color);
            else
            {
                bool success = true;
                success &= xmlReader.TryGetFloatValue(item + "/R", ref Color.R);
                success &= xmlReader.TryGetFloatValue(item + "/G", ref Color.G);
                success &= xmlReader.TryGetFloatValue(item + "/B", ref Color.B);
                success &= xmlReader.TryGetFloatValue(item + "/A", ref Color.A);

                if (_Theme.Type != EBackgroundTypes.None)
                    ThemeLoaded &= success;
            }

            _Theme.Color.Color = Color;

            int i = 1;

            while (xmlReader.ItemExists(item + "/SlideShow" + i))
            {
                string slideShow;
                xmlReader.GetValue(item + "/SlideShow" + i, out slideShow, String.Empty);
                if (slideShow != "")
                    _Theme.SlideShowTextures.Add(slideShow);
                i++;
            }

            if (ThemeLoaded)
            {
                _Theme.Name = elementName;
                LoadSkin();
            }
            return ThemeLoaded;
        }

        public void Resume()
        {
            if (_VideoStream != null)
                CBase.Video.Resume(_VideoStream);
        }

        public void Pause()
        {
            if (_VideoStream != null)
                CBase.Video.Pause(_VideoStream);
        }

        public void Draw()
        {
            bool ok = false;
            if (_Theme.Type == EBackgroundTypes.Video && CBase.Config.GetVideoBackgrounds() == EOffOn.TR_CONFIG_ON)
            {
                if (CBase.Config.GetVideosToBackground() == EOffOn.TR_CONFIG_ON && CBase.BackgroundMusic.IsPlaying() && CBase.BackgroundMusic.SongHasVideo() &&
                    CBase.BackgroundMusic.VideoEnabled())
                {
                    Pause();
                    ok = _DrawBackgroundMusicVideo();
                }
                else
                {
                    Resume();
                    ok = _DrawVideo();
                }
            }
            else if (_Theme.Type == EBackgroundTypes.SlideShow && _SlideShowTextures.Count > 0)
                ok = _DrawSlideShow();

            if (!String.IsNullOrEmpty(_Theme.Skin) && (_Theme.Type == EBackgroundTypes.Texture || !ok))
                ok = _DrawTexture();

            if (_Theme.Type == EBackgroundTypes.Color || !ok)
                _DrawColor();
        }

        public void UnloadSkin()
        {
            if (!ThemeLoaded)
                return;
            _SlideShowTextures.Clear();
            CBase.Video.Close(ref _VideoStream);
        }

        public void LoadSkin()
        {
            if (!ThemeLoaded)
                return;
            _Theme.Color.Get(_PartyModeID, out Color);

            if (_Theme.Type == EBackgroundTypes.SlideShow)
            {
                foreach (string s in _Theme.SlideShowTextures)
                    _SlideShowTextures.Add(CBase.Themes.GetSkinTexture(s, _PartyModeID));
            }

            if (_Theme.Type == EBackgroundTypes.Video)
                _VideoStream = CBase.Themes.GetSkinVideo(_Theme.VideoName, _PartyModeID, true);
        }

        public void AddSlideShowTexture(string image)
        {
            _Theme.Type = EBackgroundTypes.SlideShow;
            if (!String.IsNullOrEmpty(image))
            {
                CTextureRef texture = _SlideShowTextures.Count == 0 ? CBase.Drawing.AddTexture(image) : CBase.Drawing.EnqueueTexture(image);
                if (texture != null)
                    _SlideShowTextures.Add(texture);
            }
        }

        public void RemoveSlideShowTextures()
        {
            foreach (CTextureRef tex in _SlideShowTextures)
            {
                CTextureRef texture = tex;
                CBase.Drawing.RemoveTexture(ref texture);
            }
            _SlideShowTextures.Clear();
        }

        public void ReloadSkin()
        {
            UnloadSkin();
            LoadSkin();
        }

        public object GetTheme()
        {
            return _Theme;
        }
        #endregion public

        #region internal
        public override float X
        {
            get { return 0; }
        }
        public override float Y
        {
            get { return 0; }
        }
        public override float Z
        {
            get { return CBase.Settings.GetZFar() / 4; }
        }
        public override float W
        {
            get { return CBase.Settings.GetRenderW(); }
        }
        public override float H
        {
            get { return CBase.Settings.GetRenderH(); }
        }

        private void _DrawColor()
        {
            CBase.Drawing.DrawRect(Color, Rect);
        }

        private bool _DrawTexture()
        {
            CTextureRef texture = CBase.Themes.GetSkinTexture(_Theme.Skin, _PartyModeID);
            if (texture != null)
            {
                CBase.Drawing.DrawTexture(texture, Rect, EAspect.Crop);
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
                    else
                        _SlideShowCurrent = 0;
                }

                CTextureRef texture = _SlideShowTextures[_SlideShowCurrent];

                if (texture == null)
                    return false;

                CBase.Drawing.DrawTexture(texture, Rect, EAspect.Crop);

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
                        SColorF color = texture.Color;
                        color.A = (_SlideShowTimer.ElapsedMilliseconds - CBase.Settings.GetSlideShowImageTime()) / CBase.Settings.GetSlideShowFadeTime();
                        CBase.Drawing.DrawTexture(texture, Rect, EAspect.Crop, color);
                    }
                }

                return true;
            }
            return false;
        }

        private bool _DrawVideo()
        {
            if (_VideoStream != null)
            {
                CBase.Video.GetFrame(_VideoStream, _VideoStream.VideoTime);
                if (_VideoStream.Texture != null)
                {
                    CBase.Drawing.DrawTexture(_VideoStream.Texture, Rect, EAspect.Crop);
                    return true;
                }
                return false;
            }
            return false;
        }

        private bool _DrawBackgroundMusicVideo()
        {
            CTextureRef videoTexture = CBase.BackgroundMusic.GetVideoTexture();
            if (videoTexture != null)
            {
                CBase.Drawing.DrawTexture(videoTexture, Rect, EAspect.Crop);
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