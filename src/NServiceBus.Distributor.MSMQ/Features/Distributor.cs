namespace NServiceBus.Distributor.MSMQ
{
    using Features;
    using Logging;
    using QueueCreators;
    using Unicast;

    /// <summary>
    /// Distributor
    /// </summary>
    public class Distributor : Feature
    {
        internal Distributor()
        {
            Defaults(s => s.Set("Distributor.Enabled", true));
        }
        /// <summary>
        /// <see cref="Feature.Setup"/>
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var endpointName = context.Settings.EndpointName();
            var applicativeInputQueue = Address.Parse(endpointName).SubScope("worker");

            context.Container.ConfigureComponent<UnicastBus>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(r => r.InputAddress, applicativeInputQueue)
                .ConfigureProperty(r => r.DoNotStartTransport, !context.Settings.GetOrDefault<bool>("Distributor.WithWorker"));

            if (!context.Container.HasComponent<WorkerQueueCreator>())
            {
                context.Container.ConfigureComponent<WorkerQueueCreator>(DependencyLifecycle.InstancePerCall)
                    .ConfigureProperty(p => p.DistributorEnabled, true)
                    .ConfigureProperty(p => p.Address, applicativeInputQueue);
            }
            else
            {
                context.Container.ConfigureProperty<WorkerQueueCreator>(p => p.DistributorEnabled, true);
                context.Container.ConfigureProperty<WorkerQueueCreator>(p => p.Address, applicativeInputQueue);
            }

            if (!context.Container.HasComponent<IWorkerAvailabilityManager>())
            {
                context.Container.ConfigureComponent<MsmqWorkerAvailabilityManager>(DependencyLifecycle.SingleInstance);
            }

            Logger.InfoFormat("Endpoint configured to host the distributor, applicative input queue re routed to {0}",
                applicativeInputQueue);
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(Distributor));
    }
}