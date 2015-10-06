namespace NServiceBus.Distributor.MSMQ
{
    using System;
    using System.Messaging;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    ///     MSMQ-related utility functions
    /// </summary>
    class MsmqUtilities
    {
        /// <summary>
        ///     Turns a '@' separated value into a full path.
        ///     Format is 'queue@machine', or 'queue@ipaddress'
        /// </summary>
        public static string GetFullPath(Address value)
        {
            IPAddress ipAddress;
            if (IPAddress.TryParse(value.Machine, out ipAddress))
            {
                return PREFIX_TCP + GetFullPathWithoutPrefix(value);
            }

            return PREFIX + GetFullPathWithoutPrefix(value);
        }

        public static Address GetIndependentAddressForQueue(MessageQueue q)
        {
            if (q == null)
            {
                return null;
            }

            var arr = q.FormatName.Split('\\');
            var queueName = arr[arr.Length - 1];

            var directPrefixIndex = arr[0].IndexOf(DIRECTPREFIX);
            if (directPrefixIndex >= 0)
            {
                return new Address(queueName, arr[0].Substring(directPrefixIndex + DIRECTPREFIX.Length));
            }

            var tcpPrefixIndex = arr[0].IndexOf(DIRECTPREFIX_TCP);
            if (tcpPrefixIndex >= 0)
            {
                return new Address(queueName, arr[0].Substring(tcpPrefixIndex + DIRECTPREFIX_TCP.Length));
            }

            try
            {
                // the pessimistic approach failed, try the optimistic approach
                arr = q.QueueName.Split('\\');
                queueName = arr[arr.Length - 1];
                return new Address(queueName, q.MachineName);
            }
            catch
            {
                throw new Exception("Could not translate format name to independent name: " + q.FormatName);
            }
        }

        static string GetFullPathWithoutPrefix(Address address)
        {
            var queueName = address.Queue;
            var msmqTotalQueueName = address.Machine + PRIVATE + queueName;

            if (msmqTotalQueueName.Length >= 114) //Setpermission is limiting us here, docs say it should be 380 + 124
            {
                msmqTotalQueueName = address.Machine + PRIVATE + Create(queueName);
            }

            return msmqTotalQueueName;
        }

        static Guid Create(params object[] data)
        {
            // use MD5 hash to get a 16-byte hash of the string
            using (var provider = new MD5CryptoServiceProvider())
            {
                var inputBytes = Encoding.Default.GetBytes(string.Concat(data));
                var hashBytes = provider.ComputeHash(inputBytes);
                // generate a guid from the hash:
                return new Guid(hashBytes);
            }
        }

        const string DIRECTPREFIX = "DIRECT=OS:";
        const string DIRECTPREFIX_TCP = "DIRECT=TCP:";
        const string PREFIX_TCP = "FormatName:" + DIRECTPREFIX_TCP;
        const string PREFIX = "FormatName:" + DIRECTPREFIX;
        internal const string PRIVATE = "\\private$\\";
    }
}