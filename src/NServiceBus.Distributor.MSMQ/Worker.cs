namespace NServiceBus.Distributor.MSMQ
{
    /// <summary>
    ///     Worker details class, to be used with <see cref="IWorkerAvailabilityManager" />.
    /// </summary>
    public class Worker
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Worker" /> class.
        /// </summary>
        /// <param name="address">The <see cref="Address" /> of the worker that will accept the dispatched message.</param>
        public Worker(Address address)
        {
            Address = address;
        }

        /// <summary>
        ///     The <see cref="Address" /> of the worker that will accept the dispatched message.
        /// </summary>
        public Address Address { get; set; }

        /// <summary>
        ///     The worker current sessionId.
        ///     Obsolete - The worker current sessionId is no longer being used. 
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "6.0", TreatAsErrorFromVersion = "6.0")]
        public string SessionId { get; set; }
    }
}