using Akka.Actor;
using Akka.Event;
using Akka.Streams;
using Akka.Streams.Dsl;
using Azure.Messaging.ServiceBus;
using System;
using System.Threading.Tasks;

namespace AkkaAzureServiceBusCleaner.Actors
{
    public class StreamingDeferralActor : UntypedActor, IWithTimers
    {
        public ITimerScheduler Timers { get; set; }

        private readonly ActorSystem System;
        private readonly ServiceBusReceiver Receiver;
        private readonly ILoggingAdapter Logger;

        private readonly ActorMaterializer Materializer;

        public StreamingDeferralActor(
            ActorSystem system,
            ServiceBusReceiver receiver,
            ILoggingAdapter logger
        )
        {
            if (receiver.ReceiveMode != ServiceBusReceiveMode.ReceiveAndDelete)
            {
                throw new ArgumentException("Receiver must be in peek and delete mode.");
            }

            System = system;
            Receiver = receiver;
            Logger = logger;

            Materializer = System.Materializer();
        }

        private Task StreamSequenceNumbers(long[] sequenceNumbers)
        {
            var actorSink = Sink.ActorRef<Result>(Self, Result.IterationComplete);
            return Source.From(sequenceNumbers)
                .Log("source")
                .SelectAsync(1, x => CompleteMessageOrNot(x))
                .Log("select")
                .Recover(_ => { return Result.Interrupted; })
                .Log("recover")
                .AlsoTo(actorSink)
                .Log("toActor")
                .RunWith(Sink.Ignore<Result>(), Materializer);
        }

        private async Task<Result> CompleteMessageOrNot(long sequenceNumber)
        {
            try
            {
                var lockedMessage = await Receiver.ReceiveDeferredMessageAsync(sequenceNumber);
                Logger.Debug("Message {id} is to be removed", lockedMessage.MessageId);
                return Result.Ok;
            }
            catch (Exception _) // to be imporved exception type!
            {
                Logger.Debug("Message at sequnce number {id} must stay", sequenceNumber);
                return Result.Ko;
            }
        }

        protected async override void OnReceive(object message)
        {
            switch (message)
            {
                case long[] sequenceNumbers:
                    Logger.Info("Received streaming request message by {res}", message);
                    await StreamSequenceNumbers(sequenceNumbers);
                    break;
                case Result.Interrupted:
                    Logger.Warning("Received interrupted");
                    break;
                case Result.ProcessingComplete:
                case Result.ProcessingStopped:
                    Logger.Info("Completed with status {message}", message);
                    break;
                case Result.IterationComplete: // To be fixed handling of rerun in case of error
                    Logger.Info("Completed iteration");
                    break;
                default:
                    Logger.Info("unexpected message");
                    break;
            }
        }

        protected override void PreStart()
        {
            Timers.StartPeriodicTimer("runStreamKey", Result.Start, TimeSpan.FromSeconds(3), TimeSpan.FromHours(12));
        }

        public static Props Props(
            ActorSystem system,
            ServiceBusReceiver receiver,
            ILoggingAdapter logger
        )
        {
            return Akka.Actor.Props.Create(() => new StreamingDeferralActor(system, receiver, logger));
        }
    }
}
