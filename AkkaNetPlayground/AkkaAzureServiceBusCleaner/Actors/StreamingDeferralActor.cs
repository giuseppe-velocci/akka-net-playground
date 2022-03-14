using Akka.Actor;
using Akka.Event;
using Akka.Streams;
using Akka.Streams.Dsl;
using Azure.Messaging.ServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AkkaAzureServiceBusCleaner.Actors
{
    public class StreamingDeferralActor : UntypedActor, IWithTimers
    {
        public ITimerScheduler Timers { get; set; }

        private readonly ActorSystem System;
        private readonly ServiceBusReceiver Receiver;
        private readonly DateTime Ago;
        private readonly ILoggingAdapter Logger;

        private int maxMessages = 10;
        private readonly ActorMaterializer Materializer;
        private long sequenceNumber = 0;

        public StreamingDeferralActor(
            ActorSystem system,
            ServiceBusReceiver receiver,
            TimeSpan ago,
            ILoggingAdapter logger
        )
        {
            System = system;
            Receiver = receiver;
            Ago = DateTime.UtcNow.Subtract(ago);
            Logger = logger;

            Materializer = System.Materializer();
        }

        public Task StreamToComplete()
        {
            var messages = PeekMessages();
            if (messages.Count <= 0)
            {
                Self.Ask(Result.ProcessingComplete).Wait();
            }

            return StreamFromMessages(messages);
        }

        private Task StreamFromMessages(ICollection<ServiceBusReceivedMessage> messages)
        {
            var actorSink = Sink.ActorRef<Result>(Self, Result.IterationComplete);
            return Source.From(messages)
                .Log("source")
                .SelectAsync(1, x => CompleteMessageOrNot(x))
                .Log("select")
                .Recover(_ => { return Result.Interrupted; })
                .Log("recover")
                .AlsoTo(actorSink)
                .Log("toActor")
                .RunWith(Sink.Ignore<Result>(), Materializer);
        }

        private ICollection<ServiceBusReceivedMessage> PeekMessages()
        {
            var cancellationToken = new CancellationToken();
            var messagesTask = Receiver.PeekMessagesAsync(maxMessages, sequenceNumber, cancellationToken);
            messagesTask.Wait();
            sequenceNumber = messagesTask.Result.Last().SequenceNumber;
            return messagesTask.Result
                .ToList();
        }

        private async Task<Result> CompleteMessageOrNot(ServiceBusReceivedMessage peekedMessage)
        {
            if (peekedMessage.ExpiresAt < Ago)
            {
                var lockedMessage = await Receiver.ReceiveDeferredMessageAsync(peekedMessage.SequenceNumber);
                Logger.Debug("Message {id} is to be removed", lockedMessage.MessageId);
                await Receiver.CompleteMessageAsync(lockedMessage);
                return Result.Ok;
            }
            else
            {
                Logger.Debug("Message {id} must stay", peekedMessage.MessageId);
                return Result.Ko;
            }
        }

        protected async override void OnReceive(object message)
        {
            switch (message)
            {
                case Result.Start:
                    Logger.Info("Received streaming request message by {res}", message);
                    await StreamToComplete();
                    break;
                case Result.Interrupted:
                    Logger.Warning("Received interrupted");
                    await StreamToComplete();
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
            TimeSpan ago,
            ILoggingAdapter logger
        )
        {
            return Akka.Actor.Props.Create(() => new StreamingDeferralActor(system, receiver, ago, logger));
        }
    }
}
