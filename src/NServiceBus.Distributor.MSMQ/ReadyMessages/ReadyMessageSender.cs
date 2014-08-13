namespace NServiceBus.Distributor.MSMQ.ReadyMessages
{
    using System;
    using Settings;
    using Transports;
    using Unicast;
    using Unicast.Transport;

    internal class ReadyMessageSender : IWantToRunWhenBusStartsAndStops
    {
        public ISendMessages MessageSender { get; set; }

        public UnicastBus Bus { get; set; }

        public Address DistributorControlAddress { get; set; }

        public void Start()
        {
            if (!ConfigureMSMQDistributor.WorkerRunsOnThisEndpoint() || SettingsHolder.Get<int>("Distributor.Version") != 2)
            {
                return;
            }

            transport = Bus.Transport;
            var capacityAvailable = transport.MaximumConcurrencyLevel;
            SendReadyMessage(capacityAvailable, true);
       }

        public void Stop()
        {
        }

        void SendReadyMessage(int capacityAvailable = 1, bool isStarting = false)
        {
            //we use the actual address to make sure that the worker inside the master node will check in correctly
            var readyMessage = ControlMessage.Create(Bus.InputAddress);

            readyMessage.Headers.Add(Headers.WorkerCapacityAvailable, capacityAvailable.ToString());
        
            if (isStarting)
            {
                readyMessage.Headers.Add(Headers.WorkerStarting, Boolean.TrueString);
            }

            MessageSender.Send(readyMessage, DistributorControlAddress);
        }

        ITransport transport;
    }
}