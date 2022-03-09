using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using AkkaAzureServiceBusCleaner.Service;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkaAzureServiceBusCleaner.Services
{
    public class StreamService : IStream
    {
        private readonly ISourceService<Result> Service;
        private readonly ActorSystem System;
        private readonly string ActorPath;
        private readonly int BatchSize = 10;

        public StreamService(
            ISourceService<Result> service,
            ActorSystem system,
            string actorPath
        )
        {
            Service = service;
            System = system;
            ActorPath = actorPath;
        }

        public Task Stream()
        {
            var mat = System.Materializer();
            var receiverActorTask = System.ActorSelection(ActorPath).ResolveOne(new System.TimeSpan(0, 0, 5));
            receiverActorTask.Wait();
            var sink = Sink.ActorRefWithAck<Result>(receiverActorTask.Result, "start", "ack", Result.Done);

            return Source
                .FromEnumerator(() => Enumerable.Range(1, BatchSize).GetEnumerator())
                .Log("Enum Source")
                .SelectAsync(2, _ => Service.Execute())
                .Log("SB Source")
                .Recover(_ => Result.Interrupted)
                .AlsoTo(sink)
                .Log("Sink")
                .RunWith(Sink.Ignore<Result>(), mat);
        }
    }
}
