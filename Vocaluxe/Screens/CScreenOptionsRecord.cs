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
            get { return 6; }
        }

        private const string _EditSelectSlideRecordDevices = "EditSelectSlideRecordDevices";
        private const string _EditSelectSlideRecordChannels = "EditSelectSlideRecordChannels";

        private const string _SelectSlideDelay = "SelectSlideDelay";

        private const string _StaticWarning = "StaticWarning";
        private const string _TextWarning = "TextWarning";
        private const string _EditTextDevice = "EditTextDevice";
        private const string _EditTextChannel = "EditTextChannel";

        private const string _ButtonExit = "ButtonExit";
        private const string _ButtonDelayTest = "ButtonDelayTest";
        private const string _EditButtonApply = "EditButtonApply";
        private const string _EditButtonCancel = "EditButtonCancel";

        private string[] _TextPlayer;

        private string[] _TextDelayPlayer;

        private string[] _TextChannelPlayer;

        private string[] _TextDevicePlayer;

        private string[] _ButtonsPlayer;

        private string[] _EqualizerPlayer;

        private string[] _StaticEnergyPlayer;
        private float[] _ChannelEnergy;

        private ReadOnlyCollection<CRecordDevice> _Devices;
        private int _DeviceNr;

        private int _SelectedPlayer = -1;
        private bool _PlayerEditActive = false;

        private readonly CDelayTest _DelayTest = new CDelayTest(CSettings.MaxNumPlayer);

        public override EMusicType CurrentMusicType
        {
            get { return EMusicType.Game; }
        }

        public override void Init()
        {
            base.Init();

            _BuildStrings();

            var statics = new List<string> { _StaticWarning };
            statics.AddRange(_StaticEnergyPlayer);
            _ThemeStatics = statics.ToArray();

            var texts = new List<string> { _TextWarning, _EditTextDevice, _EditTextChannel };
            texts.AddRange(_TextPlayer);
            texts.AddRange(_TextDelayPlayer);
            texts.AddRange(_TextChannelPlayer);
            texts.AddRange(_TextDevicePlayer);
            _ThemeTexts = texts.ToArray();

            var buttons = new List<string> { _ButtonExit, _ButtonDelayTest, _EditButtonCancel, _EditButtonApply };
            buttons.AddRange(_ButtonsPlayer);
            _ThemeButtons = buttons.ToArray();

            _ThemeSelectSlides = new string[] { _EditSelectSlideRecordDevices, _EditSelectSlideRecordChannels, _SelectSlideDelay };

            var equalizers = new List<string>();
            equalizers.AddRange(_EqualizerPlayer);
            _ThemeEqualizers = equalizers.ToArray();
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            for (int i = 0; i < 26; i++)
                _SelectSlides[_SelectSlideDelay].AddValue((i * 20) + " ms");

            _ChannelEnergy = new float[_StaticEnergyPlayer.Length];

            for (int p = 0; p < CSettings.MaxNumPlayer; p++)
            {
                _Statics[_StaticEnergyPlayer[p]].Visible = false;
                _Texts[_TextPlayer[p]].Text = CLanguage.Translate("TR_SCREENORECORD_PLAYER_N").Replace("%n", (p + 1).ToString());
                _Texts[_TextDelayPlayer[p]].Text = "––– ms";
                _ChannelEnergy[p] = 0f;
            }


        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            base.HandleInput(keyEvent);

            if (keyEvent.KeyPressed) { }
            else
            {
                switch (keyEvent.Key)
                {
                    case Keys.Escape:
                    case Keys.Back:
                        if (_PlayerEditActive)
                        {
                            _PlayerEditActive = false;
                            break;
                        }
                        _SaveMicConfig();
                        CGraphics.FadeTo(EScreen.Options);
                        break;

                    case Keys.S:
                        CParty.SetNormalGameMode();
                        CGraphics.FadeTo(EScreen.Song);
                        break;

                    case Keys.Enter:
                        if (_HandlePlayerButtonPress())
                            break;

                        if (_Buttons[_EditButtonApply].Selected)
                        {
                            _SetMicConfig();
                            _PlayerEditActive = false;
                        }

                        if (_Buttons[_EditButtonCancel].Selected)
                        {
                            _PlayerEditActive = false;
                        }

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
                if (_PlayerEditActive)
                {
                    _PlayerEditActive = false;
                }
                else
                {
                    _SaveMicConfig();
                    CGraphics.FadeTo(EScreen.Options);
                }
            }

            if (mouseEvent.LB && _IsMouseOverCurSelection(mouseEvent))
            {
                _SelectSlideAction();

                _HandlePlayerButtonPress();

                if (_Buttons[_EditButtonApply].Selected)
                {
                    _SetMicConfig();
                    _PlayerEditActive = false;
                }

                if (_Buttons[_EditButtonCancel].Selected)
                {
                    _PlayerEditActive = false;
                }

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

            _UpdateTextColors();

            _SetEditVisibility(_PlayerEditActive);

            if (_DelayTest.Running)
            {
                _DelayTest.Update();
                for (int p = 0; p < CSettings.MaxNumPlayer; ++p)
                    _Texts[_TextDelayPlayer[p]].Text = (_DelayTest.Delays[p] == 0) ? "––– ms" : _DelayTest.Delays[p].ToString("000") + " ms";
            }

            for (int p = 0; p < CSettings.MaxNumPlayer; ++p)
            {
                _ChannelEnergy[p] = CRecord.GetMaxVolume(p);
                _Equalizers[_EqualizerPlayer[p]].Update(CRecord.ToneWeigth(p), CRecord.GetMaxVolume(p));
            }

            bool showWarning = !_CheckMicConfig();
            _Statics[_StaticWarning].Visible = showWarning;
            _Texts[_TextWarning].Visible = showWarning;

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            _SelectSlides[_EditSelectSlideRecordDevices].Clear();

            for (int i = 0; i < _ChannelEnergy.Length; i++)
                _ChannelEnergy[i] = 0f;

            _UpdateDevices();

            for (int p = 0; p < CSettings.MaxNumPlayer; ++p)
                _SelectSlides[_EditSelectSlideRecordChannels].Visible = _Devices != null;

            _UpdatePlayerTexts();

            _SetEditVisibility(_PlayerEditActive);
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
                                          _Statics[_StaticEnergyPlayer[i]].Rect.Y + (_Statics[_StaticEnergyPlayer[i]].Rect.H - (_Statics[_StaticEnergyPlayer[i]].Rect.H * _ChannelEnergy[i])),
                                          _Statics[_StaticEnergyPlayer[i]].Rect.W,
                                          _Statics[_StaticEnergyPlayer[i]].Rect.H * _ChannelEnergy[i],
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

        private void _BuildStrings()
        {
            _TextPlayer = new string[CSettings.MaxNumPlayer];
            _TextDelayPlayer = new string[CSettings.MaxNumPlayer];
            _TextChannelPlayer = new string[CSettings.MaxNumPlayer];
            _TextDevicePlayer = new string[CSettings.MaxNumPlayer];
            _ButtonsPlayer = new string[CSettings.MaxNumPlayer];
            _EqualizerPlayer = new string[CSettings.MaxNumPlayer];
            _StaticEnergyPlayer = new string[CSettings.MaxNumPlayer];
            for (int p = 0; p < CSettings.MaxNumPlayer; p++)
            {
                _TextPlayer[p] = "TextPlayer" + (p + 1);
                _TextDelayPlayer[p] = "TextDelayPlayer" + (p + 1);
                _TextChannelPlayer[p] = "TextChannelPlayer" + (p + 1);
                _TextDevicePlayer[p] = "TextDevicePlayer" + (p + 1);
                _ButtonsPlayer[p] = "ButtonPlayer" + (p + 1);
                _EqualizerPlayer[p] = "EqualizerPlayer" + (p + 1);
                _StaticEnergyPlayer[p] = "StaticEnergyPlayer" + (p + 1);
            }
        }

        private void _UpdateTextColors()
        {
            for (int p = 0; p < CSettings.MaxNumPlayer; ++p)
            {
                _Texts[_TextChannelPlayer[p]].Selected = _Buttons[_ButtonsPlayer[p]].Selected;
                _Texts[_TextDevicePlayer[p]].Selected = _Buttons[_ButtonsPlayer[p]].Selected;
                _Texts[_TextDelayPlayer[p]].Selected = _Buttons[_ButtonsPlayer[p]].Selected;
                _Texts[_TextPlayer[p]].Selected = _Buttons[_ButtonsPlayer[p]].Selected;

            }
        }

        private void _UpdatePlayerTexts()
        {
            for (int p = 0; p < CSettings.MaxNumPlayer; ++p)
            {
                _Texts[_TextChannelPlayer[p]].Text = "–";
                _Texts[_TextChannelPlayer[p]].Selected = _Buttons[_ButtonsPlayer[p]].Selected;
                _Texts[_TextDevicePlayer[p]].Text = CLanguage.Translate("TR_SCREENORECORD_PRESS_TO_CONFIGURE");
                _Texts[_TextDevicePlayer[p]].Selected = _Buttons[_ButtonsPlayer[p]].Selected;

            }

            foreach (CRecordDevice device in _Devices)
            {
                for (int c = 0; c < device.Channels; c++)
                {
                    if (device.PlayerChannel[c] > 0)
                    {
                        _Texts[_TextChannelPlayer[device.PlayerChannel[c] - 1]].Text = CConfig.Config.Record.MicConfig[device.PlayerChannel[c] - 1].Channel.ToString();
                        _Texts[_TextDevicePlayer[device.PlayerChannel[c] - 1]].Text = CConfig.Config.Record.MicConfig[device.PlayerChannel[c] - 1].DeviceName;
                    }
                }
            }
        }

        private void _SetEditVisibility(bool b)
        {
            foreach (CStatic se in _Statics)
            {
                if (se.GetThemeName() != null && se.GetThemeName().StartsWith("EditStatic"))
                {
                    se.Visible = b;
                }
            }

            foreach (CText te in _Texts)
            {
                if (te.GetThemeName() != null && te.GetThemeName().StartsWith("EditText"))
                {
                    te.Visible = b;
                }
            }

            foreach (CSelectSlide sse in _SelectSlides)
            {
                if (sse.GetThemeName() != null && sse.GetThemeName().StartsWith("EditSelectSlide"))
                {
                    sse.Visible = b;
                }
                else
                {
                    sse.Selectable = !b;
                }
            }

            foreach (CButton be in _Buttons)
            {
                if (be.GetThemeName() != null && be.GetThemeName().StartsWith("EditButton"))
                {
                    be.Visible = b;
                }
                else
                {
                    be.Selectable = !b;
                }
            }
        }

        private bool _HandlePlayerButtonPress()
        {
            for (int p = 0; p < CSettings.MaxNumPlayer; p++)
            {
                if (_Buttons[_ButtonsPlayer[p]].Selected)
                {
                    _SelectedPlayer = p;
                    _GetPlayerConfig(_SelectedPlayer);
                    _PlayerEditActive = true;
                    return true;
                }
            }
            return false;
        }

        private void _GetPlayerConfig(int player)
        {
            player++;
            if (player > 0)
            {
                for (int d = 0; d < _Devices.Count; d++)
                {
                    for (int c = 0; c < _Devices[d].Channels; c++)
                    {
                        if (_Devices[d].PlayerChannel[c] == player)
                        {
                            _SelectSlides[_EditSelectSlideRecordDevices].Selection = d;
                            _DeviceNr = d;

                            _UpdateChannels();
                            _SelectSlides[_EditSelectSlideRecordChannels].Selection = c + 1;
                            return;
                        }
                    }
                }
            }
            _SelectSlides[_EditSelectSlideRecordDevices].Selection = 0;
            _DeviceNr = 0;

            _UpdateChannels();
            _SelectSlides[_EditSelectSlideRecordChannels].Selection = 0;
        }

        private void _OnDeviceEvent()
        {
            if (_SelectSlides[_EditSelectSlideRecordDevices].Selection != _DeviceNr)
            {
                _DeviceNr = _SelectSlides[_EditSelectSlideRecordDevices].Selection;
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
                            CConfig.Config.Record.MicConfig[device.PlayerChannel[ch] - 1].Channel = ch + 1;
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

            for (int d = 0; d < _Devices.Count; d++)
            {
                for (int c = 0; c < _Devices[d].Channels; c++)
                {
                    if (_Devices[d].PlayerChannel[c] == _SelectedPlayer + 1)
                    {
                        _Devices[d].PlayerChannel[c] = 0;
                    }
                }
            }

            CRecordDevice device = _Devices[_DeviceNr];

            _Texts[_TextDelayPlayer[_SelectedPlayer]].Text = "––– ms";
            if (_SelectSlides[_EditSelectSlideRecordChannels].Selection > 0)
            {
                if (device.PlayerChannel[_SelectSlides[_EditSelectSlideRecordChannels].Selection - 1] > 0)
                {

                    string delay = _Texts[_TextDelayPlayer[device.PlayerChannel[_SelectSlides[_EditSelectSlideRecordChannels].Selection - 1] - 1]].Text;
                    _Texts[_TextDelayPlayer[device.PlayerChannel[_SelectSlides[_EditSelectSlideRecordChannels].Selection - 1] - 1]].Text = _Texts[_TextDelayPlayer[_SelectedPlayer]].Text;
                    _Texts[_TextDelayPlayer[_SelectedPlayer]].Text = delay;
                }

                device.PlayerChannel[_SelectSlides[_EditSelectSlideRecordChannels].Selection - 1] = _SelectedPlayer + 1;
            }

            _SaveMicConfig();

            _UpdatePlayerTexts();

        }

        private void _SelectSlideAction()
        {
            if (_SelectSlides[_EditSelectSlideRecordDevices].Selected)
                _OnDeviceEvent();

            if (_SelectSlides[_SelectSlideDelay].Selected)
                _SaveDelayConfig();
        }

        private void _UpdateDevices()
        {
            _DeviceNr = -1;

            _Devices = CRecord.GetDevices();
            if (_Devices != null)
            {
                _DeviceNr = 0;

                foreach (CRecordDevice device in _Devices)
                    _SelectSlides[_EditSelectSlideRecordDevices].AddValue(device.Name);
                _SelectSlides[_EditSelectSlideRecordDevices].Selection = _DeviceNr;

                _UpdateChannels();
            }
        }

        private void _UpdateChannels()
        {
            _SelectSlides[_EditSelectSlideRecordChannels].Clear();
            _SelectSlides[_EditSelectSlideRecordChannels].AddValue(CLanguage.Translate("TR_CONFIG_OFF"));

            for (int i = 1; i <= _Devices[_DeviceNr].Channels; ++i)
                _SelectSlides[_EditSelectSlideRecordChannels].AddValue(i.ToString());

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
            int[] p = new int[CSettings.MaxNumPlayer];
            for (int i = 0; i < CSettings.MaxNumPlayer; i++)
            {
                p[i] = i;
            }
            _DelayTest.Start(p);
        }
    }
}