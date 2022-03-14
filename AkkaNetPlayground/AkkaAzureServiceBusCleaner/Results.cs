namespace AkkaAzureServiceBusCleaner
{
    public enum Result
    {
        Undefined,
        Start,
        Ok,
        Ko,
        Interrupted,
        IterationComplete, // completed one iteration
        ProcessingComplete // completed all messages
    }
}
