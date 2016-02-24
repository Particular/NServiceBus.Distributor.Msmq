namespace NServiceBus.Distributor.MSMQ
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Messaging;
    using System.Threading;
    using Logging;
    using Transports.Msmq;

    /// <summary>
    ///     An implementation of <see cref="IWorkerAvailabilityManager" /> for MSMQ to be used
    ///     with the <see cref="DistributorSatellite" /> class.
    /// </summary>
    internal class MsmqWorkerAvailabilityManager : IWorkerAvailabilityManager, IDisposable
    {
        readonly Configure configure;
        MsmqUnitOfWork unitOfWork;

        public MsmqWorkerAvailabilityManager(Configure configure, MsmqUnitOfWork unitOfWork)
        {
            this.configure = configure;
            this.unitOfWork = unitOfWork;
        }

        public void Init()
        {
            lock (lockObj)
            {
                if (storageQueue != null)
                {
                    return;
                }

                var storageQueueAddress = configure.LocalAddress.SubScope("distributor.storage");
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

                var transactional = false;

                try
                {
                    transactional = storageQueue.Transactional;
                }
                catch (MessageQueueException ex)
                {
                    if (ex.MessageQueueErrorCode == MessageQueueErrorCode.QueueNotFound)
                    {
                        throw new Exception($"Queue [{path}] does not exist.");
                    }
                }

                if ((!transactional) && (configure.Settings.Get<bool>("Transactions.Enabled")))
                {
                    throw new Exception($"Queue [{path}] must be transactional.");
                }
            }
        }

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
                    if (unitOfWork.HasActiveTransaction())
                    {
                        availableWorker = storageQueue.Receive(MaxTimeToWaitForAvailableWorker, unitOfWork.Transaction);
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

                if (string.IsNullOrEmpty(sessionId)) //Old worker
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
                Logger.InfoFormat("Worker at '{0}' has been diconnected. Not adding a storage entry.", address);
                return;
            }

            Logger.InfoFormat("Worker at '{0}' is available to take on more work.", worker.Address);

            AddWorkerToStorageQueue(worker);
        }

        public void UnregisterWorker(Address address)
        {
            disconnectedWorkers.Add(address);
            ClearAvailabilityForWorker(address);
            Logger.Info($"Worker at '{address}' has been disconnected. All storage entries cleared.");
        }

        public void RegisterNewWorker(Worker worker, int capacity)
        {
            // Need to handle backwards compatibility
            if (worker.SessionId == string.Empty)
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
                    if (unitOfWork.HasActiveTransaction())
                    {
                        storageQueue.ReceiveById(m.Id, unitOfWork.Transaction);
                    }
                    else
                    {
                        storageQueue.ReceiveById(m.Id, MessageQueueTransactionType.Automatic);
                    }
                }

                Logger.Info($"Cleared {messages.Count} storage entries for worker {address}");
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
                    if (unitOfWork.HasActiveTransaction())
                    {
                        storageQueue.Send(message, unitOfWork.Transaction);
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
        object lockObj = new object();
    }
}