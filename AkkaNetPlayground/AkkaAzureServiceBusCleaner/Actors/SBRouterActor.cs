using Akka.Actor;
using Akka.Event;
using Akka.Routing;
using Azure.Messaging.ServiceBus;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace AkkaAzureServiceBusCleaner.Actors
{
    internal class SBRouterActor : UntypedActor, IWithTimers
    {
        public ITimerScheduler Timers { get; set; }
        private readonly ActorSystem System;
        private readonly ServiceBusReceiver Receiver;
        private readonly SBRouterConfig Config;
        private readonly ILoggingAdapter Logger;

        private IActorRef Router;
        private DateTime ExpirationDate;
        private long sequenceNumber = 0;

        public SBRouterActor(
            ActorSystem system,
            ServiceBusReceiver receiver,
            ILoggingAdapter logger,
            SBRouterConfig config
        )
        {
            System = system;
            Receiver = receiver;
            Logger = logger;
            Config = config;
        }

        protected override void OnReceive(object message)
        {
            if (Router == null)
            {
                ExpirationDate = DateTime.UtcNow.Subtract(Config.MessageTimeToLive);
                Logger.Debug("Expiration date set to {exp}", ExpirationDate);
                var props = StreamingDeferralActor
                    .Props(System, Receiver, Logger)
                    .WithRouter(new RoundRobinPool(Config.NumberOfWorkers));
                Router = System.ActorOf(props, "router");
            }

            switch (message)
            {
                case Result.Start:
                case Result.IterationComplete:
                    RunSequence();
                    break;
                case Result.ProcessingComplete:
                    Logger.Info("Completed processing");
                    Router.Tell(PoisonPill.Instance);
                    break;
                default:
                    Logger.Warning("Unexpected message");
                    break;
            }
        }

        private void RunSequence()
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            var sequenceNumbers = PeekExpiredMessages();
            if (sequenceNumbers.Length == 0)
            {
                Self.Tell(Result.ProcessingComplete);
                return;
            }

            var splitSequenceOfNumbers = sequenceNumbers.GroupBy(k => k % Config.NumberOfWorkers);
            foreach (var array in splitSequenceOfNumbers) {
                Router.Tell(array.ToArray());
            }

            watch.Stop();
            Logger.Debug("Completed iteration in {elapsed}", watch.Elapsed);

            Self.Tell(Result.IterationComplete);
        }

        private long[] PeekExpiredMessages()
        {
            var cancellationToken = new CancellationToken();
            var messagesTask = Receiver.PeekMessagesAsync(Config.MaxNumberOfPeekedMessages, sequenceNumber, cancellationToken);
            messagesTask.Wait();
            sequenceNumber = messagesTask.Result.LastOrDefault() is null ? 0 : messagesTask.Result.Last().SequenceNumber;
            return messagesTask.Result
                .Where(x => x.State == ServiceBusMessageState.Deferred && x.ExpiresAt.DateTime < ExpirationDate)
                .Select(x => x.SequenceNumber)
                .ToArray();
        }

        protected override void PreStart()
        {
            Timers.StartPeriodicTimer("runStreamKey", Result.Start, TimeSpan.FromSeconds(3), Config.RecurringProcessingInterval);
        }

        public static Props Props(
            ActorSystem system,
            ServiceBusReceiver receiver,
            ILoggingAdapter logger,
            SBRouterConfig config
        )
        {
            return Akka.Actor.Props.Create(() => new SBRouterActor(system, receiver, logger, config));
        }
    }
}
