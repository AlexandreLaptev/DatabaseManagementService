namespace DatabaseManagement
{
    /// <summary>
    /// RabbitMQ configuration settings.
    /// </summary>
    public class RabbitMQConfig
    {
        /// <summary>
        /// The host name of the broker.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// The virtual host to use.
        /// </summary>
        public string VirtualHost { get; set; }

        /// <summary>
        /// The username for the connection to RabbitMQ.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The password for the connection to RabbitMQ.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// The retry count.
        /// </summary>
        public int RetryCount { get; set; }
    }
}