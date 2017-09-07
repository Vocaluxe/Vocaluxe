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
using System.Windows.Forms;
using Vocaluxe.Base;
using Vocaluxe.Lib.Webcam;
using VocaluxeLib;
using VocaluxeLib.Menu;
using VocaluxeLib.Draw;

namespace Vocaluxe.Screens
{
    public class CScreenOptionsVideo : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 5; }
        }

        private const string _SelectSlideVideoBackgrounds = "SelectSlideVideoBackgrounds";
        private const string _SelectSlideVideoPreview = "SelectSlideVideoPreview";
        private const string _SelectSlideVideosInSongs = "SelectSlideVideosInSongs";
        private const string _SelectSlideVideosToBackground = "SelectSlideVideosToBackground";
        private const string _SelectSlideWebcamDevices = "SelectSlideWebcamDevices";
        private const string _SelectSlideWebcamCapabilities = "SelectSlideWebcamCapabilities";

        private const string _StaticWebcamOutput = "StaticWebcamOutput";

        private const string _ButtonScreenAdjustments = "ButtonScreenAdjustments";
        private const string _ButtonExit = "ButtonExit";

        private const string _TextWebcams = "TextWebcams";
        private const string _TextWebcamResolution = "TextWebcamResolution";

        private SWebcamConfig _Config;
        private CTextureRef _WebcamTexture;
        private int _DeviceNr;
        private int _CapabilityNr;

        public override void Init()
        {
            base.Init();

            _ThemeStatics = new string[] {_StaticWebcamOutput};
            _ThemeButtons = new string[] {_ButtonExit, _ButtonScreenAdjustments};
            _ThemeSelectSlides = new string[]
                {
                    _SelectSlideVideoBackgrounds, _SelectSlideVideoPreview, _SelectSlideVideosInSongs, _SelectSlideVideosToBackground, _SelectSlideWebcamDevices,
                    _SelectSlideWebcamCapabilities
                };
            _ThemeTexts = new string[] {_TextWebcams, _TextWebcamResolution};
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            _SelectSlides[_SelectSlideVideoBackgrounds].SetValues<EOffOn>((int)CConfig.Config.Video.VideoBackgrounds);
            _SelectSlides[_SelectSlideVideoPreview].SetValues<EOffOn>((int)CConfig.Config.Video.VideoPreview);
            _SelectSlides[_SelectSlideVideosInSongs].SetValues<EOffOn>((int)CConfig.Config.Video.VideosInSongs);
            _SelectSlides[_SelectSlideVideosToBackground].SetValues<EOffOn>((int)CConfig.Config.Video.VideosToBackground);
            _Statics[_StaticWebcamOutput].Aspect = EAspect.Crop;
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            base.HandleInput(keyEvent);

            if (keyEvent.KeyPressed) {}
            else
            {
                switch (keyEvent.Key)
                {
                    case Keys.Escape:
                    case Keys.Back:
                        _SaveConfig();
                        CGraphics.FadeTo(EScreen.Options);
                        break;

                    case Keys.S:
                        CParty.SetNormalGameMode();
                        _SaveConfig();
                        CGraphics.FadeTo(EScreen.Song);
                        break;

                    case Keys.Enter:
                        if (_Buttons[_ButtonExit].Selected)
                        {
                            _SaveConfig();
                            CGraphics.FadeTo(EScreen.Options);
                        }
                        else if (_Buttons[_ButtonScreenAdjustments].Selected)
                        {
                            _SaveConfig();
                            CGraphics.FadeTo(EScreen.OptionsVideoAdjustments);
                        }
                        break;

                    case Keys.Left:
                        if (_SelectSlides[_SelectSlideWebcamDevices].Selected)
                            _OnDeviceEvent();
                        if (_SelectSlides[_SelectSlideWebcamCapabilities].Selected)
                            _OnCapabilitiesEvent();
                        _SaveConfig();
                        break;

                    case Keys.Right:
                        if (_SelectSlides[_SelectSlideWebcamDevices].Selected)
                            _OnDeviceEvent();
                        if (_SelectSlides[_SelectSlideWebcamCapabilities].Selected)
                            _OnCapabilitiesEvent();
                        _SaveConfig();
                        break;
                }
            }
            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);

            if (mouseEvent.RB)
            {
                _SaveConfig();
                CGraphics.FadeTo(EScreen.Options);
            }

            if (mouseEvent.LB && _IsMouseOverCurSelection(mouseEvent))
            {
                if (_SelectSlides[_SelectSlideWebcamDevices].Selected)
                    _OnDeviceEvent();
                if (_SelectSlides[_SelectSlideWebcamCapabilities].Selected)
                    _OnCapabilitiesEvent();
                _SaveConfig();
                if (_Buttons[_ButtonExit].Selected)
                    CGraphics.FadeTo(EScreen.Options);
                if (_Buttons[_ButtonScreenAdjustments].Selected)
                    CGraphics.FadeTo(EScreen.OptionsVideoAdjustments);
            }
            return true;
        }

        public override bool UpdateGame()
        {
            if (CWebcam.GetFrame(ref _WebcamTexture))
                _Statics[_StaticWebcamOutput].Texture = _WebcamTexture;
            _SelectSlides[_SelectSlideVideosToBackground].Selection = (int)CConfig.Config.Video.VideosToBackground;
            return true;
        }

        public override void OnClose()
        {
            base.OnClose();
            CWebcam.Stop();
        }

        public override void OnShow()
        {
            base.OnShow();
            _SelectSlides[_SelectSlideWebcamDevices].Clear();
            _SelectSlides[_SelectSlideWebcamCapabilities].Clear();

            _DeviceNr = -1;
            _CapabilityNr = -1;

            SWebcamDevice[] devices = CWebcam.GetDevices();

            bool ssVisible = false;
            try
            {
                if (devices != null && devices.Length > 0)
                {
                    foreach (SWebcamDevice d in devices)
                        _SelectSlides[_SelectSlideWebcamDevices].AddValue(d.Name);

                    int devNr;
                    int capNr;
                    if (_GetFirstConfiguredWebcamDevice(out devNr, out capNr))
                    {
                        _SelectSlides[_SelectSlideWebcamDevices].Selection = devNr;
                        _OnDeviceEvent();
                        _SelectSlides[_SelectSlideWebcamCapabilities].Selection = capNr;
                        _OnCapabilitiesEvent();

                        ssVisible = true;
                    }
                }
            }
            catch (Exception e)
            {
                CLog.LogError("Error on listing webcam capabilities: " + e.Message);
            }

            _SelectSlides[_SelectSlideWebcamDevices].Visible = ssVisible;
            _SelectSlides[_SelectSlideWebcamCapabilities].Visible = ssVisible;
            _Statics[_StaticWebcamOutput].Visible = ssVisible;
            _Texts[_TextWebcams].Visible = ssVisible;
            _Texts[_TextWebcamResolution].Visible = ssVisible;
        }

        private void _SaveConfig()
        {
            CConfig.Config.Video.VideoBackgrounds = (EOffOn)_SelectSlides[_SelectSlideVideoBackgrounds].Selection;
            CConfig.Config.Video.VideoPreview = (EOffOn)_SelectSlides[_SelectSlideVideoPreview].Selection;
            CConfig.Config.Video.VideosInSongs = (EOffOn)_SelectSlides[_SelectSlideVideosInSongs].Selection;
            CConfig.Config.Video.VideosToBackground = (EOffOn)_SelectSlides[_SelectSlideVideosToBackground].Selection;

            CConfig.Config.Video.WebcamConfig = _Config;
            CBackgroundMusic.VideoEnabled = CConfig.Config.Video.VideoBackgrounds == EOffOn.TR_CONFIG_ON && CConfig.Config.Video.VideosToBackground == EOffOn.TR_CONFIG_ON;

            CConfig.SaveConfig();
        }

        private void _OnDeviceEvent()
        {
            if (_SelectSlides[_SelectSlideWebcamDevices].Selection != _DeviceNr)
            {
                _SelectSlides[_SelectSlideWebcamCapabilities].Clear();
                _DeviceNr = _SelectSlides[_SelectSlideWebcamDevices].Selection;

                SWebcamDevice d = CWebcam.GetDevices()[_DeviceNr];
                for (int i = 0; i < d.Capabilities.Count; i++)
                {
                    _SelectSlides[_SelectSlideWebcamCapabilities].AddValue(d.Capabilities[i].Width + " x " + d.Capabilities[i].Height +
                                                                           " @ " + d.Capabilities[i].Framerate + "FPS");
                }
                _CapabilityNr = -1;
                _OnCapabilitiesEvent();
            }
        }

        private void _OnCapabilitiesEvent()
        {
            if (_SelectSlides[_SelectSlideWebcamCapabilities].Selection != _CapabilityNr)
            {
                _CapabilityNr = _SelectSlides[_SelectSlideWebcamCapabilities].Selection;

                SWebcamDevice d = CWebcam.GetDevices()[_DeviceNr];
                _Config.MonikerString = d.MonikerString;
                _Config.Width = d.Capabilities[_CapabilityNr].Width;
                _Config.Height = d.Capabilities[_CapabilityNr].Height;
                _Config.Framerate = d.Capabilities[_CapabilityNr].Framerate;

                if (CWebcam.Select(_Config))
                    CWebcam.Start();
            }
        }

        private bool _GetFirstConfiguredWebcamDevice(out int device, out int capabilities)
        {
            SWebcamDevice[] devices = CWebcam.GetDevices();

            if (devices == null)
            {
                device = -1;
                capabilities = -1;
                return false;
            }
            SWebcamConfig curConfig = CConfig.Config.Video.WebcamConfig.HasValue ? CConfig.Config.Video.WebcamConfig.Value : new SWebcamConfig();
            for (int i = 0; i < devices.Length; i++)
            {
                if (devices[i].MonikerString != curConfig.MonikerString)
                    continue;
                for (int j = 0; j < devices[i].Capabilities.Count; j++)
                {
                    if (devices[i].Capabilities[j].Framerate == curConfig.Framerate &&
                        devices[i].Capabilities[j].Width == curConfig.Width &&
                        devices[i].Capabilities[j].Height == curConfig.Height)
                    {
                        device = i;
                        capabilities = j;
                        return true;
                    }
                }
            }
            device = 0;
            capabilities = 0;
            return true;
        }
    }
}