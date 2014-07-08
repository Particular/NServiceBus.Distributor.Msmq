namespace NServiceBus.Distributor.MSMQ
{
    using System;
    using Features;
    using NServiceBus.Config;
    using Pipeline;
    using Pipeline.Contexts;
    using QueueCreators;
    using ReadyMessages;

    /// <summary>
    /// Worker
    /// </summary>
    public class WorkerNode : Feature
    {
        internal WorkerNode()
        {
            Defaults(s => s.Set("Worker.Enabled", true));
            Defaults(s =>
            {
                var masterNodeAddress = MasterNodeConfiguration.GetMasterNodeAddress(s);
                s.Set("MasterNode.Address", masterNodeAddress);

                if(!string.IsNullOrEmpty(MasterNodeConfiguration.GetMasterNode(s)))
                {
                    s.SetDefault("SecondLevelRetries.AddressOfRetryProcessor", masterNodeAddress.SubScope("Retries"));
                }
            });

            RegisterStartupTask<ReadyMessageSender>();
        }

        /// <summary>
        /// Called when the features is activated
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var masterNodeAddress = MasterNodeConfiguration.GetMasterNodeAddress(context.Settings);
            var distributorControlAddress = masterNodeAddress.SubScope("distributor.control");

            var unicastBusConfig = context.Settings.GetConfigSection<UnicastBusConfig>();

            //allow users to override control address in config
            if (unicastBusConfig != null && !string.IsNullOrWhiteSpace(unicastBusConfig.DistributorControlAddress))
            {
                distributorControlAddress = Address.Parse(unicastBusConfig.DistributorControlAddress);
            }

            if (!context.Container.HasComponent<WorkerQueueCreator>())
            {
                context.Container.ConfigureComponent<WorkerQueueCreator>(DependencyLifecycle.InstancePerCall)
                    .ConfigureProperty(p => p.WorkerEnabled, true);
            }
            else
            {
                context.Container.ConfigureProperty<WorkerQueueCreator>(p => p.WorkerEnabled, true);
            }

            context.Container.ConfigureComponent<ReadyMessageSender>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.DistributorControlAddress, distributorControlAddress);

            Address.OverridePublicReturnAddress(masterNodeAddress);

            context.Pipeline.Register<ReturnAddressRewriterBehavior.Registration>();
            context.Container.ConfigureComponent<ReturnAddressRewriterBehavior>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(r => r.DistributorDataAddress, masterNodeAddress);
        }
    }

    class ReturnAddressRewriterBehavior : IBehavior<OutgoingContext>
    {
        public Address DistributorDataAddress { get; set; }

        public void Invoke(OutgoingContext context, Action next)
        {
            context.OutgoingLogicalMessage.Headers.Add(NServiceBus.Headers.ReplyToAddress, DistributorDataAddress.ToString());
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