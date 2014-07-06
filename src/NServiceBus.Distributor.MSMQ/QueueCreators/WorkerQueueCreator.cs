namespace NServiceBus.Distributor.MSMQ.QueueCreators
{
    using Unicast.Queuing;

    /// <summary>
    ///     Signal to create the queue for the worker
    /// </summary>
    internal class WorkerQueueCreator : IWantQueueCreated
    {
        // ReSharper disable UnassignedField.Compiler
#pragma warning disable 649
        public bool WorkerEnabled;

        public bool DistributorEnabled;
#pragma warning restore 649

        // ReSharper restore UnassignedField.Compiler
        public bool ShouldCreateQueue()
        {
            return DistributorEnabled && WorkerEnabled;
        }

        /// <summary>
        ///     Address of worker queue
        /// </summary>
        public Address Address
        {
            get { return Address.Local.SubScope("Worker"); }
        }
    }
}