using System;
using System.Net;
using System.Net.Sockets;

namespace Multicast
{

    public interface IClientData
    {
        byte[] data { get; }

        IPEndPoint GetSource();

        SocketFlags flags { get; }
    }

    class ClientDataEx : IClientData
    {
        readonly IPEndPoint source;
        readonly SocketFlags flags;
        readonly byte[] data;

        internal ClientDataEx(byte[] data, IPEndPoint source, SocketFlags flags)
        {
            this.data = data;
            this.source = source;
            this.flags = flags;
        }

        public IPEndPoint GetSource()
        {
            return source;
        }

        byte[] IClientData.data
        {
            get { return data; }
        }

        SocketFlags IClientData.flags
        {
            get { return flags; }
        }
    }
}
