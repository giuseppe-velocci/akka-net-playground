using System;

namespace AkkaAzureServiceBusCleaner
{
    public class SBRouterConfig
    {
        public TimeSpan MessageTimeToLive { get;}
        public TimeSpan RecurringProcessingInterval { get;}

        public SBRouterConfig(
            TimeSpan messageTimeToLive,
            TimeSpan recurringProcessingInterval
        )
        {
            MessageTimeToLive = messageTimeToLive;
            RecurringProcessingInterval = recurringProcessingInterval;
        }
    }
}
