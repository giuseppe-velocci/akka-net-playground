using System;
using System.Net;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Akka.Streams;
using Akka.Streams.Dsl;

namespace AkkaNetPlayground.Service
{
    public class RecurringCallApiStream : IStream
    {
        private readonly IFlowService<HttpStatusCode> FlowService;
        private readonly ISinkService<HttpStatusCode> SinkService;
        private readonly ActorSystem System;

        public RecurringCallApiStream(
            ActorSystem system,
            IFlowService<HttpStatusCode> flowService,
            ISinkService<HttpStatusCode> sinkService
            )
        {
            System = system;
            FlowService = flowService;
            SinkService = sinkService;
        }

        public Task RunStream()
        {
            var source = Source.FromTask(FlowService.Execute())
                .Log("source")
                .WithAttributes(Attributes.CreateLogLevels(onElement: LogLevel.DebugLevel));

            var sink = Sink.ForEach<HttpStatusCode>(statusCode => SinkService.Execute(statusCode));

            return source
                .Recover(_ => HttpStatusCode.InternalServerError)
                .RunWith(sink, System.Materializer());
        }
    }
}
