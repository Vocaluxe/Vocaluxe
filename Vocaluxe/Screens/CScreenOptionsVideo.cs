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

        private const string SelectSlideVideoBackgrounds = "SelectSlideVideoBackgrounds";
        private const string SelectSlideVideoPreview = "SelectSlideVideoPreview";
        private const string SelectSlideVideosInSongs = "SelectSlideVideosInSongs";
        private const string SelectSlideVideosToBackground = "SelectSlideVideosToBackground";
        private const string SelectSlideWebcamDevices = "SelectSlideWebcamDevices";
        private const string SelectSlideWebcamCapabilities = "SelectSlideWebcamCapabilities";

        private const string StaticWebcamOutput = "StaticWebcamOutput";

        private const string ButtonExit = "ButtonExit";

        private SWebcamConfig _Config;
        private STexture _WebcamTexture = new STexture(-1);
        private int _DeviceNr;
        private int _CapabilityNr;

        public override void Init()
        {
            base.Init();

            _ThemeStatics = new string[] {StaticWebcamOutput};
            _ThemeButtons = new string[] {ButtonExit};
            _ThemeSelectSlides = new string[]
                {
                    SelectSlideVideoBackgrounds, SelectSlideVideoPreview, SelectSlideVideosInSongs, SelectSlideVideosToBackground, SelectSlideWebcamDevices,
                    SelectSlideWebcamCapabilities
                };
        }

        public override void LoadTheme(string XmlPath)
        {
            base.LoadTheme(XmlPath);

            SelectSlides[SelectSlideVideoBackgrounds].SetValues<EOffOn>((int)CConfig.VideoBackgrounds);
            SelectSlides[SelectSlideVideoPreview].SetValues<EOffOn>((int)CConfig.VideoPreview);
            SelectSlides[SelectSlideVideosInSongs].SetValues<EOffOn>((int)CConfig.VideosInSongs);
            SelectSlides[SelectSlideVideosToBackground].SetValues<EOffOn>((int)CConfig.VideosToBackground);
        }

        public override bool HandleInput(KeyEvent keyEvent)
        {
            base.HandleInput(keyEvent);

            if (keyEvent.KeyPressed) {}
            else
            {
                switch (keyEvent.Key)
                {
                    case Keys.Escape:
                    case Keys.Back:
                        SaveConfig();
                        CGraphics.FadeTo(EScreens.ScreenOptions);
                        break;

                    case Keys.S:
                        CParty.SetNormalGameMode();
                        CGraphics.FadeTo(EScreens.ScreenSong);
                        break;

                    case Keys.Enter:
                        if (Buttons[ButtonExit].Selected)
                        {
                            SaveConfig();
                            CGraphics.FadeTo(EScreens.ScreenOptions);
                        }
                        break;

                    case Keys.Left:
                        if (SelectSlides[SelectSlideWebcamDevices].Selected)
                            OnDeviceEvent();
                        if (SelectSlides[SelectSlideWebcamCapabilities].Selected)
                            OnCapabilitiesEvent();
                        SaveConfig();
                        break;

                    case Keys.Right:
                        if (SelectSlides[SelectSlideWebcamDevices].Selected)
                            OnDeviceEvent();
                        if (SelectSlides[SelectSlideWebcamCapabilities].Selected)
                            OnCapabilitiesEvent();
                        SaveConfig();
                        break;
                }
            }
            return true;
        }

        public override bool HandleMouse(MouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);

            if (mouseEvent.RB)
            {
                SaveConfig();
                CGraphics.FadeTo(EScreens.ScreenOptions);
            }

            if (mouseEvent.LB && IsMouseOver(mouseEvent))
            {
                if (SelectSlides[SelectSlideWebcamDevices].Selected)
                    OnDeviceEvent();
                if (SelectSlides[SelectSlideWebcamCapabilities].Selected)
                    OnCapabilitiesEvent();
                SaveConfig();
                if (Buttons[ButtonExit].Selected)
                    CGraphics.FadeTo(EScreens.ScreenOptions);
            }
            return true;
        }

        public override bool UpdateGame()
        {
            CWebcam.GetFrame(ref _WebcamTexture);
            Statics[StaticWebcamOutput].Texture = _WebcamTexture;
            SelectSlides[SelectSlideVideosToBackground].Selection = (int)CConfig.VideosToBackground;
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
            SelectSlides[SelectSlideWebcamDevices].Clear();
            SelectSlides[SelectSlideWebcamCapabilities].Clear();

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
                        GetFirstConfiguredWebcamDevice(ref _DeviceNr, ref _CapabilityNr);

                        foreach (SWebcamDevice d in devices)
                            SelectSlides[SelectSlideWebcamDevices].AddValue(d.Name);
                        SelectSlides[SelectSlideWebcamDevices].Selection = _DeviceNr;

                        foreach (SCapabilities c in devices[_DeviceNr].Capabilities)
                            SelectSlides[SelectSlideWebcamCapabilities].AddValue(c.Width.ToString() + " x " + c.Height.ToString() + " @ " + c.Framerate.ToString() + " FPS ");
                        _Config.MonikerString = devices[SelectSlides[SelectSlideWebcamDevices].Selection].MonikerString;
                        _Config.Width = devices[SelectSlides[SelectSlideWebcamDevices].Selection].Capabilities[SelectSlides[SelectSlideWebcamCapabilities].Selection].Width;
                        _Config.Height = devices[SelectSlides[SelectSlideWebcamDevices].Selection].Capabilities[SelectSlides[SelectSlideWebcamCapabilities].Selection].Height;
                        _Config.Framerate = devices[SelectSlides[SelectSlideWebcamDevices].Selection].Capabilities[SelectSlides[SelectSlideWebcamCapabilities].Selection].Framerate;
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

            SelectSlides[SelectSlideWebcamDevices].Visible = devices != null && devices.Length > 0;
            SelectSlides[SelectSlideWebcamCapabilities].Visible = devices != null && devices.Length > 0;
            Statics[StaticWebcamOutput].Visible = devices != null && devices.Length > 0;
        }

        private void SaveConfig()
        {
            CConfig.VideoBackgrounds = (EOffOn)SelectSlides[SelectSlideVideoBackgrounds].Selection;
            CConfig.VideoPreview = (EOffOn)SelectSlides[SelectSlideVideoPreview].Selection;
            CConfig.VideosInSongs = (EOffOn)SelectSlides[SelectSlideVideosInSongs].Selection;
            CConfig.VideosToBackground = (EOffOn)SelectSlides[SelectSlideVideosToBackground].Selection;

            CConfig.WebcamConfig = _Config;
            CBackgroundMusic.VideoEnabled = true;

            CConfig.SaveConfig();
        }

        private void OnDeviceEvent()
        {
            if (SelectSlides[SelectSlideWebcamDevices].Selection != _DeviceNr)
            {
                SelectSlides[SelectSlideWebcamCapabilities].Clear();
                _DeviceNr = SelectSlides[SelectSlideWebcamDevices].Selection;
                _CapabilityNr = 0;

                SWebcamDevice[] d = CWebcam.GetDevices();
                for (int i = 0; i < d[SelectSlides[SelectSlideWebcamDevices].Selection].Capabilities.Count; i++)
                {
                    SelectSlides[SelectSlideWebcamCapabilities].AddValue(d[_DeviceNr].Capabilities[i].Width.ToString() + " x " + d[_DeviceNr].Capabilities[i].Height.ToString() +
                                                                         " @ " + d[_DeviceNr].Capabilities[i].Framerate.ToString() + "FPS");
                }
                SelectSlides[SelectSlideWebcamCapabilities].Selection = 0;

                _Config.MonikerString = d[SelectSlides[SelectSlideWebcamDevices].Selection].MonikerString;
                _Config.Width = d[SelectSlides[SelectSlideWebcamDevices].Selection].Capabilities[SelectSlides[SelectSlideWebcamCapabilities].Selection].Width;
                _Config.Height = d[SelectSlides[SelectSlideWebcamDevices].Selection].Capabilities[SelectSlides[SelectSlideWebcamCapabilities].Selection].Height;
                _Config.Framerate = d[SelectSlides[SelectSlideWebcamDevices].Selection].Capabilities[SelectSlides[SelectSlideWebcamCapabilities].Selection].Framerate;

                CWebcam.Close();
                CWebcam.Select(_Config);
                CWebcam.Start();
            }
        }

        private void OnCapabilitiesEvent()
        {
            if (SelectSlides[SelectSlideWebcamCapabilities].Selection != _CapabilityNr)
            {
                _CapabilityNr = SelectSlides[SelectSlideWebcamCapabilities].Selection;

                SWebcamDevice[] d = CWebcam.GetDevices();
                _Config.MonikerString = d[SelectSlides[SelectSlideWebcamDevices].Selection].MonikerString;
                _Config.Width = d[SelectSlides[SelectSlideWebcamDevices].Selection].Capabilities[SelectSlides[SelectSlideWebcamCapabilities].Selection].Width;
                _Config.Height = d[SelectSlides[SelectSlideWebcamDevices].Selection].Capabilities[SelectSlides[SelectSlideWebcamCapabilities].Selection].Height;
                _Config.Framerate = d[SelectSlides[SelectSlideWebcamDevices].Selection].Capabilities[SelectSlides[SelectSlideWebcamCapabilities].Selection].Framerate;

                CWebcam.Close();
                CWebcam.Select(_Config);
                CWebcam.Start();
            }
        }

        private void GetFirstConfiguredWebcamDevice(ref int Device, ref int Capabilities)
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
                            Device = i;
                            Capabilities = j;
                            return;
                        }
                    }
                }
            }
        }
    }
}