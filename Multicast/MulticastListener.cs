using System;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;

namespace Multicast
{
    class MulticastListener
    {
        readonly Socket socket;
        readonly IPAddress multicastGroup;
        readonly IPEndPoint localEndPoint;
        readonly bool ignoreLocal;

        const int bufferSize = 512;

        readonly ObserverHandler OnEvent;

        public MulticastListener(ref Socket socket, IPEndPoint localEndPoint, IPAddress multicastGroup, ObserverHandler OnEvent, bool ignoreLocal)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            this.socket = socket;
            this.multicastGroup = multicastGroup;
            this.localEndPoint = localEndPoint;
            this.OnEvent = OnEvent;
            this.ignoreLocal = ignoreLocal;
        }

        internal static IPEndPoint EndPointAny = new IPEndPoint(IPAddress.Any, 0);

        public void Start()
        {
            try
            {
                socket.Bind(localEndPoint);
            }
            catch (SocketException ex)
            {
                OnEvent().OnError(ex);
                return;
            }
            if (multicastGroup != null)
            {
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, (ignoreLocal) ? 0 : 1);
                //register group
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(multicastGroup, localEndPoint.Address));
                //set TTL to local segment
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 0);
            }
            //FIXME: unsafe, do better
            lock (MulticastService.startLock)
            {
                Monitor.Pulse(MulticastService.startLock);
            }
            byte[] buffer = new byte[bufferSize];
            int size;
            ClientDataEx clientData;
            try
            {
                do
                {
                    EndPoint source = EndPointAny;
                    IPPacketInformation packetInfo;
                    SocketFlags flags = SocketFlags.None;
                    //FIXME: processing should be async to reduce listening latency
                    //size = socket.ReceiveFrom(buffer, bufferSize, SocketFlags.None, ref source);
                    size = socket.ReceiveMessageFrom(buffer, 0, bufferSize, ref flags, ref source, out packetInfo);
                    IPEndPoint client = (IPEndPoint)source;
                    //if (!ignoreLocal || !MulticastService.isLocal(client.Address))
                    {
                        byte[] result = new byte[size];
                        //Array.Copy(buffer, result, size);
                        Buffer.BlockCopy(buffer, 0, result, 0, size);
                        clientData = new ClientDataEx(result, client, flags);
                        OnEvent().OnNext(clientData);
                    }
                } while (true);
            }
            catch (ThreadAbortException)
            {
                OnEvent().OnCompleted();
            }
            catch (Exception ex)
            {
                OnEvent().OnError(ex);
            }
        }
    }
}