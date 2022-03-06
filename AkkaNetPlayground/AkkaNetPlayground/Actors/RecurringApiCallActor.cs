using Akka.Actor;
using Akka.Event;
using AkkaNetPlayground.Service;
using System;

namespace AkkaNetPlayground.Actors
{
    public class RecurringApiCallActor : UntypedActor, IWithTimers
    {
        private readonly IStream Stream;
        private readonly ILoggingAdapter Logger;
        public ITimerScheduler Timers { get; set; }

        public RecurringApiCallActor(IStream stream, ILoggingAdapter logger)
        {
            Stream = stream;
            Logger = logger;
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case "stream":
                    Stream.RunStream();
                    Logger.Info("Received streaming request message");
                    break;
                case "helloRemote!":
                    Logger.Info("Received message from Remote sender {@sender}", Sender.Path.Address);
                    break;
                default:
                    Logger.Info("Received unknown message");
                    break;
            }
        }

        protected override void PreStart()
        {
            Timers.StartPeriodicTimer("runStreamKey", "stream", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
        }

        // static ctor
        public static Props Props(IStream stream, ILoggingAdapter logger)
        {
            return Akka.Actor.Props.Create(() => new RecurringApiCallActor(stream, logger));
        }
    }
}
