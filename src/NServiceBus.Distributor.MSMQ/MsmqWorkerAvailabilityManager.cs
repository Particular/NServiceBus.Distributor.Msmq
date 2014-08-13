namespace NServiceBus.Distributor.MSMQ
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Messaging;
    using System.Threading;
    using Logging;
    using Settings;
    using Transports.Msmq;

    /// <summary>
    ///     An implementation of <see cref="IWorkerAvailabilityManager" /> for MSMQ to be used
    ///     with the <see cref="DistributorSatellite" /> class.
    /// </summary>
    internal class MsmqWorkerAvailabilityManager : IWorkerAvailabilityManager
    {
        /// <summary>
        ///     Pops the next available worker from the available worker queue
        ///     and returns its address.
        /// </summary>
        [DebuggerNonUserCode]
        public Worker NextAvailableWorker()
        {
            // Do we have at least one available worker?
            if (!registeredWorkerAddresses.Any())
            {
                return null;
            }

            // We have a list of registered workers. Round robin and return the available worker
            currentAvailableWorkerIndex = currentAvailableWorkerIndex % registeredWorkerAddresses.Count;
            var worker = registeredWorkerAddresses.ElementAt(currentAvailableWorkerIndex);
            Interlocked.Increment(ref currentAvailableWorkerIndex);
            return new Worker(worker); 
        }

        /// <summary>
        /// This method is no longer needed. It was relevant trying to record ready messages
        /// as they came in, after each worker sent a ready message after it processed the message.
        /// </summary>
        /// <param name="worker"></param>
        [ObsoleteEx(RemoveInVersion = "6.0", TreatAsErrorFromVersion = "6.0")]
        public void WorkerAvailable(Worker worker)
        {
            throw new NotImplementedException("This is obsolete. Workers will not send ready messages after processing each message");
        }

        /// <summary>
        /// Disconnect the worker
        /// </summary>
        /// <param name="address">Address of the worker</param>
        public void UnregisterWorker(Address address)
        {
            // Remove this worker from the list of available workers.
            registeredWorkerAddresses.RemoveAll(x => x == address);
        }

        /// <summary>
        /// Register the new worker
        /// </summary>
        /// <param name="worker">Address of the new worker</param>
        /// <param name="capacity">Number of concurrent messages the worker can process at one time</param>
        public void RegisterNewWorker(Worker worker, int capacity)
        {
            lock (lockObject)
            {
                for (var index = 0; index < capacity; index++)
                {
                    registeredWorkerAddresses.Add(worker.Address);
                }
            }
            Logger.InfoFormat("Worker at '{0}' has been registered with {1} capacity.", worker.Address, capacity);
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(MsmqWorkerAvailabilityManager));
        readonly object lockObject = new object();
        int currentAvailableWorkerIndex;
        List<Address> registeredWorkerAddresses = new List<Address>();
        
    }
}