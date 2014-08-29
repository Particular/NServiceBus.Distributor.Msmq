﻿namespace NServiceBus.Distributor.MSMQ.Profiles
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using Hosting.Profiles;

    internal class MasterProfileHandler : IHandleProfile<MSMQMaster>, IWantTheListOfActiveProfiles
    {
        public void ProfileActivated(BusConfiguration config)
        {
            if (ActiveProfiles.Contains(typeof(MSMQWorker)))
            {
                throw new ConfigurationErrorsException("Master profile and Worker profile should not coexist.");
            }

            config.AsMSMQMasterNode();
        }

        public void ProfileActivated(Configure config)
        {
        }

        public IEnumerable<Type> ActiveProfiles { get; set; }
    }
}