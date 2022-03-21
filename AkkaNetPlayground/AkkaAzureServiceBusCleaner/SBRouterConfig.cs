using System;

namespace AkkaAzureServiceBusCleaner
{
    public class SBRouterConfig
    {
        public TimeSpan MessageTimeToLive { get;}
        public TimeSpan RecurringProcessingInterval { get;}
        private int maxNumberOfPeekedMessages;
        public int MaxNumberOfPeekedMessages
        {
            get { return maxNumberOfPeekedMessages; }
            set { 
                if (value <= 0)
                {
                    throw new ArgumentException("too small value for max peeked of messages, must be greater than 0 and less or equal to 100");
                }
                if (value > 100)
                {
                    throw new ArgumentException("too big value for max peeked of messages, max allowed is 100");
                }
                maxNumberOfPeekedMessages = value;
            }
        }
        private int numberOfWorkers;
        public int NumberOfWorkers
        {
            get { return numberOfWorkers; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException("too small value for numberOfWorkers, must be greater than 0 and less or equal to 100");
                }
                if (value > 100)
                {
                    throw new ArgumentException("too big value for numberOfWorkers, max allowed is 100");
                }
                numberOfWorkers = value;
            }
        }


        public SBRouterConfig(
            TimeSpan messageTimeToLive,
            TimeSpan recurringProcessingInterval,
            int maxNumberOfPeekedMessages,
            int numberOfWorkers
        )
        {
            MessageTimeToLive = messageTimeToLive;
            RecurringProcessingInterval = recurringProcessingInterval;
            MaxNumberOfPeekedMessages = maxNumberOfPeekedMessages;
            NumberOfWorkers = numberOfWorkers;
        }
    }
}
