using System;
using System.Collections.Generic;

namespace WorkflowApi.Core
{
    internal class Unsubscriber<T> : IDisposable
    {
        private List<IObserver<T>> _Observers;
        private IObserver<T> _Observer;

        internal Unsubscriber(List<IObserver<T>> observers, IObserver<T> observer)
        {
            _Observers = observers;
            _Observer = observer;
        }

        public void Dispose()
        {
            if (_Observers.Contains(_Observer))
            {
                _Observers.Remove(_Observer);
            }
        }
    }
}
