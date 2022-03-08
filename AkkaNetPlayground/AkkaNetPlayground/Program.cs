using Akka.Actor;
using Akka.Configuration;
using AkkaNetPlayground.Actors;
using AkkaNetPlayground.Service;
using Serilog;

namespace AkkaNetPlayground
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

            var config = ConfigurationFactory.ParseString(@"
akka {  
    loglevel=DEBUG, 
    loggers=[""Akka.Logger.Serilog.SerilogLogger, Akka.Logger.Serilog""]

    actor {
        provider = remote
    }

    remote {
        dot-netty.tcp {
            port = 9000
            hostname = localhost
        }
    }
}");

            using (var system = ActorSystem.Create("system", config))
            {
                var apiCallService = new ApiService();
                var apiResponseHandlerService = new ApiResposeHandlerService(system.Log);

                var stream = new RecurringCallApiStream(
                    system,
                    apiCallService,
                    apiResponseHandlerService
                );

                system.ActorOf(RecurringApiCallActor.Props(stream, system.Log), "apiActor");

                system.WhenTerminated.Wait();
            }
        }
    }
}
