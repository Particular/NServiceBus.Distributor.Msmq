namespace NServiceBus.Distributor.MSMQ.QueueCreators
{
    using Unicast.Queuing;

    // Signal to create the queue for the worker
    class WorkerQueueCreator : IWantQueueCreated
    {
        public bool WorkerEnabled { get; set; }

        public bool DistributorEnabled { get; set; }

        public bool ShouldCreateQueue()
        {
            return DistributorEnabled && WorkerEnabled;
        }

        // Address of worker queue
        public Address Address { get; set; }
    }
}