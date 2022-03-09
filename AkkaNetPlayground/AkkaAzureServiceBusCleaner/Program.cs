using Akka.Actor;
using Akka.Configuration;
using AkkaAzureServiceBusCleaner.Actors;
using AkkaAzureServiceBusCleaner.Services;
using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using Serilog;
using System;
using System.IO;

namespace AkkaAzureServiceBusCleaner
{
    class Program
    {
        static void Main(string[] args)
        {
            var logger = new LoggerConfiguration()
               .WriteTo.Console()
               .MinimumLevel.Debug()
               .CreateLogger();

            Log.Logger = logger;

            string jsonConfig = File.ReadAllText("app-config.json");
            ServiceBusConfig sbConfig = JsonConvert.DeserializeObject<ServiceBusConfig>(jsonConfig);

            // since ServiceBusClient implements IAsyncDisposable we create it with "await using"
            var client = new ServiceBusClient(sbConfig.ConnectionString);
            ServiceBusReceiver receiver = client.CreateReceiver(sbConfig.TopicName, sbConfig.Subscription);

            CompletePeekedMessagesService sbService = new CompletePeekedMessagesService(receiver, new TimeSpan(36, 0, 0), logger);

            var config = ConfigurationFactory.ParseString(@"
akka {  
    loglevel=DEBUG, 
    loggers=[""Akka.Logger.Serilog.SerilogLogger, Akka.Logger.Serilog""]
}");

            using (var system = ActorSystem.Create("cleanerSystem", config))
            {
                var stream = new StreamService(sbService, system, "user/receiverActor");
                var streamActor = system.ActorOf(StreamActor.Props(stream, system.Log), "streamActor");
                system.ActorOf(StreamReceiverActor.Props(streamActor, system.Log), "receiverActor");

                system.WhenTerminated.Wait();
            }
        }
    }
}
