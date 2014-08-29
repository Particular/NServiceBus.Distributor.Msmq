namespace NServiceBus
{
    using System;
    using System.Configuration;
    using System.Linq;
    using System.Net;
    using Configuration.AdvanceExtensibility;
    using Logging;
    using Settings;


    /// <summary>
    /// Extension methods to configure Distributor.
    /// </summary>
    public static class ConfigureMSMQDistributor
    {
        /// <summary>
        /// Configure this endpoint as both a Distributor and a Worker.
        /// </summary>
        public static void AsMSMQMasterNode(this BusConfiguration config)
        {
            config.RunMSMQDistributor();
        }

        /// <summary>
        ///     Configure the distributor to run on this endpoint
        /// </summary>
        /// <param name="config"></param>
        /// <param name="withWorker"><value>true</value> if this endpoint should enlist as a worker, otherwise <value>false</value>. Default is <value>true</value>.</param>
        public static void RunMSMQDistributor(this BusConfiguration config, bool withWorker = true)
        {
            config.EnableFeature<Distributor.MSMQ.Distributor>();

            if (withWorker)
            {
                config.GetSettings().Set("Distributor.WithWorker", true);
                EnlistWithMSMQDistributor(config);
            }
        }

        /// <summary>
        ///     Enlist Worker with Master node defined in the config.
        /// </summary>
        public static void EnlistWithMSMQDistributor(this BusConfiguration config)
        {
            config.EnableFeature<Distributor.MSMQ.WorkerNode>();
        }
    }
}