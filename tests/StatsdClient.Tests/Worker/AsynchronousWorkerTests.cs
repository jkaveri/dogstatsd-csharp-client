using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Moq;
using System.Threading.Tasks;
using StatsdClient.Worker;
using System.Linq;
using System.Collections.Concurrent;

namespace Tests
{
    [TestFixture]
    public class AsynchronousWorkerTests
    {
        Mock<IAsynchronousWorkerHandler<int>> _handler;

        Mock<IWaiter> _waiter;
        readonly List<AsynchronousWorker<int>> _workers = new List<AsynchronousWorker<int>>();

        [SetUp]
        public void Init()
        {
            _handler = new Mock<IAsynchronousWorkerHandler<int>>();
            _waiter = new Mock<IWaiter>();
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (var worker in _workers)
                worker.Dispose();
            _workers.Clear();
        }

        [Test]
        public void TryEnqueue()
        {
            var valueReceived = new ManualResetEvent(false);

            _handler.Setup(h => h.OnNewValue(42)).Callback(() => valueReceived.Set());
            var worker = CreateWorker();
            Assert.IsTrue(worker.TryEnqueue(42));
            Assert.IsTrue(valueReceived.WaitOne(TimeSpan.FromSeconds(3)));
        }

        [Test, Timeout(30000)]
        public async Task OnIdle()
        {
            var waitDurationQueue = new ConcurrentQueue<TimeSpan>();

            _handler.Setup(h => h.OnIdle()).Returns(true);
            _waiter.Setup(w => w.Wait(It.IsAny<TimeSpan>()))
                   .Callback<TimeSpan>(t => waitDurationQueue.Enqueue(t));

            using (var worker = CreateWorker(workerThreadCount: 1))
            {
                while (waitDurationQueue.Count() < 100)
                    await Task.Delay(TimeSpan.FromMilliseconds(1));
            }

            var waitDurations = new List<TimeSpan>(waitDurationQueue);

            Assert.GreaterOrEqual(waitDurations.Min(), AsynchronousWorker<int>.MinWaitDuration);
            Assert.LessOrEqual(waitDurations.Max(), AsynchronousWorker<int>.MaxWaitDuration);
            Assert.That(waitDurations, Is.Ordered);
        }

        [Test, Timeout(2000)]
        public void DisposeNotBlock()
        {
            var worker = CreateWorker();
            // Check we do not block
            worker.Dispose();
        }
        AsynchronousWorker<int> CreateWorker(int workerThreadCount = 2)
        {
            var worker = new AsynchronousWorker<int>(
                _handler.Object,
                _waiter.Object,
                workerThreadCount,
                10,
                null);
            _workers.Add(worker);
            return worker;
        }
    }
}