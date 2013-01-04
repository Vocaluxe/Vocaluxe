using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.Menu;
using Vocaluxe.Lib.Webcam;


namespace Vocaluxe.Screens
{
    class CScreenOptionsVideo : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        const int ScreenVersion = 3;

        private const string SelectSlideVideoBackgrounds = "SelectSlideVideoBackgrounds";
        private const string SelectSlideVideoPreview = "SelectSlideVideoPreview";
        private const string SelectSlideVideosInSongs = "SelectSlideVideosInSongs";
        private const string SelectSlideVideosToBackground = "SelectSlideVideosToBackground";
        private const string SelectSlideWebcamDevices = "SelectSlideWebcamDevices";
        private const string SelectSlideWebcamCapabilities = "SelectSlideWebcamCapabilities";

        private const string StaticWebcamOutput = "StaticWebcamOutput";

        private const string ButtonExit = "ButtonExit";

        private SWebcamConfig _Config;
        STexture _WebcamTexture = new STexture(-1);
        private int _DeviceNr;
        private int _CapabilityNr;

        public CScreenOptionsVideo()
        {
        }

        protected override void Init()
        {
            base.Init();

            _ThemeName = "ScreenOptionsVideo";
            _ScreenVersion = ScreenVersion;

            _ThemeStatics = new string[] { StaticWebcamOutput };
            _ThemeButtons = new string[] { ButtonExit };
            _ThemeSelectSlides = new string[] { SelectSlideVideoBackgrounds, SelectSlideVideoPreview, SelectSlideVideosInSongs, SelectSlideVideosToBackground, SelectSlideWebcamDevices, SelectSlideWebcamCapabilities };
        }

        public override void LoadTheme(string XmlPath)
        {
            base.LoadTheme(XmlPath);

            SelectSlides[htSelectSlides(SelectSlideVideoBackgrounds)].SetValues<EOffOn>((int)CConfig.VideoBackgrounds);
            SelectSlides[htSelectSlides(SelectSlideVideoPreview)].SetValues<EOffOn>((int)CConfig.VideoPreview);
            SelectSlides[htSelectSlides(SelectSlideVideosInSongs)].SetValues<EOffOn>((int)CConfig.VideosInSongs);
            SelectSlides[htSelectSlides(SelectSlideVideosToBackground)].SetValues<EOffOn>((int)CConfig.VideosToBackground);
        }

        public override bool HandleInput(KeyEvent KeyEvent)
        {
            base.HandleInput(KeyEvent);

            if (KeyEvent.KeyPressed)
            {

            }
            else
            {
                switch (KeyEvent.Key)
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
                        if (Buttons[htButtons(ButtonExit)].Selected)
                        {
                            SaveConfig();
                            CGraphics.FadeTo(EScreens.ScreenOptions);
                        }
                        break;

                    case Keys.Left:
                        if (SelectSlides[htSelectSlides(SelectSlideWebcamDevices)].Selected)
                            OnDeviceEvent();
                        if (SelectSlides[htSelectSlides(SelectSlideWebcamCapabilities)].Selected)
                            OnCapabilitiesEvent();
                        SaveConfig();
                        break;

                    case Keys.Right:
                        if (SelectSlides[htSelectSlides(SelectSlideWebcamDevices)].Selected)
                            OnDeviceEvent();
                        if (SelectSlides[htSelectSlides(SelectSlideWebcamCapabilities)].Selected)
                            OnCapabilitiesEvent();
                        SaveConfig();
                        break;
                }
            }
            return true;
        }

        public override bool HandleMouse(MouseEvent MouseEvent)
        {
            base.HandleMouse(MouseEvent);

            if (MouseEvent.RB)
            {
                SaveConfig();
                CGraphics.FadeTo(EScreens.ScreenOptions);
            }

            if (MouseEvent.LB && IsMouseOver(MouseEvent))
            {
                if (SelectSlides[htSelectSlides(SelectSlideWebcamDevices)].Selected)
                    OnDeviceEvent();
                if (SelectSlides[htSelectSlides(SelectSlideWebcamCapabilities)].Selected)
                    OnCapabilitiesEvent();
                SaveConfig();
                if (Buttons[htButtons(ButtonExit)].Selected)
                {
                    CGraphics.FadeTo(EScreens.ScreenOptions);
                }
            }
            return true;
        }

        public override bool UpdateGame()
        {
            CWebcam.GetFrame(ref _WebcamTexture);
            Statics[htStatics(StaticWebcamOutput)].Texture = _WebcamTexture;
            SelectSlides[htSelectSlides(SelectSlideVideosToBackground)].Selection = (int)CConfig.VideosToBackground;
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
            SelectSlides[htSelectSlides(SelectSlideWebcamDevices)].Clear();
            SelectSlides[htSelectSlides(SelectSlideWebcamCapabilities)].Clear();

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
                        {
                            SelectSlides[htSelectSlides(SelectSlideWebcamDevices)].AddValue(d.Name);
                        }
                        SelectSlides[htSelectSlides(SelectSlideWebcamDevices)].Selection = _DeviceNr;

                        foreach (SCapabilities c in devices[_DeviceNr].Capabilities)
                        {
                            SelectSlides[htSelectSlides(SelectSlideWebcamCapabilities)].AddValue(c.Width.ToString() + " x " + c.Height.ToString() + " @ " + c.Framerate.ToString() + " FPS ");
                        }
                        _Config.MonikerString = devices[SelectSlides[htSelectSlides(SelectSlideWebcamDevices)].Selection].MonikerString;
                        _Config.Width = devices[SelectSlides[htSelectSlides(SelectSlideWebcamDevices)].Selection].Capabilities[SelectSlides[htSelectSlides(SelectSlideWebcamCapabilities)].Selection].Width;
                        _Config.Height = devices[SelectSlides[htSelectSlides(SelectSlideWebcamDevices)].Selection].Capabilities[SelectSlides[htSelectSlides(SelectSlideWebcamCapabilities)].Selection].Height;
                        _Config.Framerate = devices[SelectSlides[htSelectSlides(SelectSlideWebcamDevices)].Selection].Capabilities[SelectSlides[htSelectSlides(SelectSlideWebcamCapabilities)].Selection].Framerate;
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
            
            SelectSlides[htSelectSlides(SelectSlideWebcamDevices)].Visible = (devices != null && devices.Length > 0);
            SelectSlides[htSelectSlides(SelectSlideWebcamCapabilities)].Visible = (devices != null && devices.Length > 0);
            Statics[htStatics(StaticWebcamOutput)].Visible = (devices != null && devices.Length > 0);
        }

        private void SaveConfig()
        {
            CConfig.VideoBackgrounds = (EOffOn)SelectSlides[htSelectSlides(SelectSlideVideoBackgrounds)].Selection;
            CConfig.VideoPreview = (EOffOn)SelectSlides[htSelectSlides(SelectSlideVideoPreview)].Selection;
            CConfig.VideosInSongs = (EOffOn)SelectSlides[htSelectSlides(SelectSlideVideosInSongs)].Selection;
            CConfig.VideosToBackground = (EOffOn)SelectSlides[htSelectSlides(SelectSlideVideosToBackground)].Selection;

            CConfig.WebcamConfig = _Config;
            CBackgroundMusic.VideoEnabled = true;

            CConfig.SaveConfig();
        }

        private void OnDeviceEvent()
        {
            if (SelectSlides[htSelectSlides(SelectSlideWebcamDevices)].Selection != _DeviceNr)
            {
                SelectSlides[htSelectSlides(SelectSlideWebcamCapabilities)].Clear();
                _DeviceNr = SelectSlides[htSelectSlides(SelectSlideWebcamDevices)].Selection;
                _CapabilityNr = 0;

                SWebcamDevice[] d = CWebcam.GetDevices();
                for (int i = 0; i < d[SelectSlides[htSelectSlides(SelectSlideWebcamDevices)].Selection].Capabilities.Count; i++)
                {
                    SelectSlides[htSelectSlides(SelectSlideWebcamCapabilities)].AddValue(d[_DeviceNr].Capabilities[i].Width.ToString() + " x " + d[_DeviceNr].Capabilities[i].Height.ToString() + " @ " + d[_DeviceNr].Capabilities[i].Framerate.ToString() + "FPS");
                }
                SelectSlides[htSelectSlides(SelectSlideWebcamCapabilities)].Selection = 0;

                _Config.MonikerString = d[SelectSlides[htSelectSlides(SelectSlideWebcamDevices)].Selection].MonikerString;
                _Config.Width = d[SelectSlides[htSelectSlides(SelectSlideWebcamDevices)].Selection].Capabilities[SelectSlides[htSelectSlides(SelectSlideWebcamCapabilities)].Selection].Width;
                _Config.Height = d[SelectSlides[htSelectSlides(SelectSlideWebcamDevices)].Selection].Capabilities[SelectSlides[htSelectSlides(SelectSlideWebcamCapabilities)].Selection].Height;
                _Config.Framerate = d[SelectSlides[htSelectSlides(SelectSlideWebcamDevices)].Selection].Capabilities[SelectSlides[htSelectSlides(SelectSlideWebcamCapabilities)].Selection].Framerate;

                CWebcam.Close();
                CWebcam.Select(_Config);
                CWebcam.Start();
            }
        }

        private void OnCapabilitiesEvent()
        {
            if (SelectSlides[htSelectSlides(SelectSlideWebcamCapabilities)].Selection != _CapabilityNr)
            {
                _CapabilityNr = SelectSlides[htSelectSlides(SelectSlideWebcamCapabilities)].Selection;

                SWebcamDevice[] d = CWebcam.GetDevices();
                _Config.MonikerString = d[SelectSlides[htSelectSlides(SelectSlideWebcamDevices)].Selection].MonikerString;
                _Config.Width = d[SelectSlides[htSelectSlides(SelectSlideWebcamDevices)].Selection].Capabilities[SelectSlides[htSelectSlides(SelectSlideWebcamCapabilities)].Selection].Width;
                _Config.Height = d[SelectSlides[htSelectSlides(SelectSlideWebcamDevices)].Selection].Capabilities[SelectSlides[htSelectSlides(SelectSlideWebcamCapabilities)].Selection].Height;
                _Config.Framerate = d[SelectSlides[htSelectSlides(SelectSlideWebcamDevices)].Selection].Capabilities[SelectSlides[htSelectSlides(SelectSlideWebcamCapabilities)].Selection].Framerate;

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
                        if (devices[i].Capabilities[j].Framerate == CConfig.WebcamConfig.Framerate && devices[i].Capabilities[j].Width == CConfig.WebcamConfig.Width && devices[i].Capabilities[j].Height == CConfig.WebcamConfig.Height)
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
