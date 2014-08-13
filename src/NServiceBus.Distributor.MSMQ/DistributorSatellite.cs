namespace NServiceBus.Distributor.MSMQ
{
    using System;
    using Logging;
    using NServiceBus.ObjectBuilder;
    using ReadyMessages;
    using Satellites;
    using Settings;
    using Transports;
    using Unicast;
    using Unicast.Transport;

    /// <summary>
    ///     Provides functionality for distributing messages from a bus
    ///     to multiple workers when using a unicast transport.
    /// </summary>
    class DistributorSatellite : IAdvancedSatellite
    {
        readonly ISendMessages messageSender;
        readonly IWorkerAvailabilityManager workerManager;

        public DistributorSatellite(IBuilder builder, ReadOnlySettings settings)
        {
            disable = !settings.GetOrDefault<bool>("Distributor.Enabled");

            if (disable)
            {
                return;
            }

            messageSender = builder.Build<ISendMessages>();
            workerManager = builder.Build<IWorkerAvailabilityManager>();
            address = MasterNodeConfiguration.GetMasterNodeAddress(settings);
        }

        /// <summary>
        ///     The <see cref="address" /> for this <see cref="ISatellite" /> to use when receiving messages.
        /// </summary>
        public Address InputAddress
        {
            get { return address; }
        }

        /// <summary>
        ///     Set to <code>true</code> to disable this <see cref="ISatellite" />.
        /// </summary>
        public bool Disabled
        {
            get { return disable; }
        }

        /// <summary>
        ///     Starts the Distributor.
        /// </summary>
        public void Start()
        {
        }

        /// <summary>
        ///     Stops the Distributor.
        /// </summary>
        public void Stop()
        {
        }

        public Action<TransportReceiver> GetReceiverCustomization()
        {
            return receiver =>
            {
                //we don't need any DTC for the distributor
                receiver.TransactionSettings.SuppressDistributedTransactions = true;
                receiver.TransactionSettings.DoNotWrapHandlersExecutionInATransactionScope = true;
            };
        }

        /// <summary>
        ///     This method is called when a message is available to be processed.
        /// </summary>
        /// <param name="message">The <see cref="TransportMessage" /> received.</param>
        public bool Handle(TransportMessage message)
        {
            var worker = workerManager.NextAvailableWorker();

            if (worker == null)
            {
                return false;
            }

            Logger.DebugFormat("Forwarding message to '{0}'.", worker.Address);
            messageSender.Send(message, new SendOptions(worker.Address));
            return true;
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(DistributorSatellite));

        readonly Address address;
        readonly bool disable;
    }
}