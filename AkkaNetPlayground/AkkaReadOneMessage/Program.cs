using Akka.Actor;
using Akka.Configuration;
using AkkaReadOneMessage.Actor;
using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using Serilog;
using System.IO;
using System.Threading.Tasks;

namespace AkkaReadOneMessage
{
    class Program
    {
        static async Task Main(string[] args)
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

            var config = ConfigurationFactory.ParseString(@"
akka {  
    loglevel=DEBUG, 
    loggers=[""Akka.Logger.Serilog.SerilogLogger, Akka.Logger.Serilog""]
}");

            /*
            using (var system = ActorSystem.Create("cleanerSystem", config))
            {
                var sbActor = system.ActorOf(SBActor.Props(receiver, system.Log));

                system.WhenTerminated.Wait();
            }
            */
            var message = await receiver.PeekMessageAsync();
            logger.Information("{message}", message);
        }
    }
}
