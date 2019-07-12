namespace NServiceBus.Distributor.MSMQ
{
    using System;
    using Logging;
    using ObjectBuilder;
    using ReadyMessages;
    using Satellites;
    using Settings;
    using Transports;
    using Unicast;
    using Unicast.Transport;

    // Provides functionality for distributing messages from a bus
    class DistributorSatellite : IAdvancedSatellite
    {
        readonly ISendMessages messageSender;
        readonly IWorkerAvailabilityManager workerManager;

        public DistributorSatellite(IBuilder builder, ReadOnlySettings settings)
        {
            Disabled = !settings.GetOrDefault<bool>("Distributor.Enabled");

            if (Disabled)
            {
                return;
            }

            messageSender = builder.Build<ISendMessages>();
            workerManager = builder.Build<IWorkerAvailabilityManager>();
            InputAddress = MasterNodeConfiguration.GetMasterNodeAddress(settings);
        }

        public Address InputAddress { get; }

        public bool Disabled { get; }

        public void Start()
        {
            var msmqWorkerAvailabilityManager = workerManager as MsmqWorkerAvailabilityManager;
            msmqWorkerAvailabilityManager?.Init();
        }

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

        public bool Handle(TransportMessage message)
        {
            var worker = workerManager.NextAvailableWorker();

            if (worker == null)
            {
                return false;
            }

            Logger.DebugFormat("Forwarding message to '{0}'.", worker.Address);

            message.Headers[Headers.WorkerSessionId] = worker.SessionId;

            messageSender.Send(message, new SendOptions(worker.Address));

            return true;
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(DistributorSatellite));
    }
}