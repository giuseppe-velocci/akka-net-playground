using Akka.Actor;
using Akka.Event;

namespace AkkaAzureServiceBusCleaner.Actors
{
    public class StreamReceiverActor : UntypedActor
    {
        private readonly IActorRef Actor;
        private readonly ILoggingAdapter Logger;

        public StreamReceiverActor(
            IActorRef actor,
            ILoggingAdapter logger
        )
        {
            Actor  = actor;
            Logger = logger;
        }

        protected override void OnReceive(object message)
        { 
            switch (message)
            {
                case "start":
                    Logger.Info("Received streaming request message");
                    break;
                case "ack":
                    Logger.Info("Received ack message");
                    break;
                case Result.Done:
                    Logger.Info("DONE");
                  //  Actor.Tell(PoisonPill.Instance);
                    break;
                default:
                    Logger.Info("Received unknown message: {message", message);
                    break;
            }
        }

        public static Props Props(IActorRef actor, ILoggingAdapter logger)
        {
            return Akka.Actor.Props.Create(() => new StreamReceiverActor(actor, logger));
        }
    }
}
