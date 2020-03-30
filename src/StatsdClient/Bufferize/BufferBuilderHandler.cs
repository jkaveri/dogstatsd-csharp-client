namespace StatsdClient.Bufferize
{
    internal class BufferBuilderHandler: IBufferBuilderHandler
    {
        private readonly Telemetry _telemetry;
        private readonly StatsSender _statsSender;

        public BufferBuilderHandler(
            Telemetry telemetry,
            StatsSender statsSender)
        {
            _telemetry = telemetry;
            _statsSender = statsSender;
        }

        public void Handle(byte[] buffer, int length)
        {
            if (_statsSender.Send(buffer, length))
            {
                _telemetry.PacketSent(length);
            } else {
                _telemetry.PacketDropped(length);
            }
        }        
    }
}