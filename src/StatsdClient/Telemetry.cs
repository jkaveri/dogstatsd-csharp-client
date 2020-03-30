using System;
using System.Collections.Generic;
using System.Threading;
using StatsdClient.Bufferize;
using static StatsdClient.Statsd;

namespace StatsdClient
{
    internal class Telemetry : IDisposable
    {
        private int _metricsSent;
        private int _eventsSent;
        private int _serviceChecksSent;
        private int _bytesSent;
        private int _bytesDropped;
        private int _packetsSent;
        private int _packetsDropped;
        private int _packetsDroppedQueue;

        private readonly Timer _timer;
        private readonly string[] _tags;
        private readonly IStatsSender _statsSender;

        private static string _telemetryPrefix = "datadog.dogstatsd.client.";

        public static string MetricsName = _telemetryPrefix + "metrics";
        public static string EventsName = _telemetryPrefix + "events";
        public static string ServiceCheckName = _telemetryPrefix + "service_checks";
        public static string BytesSentName = _telemetryPrefix + "bytes_sent";
        public static string BytesDroppedName = _telemetryPrefix + "bytes_dropped";
        public static string PacketsSentName = _telemetryPrefix + "packets_sent";
        public static string PacketsDroppedName = _telemetryPrefix + "packets_dropped";
        public static string PacketsDroppedQueueName = _telemetryPrefix + "packets_dropped_queue";


        public Telemetry() { }

        public Telemetry(string assemblyVersion, TimeSpan flushInterval, IStatsSender statsSender)
        {
            _statsSender = statsSender;

            string transport;
            switch (statsSender.TransportType)
            {
                case StatsSenderTransport.UDP: transport = "udp"; break;
                case StatsSenderTransport.UDS: transport = "uds"; break;
                default: transport = statsSender.TransportType.ToString(); break;
            };

            _tags = new[] { "client:csharp", $"client_version:{assemblyVersion}", $"client_transport:{transport}" };

            _timer = new Timer(o => Flush(),
                               null,
                               flushInterval,
                               flushInterval);
        }

        public void Flush()
        {
            SendMetric(MetricsName, Interlocked.Exchange(ref _metricsSent, 0));
            SendMetric(EventsName, Interlocked.Exchange(ref _eventsSent, 0));
            SendMetric(ServiceCheckName, Interlocked.Exchange(ref _serviceChecksSent, 0));
            SendMetric(BytesSentName, Interlocked.Exchange(ref _bytesSent, 0));
            SendMetric(BytesDroppedName, Interlocked.Exchange(ref _bytesDropped, 0));
            SendMetric(PacketsSentName, Interlocked.Exchange(ref _packetsSent, 0));
            SendMetric(PacketsDroppedName, Interlocked.Exchange(ref _packetsDropped, 0));
            SendMetric(PacketsDroppedQueueName, Interlocked.Exchange(ref _packetsDroppedQueue, 0));
        }

        void SendMetric(string metricName, int value)
        {
            var message = Statsd.Metric.GetCommand<Counting, int>(string.Empty,
                                                    metricName,
                                                    value,
                                                    1.0,
                                                    _tags);
            var bytes = BufferBuilder.GetBytes(message);
            _statsSender.Send(bytes, bytes.Length);
        }

        public void MetricSent() { Interlocked.Increment(ref _metricsSent); }
        public void EventSent() { Interlocked.Increment(ref _eventsSent); }
        public void ServiceCheckSent() { Interlocked.Increment(ref _serviceChecksSent); }

        public void PacketSent(int packetSize)
        {
            Interlocked.Increment(ref _packetsSent);
            Interlocked.Add(ref _bytesSent, packetSize);
        }

        public void PacketDropped(int packetSize)
        {
            Interlocked.Increment(ref _packetsDropped);
            Interlocked.Add(ref _bytesDropped, packetSize);
        }

        public void PacketsDroppedQueue() { Interlocked.Increment(ref _packetsDroppedQueue); }

        public void Dispose()
        {
            // Should we also dispose _statsSender ?
            _timer.Dispose();
        }
    }
}