using System;
using System.Windows.Forms;
using Vocaluxe.Base;
using Vocaluxe.Lib.Webcam;
using VocaluxeLib.Menu;

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
        private STexture _WebcamTexture = new STexture(-1);
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

            SelectSlides[_SelectSlideVideoBackgrounds].SetValues<EOffOn>((int)CConfig.VideoBackgrounds);
            SelectSlides[_SelectSlideVideoPreview].SetValues<EOffOn>((int)CConfig.VideoPreview);
            SelectSlides[_SelectSlideVideosInSongs].SetValues<EOffOn>((int)CConfig.VideosInSongs);
            SelectSlides[_SelectSlideVideosToBackground].SetValues<EOffOn>((int)CConfig.VideosToBackground);
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
                        if (Buttons[_ButtonExit].Selected)
                        {
                            _SaveConfig();
                            CGraphics.FadeTo(EScreens.ScreenOptions);
                        }
                        break;

                    case Keys.Left:
                        if (SelectSlides[_SelectSlideWebcamDevices].Selected)
                            _OnDeviceEvent();
                        if (SelectSlides[_SelectSlideWebcamCapabilities].Selected)
                            _OnCapabilitiesEvent();
                        _SaveConfig();
                        break;

                    case Keys.Right:
                        if (SelectSlides[_SelectSlideWebcamDevices].Selected)
                            _OnDeviceEvent();
                        if (SelectSlides[_SelectSlideWebcamCapabilities].Selected)
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

            if (mouseEvent.LB && IsMouseOver(mouseEvent))
            {
                if (SelectSlides[_SelectSlideWebcamDevices].Selected)
                    _OnDeviceEvent();
                if (SelectSlides[_SelectSlideWebcamCapabilities].Selected)
                    _OnCapabilitiesEvent();
                _SaveConfig();
                if (Buttons[_ButtonExit].Selected)
                    CGraphics.FadeTo(EScreens.ScreenOptions);
            }
            return true;
        }

        public override bool UpdateGame()
        {
            CWebcam.GetFrame(ref _WebcamTexture);
            Statics[_StaticWebcamOutput].Texture = _WebcamTexture;
            SelectSlides[_SelectSlideVideosToBackground].Selection = (int)CConfig.VideosToBackground;
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
            SelectSlides[_SelectSlideWebcamDevices].Clear();
            SelectSlides[_SelectSlideWebcamCapabilities].Clear();

            _DeviceNr = -1;
            _CapabilityNr = -1;

            SWebcamDevice[] devices = CWebcam.GetDevices();

            try
            {
                if (devices != null)
                {
                    if (devices.Length > 0)
                    {
                        _DeviceNr = 0;
                        _CapabilityNr = 0;
                        _GetFirstConfiguredWebcamDevice(ref _DeviceNr, ref _CapabilityNr);

                        foreach (SWebcamDevice d in devices)
                            SelectSlides[_SelectSlideWebcamDevices].AddValue(d.Name);
                        SelectSlides[_SelectSlideWebcamDevices].Selection = _DeviceNr;

                        foreach (SCapabilities c in devices[_DeviceNr].Capabilities)
                            SelectSlides[_SelectSlideWebcamCapabilities].AddValue(c.Width.ToString() + " x " + c.Height.ToString() + " @ " + c.Framerate.ToString() + " FPS ");
                        _Config.MonikerString = devices[SelectSlides[_SelectSlideWebcamDevices].Selection].MonikerString;
                        _Config.Width = devices[SelectSlides[_SelectSlideWebcamDevices].Selection].Capabilities[SelectSlides[_SelectSlideWebcamCapabilities].Selection].Width;
                        _Config.Height = devices[SelectSlides[_SelectSlideWebcamDevices].Selection].Capabilities[SelectSlides[_SelectSlideWebcamCapabilities].Selection].Height;
                        _Config.Framerate = devices[SelectSlides[_SelectSlideWebcamDevices].Selection].Capabilities[SelectSlides[_SelectSlideWebcamCapabilities].Selection].Framerate;
                        CWebcam.Close();
                        CWebcam.Select(CConfig.WebcamConfig);
                        CWebcam.Start();
                    }
                }
            }
            catch (Exception e)
            {
                CLog.LogError("Error on listing webcam capabilities: " + e.Message);
                devices = null;
            }

            SelectSlides[_SelectSlideWebcamDevices].Visible = devices != null && devices.Length > 0;
            SelectSlides[_SelectSlideWebcamCapabilities].Visible = devices != null && devices.Length > 0;
            Statics[_StaticWebcamOutput].Visible = devices != null && devices.Length > 0;
        }

        private void _SaveConfig()
        {
            CConfig.VideoBackgrounds = (EOffOn)SelectSlides[_SelectSlideVideoBackgrounds].Selection;
            CConfig.VideoPreview = (EOffOn)SelectSlides[_SelectSlideVideoPreview].Selection;
            CConfig.VideosInSongs = (EOffOn)SelectSlides[_SelectSlideVideosInSongs].Selection;
            CConfig.VideosToBackground = (EOffOn)SelectSlides[_SelectSlideVideosToBackground].Selection;

            CConfig.WebcamConfig = _Config;
            CBackgroundMusic.VideoEnabled = true;

            CConfig.SaveConfig();
        }

        private void _OnDeviceEvent()
        {
            if (SelectSlides[_SelectSlideWebcamDevices].Selection != _DeviceNr)
            {
                SelectSlides[_SelectSlideWebcamCapabilities].Clear();
                _DeviceNr = SelectSlides[_SelectSlideWebcamDevices].Selection;
                _CapabilityNr = 0;

                SWebcamDevice[] d = CWebcam.GetDevices();
                for (int i = 0; i < d[SelectSlides[_SelectSlideWebcamDevices].Selection].Capabilities.Count; i++)
                {
                    SelectSlides[_SelectSlideWebcamCapabilities].AddValue(d[_DeviceNr].Capabilities[i].Width.ToString() + " x " + d[_DeviceNr].Capabilities[i].Height.ToString() +
                                                                         " @ " + d[_DeviceNr].Capabilities[i].Framerate.ToString() + "FPS");
                }
                SelectSlides[_SelectSlideWebcamCapabilities].Selection = 0;

                _Config.MonikerString = d[SelectSlides[_SelectSlideWebcamDevices].Selection].MonikerString;
                _Config.Width = d[SelectSlides[_SelectSlideWebcamDevices].Selection].Capabilities[SelectSlides[_SelectSlideWebcamCapabilities].Selection].Width;
                _Config.Height = d[SelectSlides[_SelectSlideWebcamDevices].Selection].Capabilities[SelectSlides[_SelectSlideWebcamCapabilities].Selection].Height;
                _Config.Framerate = d[SelectSlides[_SelectSlideWebcamDevices].Selection].Capabilities[SelectSlides[_SelectSlideWebcamCapabilities].Selection].Framerate;

                CWebcam.Close();
                CWebcam.Select(_Config);
                CWebcam.Start();
            }
        }

        private void _OnCapabilitiesEvent()
        {
            if (SelectSlides[_SelectSlideWebcamCapabilities].Selection != _CapabilityNr)
            {
                _CapabilityNr = SelectSlides[_SelectSlideWebcamCapabilities].Selection;

                SWebcamDevice[] d = CWebcam.GetDevices();
                _Config.MonikerString = d[SelectSlides[_SelectSlideWebcamDevices].Selection].MonikerString;
                _Config.Width = d[SelectSlides[_SelectSlideWebcamDevices].Selection].Capabilities[SelectSlides[_SelectSlideWebcamCapabilities].Selection].Width;
                _Config.Height = d[SelectSlides[_SelectSlideWebcamDevices].Selection].Capabilities[SelectSlides[_SelectSlideWebcamCapabilities].Selection].Height;
                _Config.Framerate = d[SelectSlides[_SelectSlideWebcamDevices].Selection].Capabilities[SelectSlides[_SelectSlideWebcamCapabilities].Selection].Framerate;

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
                if (devices[i].MonikerString == CConfig.WebcamConfig.MonikerString)
                {
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
}