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
            get { return 5; }
        }

        private const string _SelectSlideRecordDevices = "SelectSlideRecordDevices";

        private readonly string[] _SelectSlideRecordPlayer = { "SelectSlideRecordPlayer1", "SelectSlideRecordPlayer2", "SelectSlideRecordPlayer3", "SelectSlideRecordPlayer4", "SelectSlideRecordPlayer5", "SelectSlideRecordPlayer6" };

        private const string _SelectSlideDelay = "SelectSlideDelay";

        private const string _StaticWarning = "StaticWarning";
        private const string _TextWarning = "TextWarning";

        private const string _ButtonExit = "ButtonExit";
        private const string _ButtonDelayTest = "ButtonDelayTest";

        private readonly string[] _TextPlayer = { "TextPlayer1", "TextPlayer2", "TextPlayer3", "TextPlayer4", "TextPlayer5", "TextPlayer6" };

        private readonly string[] _TextDelayPlayer = { "TextDelayPlayer1", "TextDelayPlayer2", "TextDelayPlayer3", "TextDelayPlayer4", "TextDelayPlayer5", "TextDelayPlayer6" };

        private readonly string[] _EqualizerPlayer = { "EqualizerPlayer1", "EqualizerPlayer2", "EqualizerPlayer3", "EqualizerPlayer4", "EqualizerPlayer5", "EqualizerPlayer6" };

        private readonly string[] _StaticEnergyPlayer = new string[] { "StaticEnergyPlayer1", "StaticEnergyPlayer2", "StaticEnergyPlayer3", "StaticEnergyPlayer4", "StaticEnergyPlayer5", "StaticEnergyPlayer6" };
        private float[] _ChannelEnergy;

        private ReadOnlyCollection<CRecordDevice> _Devices;
        private int _DeviceNr;

        private readonly CDelayTest _DelayTest = new CDelayTest(6);

        public override EMusicType CurrentMusicType
        {
            get { return EMusicType.Game; }
        }

        public override void Init()
        {
            base.Init();

            var values = new List<string> {_StaticWarning};
            values.AddRange(_StaticEnergyPlayer);
            _ThemeStatics = values.ToArray();

            _ThemeTexts = new string[] {_TextWarning, _TextPlayer[0], _TextPlayer[1], _TextPlayer[2], _TextPlayer[3], _TextPlayer[4], _TextPlayer[5], _TextDelayPlayer[0], _TextDelayPlayer[1], _TextDelayPlayer[2], _TextDelayPlayer[3], _TextDelayPlayer[4], _TextDelayPlayer[5] };
            _ThemeButtons = new string[] {_ButtonExit, _ButtonDelayTest};
            _ThemeSelectSlides = new string[] {_SelectSlideRecordDevices, _SelectSlideRecordPlayer[0], _SelectSlideRecordPlayer[1], _SelectSlideRecordPlayer[2], _SelectSlideRecordPlayer[3], _SelectSlideRecordPlayer[4], _SelectSlideRecordPlayer[5], _SelectSlideDelay };
            _ThemeEqualizers = new string[] {_EqualizerPlayer[0], _EqualizerPlayer[1], _EqualizerPlayer[2], _EqualizerPlayer[3], _EqualizerPlayer[4], _EqualizerPlayer[5]};
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            for (int i = 0; i < 26; i++)
                _SelectSlides[_SelectSlideDelay].AddValue((i * 20) + " ms");

            _ChannelEnergy = new float[_StaticEnergyPlayer.Length];

            for (int i = 0; i < _ChannelEnergy.Length; i++)
            {
                _Statics[_StaticEnergyPlayer[i]].Visible = false;
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
                        _SaveMicConfig();
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
                        _SelectSlideAction();
                        break;

                    case Keys.Right:
                        _SelectSlideAction();
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
                _SelectSlideAction();

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
                for (int p = 0; p < CSettings.MaxNumPlayer; ++p)
                    _Texts[_TextDelayPlayer[p]].Text = (_DelayTest.Delays[p] == 0) ? "??? ms" : _DelayTest.Delays[p].ToString("000") + " ms";
            }

            if (_CheckMicConfig())
            {
                for (int p = 0; p < CSettings.MaxNumPlayer; ++p)
                {
                    if(_SelectSlides[_SelectSlideRecordPlayer[p]].Selection > 0)
                    {
                        _ChannelEnergy[p] = CRecord.GetMaxVolume(p);
                        _Equalizers[_EqualizerPlayer[p]].Update(CRecord.ToneWeigth(p), CRecord.GetMaxVolume(p));
                    }
                    else
                    {
                        _ChannelEnergy[p] = 0f;
                        _Equalizers[_EqualizerPlayer[p]].Reset();
                    }
                }
            }
            else
            {
                for (int i = 0; i < _ChannelEnergy.Length; i++)
                    _ChannelEnergy[i] = 0f;
                for (int p = 0; p < CSettings.MaxNumPlayer; ++p)
                    _Equalizers[_EqualizerPlayer[p]].Reset();
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
            for (int p = 0; p < CSettings.MaxNumPlayer; ++p)
                _SelectSlides[_SelectSlideRecordPlayer[p]].Visible = _Devices != null;

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

            for (int i = 0; i < _StaticEnergyPlayer.Length; i++)
            {
                if (_ChannelEnergy[i] > 0f)
                {
                    var rect = new SRectF(_Statics[_StaticEnergyPlayer[i]].Rect.X,
                                          _Statics[_StaticEnergyPlayer[i]].Rect.Y,
                                          _Statics[_StaticEnergyPlayer[i]].Rect.W * _ChannelEnergy[i],
                                          _Statics[_StaticEnergyPlayer[i]].Rect.H,
                                          _Statics[_StaticEnergyPlayer[i]].Rect.Z);

                    CDraw.DrawTexture(_Statics[_StaticEnergyPlayer[i]].Texture, _Statics[_StaticEnergyPlayer[i]].Rect,
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
                    for (int ch = 0; ch < (uint)device.Channels; ++ch)
                    {
                        if (device.PlayerChannel[ch] > 0)
                        {
                            CConfig.Config.Record.MicConfig[device.PlayerChannel[ch] - 1].Channel = ch+1;
                            CConfig.Config.Record.MicConfig[device.PlayerChannel[ch] - 1].DeviceName = device.Name;
                            CConfig.Config.Record.MicConfig[device.PlayerChannel[ch] - 1].DeviceDriver = device.Driver;
                        }
                    }
                }
                CConfig.SaveConfig();
            }
            CRecord.Start();
        }

        private void _SetMicConfig()
        {
            if (_DeviceNr < 0)
                return;
            CRecordDevice device = _Devices[_DeviceNr];

            for (int ch = 0; ch < device.Channels; ++ch)
                device.PlayerChannel[ch] = 0;

            for (int p = 0; p < CSettings.MaxNumPlayer; ++p)
            {
                int ch = _SelectSlides[_SelectSlideRecordPlayer[p]].Selection;

                if (ch > 0)
                    device.PlayerChannel[ch-1] = p+1;
                
            }
        }

        private void _SelectSlideAction()
        {
            if (_SelectSlides[_SelectSlideRecordDevices].Selected)
                _OnDeviceEvent();

            for (int i = 0; i < _SelectSlideRecordPlayer.Length; ++i)
                if (_SelectSlides[_SelectSlideRecordPlayer[i]].Selected)
                    _SetMicConfig();

            if (_SelectSlides[_SelectSlideDelay].Selected)
                _SaveDelayConfig();
        }

        private void _UpdateChannels()
        {
            int max = _Devices[_DeviceNr].Channels + 1;

            for (int p = 0; p < CSettings.MaxNumPlayer; ++p)
            {
                _SelectSlides[_SelectSlideRecordPlayer[p]].Clear();
                _SelectSlides[_SelectSlideRecordPlayer[p]].NumVisible = max;
                _SelectSlides[_SelectSlideRecordPlayer[p]].AddValue(CLanguage.Translate("TR_CONFIG_OFF"));
            }

            for (int i = 1; i <= _Devices[_DeviceNr].Channels; ++i)
            {
                for (int p = 0; p < CSettings.MaxNumPlayer; ++p)
                    _SelectSlides[_SelectSlideRecordPlayer[p]].AddValue(i.ToString());
            
                int pc = _Devices[_DeviceNr].PlayerChannel[i-1];
                if (pc > 0)
                {
                    _SelectSlides[_SelectSlideRecordPlayer[pc-1]].Selection = i;
                }
            }

            _SaveMicConfig();
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
                for (int i = 0; i < device.Channels; ++i)
                {
                    if (device.PlayerChannel[i] > 0)
                    {
                        if (isSet[device.PlayerChannel[i] - 1])
                            return false;

                        isSet[device.PlayerChannel[i] - 1] = true;
                    }
                }
            }
            return true;
        }

        private void _TestDelay()
        {
            _SaveMicConfig();
            _DelayTest.Reset();
            _DelayTest.Start(new int[] {0, 1, 2, 3, 4, 5});
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