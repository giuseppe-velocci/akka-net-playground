using Akka.Actor;
using System;
using AkkaAzureServiceBusCleaner.Services;
using Akka.Event;

namespace AkkaAzureServiceBusCleaner.Actors
{
    public class StreamActor : UntypedActor, IWithTimers
    {
        private readonly IStream Stream;
        private readonly ILoggingAdapter Logger;

        public ITimerScheduler Timers { get; set; }

        public StreamActor(
            IStream stream,
            ILoggingAdapter logger
        )
        {
            Stream = stream;
            Logger = logger;
        }

        protected async override void OnReceive(object message)
        {
            switch (message)
            {
                case "start":
                    await Stream.Stream();
                    Logger.Info("Received streaming request message");
                    break;
                default:
                    Logger.Info("Received unknown message");
                    break;
            }
        }

        protected override void PreStart()
        {
            Timers.StartPeriodicTimer("runStreamKey", "start", TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.5));
        }
      
        public static Props Props(IStream stream, ILoggingAdapter logger)
        {
            return Akka.Actor.Props.Create(() => new StreamActor(stream, logger));
        }
    }
}
