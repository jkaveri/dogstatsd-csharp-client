namespace StatsdClient
{
    enum StatsSenderTransport
    {
        UDS,
        UDP
    }

    internal interface IStatsSender
    {
        bool Send(byte[] buffer, int length);
        StatsSenderTransport TransportType { get; }
    }
}