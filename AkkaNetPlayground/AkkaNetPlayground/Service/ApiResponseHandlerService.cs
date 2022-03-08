using Akka.Event;
using System.Net;
using System.Threading.Tasks;

namespace AkkaNetPlayground.Service
{
    public class ApiResposeHandlerService : ISinkService<HttpStatusCode>
    {
        private readonly ILoggingAdapter Logger;

        public ApiResposeHandlerService(ILoggingAdapter logger)
        {
            Logger = logger;
        }

        public Task Execute(HttpStatusCode apiResponse)
        {
            Logger.Info($"Response status code is: {apiResponse}");
            return Task.CompletedTask;
        }
    }
}
