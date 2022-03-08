namespace AkkaAzureServiceBusCleaner
{
    public class ServiceBusConfig
    {
        public string ConnectionString { get; }
        public string TopicName { get; }
        public string Subscription { get; }

        public ServiceBusConfig(
            string connectionString,
            string topicName,
            string subscription
        )
        {
            ConnectionString = connectionString;
            TopicName = topicName;
            Subscription = subscription;
        }
    }
}
