namespace NServiceBus.Distributor.MSMQ.QueueCreators
{
    using ObjectBuilder.Common;
    using Unicast.Queuing;

    /// <summary>
    ///     Signal to create the queue to store worker availability information.
    /// </summary>
    internal class DistributorStorageQueueCreator : IWantQueueCreated
    {
        /// <summary>
        ///     Holds storage queue address.
        /// </summary>
        public DistributorStorageQueueCreator(Configure config, IContainer container)
        {
            disabled = !container.HasComponent(typeof(MsmqWorkerAvailabilityManager));

            if (disabled)
            {
                return;
            }

            address = config.LocalAddress.SubScope("distributor.storage");
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