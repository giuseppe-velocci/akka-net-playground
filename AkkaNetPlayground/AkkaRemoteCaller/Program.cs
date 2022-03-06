using Akka.Actor;
using Akka.Configuration;
using AkkaRemoteCaller.Actors;

namespace AkkaRemoteCaller
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = ConfigurationFactory.ParseString(@"
akka {
    actor {
        provider = remote
    }

    remote {
        dot-netty.tcp {
            port = 0 # bound to a dynamic port assigned by the OS
            hostname = localhost
        }
    }
}");

            using (var system = ActorSystem.Create("clientSystem", config))
            {
                system.ActorOf(RemoteCallerActor.Props(), "apiActor");

                system.WhenTerminated.Wait();
            }
        }
    }
}
