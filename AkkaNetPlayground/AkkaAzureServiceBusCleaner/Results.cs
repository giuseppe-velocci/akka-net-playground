namespace AkkaAzureServiceBusCleaner
{
    public enum Result
    {
        Undefined,
        Start,
        Ok,
        Ko,
        Interrupted,
        ProcessingStopped, // stop further processing
        IterationComplete, // completed one iteration
        ProcessingComplete // completed all messages
    }
}
