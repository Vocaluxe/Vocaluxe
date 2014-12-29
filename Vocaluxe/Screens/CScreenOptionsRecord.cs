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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Forms;
using Vocaluxe.Base;
using Vocaluxe.Base.ThemeSystem;
using Vocaluxe.Lib.Sound.Record;
using VocaluxeLib;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    public class CScreenOptionsRecord : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 4; }
        }

        private const string _SelectSlideRecordDevices = "SelectSlideRecordDevices";

        private const string _SelectSlideRecordChannel1 = "SelectSlideRecordChannel1";
        private const string _SelectSlideRecordChannel2 = "SelectSlideRecordChannel2";
        private const string _SelectSlideDelay = "SelectSlideDelay";

        private const string _StaticWarning = "StaticWarning";
        private const string _TextWarning = "TextWarning";

        private const string _ButtonExit = "ButtonExit";
        private const string _ButtonDelayTest = "ButtonDelayTest";

        private const string _TextChannel1 = "TextChannel1";
        private const string _TextChannel2 = "TextChannel2";

        private const string _TextDelayChannel1 = "TextDelayChannel1";
        private const string _TextDelayChannel2 = "TextDelayChannel2";

        private const string _EqualizerChannel1 = "EqualizerChannel1";
        private const string _EqualizerChannel2 = "EqualizerChannel2";

        private readonly string[] _StaticEnergyChannel = new string[] {"StaticEnergyChannel1", "StaticEnergyChannel2"};
        private float[] _ChannelEnergy;

        private ReadOnlyCollection<CRecordDevice> _Devices;
        private int _DeviceNr;

        private readonly CDelayTest _DelayTest = new CDelayTest(2);

        public override EMusicType CurrentMusicType
        {
            get { return EMusicType.Game; }
        }

        public override void Init()
        {
            base.Init();

            var values = new List<string> {_StaticWarning};
            values.AddRange(_StaticEnergyChannel);
            _ThemeStatics = values.ToArray();

            _ThemeTexts = new string[] {_TextWarning, _TextChannel1, _TextChannel2, _TextDelayChannel1, _TextDelayChannel2};
            _ThemeButtons = new string[] {_ButtonExit, _ButtonDelayTest};
            _ThemeSelectSlides = new string[] {_SelectSlideRecordDevices, _SelectSlideRecordChannel1, _SelectSlideRecordChannel2, _SelectSlideDelay};
            _ThemeEqualizers = new string[] {_EqualizerChannel1, _EqualizerChannel2};
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            int max = CSettings.MaxNumPlayer + 1;
            if (max > 7)
                max = 7;

            _SelectSlides[_SelectSlideRecordChannel1].NumVisible = max;
            _SelectSlides[_SelectSlideRecordChannel2].NumVisible = max;

            _SelectSlides[_SelectSlideRecordChannel1].AddValue(CLanguage.Translate("TR_CONFIG_OFF"));
            _SelectSlides[_SelectSlideRecordChannel2].AddValue(CLanguage.Translate("TR_CONFIG_OFF"));

            for (int i = 1; i <= CSettings.MaxNumPlayer; i++)
            {
                _SelectSlides[_SelectSlideRecordChannel1].AddValue(i.ToString());
                _SelectSlides[_SelectSlideRecordChannel2].AddValue(i.ToString());
            }

            for (int i = 0; i < 26; i++)
                _SelectSlides[_SelectSlideDelay].AddValue((i * 20) + " ms");

            _ChannelEnergy = new float[_StaticEnergyChannel.Length];

            for (int i = 0; i < _ChannelEnergy.Length; i++)
            {
                _Statics[_StaticEnergyChannel[i]].Visible = false;
                _ChannelEnergy[i] = 0f;
            }
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
                        CGraphics.FadeTo(EScreen.Options);
                        break;

                    case Keys.S:
                        CParty.SetNormalGameMode();
                        CGraphics.FadeTo(EScreen.Song);
                        break;

                    case Keys.Enter:
                        if (_Buttons[_ButtonExit].Selected)
                        {
                            _SaveMicConfig();
                            CGraphics.FadeTo(EScreen.Options);
                        }

                        if (_Buttons[_ButtonDelayTest].Selected)
                            _TestDelay();

                        break;

                    case Keys.D:
                        _TestDelay();
                        break;

                    case Keys.Left:
                        if (_SelectSlides[_SelectSlideRecordDevices].Selected)
                            _OnDeviceEvent();

                        if (_SelectSlides[_SelectSlideRecordChannel1].Selected ||
                            _SelectSlides[_SelectSlideRecordChannel2].Selected)
                            _SetMicConfig();

                        if (_SelectSlides[_SelectSlideDelay].Selected)
                            _SaveDelayConfig();
                        break;

                    case Keys.Right:
                        if (_SelectSlides[_SelectSlideRecordDevices].Selected)
                            _OnDeviceEvent();

                        if (_SelectSlides[_SelectSlideRecordChannel1].Selected ||
                            _SelectSlides[_SelectSlideRecordChannel2].Selected)
                            _SetMicConfig();

                        if (_SelectSlides[_SelectSlideDelay].Selected)
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
                CGraphics.FadeTo(EScreen.Options);
            }

            if (mouseEvent.LB && _IsMouseOverCurSelection(mouseEvent))
            {
                if (_SelectSlides[_SelectSlideRecordDevices].Selected)
                    _OnDeviceEvent();

                if (_SelectSlides[_SelectSlideRecordChannel1].Selected ||
                    _SelectSlides[_SelectSlideRecordChannel2].Selected)
                    _SetMicConfig();

                if (_SelectSlides[_SelectSlideDelay].Selected)
                    _SaveDelayConfig();

                if (_Buttons[_ButtonExit].Selected)
                {
                    _SaveMicConfig();
                    CGraphics.FadeTo(EScreen.Options);
                }

                if (_Buttons[_ButtonDelayTest].Selected)
                    _TestDelay();
            }
            return true;
        }

        public override bool UpdateGame()
        {
            for (int i = 0; i < CSettings.MaxNumPlayer; i++)
                CRecord.AnalyzeBuffer(i);

            if (_DelayTest.Running)
            {
                _DelayTest.Update();
                _Texts[_TextDelayChannel1].Text = (_DelayTest.Delays[0] == 0) ? "???" : _DelayTest.Delays[0].ToString("000") + " ms";
                _Texts[_TextDelayChannel2].Text = (_DelayTest.Delays[1] == 0) ? "???" : _DelayTest.Delays[1].ToString("000") + " ms";
            }

            if (_CheckMicConfig())
            {
                int player = _SelectSlides[_SelectSlideRecordChannel1].Selection - 1;
                if (player >= 0)
                {
                    _ChannelEnergy[0] = CRecord.GetMaxVolume(player);
                    _Equalizers[_EqualizerChannel1].Update(CRecord.ToneWeigth(player), CRecord.GetMaxVolume(player));
                }
                else
                {
                    _ChannelEnergy[0] = 0f;
                    _Equalizers[_EqualizerChannel1].Reset();
                }

                player = _SelectSlides[_SelectSlideRecordChannel2].Selection - 1;
                if (player >= 0)
                {
                    _ChannelEnergy[1] = CRecord.GetMaxVolume(player);
                    _Equalizers[_EqualizerChannel2].Update(CRecord.ToneWeigth(player), CRecord.GetMaxVolume(player));
                }
                else
                {
                    _ChannelEnergy[1] = 0f;
                    _Equalizers[_EqualizerChannel2].Reset();
                }
            }
            else
            {
                for (int i = 0; i < _ChannelEnergy.Length; i++)
                    _ChannelEnergy[i] = 0f;
                _Equalizers[_EqualizerChannel1].Reset();
                _Equalizers[_EqualizerChannel2].Reset();
            }

            bool showWarning = !_CheckMicConfig();
            _Statics[_StaticWarning].Visible = showWarning;
            _Texts[_TextWarning].Visible = showWarning;

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            _SelectSlides[_SelectSlideRecordDevices].Clear();

            for (int i = 0; i < _ChannelEnergy.Length; i++)
                _ChannelEnergy[i] = 0f;

            _DeviceNr = -1;

            _Devices = CRecord.GetDevices();
            if (_Devices != null)
            {
                _DeviceNr = 0;
                _GetFirstConfiguredRecordDevice(ref _DeviceNr);

                foreach (CRecordDevice device in _Devices)
                    _SelectSlides[_SelectSlideRecordDevices].AddValue(device.Name);
                _SelectSlides[_SelectSlideRecordDevices].Selection = _DeviceNr;

                _UpdateChannels();
            }

            _SelectSlides[_SelectSlideRecordChannel1].Visible = _Devices != null;
            _SelectSlides[_SelectSlideRecordChannel2].Visible = _Devices != null;

            _Statics[_StaticWarning].Visible = false;
            _Texts[_TextWarning].Visible = false;

            _DelayTest.Reset();

            _SelectSlides[_SelectSlideDelay].Selection = CConfig.Config.Record.MicDelay / 20;
        }

        public override void OnShowFinish()
        {
            base.OnShowFinish();
        }

        public override void Draw()
        {
            base.Draw();

            for (int i = 0; i < _StaticEnergyChannel.Length; i++)
            {
                if (_ChannelEnergy[i] > 0f)
                {
                    var rect = new SRectF(_Statics[_StaticEnergyChannel[i]].Rect.X,
                                          _Statics[_StaticEnergyChannel[i]].Rect.Y,
                                          _Statics[_StaticEnergyChannel[i]].Rect.W * _ChannelEnergy[i],
                                          _Statics[_StaticEnergyChannel[i]].Rect.H,
                                          _Statics[_StaticEnergyChannel[i]].Rect.Z);

                    CDraw.DrawTexture(_Statics[_StaticEnergyChannel[i]].Texture, _Statics[_StaticEnergyChannel[i]].Rect,
                                      new SColorF(1f, 1f, 1f, 1f), rect);
                }
            }
        }

        public override void OnClose()
        {
            base.OnClose();
            CRecord.Stop();

            _DelayTest.Reset();
        }

        private void _OnDeviceEvent()
        {
            if (_SelectSlides[_SelectSlideRecordDevices].Selection != _DeviceNr)
            {
                _DeviceNr = _SelectSlides[_SelectSlideRecordDevices].Selection;
                _UpdateChannels();
            }
        }

        private void _SaveDelayConfig()
        {
            CConfig.Config.Record.MicDelay = _SelectSlides[_SelectSlideDelay].Selection * 20;
            CConfig.SaveConfig();
        }

        private void _SaveMicConfig()
        {
            if (_Devices == null)
                return;

            CRecord.Stop();
            _SetMicConfig();

            if (_CheckMicConfig())
            {
                for (int p = 0; p < CConfig.Config.Record.MicConfig.Length; p++)
                    CConfig.Config.Record.MicConfig[p].Channel = 0;

                foreach (CRecordDevice device in _Devices)
                {
                    if (device.PlayerChannel1 > 0)
                    {
                        CConfig.Config.Record.MicConfig[device.PlayerChannel1 - 1].Channel = 1;
                        CConfig.Config.Record.MicConfig[device.PlayerChannel1 - 1].DeviceName = device.Name;
                        CConfig.Config.Record.MicConfig[device.PlayerChannel1 - 1].DeviceDriver = device.Driver;
                    }

                    if (device.PlayerChannel2 > 0)
                    {
                        CConfig.Config.Record.MicConfig[device.PlayerChannel2 - 1].Channel = 2;
                        CConfig.Config.Record.MicConfig[device.PlayerChannel2 - 1].DeviceName = device.Name;
                        CConfig.Config.Record.MicConfig[device.PlayerChannel2 - 1].DeviceDriver = device.Driver;
                    }
                }
                CConfig.SaveConfig();
            }
            CRecord.Start();
        }

        private void _SetPlayerColors()
        {
            _Texts[_TextChannel1].Color = _SelectSlides[_SelectSlideRecordChannel1].Selection <= 0
                                              ? new SColorF(1, 1, 1, 1) : CThemes.GetPlayerColor(_SelectSlides[_SelectSlideRecordChannel1].Selection);
            _Texts[_TextChannel2].Color = _SelectSlides[_SelectSlideRecordChannel2].Selection <= 0
                                              ? new SColorF(1, 1, 1, 1) : CThemes.GetPlayerColor(_SelectSlides[_SelectSlideRecordChannel2].Selection);
        }

        private void _SetMicConfig()
        {
            if (_DeviceNr < 0)
                return;
            CRecordDevice device = _Devices[_DeviceNr];
            device.PlayerChannel1 = _SelectSlides[_SelectSlideRecordChannel1].Selection;
            device.PlayerChannel2 = _SelectSlides[_SelectSlideRecordChannel2].Selection;
            _SetPlayerColors();
        }

        private void _UpdateChannels()
        {
            _SelectSlides[_SelectSlideRecordChannel1].Selection = _Devices[_DeviceNr].PlayerChannel1;
            _SelectSlides[_SelectSlideRecordChannel2].Selection = _Devices[_DeviceNr].PlayerChannel2;

            _SaveMicConfig();
            _SetPlayerColors();
        }

        private bool _CheckMicConfig()
        {
            var isSet = new bool[CSettings.MaxNumPlayer];
            for (int i = 0; i < CSettings.MaxNumPlayer; i++)
                isSet[i] = false;

            if (_Devices == null)
                return true;

            foreach (CRecordDevice device in _Devices)
            {
                if (device.PlayerChannel1 > 0)
                {
                    if (isSet[device.PlayerChannel1 - 1])
                        return false;

                    isSet[device.PlayerChannel1 - 1] = true;
                }

                if (device.PlayerChannel2 > 0)
                {
                    if (isSet[device.PlayerChannel2 - 1])
                        return false;

                    isSet[device.PlayerChannel2 - 1] = true;
                }
            }
            return true;
        }

        private void _TestDelay()
        {
            _SaveMicConfig();
            _DelayTest.Reset();
            _DelayTest.Start(new int[] {_SelectSlides[_SelectSlideRecordChannel1].Selection - 1, _SelectSlides[_SelectSlideRecordChannel2].Selection - 1});
        }

        private void _GetFirstConfiguredRecordDevice(ref int device)
        {
            if (_Devices == null)
                return;

            if (CConfig.Config.Record.MicConfig == null)
                return;

            if (CConfig.Config.Record.MicConfig[0].Channel <= 0)
                return;

            for (int i = 0; i < _Devices.Count; i++)
            {
                if (_Devices[i].Name == CConfig.Config.Record.MicConfig[0].DeviceName && _Devices[i].Driver == CConfig.Config.Record.MicConfig[0].DeviceDriver)
                {
                    device = i;
                    return;
                }
            }
        }
    }
}