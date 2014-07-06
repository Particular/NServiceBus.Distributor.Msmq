namespace NServiceBus.Distributor.MSMQ.QueueCreators
{
    using Unicast.Queuing;

    /// <summary>
    ///     Signal to create the queue to store worker availability information.
    /// </summary>
    internal class DistributorStorageQueueCreator : IWantQueueCreated
    {
        /// <summary>
        ///     Holds storage queue address.
        /// </summary>
        public DistributorStorageQueueCreator(Configure config)
        {
            disabled = !config.Configurer.HasComponent<MsmqWorkerAvailabilityManager>();

            if (disabled)
            {
                return;
            }

            address = Address.Local.SubScope("distributor.storage");
        }

        public bool ShouldCreateQueue()
        {
            return !disabled;
        }

        /// <summary>
        ///     Address of Distributor storage queue.
        /// </summary>
        public Address Address
        {
            get { return address; }
        }

        Address address;
        bool disabled;
    }
}