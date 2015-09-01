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
using VocaluxeLib;

namespace Vocaluxe.Lib.Input
{
    public abstract class CControllerFramework : IController
    {
        protected bool _Initialized;
        private readonly List<SKeyEvent> _KeysPool = new List<SKeyEvent>();
        private List<SKeyEvent> _CurrentKeysPool;

        private readonly List<SMouseEvent> _MousePool = new List<SMouseEvent>();
        private List<SMouseEvent> _CurrentMousePool;

        public abstract string GetName();

        public virtual bool Init()
        {
            if (_Initialized)
                return false;
            _CurrentKeysPool = new List<SKeyEvent>();
            _CurrentMousePool = new List<SMouseEvent>();
            _Initialized = true;
            return true;
        }

        public virtual void Close()
        {
            if (!_Initialized)
                return;
            _CurrentKeysPool = null;
            _CurrentMousePool = null;
            lock (_KeysPool)
                _KeysPool.Clear();
            lock (_MousePool)
                _MousePool.Clear();
            _Initialized = false;
        }

        public abstract void Connect();

        public abstract void Disconnect();

        public abstract bool IsConnected();

        public virtual void Update()
        {
            if (!_Initialized)
                return;
            lock (_KeysPool)
            {
                foreach (SKeyEvent e in _KeysPool)
                    _CurrentKeysPool.Add(e);
                _KeysPool.Clear();
            }

            lock (_MousePool)
            {
                foreach (SMouseEvent e in _MousePool)
                    _CurrentMousePool.Add(e);
                _MousePool.Clear();
            }
        }

        public virtual bool PollKeyEvent(ref SKeyEvent keyEvent)
        {
            if (!_Initialized)
                return false;
            if (_CurrentKeysPool.Count > 0)
            {
                keyEvent = _CurrentKeysPool[0];
                _CurrentKeysPool.RemoveAt(0);
                return true;
            }
            return false;
        }

        public virtual bool PollMouseEvent(ref SMouseEvent mouseEvent)
        {
            if (!_Initialized)
                return false;
            if (_CurrentMousePool.Count > 0)
            {
                mouseEvent = _CurrentMousePool[0];
                _CurrentMousePool.RemoveAt(0);
                return true;
            }
            return false;
        }

        public abstract void SetRumble(float duration);

        public abstract void SetLEDs(bool led1, bool led2, bool led3, bool led4);

        public void AddKeyEvent(SKeyEvent keyEvent)
        {
            if (!_Initialized)
                return;
            lock (_KeysPool)
            {
                _KeysPool.Add(keyEvent);
            }
        }

        public void AddMouseEvent(SMouseEvent mouseEvent)
        {
            if (!_Initialized)
                return;
            lock (_MousePool)
            {
                _MousePool.Add(mouseEvent);
            }
        }
    }
}