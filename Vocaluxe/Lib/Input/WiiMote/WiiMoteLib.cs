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
using System.Drawing;
using System.Threading;
using Vocaluxe.Base;
using VocaluxeLib;

namespace Vocaluxe.Lib.Input.WiiMote
{

    #region DataTypes
    public class CWiiMoteStatus
    {
        public SAccelCalibrationInfo AccelCalibrationInfo;

        public SAccelStatus AccelState;

        public SButtons ButtonState;

        public SIRState IRState;

        // ReSharper disable NotAccessedField.Global
        public byte Battery;
        // ReSharper restore NotAccessedField.Global

        public bool Rumble;

        public SLEDStatus LEDState;

        public CWiiMoteStatus()
        {
            IRState.Sensors = new SIR[4];
        }
    }

    public struct SLEDStatus
    {
        public bool LED1;
        public bool LED2;
        public bool LED3;
        public bool LED4;
    }

    public enum EIRMode : byte
    {
        Off = 0x00,
        Basic = 0x01, // 10 bytes
        Extended = 0x03 // 12 bytes
    };

    public struct SIR
    {
        public bool Active;
        public Point Position; // X: 0-1023; Y: 0-767
        // ReSharper disable NotAccessedField.Global
        public int Width; // 0-15      
        // ReSharper restore NotAccessedField.Global
    }

    public struct SIRState
    {
        public EIRMode Mode;
        public SIR[] Sensors;
        public Point Position; //0..1023, 0..767
        public Point Distance; //between Point 0 and Point 1
    }

    public struct SAccelStatus
    {
        public SPoint3 RawValues; //0-255
        // ReSharper disable NotAccessedField.Global
        public SPoint3F Values; //0-3
        // ReSharper restore NotAccessedField.Global
    }

    public struct SAccelCalibrationInfo
    {
        public byte X0, Y0, Z0; //zero point
        public byte GravityX, GravityY, GravityZ; //gravity
    }

    public struct SButtons
    {
        public bool Up, Down, Left, Right, A, B, Plus, Minus, One, Two, Home;
    }

    public enum EInputReport : byte
    {
        Status = 0x20,
        ReadData = 0x21,
        Ack = 0x22,
        Buttons = 0x30,
        ButtonsAccel = 0x31,
        Buttons8Bytes = 0x32,
        IRAccel = 0x33,
        ButtonsExtension = 0x34,
        ExtensionAccel = 0x35,
        IRExtensionAccel = 0x37,
    };

    public enum EIRSensitivity
    {
        Level1,
        Level2,
        Level3,
        Level4,
        Level5,
        Max
    }
    #endregion DataTypes

    #region Events
    public class CWiiMoteChangedEventArgs : EventArgs
    {
        public readonly CWiiMoteStatus WiiMoteState;

        public CWiiMoteChangedEventArgs(CWiiMoteStatus ws)
        {
            WiiMoteState = ws;
        }
    }
    #endregion Events

    public sealed class CWiiMoteLib : IDisposable
    {
        // ReSharper disable InconsistentNaming
        private const ushort _VID = 0x057e;
        // ReSharper restore InconsistentNaming
        private const ushort _PID = 0x0306; //Wiimotion
        private const ushort _PIDPlus = 0x0330; //Wiimotion Plus

        // registers
        private const int _RegisterIR = 0x04b00030;
        private const int _RegisterIRSensitivity1 = 0x04b00000;
        private const int _RegisterIRSensitivity2 = 0x04b0001a;
        private const int _RegisterIRMode = 0x04b00033;

        private const int _ReportLength = 22;

        // output commands
        private enum EOutputReport : byte
        {
            // ReSharper disable UnusedMember.Local
            // ReSharper disable InconsistentNaming
            LEDs = 0x11,
            // ReSharper restore InconsistentNaming
            Type = 0x12,
            IR = 0x13,
            Status = 0x15,
            WriteMemory = 0x16,
            ReadMemory = 0x17,
            SpeakerData = 0x18,
            SpeakerMute = 0x19,
            IR2 = 0x1a,
            // ReSharper restore UnusedMember.Local
        };

        // data handling
        private IntPtr _Handle;
        private readonly byte[] _Buff = new byte[_ReportLength];
        private byte[] _ReadBuff;
        private int _Address;
        private short _Size;

        private readonly Thread _Reader;
        private bool _Active;
        private bool _Error;

        public bool Connected { get; private set; }

        private readonly CWiiMoteStatus _WiiMoteState = new CWiiMoteStatus();
        private readonly AutoResetEvent _ReadDone = new AutoResetEvent(false);
        public event EventHandler<CWiiMoteChangedEventArgs> WiiMoteChanged;

        #region Interface
        public CWiiMoteLib()
        {
            Connected = false;
            _Active = true;
            _Error = false;
            _Reader = new Thread(_ReaderLoop);
            _Reader.Start();

            if (!CHIDApi.Init())
            {
                CLog.LogError("WiiMoteLib: Can't initialize HID API");
                const string msg = "Please install the Visual C++ Redistributable Packages 2008!";
                CLog.LogError(msg);

                _Active = false;
                _Error = true;
            }
        }

        ~CWiiMoteLib()
        {
            Dispose(false);
        }

        private bool _TryConnect(ushort pid)
        {
            bool connected = CHIDApi.Open(_VID, pid, out _Handle);
            if (connected)
            {
                connected = _ReadCalibration();
                if (!connected)
                    CHIDApi.Close(_Handle);
            }
            return connected;
        }

        public bool Connect()
        {
            Connected = false;

            if (_Error)
                return false;

            //Try WiiMotion
            Connected = CHIDApi.Open(_VID, _PID, out _Handle);
            if (Connected)
            {
                Connected = _ReadCalibration();
                if (!Connected)
                    CHIDApi.Close(_Handle);
            }
            else
                CHIDApi.Close(_Handle);

            if (Connected)
                return true;

            //Try WiiMotion Plus
            Connected = CHIDApi.Open(_VID, _PIDPlus, out _Handle);
            if (Connected)
            {
                Connected = _ReadCalibration();
                if (!Connected)
                    CHIDApi.Close(_Handle);
            }
            else
                CHIDApi.Close(_Handle);


            return Connected;
        }

        public void Disconnect()
        {
            Connected = false;
            _Active = false;
            CHIDApi.Exit();
        }

        public void SetReportType(EInputReport type, EIRSensitivity irSensitivity, bool continuous)
        {
            if (!Connected)
                return;

            switch (type)
            {
                case EInputReport.IRAccel:
                    _EnableIR(EIRMode.Extended, irSensitivity);
                    break;
                default:
                    _DisableIR();
                    break;
            }

            _ClearReport();
            _Buff[0] = (byte)EOutputReport.Type;
            _Buff[1] = (byte)((continuous ? 0x04 : 0x00) | (byte)(_WiiMoteState.Rumble ? 0x01 : 0x00));
            _Buff[2] = (byte)type;

            CHIDApi.Write(_Handle, _Buff);
        }

        public void SetLEDs(bool led1, bool led2, bool led3, bool led4)
        {
            _WiiMoteState.LEDState.LED1 = led1;
            _WiiMoteState.LEDState.LED2 = led2;
            _WiiMoteState.LEDState.LED3 = led3;
            _WiiMoteState.LEDState.LED4 = led4;

            if (!Connected)
                return;

            _ClearReport();

            _Buff[0] = (byte)EOutputReport.LEDs;
            _Buff[1] = (byte)((led1 ? 0x10 : 0x00) |
                              (led2 ? 0x20 : 0x00) |
                              (led3 ? 0x40 : 0x00) |
                              (led4 ? 0x80 : 0x00) |
                              _RumbleBit);

            CHIDApi.Write(_Handle, _Buff);
        }

        public void SetRumble(bool on)
        {
            _WiiMoteState.Rumble = on;

            // the LED report also handles rumble
            SetLEDs(_WiiMoteState.LEDState.LED1,
                    _WiiMoteState.LEDState.LED2,
                    _WiiMoteState.LEDState.LED3,
                    _WiiMoteState.LEDState.LED4);
        }
        #endregion Interface

        #region Private stuff
        private void _ReaderLoop()
        {
            while (_Active)
            {
                Thread.Sleep(5);

                if (_Handle != IntPtr.Zero && Connected)
                {
                    byte[] buff = new byte[_ReportLength];

                    try
                    {
                        CHIDApi.ReadTimeout(_Handle, ref buff, _ReportLength, 200);
                    }
                    catch (Exception e)
                    {
                        CLog.LogError("(WiiMoteLib) Error reading from device: " + e);
                    }

                    if (_ParseInputReport(buff))
                    {
                        if (WiiMoteChanged != null)
                            WiiMoteChanged(this, new CWiiMoteChangedEventArgs(_WiiMoteState));
                    }
                }
            }
        }

        private bool _ParseInputReport(byte[] buff)
        {
            if (buff == null)
            {
                Connected = false;
                return false;
            }

            EInputReport type = (EInputReport)buff[0];

            switch (type)
            {
                case EInputReport.Buttons:
                    _ParseButtons(buff);
                    break;

                case EInputReport.ButtonsAccel:
                    _ParseButtons(buff);
                    _ParseAccel(buff);
                    break;

                case EInputReport.IRAccel:
                    _ParseButtons(buff);
                    _ParseAccel(buff);
                    _ParseIR(buff);
                    break;

                case EInputReport.ButtonsExtension:
                    _ParseButtons(buff);
                    break;

                case EInputReport.ExtensionAccel:
                    _ParseButtons(buff);
                    _ParseAccel(buff);
                    break;

                case EInputReport.IRExtensionAccel:
                    _ParseButtons(buff);
                    _ParseAccel(buff);
                    _ParseIR(buff);
                    break;

                case EInputReport.Status:
                    _ParseButtons(buff);
                    _WiiMoteState.Battery = buff[6];

                    _WiiMoteState.LEDState.LED1 = (buff[3] & 0x10) != 0;
                    _WiiMoteState.LEDState.LED2 = (buff[3] & 0x20) != 0;
                    _WiiMoteState.LEDState.LED3 = (buff[3] & 0x40) != 0;
                    _WiiMoteState.LEDState.LED4 = (buff[3] & 0x80) != 0;
                    break;

                case EInputReport.ReadData:
                    _ParseButtons(buff);
                    _ParseReadData(buff);
                    break;

                case EInputReport.Ack:
                    return false;

                case EInputReport.Buttons8Bytes:
                    break;

                default:
                    //CLog.LogError("(WiiMoteLib) Unknown report type: " + type.ToString("x"));
                    return false;
            }

            return true;
        }

        /*
        private byte[] _DecryptBuffer(byte[] buff)
        {
            for (int i = 0; i < buff.Length; i++)
                buff[i] = (byte)(((buff[i] ^ 0x17) + 0x17) & 0xff);

            return buff;
        }
*/

        private void _ParseButtons(byte[] buff)
        {
            _WiiMoteState.ButtonState.A = (buff[2] & 0x08) != 0;
            _WiiMoteState.ButtonState.B = (buff[2] & 0x04) != 0;
            _WiiMoteState.ButtonState.Minus = (buff[2] & 0x10) != 0;
            _WiiMoteState.ButtonState.Home = (buff[2] & 0x80) != 0;
            _WiiMoteState.ButtonState.Plus = (buff[1] & 0x10) != 0;
            _WiiMoteState.ButtonState.One = (buff[2] & 0x02) != 0;
            _WiiMoteState.ButtonState.Two = (buff[2] & 0x01) != 0;
            _WiiMoteState.ButtonState.Up = (buff[1] & 0x08) != 0;
            _WiiMoteState.ButtonState.Down = (buff[1] & 0x04) != 0;
            _WiiMoteState.ButtonState.Left = (buff[1] & 0x01) != 0;
            _WiiMoteState.ButtonState.Right = (buff[1] & 0x02) != 0;
        }

        private void _ParseAccel(byte[] buff)
        {
            _WiiMoteState.AccelState.RawValues.X = buff[3];
            _WiiMoteState.AccelState.RawValues.Y = buff[4];
            _WiiMoteState.AccelState.RawValues.Z = buff[5];

            _WiiMoteState.AccelState.Values.X = ((float)_WiiMoteState.AccelState.RawValues.X - _WiiMoteState.AccelCalibrationInfo.X0) /
                                                ((float)_WiiMoteState.AccelCalibrationInfo.GravityX - _WiiMoteState.AccelCalibrationInfo.X0);
            _WiiMoteState.AccelState.Values.Y = ((float)_WiiMoteState.AccelState.RawValues.Y - _WiiMoteState.AccelCalibrationInfo.Y0) /
                                                ((float)_WiiMoteState.AccelCalibrationInfo.GravityY - _WiiMoteState.AccelCalibrationInfo.Y0);
            _WiiMoteState.AccelState.Values.Z = ((float)_WiiMoteState.AccelState.RawValues.Z - _WiiMoteState.AccelCalibrationInfo.Z0) /
                                                ((float)_WiiMoteState.AccelCalibrationInfo.GravityZ - _WiiMoteState.AccelCalibrationInfo.Z0);
        }

        private void _ParseIR(byte[] buff)
        {
            switch (_WiiMoteState.IRState.Mode)
            {
                case EIRMode.Basic:
                    _WiiMoteState.IRState.Sensors[0].Active = !(buff[6] == 0xff && buff[7] == 0xff);
                    if (_WiiMoteState.IRState.Sensors[0].Active)
                    {
                        _WiiMoteState.IRState.Sensors[0].Position.X = buff[6] | ((buff[8] >> 4) & 0x03) << 8;
                        _WiiMoteState.IRState.Sensors[0].Position.Y = buff[7] | ((buff[8] >> 6) & 0x03) << 8;
                        _WiiMoteState.IRState.Sensors[0].Width = 0;
                    }
                    _WiiMoteState.IRState.Sensors[1].Active = !(buff[9] == 0xff && buff[10] == 0xff);
                    if (_WiiMoteState.IRState.Sensors[1].Active)
                    {
                        _WiiMoteState.IRState.Sensors[1].Position.X = buff[9] | ((buff[8] >> 0) & 0x03) << 8;
                        _WiiMoteState.IRState.Sensors[1].Position.Y = buff[10] | ((buff[8] >> 2) & 0x03) << 8;
                        _WiiMoteState.IRState.Sensors[1].Width = 0;
                    }
                    break;
                case EIRMode.Extended:
                    for (int i = 0; i < 4; i++)
                    {
                        _WiiMoteState.IRState.Sensors[i].Active = !(buff[6 + i * 3] == 0xff && buff[7 + i * 3] == 0xff && buff[8 + i * 3] == 0xff);
                        if (!_WiiMoteState.IRState.Sensors[i].Active)
                            continue;
                        _WiiMoteState.IRState.Sensors[i].Position.X = buff[6 + i * 3] | ((buff[8 + i * 3] >> 4) & 0x03) << 8;
                        _WiiMoteState.IRState.Sensors[i].Position.Y = buff[7 + i * 3] | ((buff[8 + i * 3] >> 6) & 0x03) << 8;
                        _WiiMoteState.IRState.Sensors[i].Width = buff[8 + i * 3] & 0x0f;
                    }
                    break;
            }

            if (_WiiMoteState.IRState.Sensors[0].Active && _WiiMoteState.IRState.Sensors[1].Active)
            {
                _WiiMoteState.IRState.Position.X = (_WiiMoteState.IRState.Sensors[1].Position.X + _WiiMoteState.IRState.Sensors[0].Position.X) / 2;
                _WiiMoteState.IRState.Position.Y = (_WiiMoteState.IRState.Sensors[1].Position.Y + _WiiMoteState.IRState.Sensors[0].Position.Y) / 2;

                _WiiMoteState.IRState.Distance.X = Math.Abs(_WiiMoteState.IRState.Sensors[1].Position.X - _WiiMoteState.IRState.Sensors[0].Position.X);
                _WiiMoteState.IRState.Distance.Y = Math.Abs(_WiiMoteState.IRState.Sensors[1].Position.Y - _WiiMoteState.IRState.Sensors[0].Position.Y);
            }
            else if (_WiiMoteState.IRState.Sensors[0].Active)
            {
                if (_WiiMoteState.IRState.Sensors[0].Position.X > 512)
                    _WiiMoteState.IRState.Position.X = _WiiMoteState.IRState.Sensors[0].Position.X - _WiiMoteState.IRState.Distance.X / 2;
                else
                    _WiiMoteState.IRState.Position.X = _WiiMoteState.IRState.Sensors[0].Position.X + _WiiMoteState.IRState.Distance.X / 2;

                if (_WiiMoteState.IRState.Sensors[0].Position.X < 384)
                    _WiiMoteState.IRState.Position.Y = _WiiMoteState.IRState.Sensors[0].Position.Y - _WiiMoteState.IRState.Distance.Y / 2;
                else
                    _WiiMoteState.IRState.Position.Y = _WiiMoteState.IRState.Sensors[0].Position.Y + _WiiMoteState.IRState.Distance.Y / 2;
            }
        }

        private void _ParseReadData(byte[] buff)
        {
            if ((buff[3] & 0x08) != 0)
            {
                CLog.LogError("Error reading data from WiiMote: Bytes do not exist");
                Connected = false;
            }

            if ((buff[3] & 0x07) != 0)
            {
                CLog.LogError("Error reading data from WiiMote: Attempt to read from write-only registers.");
                Connected = false;
            }

            int size = (buff[3] >> 4) + 1;
            int offset = buff[4] << 8 | buff[5];

            Array.Copy(buff, 6, _ReadBuff, offset - _Address, size);

            if (_Address + _Size == offset + size)
                _ReadDone.Set();
        }

        private byte _RumbleBit
        {
            get { return (byte)(_WiiMoteState.Rumble ? 1 : 0); }
        }

        private bool _ReadCalibration()
        {
            // this appears to change the report type to 0x31
            byte[] buff = _ReadData(0x0016, 7);
            if (buff == null)
                return false;

            _WiiMoteState.AccelCalibrationInfo.X0 = buff[0];
            _WiiMoteState.AccelCalibrationInfo.Y0 = buff[1];
            _WiiMoteState.AccelCalibrationInfo.Z0 = buff[2];
            _WiiMoteState.AccelCalibrationInfo.GravityX = buff[4];
            _WiiMoteState.AccelCalibrationInfo.GravityY = buff[5];
            _WiiMoteState.AccelCalibrationInfo.GravityZ = buff[6];

            return true;
        }

        private void _EnableIR(EIRMode mode, EIRSensitivity sensitivity)
        {
            _WiiMoteState.IRState.Mode = mode;

            _ClearReport();
            _Buff[0] = (byte)EOutputReport.IR;
            _Buff[1] = (byte)(0x04 | _RumbleBit);
            CHIDApi.Write(_Handle, _Buff);
            Thread.Sleep(50);

            _ClearReport();
            _Buff[0] = (byte)EOutputReport.IR2;
            _Buff[1] = (byte)(0x04 | _RumbleBit);
            CHIDApi.Write(_Handle, _Buff);
            Thread.Sleep(50);

            _WriteData(_RegisterIR, 0x08);
            Thread.Sleep(50);

            switch (sensitivity)
            {
                case EIRSensitivity.Level1:
                    _WriteData(_RegisterIRSensitivity1, 9, new byte[] {0x02, 0x00, 0x00, 0x71, 0x01, 0x00, 0x64, 0x00, 0xfe});
                    Thread.Sleep(50);
                    _WriteData(_RegisterIRSensitivity2, 2, new byte[] {0xfd, 0x05});
                    break;
                case EIRSensitivity.Level2:
                    _WriteData(_RegisterIRSensitivity1, 9, new byte[] {0x02, 0x00, 0x00, 0x71, 0x01, 0x00, 0x96, 0x00, 0xb4});
                    Thread.Sleep(50);
                    _WriteData(_RegisterIRSensitivity2, 2, new byte[] {0xb3, 0x04});
                    break;
                case EIRSensitivity.Level3:
                    _WriteData(_RegisterIRSensitivity1, 9, new byte[] {0x02, 0x00, 0x00, 0x71, 0x01, 0x00, 0xaa, 0x00, 0x64});
                    Thread.Sleep(50);
                    _WriteData(_RegisterIRSensitivity2, 2, new byte[] {0x63, 0x03});
                    break;
                case EIRSensitivity.Level4:
                    _WriteData(_RegisterIRSensitivity1, 9, new byte[] {0x02, 0x00, 0x00, 0x71, 0x01, 0x00, 0xc8, 0x00, 0x36});
                    Thread.Sleep(50);
                    _WriteData(_RegisterIRSensitivity2, 2, new byte[] {0x35, 0x03});
                    break;
                case EIRSensitivity.Level5:
                    _WriteData(_RegisterIRSensitivity1, 9, new byte[] {0x07, 0x00, 0x00, 0x71, 0x01, 0x00, 0x72, 0x00, 0x20});
                    Thread.Sleep(50);
                    _WriteData(_RegisterIRSensitivity2, 2, new byte[] {0x1, 0x03});
                    break;
                case EIRSensitivity.Max:
                    _WriteData(_RegisterIRSensitivity1, 9, new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x90, 0x00, 0x41});
                    Thread.Sleep(50);
                    _WriteData(_RegisterIRSensitivity2, 2, new byte[] {0x40, 0x00});
                    break;
            }
            Thread.Sleep(50);
            _WriteData(_RegisterIRMode, (byte)mode);
            Thread.Sleep(50);
            _WriteData(_RegisterIR, 0x08);
            Thread.Sleep(50);
        }

        private void _DisableIR()
        {
            _WiiMoteState.IRState.Mode = EIRMode.Off;

            _ClearReport();
            _Buff[0] = (byte)EOutputReport.IR;
            _Buff[1] = _RumbleBit;
            CHIDApi.Write(_Handle, _Buff);

            _ClearReport();
            _Buff[0] = (byte)EOutputReport.IR2;
            _Buff[1] = _RumbleBit;
            CHIDApi.Write(_Handle, _Buff);
        }

        private void _ClearReport()
        {
            Array.Clear(_Buff, 0, _ReportLength);
        }

        private byte[] _ReadData(int address, short size)
        {
            _ClearReport();

            _ReadBuff = new byte[size];
            _Address = address & 0xffff;
            _Size = size;

            _Buff[0] = (byte)EOutputReport.ReadMemory;
            _Buff[1] = (byte)(((address & 0xff000000) >> 24) | _RumbleBit);
            _Buff[2] = (byte)((address & 0x00ff0000) >> 16);
            _Buff[3] = (byte)((address & 0x0000ff00) >> 8);
            _Buff[4] = (byte)(address & 0x000000ff);

            _Buff[5] = (byte)((size & 0xff00) >> 8);
            _Buff[6] = (byte)(size & 0xff);

            CHIDApi.Write(_Handle, _Buff);

            if (!_ReadDone.WaitOne(1000, false))
            {
                Connected = false;
                return null;
            }

            return _ReadBuff;
        }

        private void _WriteData(int address, byte data)
        {
            _WriteData(address, 1, new byte[] {data});
        }

        private void _WriteData(int address, byte size, byte[] buff)
        {
            _ClearReport();

            _Buff[0] = (byte)EOutputReport.WriteMemory;
            _Buff[1] = (byte)(((address & 0xff000000) >> 24) | _RumbleBit);
            _Buff[2] = (byte)((address & 0x00ff0000) >> 16);
            _Buff[3] = (byte)((address & 0x0000ff00) >> 8);
            _Buff[4] = (byte)(address & 0x000000ff);
            _Buff[5] = size;
            Array.Copy(buff, 0, _Buff, 6, size);

            CHIDApi.Write(_Handle, _Buff);

            Thread.Sleep(100);
        }
        #endregion Private stuff

        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // ReSharper disable InconsistentNaming
        private void Dispose(bool disposing)
            // ReSharper restore InconsistentNaming
        {
            if (disposing)
            {
                Disconnect();
                _ReadDone.Close();
            }
        }
        #endregion
    }
}