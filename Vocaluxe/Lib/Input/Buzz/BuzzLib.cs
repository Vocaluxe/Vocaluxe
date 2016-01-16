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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Vocaluxe.Base;

namespace Vocaluxe.Lib.Input.Buzz
{
    #region DataTypes
    public class CBuzzStatus
    {
        public SBuzzer Buzzer1;
        public SBuzzer Buzzer2;
        public SBuzzer Buzzer3;
        public SBuzzer Buzzer4;

        public CBuzzStatus()
        {
            
        }
    }

    public struct SBuzzer
    {
        public bool red, blue, orange, green, yellow, led;
    }

    #endregion

    #region Events
    public class CBuzzChangedEventArgs : EventArgs
    {
        public readonly CBuzzStatus BuzzState;

        public CBuzzChangedEventArgs(CBuzzStatus bs)
        {
            BuzzState = bs;
        }
    }

    public class CBuzzConnectionChangedEventArgs : EventArgs
    {
        public readonly bool Connected;

        public CBuzzConnectionChangedEventArgs(bool connected)
        {
            Connected = connected;
        }
    }

    #endregion
    public sealed class CBuzzLib
    {
        private const ushort _VID = 0x054c;
        private const ushort _PID = 0x1000;
        private const int _ReportLength = 8;

        // data handling
        private IntPtr _Handle;
        private readonly byte[] _Buff = new byte[_ReportLength]; 

        private Thread _Reader;
        private readonly bool _Error;

        private bool _Connected;
        public bool Connected
        {
            get { return _Connected; }
            private set
            {
                _Connected = value;
                if (BuzzConnectionChanged != null)
                    BuzzConnectionChanged.Invoke(this, new CBuzzConnectionChangedEventArgs(value));
            }
        }

        private readonly CBuzzStatus _BuzzState = new CBuzzStatus();
        public event EventHandler<CBuzzChangedEventArgs> BuzzChanged;
        public event EventHandler<CBuzzConnectionChangedEventArgs> BuzzConnectionChanged;
        public CBuzzLib()
        {
            if (!CHIDApi.Init())
            {
                CLog.LogError("BuzzLib: Can't initialize HID API");
                CLog.LogError("Please install the Visual C++ Redistributable Packages 2008!");

                _Error = true;
            }
        }

        ~CBuzzLib()
        {
            Disconnect();
            //Wait for thread to finish (if any)
            _WaitForReader();
            CHIDApi.Exit();
        }

        public void SetLEDs(bool led1, bool led2, bool led3, bool led4)
        {
            _BuzzState.Buzzer1.led = led1;
            _BuzzState.Buzzer2.led = led2;
            _BuzzState.Buzzer3.led = led3;
            _BuzzState.Buzzer4.led = led4;

            if (!Connected)
                return;

            _ClearReport();

            _Buff[0] = (byte)0x00;
            _Buff[1] = (byte)0x00;
            _Buff[2] = (byte)(led1 ? 0xFF : 0x00);
            _Buff[3] = (byte)(led2 ? 0xFF : 0x00);
            _Buff[4] = (byte)(led3 ? 0xFF : 0x00);
            _Buff[5] = (byte)(led4 ? 0xFF : 0x00);
            _Buff[6] = (byte)0x00;
            _Buff[7] = (byte)0x00;
            CHIDApi.Write(_Handle, _Buff);
        }

        private void _StartReader()
        {
            _Reader = new Thread(_ReaderLoop) {Name = "BuzzLib"};
            _Reader.Start();
        }

        private void _WaitForReader()
        {
            if (_Reader == null)
                return;
            _Reader.Join();
            _Reader = null;
        }

        private void _TryConnect(ushort pid)
        {
            //We might have had a reader thread, that is not finished yet, so let it finish and close it's handle first or it will close the new one
            _WaitForReader();
            Connected = CHIDApi.Open(_VID, pid, out _Handle);

            if (Connected)
            {
                _StartReader();
            }
        }

        public bool Connect()
        {
            if (Connected)
                return true;

            if (_Error)
                return false;

            _TryConnect(_PID); 

            return Connected;
        }

        public void Disconnect()
        {
            Connected = false;
        }


        private void _ReaderLoop()
        {
            var buff = new byte[_ReportLength];
            while (Connected)
            {
                int bytesRead;
                try
                {
                    bytesRead = CHIDApi.ReadTimeout(_Handle, ref buff, _ReportLength, 400);
                    if (bytesRead == -1)
                    {
                        Connected = false; //Disconnected
                        Connect();
                        break;
                    }
                }
                catch (Exception e)
                {
                    CLog.LogError("(BuzzLib) Error reading from device: " + e);
                    Connected = false;
                    break;
                }

                if (bytesRead > 0 && _ParseInputReport(buff))
                {
                    if (BuzzChanged != null)
                        BuzzChanged.Invoke(this, new CBuzzChangedEventArgs(_BuzzState));
                }
                Thread.Sleep(5);
            }
            CHIDApi.Close(_Handle);
        }

        private bool _ParseInputReport(byte[] buff)
        {
            _BuzzState.Buzzer1.red = (buff[2] & 0x01) != 0;
            _BuzzState.Buzzer1.yellow = (buff[2] & 0x02) != 0;
            _BuzzState.Buzzer1.green = (buff[2] & 0x04) != 0;
            _BuzzState.Buzzer1.orange = (buff[2] & 0x08) != 0;
            _BuzzState.Buzzer1.blue = (buff[2] & 0x10) != 0;
            _BuzzState.Buzzer2.red = (buff[2] & 0x20) != 0;
            _BuzzState.Buzzer2.yellow = (buff[2] & 0x40) != 0;
            _BuzzState.Buzzer2.green = (buff[2] & 0x80) != 0;
            _BuzzState.Buzzer2.orange = (buff[3] & 0x01) != 0;
            _BuzzState.Buzzer2.blue = (buff[3] & 0x02) != 0;
            _BuzzState.Buzzer3.red = (buff[3] & 0x04) != 0;
            _BuzzState.Buzzer3.yellow = (buff[3] & 0x08) != 0;
            _BuzzState.Buzzer3.green = (buff[3] & 0x10) != 0;
            _BuzzState.Buzzer3.orange = (buff[3] & 0x20) != 0;
            _BuzzState.Buzzer3.blue = (buff[3] & 0x40) != 0;
            _BuzzState.Buzzer4.red = (buff[3] & 0x80) != 0;
            _BuzzState.Buzzer4.yellow = (buff[4] & 0x01) != 0;
            _BuzzState.Buzzer4.green = (buff[4] & 0x02) != 0;
            _BuzzState.Buzzer4.orange = (buff[4] & 0x04) != 0;
            _BuzzState.Buzzer4.blue = (buff[4] & 0x08) != 0;
            return true;
        }

        private void _ClearReport()
        {
            Array.Clear(_Buff, 0, _ReportLength);
        }
       
    }
}
