namespace NServiceBus.Distributor.MSMQ.ReadyMessages
{
    using System;
    using Features;
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
            SendReadyMessage(capacityAvailable, true);
       }

        protected override void OnStop()
        {
        }

        void SendReadyMessage(int capacityAvailable = 1, bool isStarting = false)
        {
            //we use the actual address to make sure that the worker inside the master node will check in correctly
            var readyMessage = ControlMessage.Create();

            readyMessage.Headers.Add(Headers.WorkerCapacityAvailable, capacityAvailable.ToString());
        
            if (isStarting)
            {
                readyMessage.Headers.Add(Headers.WorkerStarting, Boolean.TrueString);
            }

            MessageSender.Send(readyMessage, new SendOptions(DistributorControlAddress)
            {
                ReplyToAddress = Bus.InputAddress
            });
        }

        ITransport transport;
    }
}