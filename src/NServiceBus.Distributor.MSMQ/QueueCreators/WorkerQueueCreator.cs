namespace NServiceBus.Distributor.MSMQ.QueueCreators
{
    using Unicast.Queuing;

    /// <summary>
    ///     Signal to create the queue for the worker
    /// </summary>
    class WorkerQueueCreator : IWantQueueCreated
    {
        public bool WorkerEnabled { get; set; }

        public bool DistributorEnabled { get; set; }

        public bool ShouldCreateQueue()
        {
            return DistributorEnabled && WorkerEnabled;
        }

        /// <summary>
        ///     Address of worker queue
        /// </summary>
        public Address Address { get; set; }
    }
}