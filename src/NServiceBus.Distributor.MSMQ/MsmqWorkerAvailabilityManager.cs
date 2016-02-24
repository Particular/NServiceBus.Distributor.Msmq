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
    internal class MsmqWorkerAvailabilityManager : IWorkerAvailabilityManager, IDisposable
    {
        public MsmqWorkerAvailabilityManager()
        {
            var storageQueueAddress = Address.Local.SubScope("distributor.storage");
            var path = MsmqUtilities.GetFullPath(storageQueueAddress);
            var messageReadPropertyFilter = new MessagePropertyFilter
            {
                Id = true,
                Label = true,
                ResponseQueue = true,
            };

            storageQueue = new MessageQueue(path, false, true, QueueAccessMode.SendAndReceive)
            {
                MessageReadPropertyFilter = messageReadPropertyFilter
            };

            if ((!storageQueue.Transactional) && (SettingsHolder.Get<bool>("Transactions.Enabled")))
            {
                throw new Exception(string.Format("Queue [{0}] must be transactional.", path));
            }
        }

        /// <summary>
        ///     Msmq unit of work to be used in non DTC mode <see cref="MsmqUnitOfWork" />.
        /// </summary>
        public MsmqUnitOfWork UnitOfWork { get; set; }

        public void Dispose()
        {
            //Injected
        }

        /// <summary>
        ///     Pops the next available worker from the available worker queue
        ///     and returns its address.
        /// </summary>
        [DebuggerNonUserCode]
        public Worker NextAvailableWorker()
        {
            try
            {
                Message availableWorker;

                if (!storageLock.TryEnterReadLock(MaxTimeToWaitForAvailableWorker))
                {
                    return null;
                }

                try
                {
                    if (UnitOfWork.HasActiveTransaction())
                    {
                        availableWorker = storageQueue.Receive(MaxTimeToWaitForAvailableWorker, UnitOfWork.Transaction);
                    }
                    else
                    {
                        availableWorker = storageQueue.Receive(MaxTimeToWaitForAvailableWorker, MessageQueueTransactionType.Automatic);
                    }
                }
                finally
                {
                    storageLock.ExitReadLock();
                }

                if (availableWorker == null)
                {
                    return null;
                }

                var address = MsmqUtilities.GetIndependentAddressForQueue(availableWorker.ResponseQueue);
                var sessionId = availableWorker.Label;

                if (String.IsNullOrEmpty(sessionId)) //Old worker
                {
                    Logger.InfoFormat("Using an old version Worker at '{0}'.", address);
                    return new Worker(address, sessionId);
                }

                return new Worker(address, sessionId);
            }
            catch (MessageQueueException e) when (e.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
            {
                Logger.Debug("NextAvailableWorker IOTimeout");
                return null;
            }
        }

        public void WorkerAvailable(Worker worker)
        {
            var address = worker.Address;

            if (disconnectedWorkers.Contains(address))
            {
                // Drop ready message as this worker has been disconnected 
                Logger.InfoFormat("Worker at '{0}' has been diconnected. Not adding a storage entry.", address);
                return;
            }

            Logger.InfoFormat("Worker at '{0}' is available to take on more work.", worker.Address);

            AddWorkerToStorageQueue(worker);
        }

        public void UnregisterWorker(Address address)
        {
            disconnectedWorkers.Add(address);
        }

        public void RegisterNewWorker(Worker worker, int capacity)
        {
            // Need to handle backwards compatibility
            if (worker.SessionId == String.Empty)
            {
                ClearAvailabilityForWorker(worker.Address);
            }

            AddWorkerToStorageQueue(worker, capacity);

            disconnectedWorkers.Remove(worker.Address);

            Logger.InfoFormat("Worker at '{0}' has been registered with {1} capacity.", worker.Address, capacity);
        }

        void ClearAvailabilityForWorker(Address address)
        {
            storageLock.EnterWriteLock();

            try
            {
                var messages = storageQueue
                    .GetAllMessages()
                    .Where(m => MsmqUtilities.GetIndependentAddressForQueue(m.ResponseQueue) == address)
                    .ToList();

                foreach (var m in messages)
                {
                    if (UnitOfWork.HasActiveTransaction())
                    {
                        storageQueue.ReceiveById(m.Id, UnitOfWork.Transaction);
                    }
                    else
                    {
                        storageQueue.ReceiveById(m.Id, MessageQueueTransactionType.Automatic);
                    }
                }

                Logger.InfoFormat("Cleared {1} storage entries for worker {0}", address, messages.Count);
            }
            finally
            {
                storageLock.ExitWriteLock();
            }
        }

        void AddWorkerToStorageQueue(Worker worker, int capacity = 1)
        {
            using (var returnAddress = new MessageQueue(MsmqUtilities.GetFullPath(worker.Address), false, true, QueueAccessMode.Send))
            {
                var message = new Message
                {
                    Label = worker.SessionId,
                    ResponseQueue = returnAddress
                };

                for (var i = 0; i < capacity; i++)
                {
                    if (UnitOfWork.HasActiveTransaction())
                    {
                        storageQueue.Send(message, UnitOfWork.Transaction);
                    }
                    else
                    {
                        storageQueue.Send(message, MessageQueueTransactionType.Automatic);
                    }
                }
            }
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(MsmqWorkerAvailabilityManager));

        static TimeSpan MaxTimeToWaitForAvailableWorker = TimeSpan.FromSeconds(10);
        ReaderWriterLockSlim storageLock = new ReaderWriterLockSlim();
        HashSet<Address> disconnectedWorkers = new HashSet<Address>();
        MessageQueue storageQueue;
    }
}