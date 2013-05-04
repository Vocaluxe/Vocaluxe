using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using VocaluxeLib.Menu;

namespace Vocaluxe.Lib.Input
{
    public class CControllerFramework : IController
    {
        private List<SKeyEvent> _KeysPool;
        private List<SKeyEvent> _CurrentKeysPool;
        private readonly Object _KeyCopyLock = new Object();

        private List<SMouseEvent> _MousePool;
        private List<SMouseEvent> _CurrentMousePool;
        private readonly Object _MouseCopyLock = new Object();

        public virtual void Init()
        {
            _KeysPool = new List<SKeyEvent>();
            _CurrentKeysPool = new List<SKeyEvent>();
            _MousePool = new List<SMouseEvent>();
            _CurrentMousePool = new List<SMouseEvent>();
        }

        public virtual void Close()
        {
        }

        public virtual void Connect()
        {
        }

        public virtual void Disconnect()
        {
        }

        public virtual bool IsConnected()
        {
            return true;
        }

        public virtual void Update()
        {
            lock (_KeyCopyLock)
            {
                foreach (SKeyEvent e in _KeysPool)
                    _CurrentKeysPool.Add(e);
                _KeysPool.Clear();
            }

            lock (_MouseCopyLock)
            {
                foreach (SMouseEvent e in _MousePool)
                    _CurrentMousePool.Add(e);
                _MousePool.Clear();
            }
        }

        public virtual bool PollKeyEvent(ref SKeyEvent keyEvent)
        {
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
            if (_CurrentMousePool.Count > 0)
            {
                mouseEvent = _CurrentMousePool[0];
                _CurrentMousePool.RemoveAt(0);
                return true;
            }
            return false;
        }

        public virtual void SetRumble(float duration)
        {
        }

        public void AddKeyEvent(SKeyEvent keyEvent)
        {
            lock (_KeyCopyLock)
            {
                _KeysPool.Add(keyEvent);
            }
        }

        public void AddMouseEvent(SMouseEvent mouseEvent)
        {
            lock (_MousePool)
            {
                _MousePool.Add(mouseEvent);
            }
        }
    }
}
