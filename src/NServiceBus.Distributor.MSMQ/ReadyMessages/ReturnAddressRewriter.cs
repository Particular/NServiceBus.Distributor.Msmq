namespace NServiceBus.Distributor.MSMQ.ReadyMessages
{
    using MessageMutator;
    using Unicast.Messages;

    internal class ReturnAddressRewriter : IMutateOutgoingTransportMessages
    {
        public Address DistributorDataAddress { get; set; }

        public void MutateOutgoing(LogicalMessage message, TransportMessage transportMessage)
        {
            //when not talking to the distributor, pretend that our address is that of the distributor
            if (DistributorDataAddress != null)
            {
                message.Headers.Add(NServiceBus.Headers.ReplyToAddress, DistributorDataAddress.ToString());
            }
        }
    }
}