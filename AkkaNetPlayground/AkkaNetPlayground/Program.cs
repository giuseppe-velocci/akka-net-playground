using Akka.Actor;
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
            using (var system = ActorSystem.Create("system", "akka { loglevel=DEBUG, loggers=[\"Akka.Logger.Serilog.SerilogLogger, Akka.Logger.Serilog\"]}"))
            {
                var apiCallService = new ApiService();
                var apiResponseHandlerService = new ApiResposeHandlerService(system.Log);

                var stream = new RecurringCallApiStream(
                    system,
                    apiCallService,
                    apiResponseHandlerService
                );

                system.ActorOf(RecurringApiCallActor.Props(stream, system.Log), "demo");

                system.WhenTerminated.Wait();
            }
        }
    }
}
