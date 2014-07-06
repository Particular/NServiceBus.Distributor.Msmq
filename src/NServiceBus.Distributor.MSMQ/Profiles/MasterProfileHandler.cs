namespace NServiceBus.Distributor.MSMQ.Profiles
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using Hosting.Profiles;

    internal class MasterProfileHandler : IHandleProfile<MSMQMaster>, IWantTheListOfActiveProfiles
    {
        public void ProfileActivated(Configure config)
        {
            if (ActiveProfiles.Contains(typeof(MSMQWorker)))
            {
                throw new ConfigurationErrorsException("Master profile and Worker profile should not coexist.");
            }

            config.AsMSMQMasterNode();

            // TODO: Do I some how enable the gateway!
            //Feature.EnableByDefault<Gateway>();
        }

        public IEnumerable<Type> ActiveProfiles { get; set; }
    }
}