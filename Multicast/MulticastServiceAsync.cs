using System;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;

namespace Multicast
{
    public class MulticastServiceAsync : AbstractMulticastService
    {

        readonly object startLock = new object();

        Socket socket;

        class Disposing
        {
            public bool value;
            public Disposing(bool value)
            {
                this.value = value;
            }
            public static implicit operator bool(Disposing disposing)
            {
                return disposing.value;
            }
        }

        Disposing disposing;

        const int bufferSize = 512;
        byte[] buffer = new byte[bufferSize];


        public MulticastServiceAsync(string group = null, int port = DEFAULT_PORT, IPAddress address = null)
            : base(group, port, address) { }

        public override bool Start(bool ignoreLocalAddress = true)
        {
            lock (startLock)
            {
                if (socket != null)
                {
                    Debug.WriteLine("Server is already running.");
                }
                else
                {
                    try
                    {
                        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                        socket.Bind(localEndPoint);

                        if (multicastGroup != null)
                        {
                            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, (ignoreLocalAddress) ? 0 : 1);
                            //register group
                            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(multicastGroup, localEndPoint.Address));
                            //set TTL to local segment
                            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 0);
                        }
                        disposing = new Disposing(false);
                        BeginReceiveMessageFrom(ReceiveCallback, disposing);
                    }
                    catch (SocketException ex)
                    {
                        OnEvent().OnError(ex);
                        return false;
                    }
                    Debug.WriteLine("Server started...");
                    return true;
                }
                return false;
            }
        }

        public override bool Stop(bool onRegistered = true)
        {
            lock (startLock)
            {
                if (socket != null)
                {
                    if (!onRegistered && (hasListeners()))
                    {
                        Debug.WriteLine("Server has registered event(s)");
                    }
                    else
                    {
                        if (multicastGroup != null)
                        {
                            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DropMembership, new MulticastOption(multicastGroup, localEndPoint.Address));
                        }
                        disposing.value = true;
                        socket.Close();
                        socket = null;
                        OnEvent().OnCompleted();
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

        void BeginReceiveMessageFrom(AsyncCallback callback, object state)
        {
            EndPoint ep = EndPointAny;
            socket.BeginReceiveMessageFrom(buffer, 0, bufferSize, SocketFlags.None, ref ep, callback, state);
        }

        byte[] EndRecieveMessageFrom(IAsyncResult result, ref SocketFlags socketFlags, out IPEndPoint endPoint, out IPPacketInformation packetInfo)
        {
            EndPoint ep = EndPointAny;
            int size = socket.EndReceiveMessageFrom(result, ref socketFlags, ref ep, out packetInfo);

            byte[] data = new byte[size];
            Buffer.BlockCopy(buffer, 0, data, 0, size);

            BeginReceiveMessageFrom(ReceiveCallback, disposing);

            endPoint = (IPEndPoint)ep;
            return data;
        }

        void ReceiveCallback(IAsyncResult result)
        {
            Disposing stop = (Disposing)result.AsyncState;
            if (!stop)
            {
                IPEndPoint client;
                IPPacketInformation packetInfo;
                SocketFlags socketFlags = SocketFlags.None;

                byte[] data = EndRecieveMessageFrom(result, ref socketFlags, out client, out packetInfo);

                ClientDataEx clientData = new ClientDataEx(data, client, socketFlags);
                OnEvent().OnNext(clientData);
            }
        }

        public override void Send(IPAddress address, byte[] data, int offset, int size)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            address = (address == null) ? multicastGroup : address;
            IPEndPoint ipep = new IPEndPoint((address == null) ? IPAddress.Loopback : address, localEndPoint.Port);
            try
            {
                socket.Connect(ipep);
                socket.Send(data, offset, size, SocketFlags.None);
                socket.Close();
            }
            catch (SocketException ex)
            {
                OnEvent().OnError(ex);
            }
        }
    }
}
