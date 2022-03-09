using AkkaAzureServiceBusCleaner.Service;
using Azure.Messaging.ServiceBus;
using Serilog;
using System;
using System.Threading.Tasks;

namespace AkkaAzureServiceBusCleaner.Services
{
    public class CompletePeekedMessagesService : ISourceService<Result>
    {
        private readonly ServiceBusReceiver Receiver;
        private readonly DateTime Ago;
        private readonly ILogger Logger;

        public CompletePeekedMessagesService(
            ServiceBusReceiver receiver,
            TimeSpan ago,
            ILogger logger
        )
        {
            Receiver = receiver;
            Ago = DateTime.UtcNow.Subtract(ago);
            Logger = logger;
        }

        public async Task<Result> Execute()
        {
            ServiceBusReceivedMessage peekedMessage = await Receiver.PeekMessageAsync();
            if (peekedMessage.ExpiresAt < Ago)
            {
                Logger.Debug("Message {id} is to be removed", peekedMessage.MessageId);
                await Receiver.CompleteMessageAsync(peekedMessage);
                return Result.Ok;
            } 
            else
            {
                Logger.Debug("Message {id} must stay", peekedMessage.MessageId);
                await Receiver.AbandonMessageAsync(peekedMessage);
                return Result.Ko;
            }
        }
    }
}
