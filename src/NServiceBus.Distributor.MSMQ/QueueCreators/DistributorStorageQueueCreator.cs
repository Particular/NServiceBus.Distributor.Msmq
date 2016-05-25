namespace NServiceBus.Distributor.MSMQ.QueueCreators
{
    using ObjectBuilder;
    using Unicast.Queuing;

    // Signal to create the queue to store worker availability information.
    class DistributorStorageQueueCreator : IWantQueueCreated
    {
        // Holds storage queue address.
        public DistributorStorageQueueCreator(Configure config, IConfigureComponents container)
        {
            disabled = !container.HasComponent<MsmqWorkerAvailabilityManager>();

            if (disabled)
            {
                return;
            }

            Address = config.LocalAddress.SubScope("distributor.storage");
        }

        public bool ShouldCreateQueue()
        {
            return !disabled;
        }

        // Address of Distributor storage queue.
        public Address Address { get; }

        bool disabled;
    }
}