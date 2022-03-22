using Akka.Actor;
using Akka.Event;
using Azure.Messaging.ServiceBus;
using System;
using System.Linq;
using System.Threading;

namespace AkkaReadOneMessage.Actor
{
    class SBActor : UntypedActor
    {
        private readonly ServiceBusReceiver Receiver;
        private readonly ILoggingAdapter Logger;

        public SBActor(
            ServiceBusReceiver receiver,
            ILoggingAdapter logger
        )
        {
            if (receiver.ReceiveMode != ServiceBusReceiveMode.PeekLock)
            {
                throw new ArgumentException("Receiver must be in peeklock mode!");
            }

            Receiver = receiver;
            Logger = logger;
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case "Start":
                    PeekMessages();
                    break;
                default:
                    break;
            }
        }

        private void PeekMessages()
        {
            try
            {
                var messagesTask = Receiver.PeekMessageAsync();
                messagesTask.Wait();
                var message = messagesTask.Result;
                Logger.Info("{message}", message);
            }
            catch (Exception ex)
            {
                Logger.Error("{ex}", ex);
            }
        }

        protected override void PreStart()
        {
            Self.Tell("Start");
        }

        public static Props Props(
            ServiceBusReceiver receiver,
            ILoggingAdapter logger
        )
        {
            return Akka.Actor.Props.Create(() => new SBActor(receiver, logger));
        }
    }
}
