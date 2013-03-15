using System;
using System.Collections.Generic;
using System.Text;

namespace Multicast
{
    //FIXME: remove this for .NET4
    public interface IObserver<T>
    {
        void OnNext(T next);
        void OnError(Exception ex);
        void OnCompleted();
    }

    class DummyObserver : IObserver<IClientData>
    {
        public void OnNext(IClientData next) { }

        public void OnError(Exception ex) { }

        public void OnCompleted() { }
    }
}
