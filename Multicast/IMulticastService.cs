using System;
using System.Net;

namespace Multicast
{
    public delegate IObserver<IClientData> ObserverHandler();

    public interface IMulticastService
    {
        event ObserverHandler Observer;

        bool Start(bool ignoreLocalAddress = true);
        bool Stop(bool onRegistered = true);

        void Send(byte[] data);
        void Send(IPAddress address, byte[] data);
        void Send(byte[] data, int offset, int size);
        void Send(IPAddress address, byte[] data, int offset, int size);
    }
}
