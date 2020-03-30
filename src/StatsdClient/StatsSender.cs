using System;
using System.Net;
using System.Net.Sockets;
using Mono.Unix;
using System.Threading.Tasks;

namespace StatsdClient
{
    class StatsSender : IStatsSender, IDisposable
    {
        readonly Socket _socket;
        readonly int _noBufferSpaceAvailableRetryCount;

        static readonly TimeSpan NoBufferSpaceAvailableWait = TimeSpan.FromMilliseconds(10);

        public static StatsSender CreateUDPStatsSender(IPEndPoint endPoint)
        {
            return new StatsSender(StatsSenderTransport.UDP,
                                   endPoint,
                                   AddressFamily.InterNetwork,
                                   ProtocolType.Udp,
                                   null);
        }

        public static StatsSender CreateUnixDomainSocketStatsSender(UnixEndPoint endPoint,
                                                                    TimeSpan? udsBufferFullBlockDuration)
        {
            return new StatsSender(StatsSenderTransport.UDS,
                                   endPoint,
                                   AddressFamily.Unix,
                                   ProtocolType.IP,
                                   udsBufferFullBlockDuration);
        }

        StatsSender(
            StatsSenderTransport transport,
            EndPoint endPoint,
            AddressFamily addressFamily,
            ProtocolType protocolType,
            TimeSpan? bufferFullBlockDuration)
        {
            TransportType = transport;
            if (bufferFullBlockDuration.HasValue)
            {
                _noBufferSpaceAvailableRetryCount = (int)(bufferFullBlockDuration.Value.TotalMilliseconds
                    / NoBufferSpaceAvailableWait.TotalMilliseconds);
            }

            try
            {
                _socket = new Socket(addressFamily, SocketType.Dgram, protocolType);
            }
            catch (SocketException e)
            {
                string transportStr;
                switch (transport)
                {
                    case StatsSenderTransport.UDP: transportStr = "Unix domain socket"; break;
                    case StatsSenderTransport.UDS: transportStr = "UDP"; break;
                    default: transportStr = transport.ToString(); break;                    
                }
                throw new NotSupportedException($"{transportStr} is not supported on your operating system.", e);
            }

            // When closing, wait 2 seconds to send data.
            _socket.LingerState = new LingerOption(true, 2);
            _socket.Connect(endPoint);
        }
        
        public StatsSenderTransport TransportType { get; }

        public bool Send(byte[] buffer, int length)
        {
            for (int i = 0; i < 1 + _noBufferSpaceAvailableRetryCount; ++i)
            {
                try
                {
                    _socket.Send(buffer, 0, length, SocketFlags.None);
                    return true;
                }
                catch (SocketException e) when (e.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                {
                    Task.Delay(NoBufferSpaceAvailableWait).Wait();
                }
            }
            return false;
        }

        public void Dispose()
        {
            _socket.Dispose();
        }
    }
}