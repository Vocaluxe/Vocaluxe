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
using System.Windows.Forms;
using Vocaluxe.Base;
using Vocaluxe.Lib.Webcam;
using VocaluxeLib;
using VocaluxeLib.Menu;
using VocaluxeLib.Draw;

namespace Vocaluxe.Screens
{
    class CScreenOptionsVideo : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 3; }
        }

        private const string _SelectSlideVideoBackgrounds = "SelectSlideVideoBackgrounds";
        private const string _SelectSlideVideoPreview = "SelectSlideVideoPreview";
        private const string _SelectSlideVideosInSongs = "SelectSlideVideosInSongs";
        private const string _SelectSlideVideosToBackground = "SelectSlideVideosToBackground";
        private const string _SelectSlideWebcamDevices = "SelectSlideWebcamDevices";
        private const string _SelectSlideWebcamCapabilities = "SelectSlideWebcamCapabilities";

        private const string _StaticWebcamOutput = "StaticWebcamOutput";

        private const string _ButtonExit = "ButtonExit";

        private SWebcamConfig _Config;
        private CTexture _WebcamTexture;
        private int _DeviceNr;
        private int _CapabilityNr;

        public override void Init()
        {
            base.Init();

            _ThemeStatics = new string[] {_StaticWebcamOutput};
            _ThemeButtons = new string[] {_ButtonExit};
            _ThemeSelectSlides = new string[]
                {
                    _SelectSlideVideoBackgrounds, _SelectSlideVideoPreview, _SelectSlideVideosInSongs, _SelectSlideVideosToBackground, _SelectSlideWebcamDevices,
                    _SelectSlideWebcamCapabilities
                };
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            _SelectSlides[_SelectSlideVideoBackgrounds].SetValues<EOffOn>((int)CConfig.VideoBackgrounds);
            _SelectSlides[_SelectSlideVideoPreview].SetValues<EOffOn>((int)CConfig.VideoPreview);
            _SelectSlides[_SelectSlideVideosInSongs].SetValues<EOffOn>((int)CConfig.VideosInSongs);
            _SelectSlides[_SelectSlideVideosToBackground].SetValues<EOffOn>((int)CConfig.VideosToBackground);
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
                        CGraphics.FadeTo(EScreens.ScreenOptions);
                        break;

                    case Keys.S:
                        CParty.SetNormalGameMode();
                        CGraphics.FadeTo(EScreens.ScreenSong);
                        break;

                    case Keys.Enter:
                        if (_Buttons[_ButtonExit].Selected)
                        {
                            _SaveConfig();
                            CGraphics.FadeTo(EScreens.ScreenOptions);
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
                CGraphics.FadeTo(EScreens.ScreenOptions);
            }

            if (mouseEvent.LB && _IsMouseOver(mouseEvent))
            {
                if (_SelectSlides[_SelectSlideWebcamDevices].Selected)
                    _OnDeviceEvent();
                if (_SelectSlides[_SelectSlideWebcamCapabilities].Selected)
                    _OnCapabilitiesEvent();
                _SaveConfig();
                if (_Buttons[_ButtonExit].Selected)
                    CGraphics.FadeTo(EScreens.ScreenOptions);
            }
            return true;
        }

        public override bool UpdateGame()
        {
            CWebcam.GetFrame(ref _WebcamTexture);
            _Statics[_StaticWebcamOutput].Texture = _WebcamTexture;
            _SelectSlides[_SelectSlideVideosToBackground].Selection = (int)CConfig.VideosToBackground;
            return true;
        }

        public override bool Draw()
        {
            base.Draw();

            return true;
        }

        public override void OnClose()
        {
            base.OnClose();
            CWebcam.Close();
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
                    _DeviceNr = 0;
                    _CapabilityNr = 0;
                    _GetFirstConfiguredWebcamDevice(ref _DeviceNr, ref _CapabilityNr);

                    foreach (SWebcamDevice d in devices)
                        _SelectSlides[_SelectSlideWebcamDevices].AddValue(d.Name);
                    _SelectSlides[_SelectSlideWebcamDevices].Selection = _DeviceNr;

                    foreach (SCapabilities c in devices[_DeviceNr].Capabilities)
                        _SelectSlides[_SelectSlideWebcamCapabilities].AddValue(c.Width + " x " + c.Height + " @ " + c.Framerate + " FPS ");
                    _Config.MonikerString = devices[_SelectSlides[_SelectSlideWebcamDevices].Selection].MonikerString;
                    _Config.Width = devices[_SelectSlides[_SelectSlideWebcamDevices].Selection].Capabilities[_SelectSlides[_SelectSlideWebcamCapabilities].Selection].Width;
                    _Config.Height = devices[_SelectSlides[_SelectSlideWebcamDevices].Selection].Capabilities[_SelectSlides[_SelectSlideWebcamCapabilities].Selection].Height;
                    _Config.Framerate =
                        devices[_SelectSlides[_SelectSlideWebcamDevices].Selection].Capabilities[_SelectSlides[_SelectSlideWebcamCapabilities].Selection].Framerate;
                    CWebcam.Close();
                    CWebcam.Select(CConfig.WebcamConfig);
                    CWebcam.Start();
                    ssVisible = true;
                }
            }
            catch (Exception e)
            {
                CLog.LogError("Error on listing webcam capabilities: " + e.Message);
            }

            _SelectSlides[_SelectSlideWebcamDevices].Visible = ssVisible;
            _SelectSlides[_SelectSlideWebcamCapabilities].Visible = ssVisible;
            _Statics[_StaticWebcamOutput].Visible = ssVisible;
        }

        private void _SaveConfig()
        {
            CConfig.VideoBackgrounds = (EOffOn)_SelectSlides[_SelectSlideVideoBackgrounds].Selection;
            CConfig.VideoPreview = (EOffOn)_SelectSlides[_SelectSlideVideoPreview].Selection;
            CConfig.VideosInSongs = (EOffOn)_SelectSlides[_SelectSlideVideosInSongs].Selection;
            CConfig.VideosToBackground = (EOffOn)_SelectSlides[_SelectSlideVideosToBackground].Selection;

            CConfig.WebcamConfig = _Config;
            CBackgroundMusic.VideoEnabled = CConfig.VideoBackgrounds == EOffOn.TR_CONFIG_ON && CConfig.VideosToBackground == EOffOn.TR_CONFIG_ON;

            CConfig.SaveConfig();
        }

        private void _OnDeviceEvent()
        {
            if (_SelectSlides[_SelectSlideWebcamDevices].Selection != _DeviceNr)
            {
                _SelectSlides[_SelectSlideWebcamCapabilities].Clear();
                _DeviceNr = _SelectSlides[_SelectSlideWebcamDevices].Selection;
                _CapabilityNr = 0;

                SWebcamDevice d = CWebcam.GetDevices()[_DeviceNr];
                for (int i = 0; i < d.Capabilities.Count; i++)
                {
                    _SelectSlides[_SelectSlideWebcamCapabilities].AddValue(d.Capabilities[i].Width + " x " + d.Capabilities[i].Height +
                                                                           " @ " + d.Capabilities[i].Framerate + "FPS");
                }
                _SelectSlides[_SelectSlideWebcamCapabilities].Selection = 0;

                _Config.MonikerString = d.MonikerString;
                _Config.Width = d.Capabilities[_SelectSlides[_SelectSlideWebcamCapabilities].Selection].Width;
                _Config.Height = d.Capabilities[_SelectSlides[_SelectSlideWebcamCapabilities].Selection].Height;
                _Config.Framerate = d.Capabilities[_SelectSlides[_SelectSlideWebcamCapabilities].Selection].Framerate;

                CWebcam.Close();
                CWebcam.Select(_Config);
                CWebcam.Start();
            }
        }

        private void _OnCapabilitiesEvent()
        {
            if (_SelectSlides[_SelectSlideWebcamCapabilities].Selection != _CapabilityNr)
            {
                _CapabilityNr = _SelectSlides[_SelectSlideWebcamCapabilities].Selection;

                SWebcamDevice d = CWebcam.GetDevices()[_SelectSlides[_SelectSlideWebcamDevices].Selection];
                _Config.MonikerString = d.MonikerString;
                _Config.Width = d.Capabilities[_SelectSlides[_SelectSlideWebcamCapabilities].Selection].Width;
                _Config.Height = d.Capabilities[_SelectSlides[_SelectSlideWebcamCapabilities].Selection].Height;
                _Config.Framerate = d.Capabilities[_SelectSlides[_SelectSlideWebcamCapabilities].Selection].Framerate;

                CWebcam.Close();
                CWebcam.Select(_Config);
                CWebcam.Start();
            }
        }

        private void _GetFirstConfiguredWebcamDevice(ref int device, ref int capabilities)
        {
            SWebcamDevice[] devices = CWebcam.GetDevices();

            if (devices == null)
                return;

            for (int i = 0; i < devices.Length; i++)
            {
                if (devices[i].MonikerString != CConfig.WebcamConfig.MonikerString)
                    continue;
                for (int j = 0; j < devices[i].Capabilities.Count; j++)
                {
                    if (devices[i].Capabilities[j].Framerate == CConfig.WebcamConfig.Framerate && devices[i].Capabilities[j].Width == CConfig.WebcamConfig.Width &&
                        devices[i].Capabilities[j].Height == CConfig.WebcamConfig.Height)
                    {
                        device = i;
                        capabilities = j;
                        return;
                    }
                }
            }
        }
    }
}