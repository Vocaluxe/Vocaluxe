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
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using VocaluxeLib;

namespace Vocaluxe.Lib.Input.Buzz
{
    class CBuzz : CControllerFramework
    {
        private CBuzzLib _Buzz;

        private bool[] _oldState;
        private bool _Connected;

        private Thread _HandlerThread;
        private bool _Active;
        private AutoResetEvent _EvTerminate;

        public override string GetName()
        {
            return "Buzz";
        }

        public override bool Init()
        {
            if (!base.Init())
                return false;

            _HandlerThread = new Thread(_MainLoop) { Name = "Buzz", Priority = ThreadPriority.BelowNormal };
            _oldState = new bool[20];
            _EvTerminate = new AutoResetEvent(false);
            return true;
        }

        public override void Close()
        {
            _Active = false;
            if (_HandlerThread != null)
            {
                //Join before freeing stuff
                //This also ensures, that no other thread is created till the current one is terminated
                _HandlerThread.Join();
                _HandlerThread = null;
            }
            base.Close();
        }

        public override void Connect()
        {
            if (_Active || _HandlerThread == null)
                return;
            _Active = true;
            _HandlerThread.Start();
        }

        public override void Disconnect()
        {
            Close();
        }

        public override bool IsConnected()
        {
            return _Connected;
        }

        public override void SetRumble(float duration)
        {
            //no Rumble, so nothing to do
        }

        public override void SetLEDs(bool led1, bool led2, bool led3, bool led4)
        {
            _Buzz.SetLEDs(led1, led2, led3, led4);
        }
        
        private void _MainLoop()
        {
            _Buzz = new CBuzzLib();
            _Buzz.BuzzChanged += _BuzzChanged;

            while (_Active)
            {
                Thread.Sleep(5);
                if (!_Buzz.Connected)
                {
                    if (!_DoConnect())
                        _EvTerminate.WaitOne(1000);
                }
            }

            _Buzz.Disconnect();
            _Connected = false;
        }

        private bool _DoConnect()
        {
            try
            {
                if (!_Buzz.Connect())
                    return false;
            }
            catch
            {
                return false;
            }

            
            _Buzz.SetLEDs(true, true, true, true);                        
            Thread.Sleep(250);
            _Buzz.SetLEDs(false, false, false, false);
            Thread.Sleep(250);
            _Buzz.SetLEDs(true, true, true, true);
            Thread.Sleep(250);
            _Buzz.SetLEDs(false, false, false, false);
            Thread.Sleep(250);
            _Buzz.SetLEDs(true, true, true, true);
            Thread.Sleep(250);
            _Buzz.SetLEDs(false, false, false, false);

            _Connected = true;
            return true;
        }

        private void _BuzzChanged(object sender, CBuzzChangedEventArgs args)
        {
            if (!_Active)
                return;

            CBuzzStatus bs = args.BuzzState;
            List<Keys> key = new List<Keys>();
            if ((!bs.Buzzer1.red) && (_oldState[0]))
                key.Add(Keys.D1);
            if ((!bs.Buzzer1.blue) && (_oldState[1]))
                key.Add(Keys.D2);
            if ((!bs.Buzzer1.orange) && (_oldState[2]))
                key.Add(Keys.D3);
            if ((!bs.Buzzer1.green) && (_oldState[3]))
                key.Add(Keys.D4);
            if ((!bs.Buzzer1.yellow) && (_oldState[4]))
                key.Add(Keys.D5);
            if ((!bs.Buzzer2.red) && (_oldState[5]))
                key.Add(Keys.Q);
            if ((!bs.Buzzer2.blue) && (_oldState[6]))
                key.Add(Keys.W);
            if ((!bs.Buzzer2.orange) && (_oldState[7]))
                key.Add(Keys.E);
            if ((!bs.Buzzer2.green) && (_oldState[8]))
                key.Add(Keys.R);
            if ((!bs.Buzzer2.yellow) && (_oldState[9]))
                key.Add(Keys.T);
            if ((!bs.Buzzer3.red) && (_oldState[10]))
                key.Add(Keys.A);
            if ((!bs.Buzzer3.blue) && (_oldState[11]))
                key.Add(Keys.S);
            if ((!bs.Buzzer3.orange) && (_oldState[12]))
                key.Add(Keys.D);
            if ((!bs.Buzzer3.green) && (_oldState[13]))
                key.Add(Keys.F);
            if ((!bs.Buzzer3.yellow) && (_oldState[14]))
                key.Add(Keys.G);
            if ((!bs.Buzzer4.red) && (_oldState[15]))
                key.Add(Keys.Y);
            if ((!bs.Buzzer4.blue) && (_oldState[16]))
                key.Add(Keys.X);
            if ((!bs.Buzzer4.orange) && (_oldState[17]))
                key.Add(Keys.C);
            if ((!bs.Buzzer4.green) && (_oldState[18]))
                key.Add(Keys.V);
            if ((!bs.Buzzer4.yellow) && (_oldState[19]))
                key.Add(Keys.B);

            foreach (Keys k in key)
                AddKeyEvent(new SKeyEvent(ESender.Buzz, false, false, false, false, char.MinValue, k));

            _oldState[0] = bs.Buzzer1.red;
            _oldState[1] = bs.Buzzer1.blue;
            _oldState[2] = bs.Buzzer1.orange;
            _oldState[3] = bs.Buzzer1.green;
            _oldState[4] = bs.Buzzer1.yellow;
            _oldState[5] = bs.Buzzer2.red;
            _oldState[6] = bs.Buzzer2.blue;
            _oldState[7] = bs.Buzzer2.orange;
            _oldState[8] = bs.Buzzer2.green;
            _oldState[9] = bs.Buzzer2.yellow;
            _oldState[10] = bs.Buzzer3.red;
            _oldState[11] = bs.Buzzer3.blue;
            _oldState[12] = bs.Buzzer3.orange;
            _oldState[13] = bs.Buzzer3.green;
            _oldState[14] = bs.Buzzer3.yellow;
            _oldState[15] = bs.Buzzer4.red;
            _oldState[16] = bs.Buzzer4.blue;
            _oldState[17] = bs.Buzzer4.orange;
            _oldState[18] = bs.Buzzer4.green;
            _oldState[19] = bs.Buzzer4.yellow;
        }
    }
}
