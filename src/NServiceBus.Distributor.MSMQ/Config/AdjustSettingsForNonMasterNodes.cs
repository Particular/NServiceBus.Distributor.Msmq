namespace NServiceBus.Distributor.MSMQ.Config
{
    using Settings;

    class AdjustSettingsForNonMasterNodes : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run()
        {
            if (!MasterNodeConfiguration.HasMasterNode())
            {
                return;
            }

            SettingsHolder.SetDefault("SecondLevelRetries.AddressOfRetryProcessor", MasterNodeConfiguration.GetMasterNodeAddress().SubScope("Retries"));
        }
    }
}