using System;

namespace Vocaluxe.Base
{
    class CObservable
    {
        public event EventHandler ObjectChanged = delegate { };
        protected bool _Changed = true;

        protected void NotifyObservers()
        {
            ObjectChanged.Invoke(this, null);
        }

        protected void _SetChanged()
        {
            _Changed = true;
            NotifyObservers();
        }
    }
}