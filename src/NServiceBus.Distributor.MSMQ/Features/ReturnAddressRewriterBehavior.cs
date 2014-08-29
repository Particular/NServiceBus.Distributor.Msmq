namespace NServiceBus.Distributor.MSMQ
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class ReturnAddressRewriterBehavior : IBehavior<OutgoingContext>
    {
        public Address DistributorDataAddress { get; set; }

        public void Invoke(OutgoingContext context, Action next)
        {
            context.OutgoingLogicalMessage.Headers.Add(Headers.ReplyToAddress, DistributorDataAddress.ToString());
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("ReturnAddressRewriter", typeof(ReturnAddressRewriterBehavior), "Rewrites the return address for a worker.")
            {
                InsertAfter(WellKnownStep.MutateOutgoingTransportMessage);
                InsertBefore(WellKnownStep.DispatchMessageToTransport);
            }
        }
    }
}