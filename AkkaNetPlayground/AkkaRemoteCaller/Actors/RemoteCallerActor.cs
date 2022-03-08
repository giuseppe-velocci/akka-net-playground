using Akka.Actor;
using System;

namespace AkkaRemoteCaller.Actors
{
    public class RemoteCallerActor : UntypedActor
    {
        private readonly string remoteActorPath = "akka.tcp://system@localhost:9000/user/apiActor"; 

        protected override void OnReceive(object message)
        {
            throw new NotImplementedException();
        }

        protected override void PreStart()
        {
            Context.ActorSelection(remoteActorPath)
            .Tell("helloRemote!");
        }

        public static Props Props()
        {
            return Akka.Actor.Props.Create(() => new RemoteCallerActor());
        }
    }
}
