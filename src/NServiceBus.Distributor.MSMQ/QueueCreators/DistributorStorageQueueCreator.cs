namespace NServiceBus.Distributor.MSMQ.QueueCreators
{
    using NServiceBus.ObjectBuilder;
    using Unicast.Queuing;

    /// <summary>
    ///     Signal to create the queue to store worker availability information.
    /// </summary>
    internal class DistributorStorageQueueCreator : IWantQueueCreated
    {
        /// <summary>
        ///     Holds storage queue address.
        /// </summary>
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

        /// <summary>
        ///     Address of Distributor storage queue.
        /// </summary>
        public Address Address { get; }

        bool disabled;
    }
}