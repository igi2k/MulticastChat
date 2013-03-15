using System;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;
using System.Diagnostics;

//FIXME: rewrite to use UDPClient, asynchronous handling
namespace Multicast
{

    public class MulticastService : IMulticastService
    {

        ObserverHandler _Observer;
        public event ObserverHandler Observer
        {
            add
            {
                if (_Observer != null)
                {
                    Delegate[] delegates = _Observer.GetInvocationList();
                    foreach(Delegate d in delegates){
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

        internal static readonly object startLock = new object();

        public const int DEFAULT_PORT = 8000;

        readonly IPEndPoint localEndPoint;
        readonly IPAddress multicastGroup;

        internal Socket socket;
        Thread thread;

        readonly static DummyObserver dummyObserver = new DummyObserver();

        public MulticastService(string group = null, int port = DEFAULT_PORT, IPAddress address = null)
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

        IObserver<IClientData> OnEvent()
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

        public bool Start(bool ignoreLocalAddress = true)
        {
            lock (startLock)
            {
                if (thread != null)
                {
                    Debug.WriteLine("Server is already running.");
                }
                else
                {
                    //TODO: use parametrized thread start?
                    MulticastListener code = new MulticastListener(ref socket, localEndPoint, multicastGroup, OnEvent, ignoreLocalAddress);
                    thread = new Thread(new ThreadStart(code.Start));
                    thread.Name = "MCS-" + thread.ManagedThreadId;
                    thread.Start();
                    if (Monitor.Wait(startLock, 3000))
                    {
                        Debug.WriteLine("Server started...");
                        return true;
                    }
                    else
                    {
                        //timeout
                        Debug.WriteLine("Server didn't start within 3 sec.");
                        Stop();
                    }
                }
                return false;
            }
        }

        public bool Stop(bool onRegistered = true)
        {
            lock (startLock)
            {
                if (thread != null)
                {
                    if (!onRegistered && (_Observer != null && _Observer.GetInvocationList().Length > 0))
                    {
                        Debug.WriteLine("Server has registered event(s)");
                    }
                    else
                    {
                        thread.Abort();
                        Debug.WriteLine("Stop initiated...");
                        if (socket != null)
                        {
                            //leave multicast group
                            if (socket.IsBound && multicastGroup != null)
                            {
                                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DropMembership, new MulticastOption(multicastGroup, localEndPoint.Address));
                            }
                            socket.Close();
                            socket = null;
                        }
                        thread.Join();
                        thread = null;
                        Debug.WriteLine("Server down.");
                        return true;
                    }
                }
                else
                {
                    Debug.WriteLine("Not running.");
                }
            }
            return false;
        }

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

        public void Send(IPAddress address, byte[] data, int offset, int size)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            address = (address == null) ? multicastGroup : address;
            IPEndPoint ipep = new IPEndPoint((address == null) ? IPAddress.Loopback : address, localEndPoint.Port);
            socket.Connect(ipep);
            socket.Send(data, offset, size, SocketFlags.None);
            socket.Close();
        }

        [Obsolete("debug method")]
        public static void Send(IPAddress address, byte[] data, int port)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint ipep = new IPEndPoint(address, port);
            socket.Connect(ipep);
            socket.Send(data, 0, data.Length, SocketFlags.None);
            socket.Close();
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
    }
}
