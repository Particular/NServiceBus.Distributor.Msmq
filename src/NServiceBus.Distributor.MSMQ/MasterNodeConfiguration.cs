namespace NServiceBus
{
    using System;
    using System.Configuration;
    using Config;

    static class MasterNodeConfiguration
    {
        public static bool HasMasterNode()
        {
            return !string.IsNullOrEmpty(GetMasterNode());
        }

        public static string GetMasterNode()
        {
            var section = Configure.GetConfigSection<MasterNodeConfig>();
            return section != null ? section.Node : null;
        }

        public static Address GetMasterNodeAddress()
        {
            var unicastBusConfig = Configure.GetConfigSection<UnicastBusConfig>();

            //allow users to override data address in config
            if (unicastBusConfig != null && !string.IsNullOrWhiteSpace(unicastBusConfig.DistributorDataAddress))
            {
                return Address.Parse(unicastBusConfig.DistributorDataAddress);
            }

            var masterNode = GetMasterNode();

            if (string.IsNullOrWhiteSpace(masterNode))
            {
                return Address.Parse(Configure.EndpointName);
            }

            ValidateHostName(masterNode);

            return new Address(Configure.EndpointName, masterNode);
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