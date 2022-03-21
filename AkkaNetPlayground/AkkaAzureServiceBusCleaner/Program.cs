using Akka.Actor;
using Akka.Configuration;
using AkkaAzureServiceBusCleaner.Actors;
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
            ServiceBusReceiver receiver = client.CreateReceiver(sbConfig.TopicName, sbConfig.Subscription, new ServiceBusReceiverOptions() { ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete });

            var config = ConfigurationFactory.ParseString(@"
akka {  
    loglevel=DEBUG, 
    loggers=[""Akka.Logger.Serilog.SerilogLogger, Akka.Logger.Serilog""]
}");


            using (var system = ActorSystem.Create("cleanerSystem", config))
            { 
                var routerConfig = new SBRouterConfig(
                    new TimeSpan(24, 0, 0), 
                    new TimeSpan(12, 0, 0)
                );
                var routerActor = system.ActorOf(SBRouterActor.Props(
                    system, 
                    receiver, 
                    system.Log,
                    routerConfig
                ), "sbRouter");
                system.WhenTerminated.Wait();
            }
        }
    }
}
