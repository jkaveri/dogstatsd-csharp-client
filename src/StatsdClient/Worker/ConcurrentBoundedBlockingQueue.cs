using System;
using System.Threading;

namespace StatsdClient.Worker
{
    /// <summary>
    /// ConcurrentBoundedBlockingQueue is the same as ConcurrentBoundedQueue but 
    /// it waits for `waitTimeout` before dropping the value when the queue is full.
    /// </summary>
    class ConcurrentBoundedBlockingQueue<T> : ConcurrentBoundedQueue<T>
    {
        readonly IManualResetEvent _queueIsFullEvent;
        readonly TimeSpan _waitTimeout;

        public ConcurrentBoundedBlockingQueue(IManualResetEvent queueIsFullEvent, TimeSpan waitTimeout, int maxItemCount)
                : base(maxItemCount)
        {
            _queueIsFullEvent = queueIsFullEvent;
            _waitTimeout = waitTimeout;
            _queueIsFullEvent.Reset();
        }

        public override bool TryEnqueue(T value)
        {
            while (!base.TryEnqueue(value))
            {
                if (!_queueIsFullEvent.Wait(_waitTimeout))
                    return false;
                _queueIsFullEvent.Reset();
            }
            return true;
        }

        public override bool TryDequeue(out T value)
        {
            if (base.TryDequeue(out value))
            {
                _queueIsFullEvent.Set();
                return true;
            }
            value = default(T);
            return false;
        }
    }
}
