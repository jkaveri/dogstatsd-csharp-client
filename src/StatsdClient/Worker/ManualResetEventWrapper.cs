using System;
using System.Threading;

namespace StatsdClient.Worker
{
    /// <summary>
    /// Simple wrapper arround ManualResetEventSlim.
    /// </summary>
    class ManualResetEventWrapper : IManualResetEvent
    {
        readonly ManualResetEventSlim _manualResetEvent = new ManualResetEventSlim(false);

        public void Reset()
        {
            _manualResetEvent.Reset();
        }

        public void Set()
        {
            _manualResetEvent.Set();
        }

        public bool Wait(TimeSpan duration)
        {
            return _manualResetEvent.Wait(duration);
        }
    }
}