using Akka.Actor;
using Akka.Event;
using Akka.Routing;
using Azure.Messaging.ServiceBus;
using System;
using System.Linq;
using System.Threading;

namespace AkkaAzureServiceBusCleaner.Actors
{
    internal class SBRouterActor : UntypedActor, IWithTimers
    {
        public ITimerScheduler Timers { get; set; }
        private readonly ActorSystem System;
        private readonly ServiceBusReceiver Receiver;
        private readonly DateTime Ago;
        private readonly ILoggingAdapter Logger;
        private readonly TimeSpan RecurringProcessingInterval;

        private IActorRef Router;

        private int maxMessages = 10;
        private int maxWorkers = 5;
        private long sequenceNumber = 0;

        public SBRouterActor(
            ActorSystem system,
            ServiceBusReceiver receiver,
            TimeSpan ago,
            ILoggingAdapter logger,
            TimeSpan recurringProcessingInterval
        )
        {
            System = system;
            Receiver = receiver;
            Ago = DateTime.UtcNow.Subtract(ago);
            Logger = logger;
            RecurringProcessingInterval = recurringProcessingInterval;
        }

        protected override void OnReceive(object message)
        {
            if (Router == null)
            {
                var props = StreamingDeferralActor
                    .Props(System, Receiver, Logger)
                    .WithRouter(new RoundRobinPool(maxWorkers));
                Router = System.ActorOf(props, "router");
            }

            switch (message)
            {
                case Result.Start:
                case Result.IterationComplete:
                    RunSequence();
                    break;
                case Result.ProcessingComplete:
                    Router.Tell(PoisonPill.Instance);
                    break;
                default:
                    Logger.Warning("Unexpected message");
                    break;
            }
        }

        private void RunSequence()
        {
            var sequenceNumbers = PeekExpiredMessages();
            if (sequenceNumbers.Length == 0)
            {
                Self.Tell(Result.ProcessingComplete);
                return;
            }

            var splitSequenceOfNumbers = sequenceNumbers.GroupBy(k => k % maxWorkers);
            foreach (var array in splitSequenceOfNumbers) {
                Router.Tell(array.ToArray());
            }

            Self.Tell(Result.IterationComplete);
        }

        private long[] PeekExpiredMessages()
        {
            var cancellationToken = new CancellationToken();
            var messagesTask = Receiver.PeekMessagesAsync(maxMessages, sequenceNumber, cancellationToken);
            messagesTask.Wait();
            sequenceNumber = messagesTask.Result.Last().SequenceNumber;
            return messagesTask.Result
                .Where(x => x.ExpiresAt.DateTime < Ago)
                .Select(x => x.SequenceNumber)
                .ToArray();
        }

        protected override void PreStart()
        {
            Timers.StartPeriodicTimer("runStreamKey", Result.Start, TimeSpan.FromSeconds(3), RecurringProcessingInterval);
        }

        public static Props Props(
            ActorSystem system,
            ServiceBusReceiver receiver,
            TimeSpan ago,
            ILoggingAdapter logger,
            TimeSpan recurringProcessingInterval
        )
        {
            return Akka.Actor.Props.Create(() => new SBRouterActor(system, receiver, ago, logger, recurringProcessingInterval));
        }
    }
}
