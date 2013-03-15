using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;

namespace Multicast
{

    public abstract class AbstractMulticastService : IMulticastService
    {
        public const int DEFAULT_PORT = 8000;
        internal static IPEndPoint EndPointAny = new IPEndPoint(IPAddress.Any, 0);

        ObserverHandler _Observer;
        public event ObserverHandler Observer
        {
            add
            {
                if (_Observer != null)
                {
                    Delegate[] delegates = _Observer.GetInvocationList();
                    foreach (Delegate d in delegates)
                    {
                        if (d.Equals(value))
                        {
                            Debug.WriteLine("Already registered.");
                            return;
                        }
                    }
                }
                _Observer += value;
            }
            remove { _Observer -= value; }
        }

        protected readonly IPEndPoint localEndPoint;
        protected readonly IPAddress multicastGroup;

        readonly static DummyObserver dummyObserver = new DummyObserver();

        protected AbstractMulticastService(string group = null, int port = DEFAULT_PORT, IPAddress address = null)
        {
            if (group != null && !isValidMulticastAddress(group))
            {
                throw new ArgumentException("Valid Multicast group addr: 224.0.0.0 - 239.255.255.255");
            }
            this.multicastGroup = (group == null) ? null : IPAddress.Parse(group);
            this.localEndPoint = new IPEndPoint(
                (address == null) ? IPAddress.Any : address,
                port
            );
        }

        protected bool hasListeners()
        {
            return (_Observer != null && _Observer.GetInvocationList().Length > 0);
        }

        protected IObserver<IClientData> OnEvent()
        {
            IObserver<IClientData> result = null;
            if (_Observer != null)
            {
                result = _Observer();
            }
            if (result == null)
            {
                result = dummyObserver;
            }
            return result;
        }

        static bool isValidMulticastAddress(string ip)
        {
            try
            {
                int octet1 = Int32.Parse(ip.Split(new Char[] { '.' }, 4)[0]);
                if ((octet1 >= 224) && (octet1 <= 239)) return true;
            }
            catch (Exception) { }
            return false;
        }

        public static bool isLocal(IPAddress address)
        {
            //Obtain a reference to all network interfaces in the machine
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in adapters)
            {
                IPInterfaceProperties properties = adapter.GetIPProperties();
                foreach (IPAddressInformation uniCast in properties.UnicastAddresses)
                {
                    if (address.Equals(uniCast.Address))
                    {
                        return true;
                    }
                }

            }
            return false;
        }

        abstract public bool Start(bool ignoreLocalAddress = true);

        abstract public bool Stop(bool onRegistered = true);

        public void Send(byte[] data)
        {
            Send(null, data);
        }

        public void Send(IPAddress address, byte[] data)
        {
            Send(address, data, 0, data.Length);
        }

        public void Send(byte[] data, int offset, int size)
        {
            Send(null, data, offset, size);
        }

        abstract public void Send(IPAddress address, byte[] data, int offset, int size);
    }
}
