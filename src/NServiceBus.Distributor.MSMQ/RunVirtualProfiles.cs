namespace NServiceBus.Distributor.MSMQ
{
    using System;
    using System.Configuration;

    class RunVirtualProfiles : INeedInitialization
    {
        public void Customize(BusConfiguration builder)
        {
            var args = Environment.GetCommandLineArgs();

            bool worker = false, master = false, distributor = false;

            foreach (var arg in args)
            {
                var profilename = arg.ToLower();

                if (profilename == "nservicebus.msmqworker")
                {
                    worker = true;
                }

                if (profilename == "nservicebus.msmqmaster")
                {
                    master = true;
                }

                if (profilename == "nservicebus.msmqdistributor")
                {
                    distributor = true;
                }
            }

            if (!(worker || master || distributor))
            {
                return;
            }

            var numberOfProfileEnabled = 0;

            if (worker)
            {
                numberOfProfileEnabled++;
            }

            if (master)
            {
                numberOfProfileEnabled++;
            }

            if (distributor)
            {
                numberOfProfileEnabled++;
            }

            if (numberOfProfileEnabled > 1)
            {
                throw new ConfigurationErrorsException("You can only enable one of the following profiles per endpoint Distributor, Master or Worker.");
            }

            if (worker)
            {
                builder.EnlistWithMSMQDistributor();
            }

            if (master)
            {
                builder.AsMSMQMasterNode();
            }

            if (distributor)
            {
                builder.RunMSMQDistributor(false);
            }
        }
    }
}
