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
        const int ScreenVersion = 2;

        private const string SelectSlideVideoBackgrounds = "SelectSlideVideoBackgrounds";
        private const string SelectSlideVideoPreview = "SelectSlideVideoPreview";
        private const string SelectSlideVideosInSongs = "SelectSlideVideosInSongs";
        private const string SelectSlideVideosToBackground = "SelectSlideVideosToBackground";
        private const string SelectSlideWebcamDevices = "SelectSlideWebcamDevices";
        private const string SelectSlideWebcamCapabilities = "SelectSlideWebcamCapabilities";

        private const string StaticWebcamOutput = "StaticWebcamOutput";

        private const string ButtonExit = "ButtonExit";

        private SWebcamConfig _Config;
        Lib.Draw.STexture _WebcamTexture = new Lib.Draw.STexture(-1);

        public CScreenOptionsVideo()
        {
            Init();
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

        public override void LoadTheme()
        {
            base.LoadTheme();

            SelectSlides[htSelectSlides(SelectSlideVideoBackgrounds)].SetValues<EOffOn>((int)CConfig.VideoBackgrounds);
            SelectSlides[htSelectSlides(SelectSlideVideoPreview)].SetValues<EOffOn>((int)CConfig.VideoPreview);
            SelectSlides[htSelectSlides(SelectSlideVideosInSongs)].SetValues<EOffOn>((int)CConfig.VideosInSongs);
            SelectSlides[htSelectSlides(SelectSlideVideosToBackground)].SetValues<EOffOn>((int)CConfig.VideosToBackground);

            SWebcamDevice[] dev = CWebcam.GetDevices();
            foreach(SWebcamDevice d in dev)
            {
                SelectSlides[htSelectSlides(SelectSlideWebcamDevices)].AddValue(d.Name);
            }
            int deviceNr = SelectSlides[htSelectSlides(SelectSlideWebcamDevices)].Selection;
            for (int i = 0; i < dev[SelectSlides[htSelectSlides(SelectSlideWebcamDevices)].Selection].Capabilities.Count; i++)
            {
                SelectSlides[htSelectSlides(SelectSlideWebcamCapabilities)].AddValue(dev[deviceNr].Capabilities[i].Width.ToString() + " x " + dev[deviceNr].Capabilities[i].Height.ToString() + " @ " + dev[deviceNr].Capabilities[i].Framerate.ToString());
            }
            SelectSlides[htSelectSlides(SelectSlideWebcamCapabilities)].Selection = 0;

            _Config.MonikerString = dev[SelectSlides[htSelectSlides(SelectSlideWebcamDevices)].Selection].MonikerString;
            _Config.Width = dev[SelectSlides[htSelectSlides(SelectSlideWebcamDevices)].Selection].Capabilities[SelectSlides[htSelectSlides(SelectSlideWebcamCapabilities)].Selection].Width;
            _Config.Height = dev[SelectSlides[htSelectSlides(SelectSlideWebcamDevices)].Selection].Capabilities[SelectSlides[htSelectSlides(SelectSlideWebcamCapabilities)].Selection].Height;
            _Config.Framerate = dev[SelectSlides[htSelectSlides(SelectSlideWebcamDevices)].Selection].Capabilities[SelectSlides[htSelectSlides(SelectSlideWebcamCapabilities)].Selection].Framerate;
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
            CWebcam.Close();
            base.OnClose();
        }

        public override void OnShow()
        {
            CWebcam.Close();
            CWebcam.Select(_Config);
            CWebcam.Start();
            base.OnShow();
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
            int deviceNr = SelectSlides[htSelectSlides(SelectSlideWebcamDevices)].Selection;
            SelectSlides[htSelectSlides(SelectSlideWebcamCapabilities)].Clear();
            
            SWebcamDevice[] d = CWebcam.GetDevices();
            for (int i = 0; i < d[SelectSlides[htSelectSlides(SelectSlideWebcamDevices)].Selection].Capabilities.Count; i++)
            {
                SelectSlides[htSelectSlides(SelectSlideWebcamCapabilities)].AddValue(d[deviceNr].Capabilities[i].Width.ToString() + " x " + d[deviceNr].Capabilities[i].Height.ToString() + " @ " + d[deviceNr].Capabilities[i].Framerate.ToString());
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

        private void OnCapabilitiesEvent()
        {
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
}
