namespace NServiceBus.Distributor.MSMQ
{
    using System;
    using NServiceBus.ObjectBuilder;
    using ReadyMessages;
    using Satellites;
    using Settings;
    using Unicast.Transport;

    /// <summary>
    ///     Part of the Distributor infrastructure.
    /// </summary>
    internal class DistributorReadyMessageProcessor : IAdvancedSatellite
    {
        readonly IWorkerAvailabilityManager workerAvailabilityManager;

        public DistributorReadyMessageProcessor(IBuilder builder, ReadOnlySettings settings)
        {
            Disabled = !settings.GetOrDefault<bool>("Distributor.Enabled");

            if (Disabled)
            {
                return;
            }

            workerAvailabilityManager = builder.Build<IWorkerAvailabilityManager>();
            InputAddress = MasterNodeConfiguration.GetMasterNodeAddress(settings).SubScope("distributor.control");
        }

        /// <summary>
        ///     This method is called when a message is available to be processed.
        /// </summary>
        /// <param name="message">
        ///     The <see cref="TransportMessage" /> received.
        /// </param>
        public bool Handle(TransportMessage message)
        {
            if (!IsControlMessage(message))
            {
                return true;
            }

            if (message.Headers.ContainsKey(Headers.UnregisterWorker))
            {
                HandleDisconnectMessage(message);
                return true;
            }

            HandleControlMessage(message);

            return true;
        }

        /// <summary>
        ///     The <see cref="NServiceBus.Address" /> for this <see cref="ISatellite" /> to use when receiving messages.
        /// </summary>
        public Address InputAddress { get; }

        /// <summary>
        ///     Set to <code>true</code> to disable this <see cref="ISatellite" />.
        /// </summary>
        public bool Disabled { get; }

        /// <summary>
        ///     Starts the <see cref="ISatellite" />.
        /// </summary>
        public void Start()
        {
            var msmqWorkerAvailabilityManager = workerAvailabilityManager as MsmqWorkerAvailabilityManager;
            msmqWorkerAvailabilityManager?.Init();
        }

        /// <summary>
        ///     Stops the <see cref="ISatellite" />.
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

        bool IsControlMessage(TransportMessage transportMessage)
        {
            return transportMessage.Headers != null &&
                   transportMessage.Headers.ContainsKey(NServiceBus.Headers.ControlMessageHeader);
        }

        void HandleDisconnectMessage(TransportMessage controlMessage)
        {
            var workerAddress = Address.Parse(controlMessage.Headers[Headers.UnregisterWorker]);

            workerAvailabilityManager.UnregisterWorker(workerAddress);
        }

        void HandleControlMessage(TransportMessage controlMessage)
        {
            var replyToAddress = controlMessage.ReplyToAddress;

            string messageSessionId;
            if (!controlMessage.Headers.TryGetValue(Headers.WorkerSessionId, out messageSessionId))
            {
                messageSessionId = string.Empty;
            }

            if (controlMessage.Headers.ContainsKey(Headers.WorkerStarting))
            {
                var capacity = int.Parse(controlMessage.Headers[Headers.WorkerCapacityAvailable]);

                workerAvailabilityManager.RegisterNewWorker(new Worker(replyToAddress, messageSessionId), capacity);

                return;
            }

            workerAvailabilityManager.WorkerAvailable(new Worker(replyToAddress, messageSessionId));
        }
    }
}