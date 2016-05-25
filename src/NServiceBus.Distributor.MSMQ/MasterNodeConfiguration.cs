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
            var section = settings.GetConfigSection<MasterNodeConfig>();
            return section?.Node;
        }

        public static Address GetMasterNodeAddress(ReadOnlySettings settings)
        {
            var unicastBusConfig = settings.GetConfigSection<UnicastBusConfig>();

            //allow users to override data address in config
            if (!string.IsNullOrWhiteSpace(unicastBusConfig?.DistributorDataAddress))
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

        static void ValidateHostName(string hostName)
        {
            if (Uri.CheckHostName(hostName) == UriHostNameType.Unknown)
            {
                throw new ConfigurationErrorsException($"The 'Node' entry in MasterNodeConfig section of the configuration file: '{hostName}' is not a valid DNS name.");
            }
        }
    }
}