using System.Net;
using System.Net.Sockets;

namespace Multicast
{

    public interface IClientData
    {
        byte[] data { get; }

        IPEndPoint GetSource();

        SocketFlags flags { get; }
        
        bool Local { get; }
    }

    class ClientDataEx : IClientData
    {
        readonly IPEndPoint _source;
        readonly SocketFlags _flags;
        readonly byte[] _data;

        internal ClientDataEx(byte[] data, IPEndPoint source, SocketFlags flags)
        {
            _data = data;
            _source = source;
            _flags = flags;
        }
        
        public IPEndPoint GetSource()
        {
            return _source;
        }

        byte[] IClientData.data
        {
            get { return _data; }
        }

        SocketFlags IClientData.flags
        {
            get { return _flags; }
        }
        
        bool IClientData.Local
        {
            get { return AbstractMulticastService.isLocal(_source.Address); }
        }
    }
}
