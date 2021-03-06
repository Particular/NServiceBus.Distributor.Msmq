namespace NServiceBus.Distributor.MSMQ
{
    using System.Configuration;
    using System.Linq;
    using System.Net;
    using Features;
    using NServiceBus.Config;
    using Logging;
    using Settings;
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
                s.Set("PublicReturnAddress", masterNodeAddress);

                if(!string.IsNullOrEmpty(MasterNodeConfiguration.GetMasterNode(s)))
                {
                    s.SetDefault("SecondLevelRetries.AddressOfRetryProcessor", masterNodeAddress.SubScope("Retries"));
                }
            });
            Defaults(s =>
            {
                var workerName = ConfigurationManager.AppSettings.Get("NServiceBus/Distributor/WorkerNameToUseWhileTesting");

                if (!string.IsNullOrEmpty(workerName))
                {
                    s.Set("NServiceBus.LocalAddress", workerName);
                }
            });
            RegisterStartupTask<ReadyMessageSender>();
        }

        /// <summary>
        /// <see cref="Feature.Setup"/>
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            if (!context.Settings.GetOrDefault<bool>("Distributor.Enabled"))
            {
                var workerName = ConfigurationManager.AppSettings.Get("NServiceBus/Distributor/WorkerNameToUseWhileTesting");

                if (string.IsNullOrEmpty(workerName))
                {
                    ValidateMasterNodeAddress(context.Settings);
                }
            }

            var masterNodeAddress = MasterNodeConfiguration.GetMasterNodeAddress(context.Settings);

            var distributorControlAddress = masterNodeAddress.SubScope("distributor.control");

            var unicastBusConfig = context.Settings.GetConfigSection<UnicastBusConfig>();

            //allow users to override control address in config
            if (!string.IsNullOrWhiteSpace(unicastBusConfig?.DistributorControlAddress))
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
        }

        static void ValidateMasterNodeAddress(ReadOnlySettings settings)
        {
            var masterNodeName = MasterNodeConfiguration.GetMasterNode(settings);

            if (masterNodeName == null)
            {
                throw new ConfigurationErrorsException(
                    "When defining Worker profile, 'MasterNodeConfig' section must be defined and the 'Node' entry should point to a valid host name.");
            }

            switch (IsLocalIpAddress(masterNodeName))
            {
                case true:
                    throw new ConfigurationErrorsException($"'MasterNodeConfig.Node' points to a local host name: [{masterNodeName}]");
                case false:
                    logger.InfoFormat("'MasterNodeConfig.Node' points to a non-local valid host name: [{0}].", masterNodeName);
                    break;
                case null:
                    throw new ConfigurationErrorsException(
                        $"'MasterNodeConfig.Node' entry should point to a valid host name. Currently it is: [{masterNodeName}].");
            }
        }

        static bool? IsLocalIpAddress(string hostName)
        {
            if (string.IsNullOrWhiteSpace(hostName))
            {
                return null;
            }
            try
            {
                var hostIPs = Dns.GetHostAddresses(hostName);
                var localIPs = Dns.GetHostAddresses(Dns.GetHostName());

                if (hostIPs.Any(hostIp => (IPAddress.IsLoopback(hostIp) || (localIPs.Contains(hostIp)))))
                {
                    return true;
                }
            }
            catch
            {
                return null;
            }
            return false;
        }

        static ILog logger = LogManager.GetLogger(typeof(WorkerNode));
    }
}