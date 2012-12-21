using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.Lib.Draw;
using Vocaluxe.Lib.Input;
using Vocaluxe.Menu;

namespace Vocaluxe.Lib.Input.WiiMote
{
    #region DataTypes
	public class WiiMoteStatus
	{
		public AccelCalibrationInfo AccelCalibrationInfo = new AccelCalibrationInfo();

		public AccelStatus AccelState = new AccelStatus();

		public Buttons ButtonState = new Buttons();

		public IRState IRState = new IRState();

		public byte Battery;

		public bool Rumble;
		
		public LEDStatus LEDState;

		public WiiMoteStatus()
		{
			IRState.Sensors = new IR[4];
		}
	}

    public struct LEDStatus
    {
        public bool LED1;
        public bool LED2;
        public bool LED3;
        public bool LED4;
    }

    public enum IRMode : byte
    {
        Off = 0x00,
        Basic = 0x01,	// 10 bytes
        Extended = 0x03 // 12 bytes
    };

    public struct IR
    {
        public bool Active;
        public Point Position;  // X: 0-1023; Y: 0-767
        public int Width;       // 0-15      
    }

	public struct IRState
	{
		public IRMode Mode;
		public IR[] Sensors;
		public Point Position;  //0..1023, 0..767
        public Point Distance;  //between Point 0 and Point 1
	}

	public struct AccelStatus
	{
		public SPoint3 RawValues;   //0-255
		public SPoint3f Values;     //0-3
	}

	public struct AccelCalibrationInfo
	{
		public byte X0, Y0, Z0;     //zero point
		public byte XG, YG, ZG;     //gravity
	}

	public struct Buttons
	{
        public bool Up, Down, Left, Right, A, B, Plus, Minus, One, Two, Home;
	}

	public enum InputReport : byte
	{
		Status				= 0x20,
		ReadData			= 0x21,
        Ack                 = 0x22,
		Buttons				= 0x30,
		ButtonsAccel		= 0x31,
        Buttons8Bytes       = 0x32,
		IRAccel				= 0x33,
		ButtonsExtension	= 0x34,
		ExtensionAccel		= 0x35,
		IRExtensionAccel	= 0x37,
	};

	public enum IRSensitivity
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
	public class WiiMoteChangedEventArgs: EventArgs
	{
		public WiiMoteStatus WiiMoteState;

		public WiiMoteChangedEventArgs(WiiMoteStatus ws)
		{
			WiiMoteState = ws;
		}
	}
    #endregion Events

	public class WiiMoteLib : IDisposable
	{
        private const ushort VID = 0x057e;
        private const ushort PID = 0x0306;         //Wiimotion
        private const ushort PIDPlus = 0x0330;     //Wiimotion Plus

        // registers
        private const int REGISTER_IR = 0x04b00030;
        private const int REGISTER_IR_SENSITIVITY_1 = 0x04b00000;
        private const int REGISTER_IR_SENSITIVITY_2 = 0x04b0001a;
        private const int REGISTER_IR_MODE = 0x04b00033;

		private const int REPORT_LENGTH = 22;

		// output commands
		private enum OutputReport : byte
		{
			LEDs			= 0x11,
			Type			= 0x12,
			IR				= 0x13,
			Status			= 0x15,
			WriteMemory		= 0x16,
			ReadMemory		= 0x17,
            SpeakerData     = 0x18,
            SpeakerMute     = 0x19,
			IR2				= 0x1a,
		};
        
        // data handling
        private IntPtr _Handle;
		private readonly byte[] _Buff = new byte[REPORT_LENGTH];
		private byte[] _ReadBuff;
		private int _Address;
		private short _Size;

        private Thread _Reader;
        private bool _Active;
        private bool _Error;

        private bool _Connected;
        public bool Connected
        {
            get 
            {
                return _Connected; 
            }
        }

		private readonly WiiMoteStatus _WiiMoteState = new WiiMoteStatus();
		private readonly AutoResetEvent _ReadDone = new AutoResetEvent(false);
        public event EventHandler<WiiMoteChangedEventArgs> WiiMoteChanged;

        #region Interface
        public WiiMoteLib()
		{
            _Connected = false;
            _Active = true;
            _Error = false;
            _Reader = new Thread(ReaderLoop);
            _Reader.Start();
		}

		public bool Connect()
		{
            _Connected = false;

            if (_Error)
                return false;

            CHIDAPI.Exit();
            
            if (!CHIDAPI.Init())
            {
                CLog.LogError("WiiMoteLib: Can't initialize HID API");
                string msg = "Please install the Visual C++ Redistributable Packages 2008!";
                CLog.LogError(msg);

                _Active = false;
                _Error = true;
                return false;
            }

            //Try WiiMotion
            if (_Connected = CHIDAPI.Open(VID, PID, out _Handle))
            {
                if (!(_Connected = ReadCalibration()))
                    CHIDAPI.Close(_Handle);
            }

            if (_Connected)
                return true;
                
            //Try WiiMotion Plus
            if (_Connected = CHIDAPI.Open(VID, PIDPlus, out _Handle))
            {
                if (!(_Connected = ReadCalibration()))
                    CHIDAPI.Close(_Handle);
            }

            return _Connected;      
		}

		public void Disconnect()
		{
            _Connected = false;
            _Active = false;
            CHIDAPI.Exit(); 
		}

        public void SetReportType(InputReport type, IRSensitivity irSensitivity, bool continuous)
        {
            if (!_Connected)
                return;

            switch (type)
            {
                case InputReport.IRAccel:
                    EnableIR(IRMode.Extended, irSensitivity);
                    break;
                default:
                    DisableIR();
                    break;
            }

            ClearReport();
            _Buff[0] = (byte)OutputReport.Type;
            _Buff[1] = (byte)((continuous ? 0x04 : 0x00) | (byte)(_WiiMoteState.Rumble ? 0x01 : 0x00));
            _Buff[2] = (byte)type;

            CHIDAPI.Write(_Handle, _Buff);
        }

        public void SetLEDs(bool led1, bool led2, bool led3, bool led4)
        {
            _WiiMoteState.LEDState.LED1 = led1;
            _WiiMoteState.LEDState.LED2 = led2;
            _WiiMoteState.LEDState.LED3 = led3;
            _WiiMoteState.LEDState.LED4 = led4;

            if (!_Connected)
                return;

            ClearReport();

            _Buff[0] = (byte)OutputReport.LEDs;
            _Buff[1] = (byte)(
                        (led1 ? 0x10 : 0x00) |
                        (led2 ? 0x20 : 0x00) |
                        (led3 ? 0x40 : 0x00) |
                        (led4 ? 0x80 : 0x00) |
                        RumbleBit);

            CHIDAPI.Write(_Handle, _Buff);
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

        public WiiMoteStatus WiiMoteState
        {
            get { return _WiiMoteState; }
        }
        #endregion Interface

        #region Private stuff
        private void ReaderLoop()
        {
            while (_Active)
            {
                Thread.Sleep(5);

                if (_Handle != IntPtr.Zero && _Connected)
                {
                    byte[] buff = new byte[REPORT_LENGTH];

                    try
                    {
                        CHIDAPI.ReadTimeout(_Handle, out buff, REPORT_LENGTH, 100);
                    }
                    catch (Exception e)
                    {
                        CLog.LogError("(WiiMoteLib) Error reading from device: " + e.ToString());
                    }

                    if (ParseInputReport(buff))
                    {
                        if (WiiMoteChanged != null)
                            WiiMoteChanged(this, new WiiMoteChangedEventArgs(_WiiMoteState));
                    }
                }
            }
        }

		private bool ParseInputReport(byte[] buff)
		{
            if (buff == null)
            {
                _Connected = false;
                return false;
            }

			InputReport type = (InputReport)buff[0];

			switch(type)
			{
				case InputReport.Buttons:
					ParseButtons(buff);
					break;

				case InputReport.ButtonsAccel:
					ParseButtons(buff);
					ParseAccel(buff);
					break;

				case InputReport.IRAccel:
					ParseButtons(buff);
					ParseAccel(buff);
					ParseIR(buff);
					break;

				case InputReport.ButtonsExtension:
					ParseButtons(buff);
					break;

				case InputReport.ExtensionAccel:
					ParseButtons(buff);
					ParseAccel(buff);
					break;

				case InputReport.IRExtensionAccel:
					ParseButtons(buff);
					ParseAccel(buff);
					ParseIR(buff);
					break;

				case InputReport.Status:
					ParseButtons(buff);
					_WiiMoteState.Battery = buff[6];

					_WiiMoteState.LEDState.LED1 = (buff[3] & 0x10) != 0;
					_WiiMoteState.LEDState.LED2 = (buff[3] & 0x20) != 0;
					_WiiMoteState.LEDState.LED3 = (buff[3] & 0x40) != 0;
					_WiiMoteState.LEDState.LED4 = (buff[3] & 0x80) != 0;
					break;

				case InputReport.ReadData:
					ParseButtons(buff);
					ParseReadData(buff);
					break;

                case InputReport.Ack:
                    return false;

                case InputReport.Buttons8Bytes:
                    break;

				default:
					//CLog.LogError("(WiiMoteLib) Unknown report type: " + type.ToString("x"));
					return false;
			}

			return true;
		}

		private byte[] DecryptBuffer(byte[] buff)
		{
			for(int i = 0; i < buff.Length; i++)
				buff[i] = (byte)(((buff[i] ^ 0x17) + 0x17) & 0xff);

			return buff;
		}

		private void ParseButtons(byte[] buff)
		{
			_WiiMoteState.ButtonState.A		= (buff[2] & 0x08) != 0;
			_WiiMoteState.ButtonState.B		= (buff[2] & 0x04) != 0;
			_WiiMoteState.ButtonState.Minus	= (buff[2] & 0x10) != 0;
			_WiiMoteState.ButtonState.Home	= (buff[2] & 0x80) != 0;
			_WiiMoteState.ButtonState.Plus	= (buff[1] & 0x10) != 0;
			_WiiMoteState.ButtonState.One	= (buff[2] & 0x02) != 0;
			_WiiMoteState.ButtonState.Two	= (buff[2] & 0x01) != 0;
			_WiiMoteState.ButtonState.Up	= (buff[1] & 0x08) != 0;
			_WiiMoteState.ButtonState.Down	= (buff[1] & 0x04) != 0;
			_WiiMoteState.ButtonState.Left	= (buff[1] & 0x01) != 0;
			_WiiMoteState.ButtonState.Right	= (buff[1] & 0x02) != 0;
		}

		private void ParseAccel(byte[] buff)
		{
			_WiiMoteState.AccelState.RawValues.X = buff[3];
			_WiiMoteState.AccelState.RawValues.Y = buff[4];
			_WiiMoteState.AccelState.RawValues.Z = buff[5];

			_WiiMoteState.AccelState.Values.X = (float)((float)_WiiMoteState.AccelState.RawValues.X - _WiiMoteState.AccelCalibrationInfo.X0) / 
											((float)_WiiMoteState.AccelCalibrationInfo.XG - _WiiMoteState.AccelCalibrationInfo.X0);
			_WiiMoteState.AccelState.Values.Y = (float)((float)_WiiMoteState.AccelState.RawValues.Y - _WiiMoteState.AccelCalibrationInfo.Y0) /
											((float)_WiiMoteState.AccelCalibrationInfo.YG - _WiiMoteState.AccelCalibrationInfo.Y0);
			_WiiMoteState.AccelState.Values.Z = (float)((float)_WiiMoteState.AccelState.RawValues.Z - _WiiMoteState.AccelCalibrationInfo.Z0) /
											((float)_WiiMoteState.AccelCalibrationInfo.ZG - _WiiMoteState.AccelCalibrationInfo.Z0);
		}

		private void ParseIR(byte[] buff)
		{
			switch(_WiiMoteState.IRState.Mode)
			{
				case IRMode.Basic:
                    if (_WiiMoteState.IRState.Sensors[0].Active = !(buff[6] == 0xff && buff[7] == 0xff))
                    {
                        _WiiMoteState.IRState.Sensors[0].Position.X = buff[6] | ((buff[8] >> 4) & 0x03) << 8;
                        _WiiMoteState.IRState.Sensors[0].Position.Y = buff[7] | ((buff[8] >> 6) & 0x03) << 8;
                        _WiiMoteState.IRState.Sensors[0].Width = 0;
                    }

                    if (_WiiMoteState.IRState.Sensors[1].Active = !(buff[9] == 0xff && buff[10] == 0xff))
                    {
                        _WiiMoteState.IRState.Sensors[1].Position.X = buff[9] | ((buff[8] >> 0) & 0x03) << 8;
                        _WiiMoteState.IRState.Sensors[1].Position.Y = buff[10] | ((buff[8] >> 2) & 0x03) << 8;
                        _WiiMoteState.IRState.Sensors[1].Width = 0;
                    }
					break;
				case IRMode.Extended:
                    for (int i = 0; i < 4; i++)
                    {
                        if (_WiiMoteState.IRState.Sensors[i].Active = !(buff[6 + i * 3] == 0xff && buff[7 + i * 3] == 0xff && buff[8 + i * 3] == 0xff))
                        {
                            _WiiMoteState.IRState.Sensors[i].Position.X = buff[6 + i * 3] | ((buff[8 + i * 3] >> 4) & 0x03) << 8;
                            _WiiMoteState.IRState.Sensors[i].Position.Y = buff[7 + i * 3] | ((buff[8 + i * 3] >> 6) & 0x03) << 8;
                            _WiiMoteState.IRState.Sensors[i].Width = buff[8 + i * 3] & 0x0f;
                        }
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

		private void ParseReadData(byte[] buff)
		{
            if ((buff[3] & 0x08) != 0)
            {
                CLog.LogError("Error reading data from WiiMote: Bytes do not exist");
                _Connected = false;
            }

            if ((buff[3] & 0x07) != 0)
            {
                CLog.LogError("Error reading data from WiiMote: Attempt to read from write-only registers.");
                _Connected = false;
            }

			int size = (buff[3] >> 4) + 1;
			int offset = (buff[4] << 8 | buff[5]);

			Array.Copy(buff, 6, _ReadBuff, offset - _Address, size);

			if(_Address + _Size == offset + size)
				_ReadDone.Set();
		}

		private byte RumbleBit
		{
            get
            {
                if (_WiiMoteState.Rumble)
                    return (byte)0x1;
                return (byte)0x0;
            }
		}

		private bool ReadCalibration()
		{
			// this appears to change the report type to 0x31
			byte[] buff = ReadData(0x0016, 7);
            if (buff == null)
                return false;

			_WiiMoteState.AccelCalibrationInfo.X0 = buff[0];
			_WiiMoteState.AccelCalibrationInfo.Y0 = buff[1];
			_WiiMoteState.AccelCalibrationInfo.Z0 = buff[2];
			_WiiMoteState.AccelCalibrationInfo.XG = buff[4];
			_WiiMoteState.AccelCalibrationInfo.YG = buff[5];
			_WiiMoteState.AccelCalibrationInfo.ZG = buff[6];

            return true;
		}
        
		private void EnableIR(IRMode mode, IRSensitivity Sensitivity)
		{
			_WiiMoteState.IRState.Mode = mode;

			ClearReport();
			_Buff[0] = (byte)OutputReport.IR;
			_Buff[1] = (byte)(0x04 | RumbleBit);
            CHIDAPI.Write(_Handle, _Buff);
            Thread.Sleep(50);

			ClearReport();
			_Buff[0] = (byte)OutputReport.IR2;
			_Buff[1] = (byte)(0x04 | RumbleBit);
            CHIDAPI.Write(_Handle, _Buff);
            Thread.Sleep(50);

			WriteData(REGISTER_IR, 0x08);
            Thread.Sleep(50);

			switch(Sensitivity)
			{
				case IRSensitivity.Level1:
					WriteData(REGISTER_IR_SENSITIVITY_1, 9, new byte[] {0x02, 0x00, 0x00, 0x71, 0x01, 0x00, 0x64, 0x00, 0xfe});
                    Thread.Sleep(50);
                    WriteData(REGISTER_IR_SENSITIVITY_2, 2, new byte[] {0xfd, 0x05});
					break;
				case IRSensitivity.Level2:
					WriteData(REGISTER_IR_SENSITIVITY_1, 9, new byte[] {0x02, 0x00, 0x00, 0x71, 0x01, 0x00, 0x96, 0x00, 0xb4});
                    Thread.Sleep(50);
                    WriteData(REGISTER_IR_SENSITIVITY_2, 2, new byte[] {0xb3, 0x04});
					break;
				case IRSensitivity.Level3:
					WriteData(REGISTER_IR_SENSITIVITY_1, 9, new byte[] {0x02, 0x00, 0x00, 0x71, 0x01, 0x00, 0xaa, 0x00, 0x64});
                    Thread.Sleep(50);
                    WriteData(REGISTER_IR_SENSITIVITY_2, 2, new byte[] {0x63, 0x03});
					break;
				case IRSensitivity.Level4:
					WriteData(REGISTER_IR_SENSITIVITY_1, 9, new byte[] {0x02, 0x00, 0x00, 0x71, 0x01, 0x00, 0xc8, 0x00, 0x36});
                    Thread.Sleep(50);
                    WriteData(REGISTER_IR_SENSITIVITY_2, 2, new byte[] {0x35, 0x03});
					break;
				case IRSensitivity.Level5:
					WriteData(REGISTER_IR_SENSITIVITY_1, 9, new byte[] {0x07, 0x00, 0x00, 0x71, 0x01, 0x00, 0x72, 0x00, 0x20});
                    Thread.Sleep(50);
                    WriteData(REGISTER_IR_SENSITIVITY_2, 2, new byte[] {0x1, 0x03});
					break;
				case IRSensitivity.Max:
					WriteData(REGISTER_IR_SENSITIVITY_1, 9, new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x90, 0x00, 0x41});
                    Thread.Sleep(50);
                    WriteData(REGISTER_IR_SENSITIVITY_2, 2, new byte[] {0x40, 0x00});
					break;
			}
            Thread.Sleep(50);
			WriteData(REGISTER_IR_MODE, (byte)mode);
            Thread.Sleep(50);
			WriteData(REGISTER_IR, 0x08);
            Thread.Sleep(50);
		}

		private void DisableIR()
		{
			_WiiMoteState.IRState.Mode = IRMode.Off;

			ClearReport();
			_Buff[0] = (byte)OutputReport.IR;
			_Buff[1] = RumbleBit;
            CHIDAPI.Write(_Handle, _Buff);

			ClearReport();
			_Buff[0] = (byte)OutputReport.IR2;
			_Buff[1] = RumbleBit;
            CHIDAPI.Write(_Handle, _Buff);
		}

		private void ClearReport()
		{
			Array.Clear(_Buff, 0, REPORT_LENGTH);
		}

		private byte[] ReadData(int address, short size)
		{
			ClearReport();

			_ReadBuff = new byte[size];
			_Address = address & 0xffff;
			_Size = size;

			_Buff[0] = (byte)OutputReport.ReadMemory;
			_Buff[1] = (byte)(((address & 0xff000000) >> 24) | RumbleBit);
			_Buff[2] = (byte)((address & 0x00ff0000)  >> 16);
			_Buff[3] = (byte)((address & 0x0000ff00)  >>  8);
			_Buff[4] = (byte)(address & 0x000000ff);

			_Buff[5] = (byte)((size & 0xff00) >> 8);
			_Buff[6] = (byte)(size & 0xff);

            CHIDAPI.Write(_Handle, _Buff);

            if (!_ReadDone.WaitOne(1000, false))
            {
                _Connected = false;
                return null;
            }

			return _ReadBuff;
		}
        
		private void WriteData(int address, byte data)
		{
			WriteData(address, 1, new byte[] { data });
		}

		private void WriteData(int address, byte size, byte[] buff)
		{
			ClearReport();

			_Buff[0] = (byte)OutputReport.WriteMemory;
			_Buff[1] = (byte)(((address & 0xff000000) >> 24) | RumbleBit);
			_Buff[2] = (byte)((address & 0x00ff0000)  >> 16);
			_Buff[3] = (byte)((address & 0x0000ff00)  >>  8);
			_Buff[4] = (byte)(address & 0x000000ff);
			_Buff[5] = size;
			Array.Copy(buff, 0, _Buff, 6, size);

            CHIDAPI.Write(_Handle, _Buff);

			Thread.Sleep(100);
		}
        #endregion Private stuff

        #region IDisposable Members
        public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if(disposing)
				Disconnect();
		}
		#endregion
	}
}