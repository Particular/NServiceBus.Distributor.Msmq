namespace NServiceBus.Distributor.MSMQ.ReadyMessages
{
    using System;
    using Features;
    using NServiceBus.Logging;
    using Transports;
    using Unicast;
    using Unicast.Transport;

    internal class ReadyMessageSender : FeatureStartupTask
    {
        public ISendMessages MessageSender { get; set; }

        public UnicastBus Bus { get; set; }

        public Address DistributorControlAddress { get; set; }

        protected override void OnStart()
        {
            transport = Bus.Transport;
            var capacityAvailable = transport.MaximumConcurrencyLevel;
            SendReadyMessage(workerSessionId, capacityAvailable, true);
            Logger.DebugFormat("Ready startup message with WorkerSessionId {0} sent. ", workerSessionId);
            transport.StartedMessageProcessing += TransportOnStartedMessageProcessing;
        }

        protected override void OnStop()
        {
            //transport will be null if !WorkerRunsOnThisEndpoint
            if (transport != null)
            {
                transport.StartedMessageProcessing -= TransportOnStartedMessageProcessing;
            }
        }

        void TransportOnStartedMessageProcessing(object sender, StartedMessageProcessingEventArgs e)
        {
            //if there was a failure this "send" will be rolled back
            string messageSessionId;
            e.Message.Headers.TryGetValue(Headers.WorkerSessionId, out messageSessionId);
            Logger.DebugFormat("Got message with id {0} and messageSessionId {1}. WorkerSessionId is {2}", e.Message.Id, messageSessionId ?? string.Empty, workerSessionId);
            //If the message we are processing contains an old sessionid then we do not send an extra control message 
            //otherwise that would cause https://github.com/Particular/NServiceBus/issues/978
            if (messageSessionId == workerSessionId)
            {
                SendReadyMessage(messageSessionId);
                Logger.DebugFormat("Ready message for message with id {0} sent back with messageSessionId {1}. WorkerSessionId was {2}", e.Message.Id, messageSessionId ?? string.Empty, workerSessionId);
            }
            else
            {
                Logger.DebugFormat("SKIPPED Ready message for message with id {0} because of sessionId mismatch. MessageSessionId {1}, WorkerSessionId {2}", e.Message.Id, messageSessionId ?? string.Empty, workerSessionId);
            }
        }

        void SendReadyMessage(string sessionId, int capacityAvailable = 1, bool isStarting = false)
        {
            //we use the actual address to make sure that the worker inside the master node will check in correctly
            var readyMessage = ControlMessage.Create();

            readyMessage.Headers.Add(Headers.WorkerCapacityAvailable, capacityAvailable.ToString());
            readyMessage.Headers.Add(Headers.WorkerSessionId, sessionId);

            if (isStarting)
            {
                readyMessage.Headers.Add(Headers.WorkerStarting, bool.TrueString);
            }

            MessageSender.Send(readyMessage, new SendOptions(DistributorControlAddress)
            {
                ReplyToAddress = Bus.InputAddress
            });
        }

        ITransport transport;
        string workerSessionId = Guid.NewGuid().ToString();
        static readonly ILog Logger = LogManager.GetLogger(typeof(ReadyMessageSender));
    }
}
