namespace NServiceBus
{
    using System;
    using System.Configuration;
    using Config;
    using Settings;

    static class MasterNodeConfiguration
    {
        public static string GetMasterNode(ReadOnlySettings settings)
        {
            if (settings.GetOrDefault<bool>("Distributor.WithWorker"))
            {
                return null;
            }
            var section = settings.GetConfigSection<MasterNodeConfig>();
            return section?.Node;
        }

        public static Address GetMasterNodeAddress(ReadOnlySettings settings)
        {
            var unicastBusConfig = settings.GetConfigSection<UnicastBusConfig>();

            //allow users to override data address in config
            if (unicastBusConfig != null && !string.IsNullOrWhiteSpace(unicastBusConfig.DistributorDataAddress))
            {
                return Address.Parse(unicastBusConfig.DistributorDataAddress);
            }

            var masterNode = GetMasterNode(settings);

            if (string.IsNullOrWhiteSpace(masterNode))
            {
                return Address.Parse(settings.EndpointName());
            }

            ValidateHostName(masterNode);

            return new Address(settings.EndpointName(), masterNode);
        }

        private static void ValidateHostName(string hostName)
        {
            if (Uri.CheckHostName(hostName) == UriHostNameType.Unknown)
            {
                throw new ConfigurationErrorsException(string.Format("The 'Node' entry in MasterNodeConfig section of the configuration file: '{0}' is not a valid DNS name.", hostName));
            }
        }
    }
}