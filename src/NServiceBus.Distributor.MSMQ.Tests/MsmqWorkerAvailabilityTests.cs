namespace NServiceBus.Distributor.MSMQ.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class MsmqWorkerAvailabilityTests
    {
        [Test]
        public void When_A_Worker_Is_Disconnected_Should_Not_Use_That_Worker()
        {
            var worker1 = new Worker(Address.Parse("worker1"));
            var worker2 = new Worker(Address.Parse("worker2"));
            var worker3 = new Worker(Address.Parse("worker3"));
            var worker4 = new Worker(Address.Parse("worker4"));
            var workerAvailabilityManager = new MsmqWorkerAvailabilityManager();
            workerAvailabilityManager.RegisterNewWorker(worker1, 5);
            workerAvailabilityManager.RegisterNewWorker(worker2, 5);
            workerAvailabilityManager.RegisterNewWorker(worker3, 5);
            workerAvailabilityManager.RegisterNewWorker(worker4, 5);
            workerAvailabilityManager.UnregisterWorker(Address.Parse("worker2"));

            for (var i = 0; i < 100; i++)
            {
                var worker = workerAvailabilityManager.NextAvailableWorker();
                Assert.IsNotNull(worker);
                Assert.AreNotEqual("worker2", worker.Address.Queue);
            }
        }


        [Test]
        public void When_Multiple_Workers_Are_Registered_Should_Round_Robin()
        {
            var worker1 = new Worker(Address.Parse("worker1"));
            var worker2 = new Worker(Address.Parse("worker2"));
            var workerAvailabilityManager = new MsmqWorkerAvailabilityManager();
            workerAvailabilityManager.RegisterNewWorker(worker1, 1);
            workerAvailabilityManager.RegisterNewWorker(worker2, 1);

            var worker = workerAvailabilityManager.NextAvailableWorker();
            Assert.AreEqual("worker1", worker.Address.Queue);

            worker = workerAvailabilityManager.NextAvailableWorker();
            Assert.AreEqual("worker2", worker.Address.Queue);

            worker = workerAvailabilityManager.NextAvailableWorker();
            Assert.AreEqual("worker1", worker.Address.Queue);
        }

        [Test]
        public void When_All_Workers_Are_Unregistered_Should_Return_Null()
        {
            var worker1 = new Worker(Address.Parse("worker1"));
            var worker2 = new Worker(Address.Parse("worker2"));
            var workerAvailabilityManager = new MsmqWorkerAvailabilityManager();
            workerAvailabilityManager.RegisterNewWorker(worker1, 5);
            workerAvailabilityManager.RegisterNewWorker(worker2, 5);
            workerAvailabilityManager.UnregisterWorker(Address.Parse("worker1"));
            workerAvailabilityManager.UnregisterWorker(Address.Parse("worker2"));

            var worker = workerAvailabilityManager.NextAvailableWorker();
            Assert.IsNull(worker);
        }

        [Test]
        public void When_Some_Workers_Are_Unregistered_Should_Round_Robin_Between_Available_Workers()
        {
            var worker1 = new Worker(Address.Parse("worker1"));
            var worker2 = new Worker(Address.Parse("worker2"));
            var worker3 = new Worker(Address.Parse("worker3"));
            var workerAvailabilityManager = new MsmqWorkerAvailabilityManager();
            workerAvailabilityManager.RegisterNewWorker(worker1, 10);
            workerAvailabilityManager.RegisterNewWorker(worker2, 1);
            workerAvailabilityManager.RegisterNewWorker(worker3, 1);
            workerAvailabilityManager.UnregisterWorker(Address.Parse("worker1"));

            var worker = workerAvailabilityManager.NextAvailableWorker();
            Assert.AreEqual("worker2", worker.Address.Queue);

            worker = workerAvailabilityManager.NextAvailableWorker();
            Assert.AreEqual("worker3", worker.Address.Queue);

            worker = workerAvailabilityManager.NextAvailableWorker();
            Assert.AreEqual("worker2", worker.Address.Queue);
        }

        [Test]
        public void When_Workers_Have_More_Capacity_Should_Round_Robin_Based_On_Capacity()
        {
            const int worker1Capacity = 3;
            const int worker2Capacity = 1;
            const int worker3Capacity = 2;

            var worker1 = new Worker(Address.Parse("worker1"));
            var worker2 = new Worker(Address.Parse("worker2"));
            var worker3 = new Worker(Address.Parse("worker3"));
            var workerAvailabilityManager = new MsmqWorkerAvailabilityManager();
            workerAvailabilityManager.RegisterNewWorker(worker1, 3);
            workerAvailabilityManager.RegisterNewWorker(worker2, 1);
            workerAvailabilityManager.RegisterNewWorker(worker3, 2);
            // For the first 3 messages, should use worker1
            for (var i = 0; i < worker1Capacity; i++)
            {
                var worker = workerAvailabilityManager.NextAvailableWorker();
                Assert.AreEqual("worker1", worker.Address.Queue);
            }

            // Next should use worker2, as capacity for worker2 is 1
            for (var i = 0; i < worker2Capacity; i++)
            {
                var worker = workerAvailabilityManager.NextAvailableWorker();
                Assert.AreEqual("worker2", worker.Address.Queue);
            }

            for (var i = 0; i < worker3Capacity; i++)
            {
                var worker = workerAvailabilityManager.NextAvailableWorker();
                Assert.AreEqual("worker3", worker.Address.Queue);
            }

            var nextAvailableWorker = workerAvailabilityManager.NextAvailableWorker();
            Assert.AreEqual("worker1", nextAvailableWorker.Address.Queue);
        }
    }
}

