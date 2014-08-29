namespace NServiceBus.Distributor.MSMQ
{
    using System;
    using System.Configuration;
    using System.Linq;
    using System.Net;
    using Features;
    using NServiceBus.Config;
    using NServiceBus.Logging;
    using NServiceBus.Settings;
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

            ValidateMasterNodeAddress(context.Settings); 
            
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

            context.Pipeline.Register<ReturnAddressRewriterBehavior.Registration>();
            context.Container.ConfigureComponent<ReturnAddressRewriterBehavior>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(r => r.DistributorDataAddress, masterNodeAddress);
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
                    throw new Exception(string.Format("'MasterNodeConfig.Node' points to a local host name: [{0}]", masterNodeName));
                case false:
                    logger.InfoFormat("'MasterNodeConfig.Node' points to a non-local valid host name: [{0}].", masterNodeName);
                    break;
                case null:
                    throw new ConfigurationErrorsException(
                        string.Format("'MasterNodeConfig.Node' entry should point to a valid host name. Currently it is: [{0}].", masterNodeName));
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

        static ILog logger = LogManager.GetLogger(typeof(MSMQWorker));

    }
}