using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using Vocaluxe.Base;
using Vocaluxe.Lib.Sound;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    struct SDelayTest
    {
        public Stopwatch Timer;
        public float Delay;
    }

    class CScreenOptionsRecord : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 2; }
        }

        private const float _MaxDelayTime = 1f;
        private const string _SelectSlideRecordDevices = "SelectSlideRecordDevices";
        private const string _SelectSlideRecordInputs = "SelectSlideRecordInputs";

        private const string _SelectSlideRecordChannel1 = "SelectSlideRecordChannel1";
        private const string _SelectSlideRecordChannel2 = "SelectSlideRecordChannel2";
        private const string _SelectSlideDelay = "SelectSlideDelay";

        private const string _StaticWarning = "StaticWarning";
        private const string _TextWarning = "TextWarning";

        private const string _ButtonExit = "ButtonExit";
        private const string _ButtonDelayTest = "ButtonDelayTest";

        private const string _TextDelayChannel1 = "TextDelayChannel1";
        private const string _TextDelayChannel2 = "TextDelayChannel2";

        private const string _EqualizerChannel1 = "EqualizerChannel1";
        private const string _EqualizerChannel2 = "EqualizerChannel2";

        private readonly string[] _StaticEnergyChannel = new string[] {"StaticEnergyChannel1", "StaticEnergyChannel2"};
        private float[] _ChannelEnergy;

        private SRecordDevice[] _Devices;
        private int _DeviceNr;
        private int _InputNr;

        private SDelayTest[] _DelayTest;
        private bool _DelayTestRunning;
        private int _DelaySound;

        public override void Init()
        {
            base.Init();

            List<string> values = new List<string>();
            values.Add(_StaticWarning);
            values.AddRange(_StaticEnergyChannel);
            _ThemeStatics = values.ToArray();

            _ThemeTexts = new string[] {_TextWarning, _TextDelayChannel1, _TextDelayChannel2};
            _ThemeButtons = new string[] {_ButtonExit, _ButtonDelayTest};
            _ThemeSelectSlides = new string[] {_SelectSlideRecordDevices, _SelectSlideRecordInputs, _SelectSlideRecordChannel1, _SelectSlideRecordChannel2, _SelectSlideDelay};
            _ThemeEqualizers = new string[] {_EqualizerChannel1, _EqualizerChannel2};
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            int max = CSettings.MaxNumPlayer + 1;
            if (max > 7)
                max = 7;

            SelectSlides[_SelectSlideRecordChannel1].NumVisible = max;
            SelectSlides[_SelectSlideRecordChannel2].NumVisible = max;

            SelectSlides[_SelectSlideRecordChannel1].AddValue(CLanguage.Translate("TR_CONFIG_OFF"));
            SelectSlides[_SelectSlideRecordChannel2].AddValue(CLanguage.Translate("TR_CONFIG_OFF"));

            for (int i = 1; i <= CSettings.MaxNumPlayer; i++)
            {
                SelectSlides[_SelectSlideRecordChannel1].AddValue(i.ToString());
                SelectSlides[_SelectSlideRecordChannel2].AddValue(i.ToString());
            }

            for (int i = 0; i < 26; i++)
                SelectSlides[_SelectSlideDelay].AddValue((i * 20).ToString() + " ms");

            _ChannelEnergy = new float[_StaticEnergyChannel.Length];

            for (int i = 0; i < _ChannelEnergy.Length; i++)
            {
                Statics[_StaticEnergyChannel[i]].Visible = false;
                _ChannelEnergy[i] = 0f;
            }

            Equalizers[_EqualizerChannel1].ScreenHandles = true;
            Equalizers[_EqualizerChannel2].ScreenHandles = true;
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
                        _SaveMicConfig();
                        CGraphics.FadeTo(EScreens.ScreenOptions);
                        break;

                    case Keys.S:
                        CParty.SetNormalGameMode();
                        CGraphics.FadeTo(EScreens.ScreenSong);
                        break;

                    case Keys.Enter:
                        if (Buttons[_ButtonExit].Selected)
                        {
                            _SaveMicConfig();
                            CGraphics.FadeTo(EScreens.ScreenOptions);
                        }

                        if (Buttons[_ButtonDelayTest].Selected)
                            _TestDelay();

                        break;

                    case Keys.D:
                        _TestDelay();
                        break;

                    case Keys.Left:
                        if (SelectSlides[_SelectSlideRecordDevices].Selected)
                            _OnDeviceEvent();

                        if (SelectSlides[_SelectSlideRecordInputs].Selected)
                            _OnInputEvent();

                        if (SelectSlides[_SelectSlideRecordChannel1].Selected ||
                            SelectSlides[_SelectSlideRecordChannel2].Selected)
                            _SetMicConfig();

                        if (SelectSlides[_SelectSlideDelay].Selected)
                            _SaveDelayConfig();
                        break;

                    case Keys.Right:
                        if (SelectSlides[_SelectSlideRecordDevices].Selected)
                            _OnDeviceEvent();

                        if (SelectSlides[_SelectSlideRecordInputs].Selected)
                            _OnInputEvent();

                        if (SelectSlides[_SelectSlideRecordChannel1].Selected ||
                            SelectSlides[_SelectSlideRecordChannel2].Selected)
                            _SetMicConfig();

                        if (SelectSlides[_SelectSlideDelay].Selected)
                            _SaveDelayConfig();
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
                _SaveMicConfig();
                CGraphics.FadeTo(EScreens.ScreenOptions);
            }

            if (mouseEvent.LB && IsMouseOver(mouseEvent))
            {
                if (SelectSlides[_SelectSlideRecordDevices].Selected)
                    _OnDeviceEvent();

                if (SelectSlides[_SelectSlideRecordInputs].Selected)
                    _OnInputEvent();

                if (SelectSlides[_SelectSlideRecordChannel1].Selected ||
                    SelectSlides[_SelectSlideRecordChannel2].Selected)
                    _SetMicConfig();

                if (SelectSlides[_SelectSlideDelay].Selected)
                    _SaveDelayConfig();

                if (Buttons[_ButtonExit].Selected)
                {
                    _SaveMicConfig();
                    CGraphics.FadeTo(EScreens.ScreenOptions);
                }

                if (Buttons[_ButtonDelayTest].Selected)
                    _TestDelay();
            }
            return true;
        }

        public override bool UpdateGame()
        {
            for (int i = 0; i < CSettings.MaxNumPlayer; i++)
                CSound.AnalyzeBuffer(i);

            if (_DelayTest != null)
            {
                for (int i = 0; i < _DelayTest.Length - 1; i++)
                {
                    if (_DelayTestRunning && !_DelayTest[i].Timer.IsRunning)
                    {
                        if (CSound.GetPosition(_DelaySound) > 0f)
                        {
                            _DelayTest[i].Timer.Reset();
                            _DelayTest[i].Timer.Start();
                        }
                    }
                    if (_DelayTest[i].Timer.IsRunning)
                    {
                        int player = 0;
                        if (i == 0)
                            player = SelectSlides[_SelectSlideRecordChannel1].Selection;

                        if (i == 1)
                            player = SelectSlides[_SelectSlideRecordChannel2].Selection;

                        if (_DelayTest[i].Timer.ElapsedMilliseconds > _MaxDelayTime * 1000f || player == 0)
                        {
                            _DelayTest[i].Delay = 0f;
                            _DelayTest[i].Timer.Stop();
                            _DelayTestRunning = false;
                        }
                        else if (CSound.RecordGetMaxVolume(player - 1) > 0.1f &&
                                 (CSound.RecordGetToneAbs(player - 1) == 9 || CSound.RecordGetToneAbs(player - 1) == 21 || CSound.RecordGetToneAbs(player - 1) == 33))
                        {
                            _DelayTest[i].Delay = _DelayTest[i].Timer.ElapsedMilliseconds;
                            _DelayTest[i].Timer.Stop();
                            _DelayTestRunning = false;
                        }
                    }
                }
                Texts[_TextDelayChannel1].Text = _DelayTest[0].Delay.ToString("000") + " ms";
                Texts[_TextDelayChannel2].Text = _DelayTest[1].Delay.ToString("000") + " ms";
            }


            if (_CheckMicConfig())
            {
                _ChannelEnergy[0] = 0f;
                int player = SelectSlides[_SelectSlideRecordChannel1].Selection;
                if (player > 0)
                {
                    _ChannelEnergy[0] = CSound.RecordGetMaxVolume(player - 1);
                    Equalizers[_EqualizerChannel1].Update(CSound.ToneWeigth(player - 1));
                }
                else
                    Equalizers[_EqualizerChannel1].Reset();

                _ChannelEnergy[1] = 0f;
                player = SelectSlides[_SelectSlideRecordChannel2].Selection;
                if (player > 0)
                {
                    _ChannelEnergy[1] = CSound.RecordGetMaxVolume(player - 1);
                    Equalizers[_EqualizerChannel2].Update(CSound.ToneWeigth(player - 1));
                }
                else
                    Equalizers[_EqualizerChannel2].Reset();
            }
            else
            {
                for (int i = 0; i < _ChannelEnergy.Length; i++)
                    _ChannelEnergy[i] = 0f;
                Equalizers[_EqualizerChannel1].Reset();
                Equalizers[_EqualizerChannel2].Reset();
            }

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            SelectSlides[_SelectSlideRecordDevices].Clear();
            SelectSlides[_SelectSlideRecordInputs].Clear();

            for (int i = 0; i < _ChannelEnergy.Length; i++)
                _ChannelEnergy[i] = 0f;

            _DeviceNr = -1;
            _InputNr = -1;

            _Devices = CSound.RecordGetDevices();
            if (_Devices != null)
            {
                _DeviceNr = 0;
                _InputNr = 0;
                _GetFirstConfiguredRecordDevice(ref _DeviceNr, ref _InputNr);

                for (int dev = 0; dev < _Devices.Length; dev++)
                    SelectSlides[_SelectSlideRecordDevices].AddValue(_Devices[dev].Name);
                SelectSlides[_SelectSlideRecordDevices].Selection = _DeviceNr;

                for (int inp = 0; inp < _Devices[0].Inputs.Count; inp++)
                    SelectSlides[_SelectSlideRecordInputs].AddValue(_Devices[0].Inputs[inp].Name);
                SelectSlides[_SelectSlideRecordInputs].Selection = _InputNr;
                _UpdateChannels();
            }

            SelectSlides[_SelectSlideRecordChannel1].Visible = _Devices != null;
            SelectSlides[_SelectSlideRecordChannel2].Visible = _Devices != null;

            Statics[_StaticWarning].Visible = false;
            Texts[_TextWarning].Visible = false;

            _DelayTest = null;
            if (_Devices != null)
            {
                _DelayTest = new SDelayTest[2];
                for (int i = 0; i < _DelayTest.Length - 1; i++)
                {
                    _DelayTest[i].Timer = new Stopwatch();
                    _DelayTest[i].Delay = 0f;
                }
            }

            SelectSlides[_SelectSlideDelay].Selection = CConfig.MicDelay / 20;

            _DelayTestRunning = false;
            _DelaySound = -1;
        }

        public override void OnShowFinish()
        {
            base.OnShowFinish();
            CBackgroundMusic.Disabled = true;
        }

        public override bool Draw()
        {
            DrawBG();

            if (!_CheckMicConfig())
            {
                Statics[_StaticWarning].Visible = true;
                Texts[_TextWarning].Visible = true;
            }
            else
            {
                Statics[_StaticWarning].Visible = false;
                Texts[_TextWarning].Visible = false;
            }

            for (int i = 0; i < _StaticEnergyChannel.Length; i++)
            {
                if (_ChannelEnergy[i] > 0f)
                {
                    SRectF rect = new SRectF(Statics[_StaticEnergyChannel[i]].Rect.X,
                                             Statics[_StaticEnergyChannel[i]].Rect.Y,
                                             Statics[_StaticEnergyChannel[i]].Rect.W * _ChannelEnergy[i],
                                             Statics[_StaticEnergyChannel[i]].Rect.H,
                                             Statics[_StaticEnergyChannel[i]].Rect.Z);

                    CDraw.DrawTexture(Statics[_StaticEnergyChannel[i]].Texture, Statics[_StaticEnergyChannel[i]].Rect,
                                      new SColorF(1f, 1f, 1f, 1f), rect);
                }
            }

            Equalizers[_EqualizerChannel1].Draw();
            Equalizers[_EqualizerChannel2].Draw();

            DrawFG();

            return true;
        }

        public override void OnClose()
        {
            base.OnClose();
            CSound.RecordStop();

            _DelayTest = null;
            CBackgroundMusic.Disabled = false;
        }

        private void _OnDeviceEvent()
        {
            if (SelectSlides[_SelectSlideRecordDevices].Selection != _DeviceNr)
            {
                SelectSlides[_SelectSlideRecordInputs].Clear();
                _DeviceNr = SelectSlides[_SelectSlideRecordDevices].Selection;
                _InputNr = 0;

                for (int inp = 0; inp < _Devices[_DeviceNr].Inputs.Count; inp++)
                    SelectSlides[_SelectSlideRecordInputs].AddValue(_Devices[_DeviceNr].Inputs[inp].Name);
                _InputNr = 0;
                SelectSlides[_SelectSlideRecordInputs].Selection = 0;
                _UpdateChannels();
            }
        }

        private void _OnInputEvent()
        {
            if (SelectSlides[_SelectSlideRecordInputs].Selection != _InputNr)
            {
                _InputNr = SelectSlides[_SelectSlideRecordInputs].Selection;

                _UpdateChannels();
            }
        }

        private void _SaveDelayConfig()
        {
            CConfig.MicDelay = SelectSlides[_SelectSlideDelay].Selection * 20;
            CConfig.SaveConfig();
        }

        private void _SaveMicConfig()
        {
            if (_Devices == null)
                return;

            CSound.RecordStop();
            _SetMicConfig();

            if (_CheckMicConfig())
            {
                for (int p = 0; p < CSettings.MaxNumPlayer; p++)
                    CConfig.MicConfig[p].Channel = 0;

                for (int dev = 0; dev < _Devices.Length; dev++)
                {
                    for (int inp = 0; inp < _Devices[dev].Inputs.Count; inp++)
                    {
                        if (_Devices[dev].Inputs[inp].PlayerChannel1 > 0)
                        {
                            CConfig.MicConfig[_Devices[dev].Inputs[inp].PlayerChannel1 - 1].Channel = 1;
                            CConfig.MicConfig[_Devices[dev].Inputs[inp].PlayerChannel1 - 1].DeviceName = _Devices[dev].Name;
                            CConfig.MicConfig[_Devices[dev].Inputs[inp].PlayerChannel1 - 1].DeviceDriver = _Devices[dev].Driver;
                            CConfig.MicConfig[_Devices[dev].Inputs[inp].PlayerChannel1 - 1].InputName = _Devices[dev].Inputs[inp].Name;
                        }

                        if (_Devices[dev].Inputs[inp].PlayerChannel2 > 0)
                        {
                            CConfig.MicConfig[_Devices[dev].Inputs[inp].PlayerChannel2 - 1].Channel = 2;
                            CConfig.MicConfig[_Devices[dev].Inputs[inp].PlayerChannel2 - 1].DeviceName = _Devices[dev].Name;
                            CConfig.MicConfig[_Devices[dev].Inputs[inp].PlayerChannel2 - 1].DeviceDriver = _Devices[dev].Driver;
                            CConfig.MicConfig[_Devices[dev].Inputs[inp].PlayerChannel2 - 1].InputName = _Devices[dev].Inputs[inp].Name;
                        }
                    }
                }
                CConfig.SaveConfig();
            }
            CSound.RecordStart();
        }

        private void _SetMicConfig()
        {
            if (_DeviceNr < 0)
                return;
            SInput input = _Devices[_DeviceNr].Inputs[_InputNr];
            input.PlayerChannel1 = SelectSlides[_SelectSlideRecordChannel1].Selection;
            input.PlayerChannel2 = SelectSlides[_SelectSlideRecordChannel2].Selection;
            _Devices[_DeviceNr].Inputs[_InputNr] = input;
        }

        private void _UpdateChannels()
        {
            SelectSlides[_SelectSlideRecordChannel1].Selection = _Devices[_DeviceNr].Inputs[_InputNr].PlayerChannel1;
            SelectSlides[_SelectSlideRecordChannel2].Selection = _Devices[_DeviceNr].Inputs[_InputNr].PlayerChannel2;

            _SaveMicConfig();
        }

        private bool _CheckMicConfig()
        {
            bool[] isSet = new bool[CSettings.MaxNumPlayer];
            for (int i = 0; i < CSettings.MaxNumPlayer; i++)
                isSet[i] = false;

            if (_Devices == null)
                return true;

            for (int dev = 0; dev < _Devices.Length; dev++)
            {
                for (int inp = 0; inp < _Devices[dev].Inputs.Count; inp++)
                {
                    if (_Devices[dev].Inputs[inp].PlayerChannel1 > 0)
                    {
                        if (isSet[_Devices[dev].Inputs[inp].PlayerChannel1 - 1])
                            return false;

                        isSet[_Devices[dev].Inputs[inp].PlayerChannel1 - 1] = true;
                    }

                    if (_Devices[dev].Inputs[inp].PlayerChannel2 > 0)
                    {
                        if (isSet[_Devices[dev].Inputs[inp].PlayerChannel2 - 1])
                            return false;

                        isSet[_Devices[dev].Inputs[inp].PlayerChannel2 - 1] = true;
                    }
                }
            }
            return true;
        }

        private void _TestDelay()
        {
            _SaveMicConfig();

            if (_DelayTest == null)
                return;

            _DelaySound = CSound.PlaySound(ESounds.T440);
            _DelayTestRunning = true;
        }

        private void _GetFirstConfiguredRecordDevice(ref int device, ref int input)
        {
            if (_Devices == null)
                return;

            if (CConfig.MicConfig == null)
                return;

            for (int i = 0; i < _Devices.Length; i++)
            {
                if (_Devices[i].Name == CConfig.MicConfig[0].DeviceName && _Devices[i].Driver == CConfig.MicConfig[0].DeviceDriver)
                {
                    for (int j = 0; j < _Devices[i].Inputs.Count; j++)
                    {
                        if (_Devices[i].Inputs[j].Name == CConfig.MicConfig[0].InputName)
                        {
                            if (CConfig.MicConfig[0].Channel > 0)
                            {
                                device = i;
                                input = j;
                                return;
                            }
                        }
                    }
                }
            }
        }
    }
}