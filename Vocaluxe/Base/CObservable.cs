using System;

namespace Vocaluxe.Base
{
    class CObservable
    {
        public event EventHandler ObjectChanged = delegate { };
        protected bool _Changed = true;

        protected void _NotifyObservers()
        {
            ObjectChanged.Invoke(this, null);
        }

        protected void _SetChanged()
        {
            _Changed = true;
            _NotifyObservers();
        }
    }
}