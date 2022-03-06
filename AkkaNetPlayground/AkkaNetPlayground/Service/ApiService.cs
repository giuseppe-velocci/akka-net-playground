using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace AkkaNetPlayground.Service
{
    public class ApiService : IFlowService<HttpStatusCode>
    {
        public async Task<HttpStatusCode> Execute()
        {
            HttpClient client = new HttpClient();
            var response = await client.GetAsync("http://localhost:57021/api/values");
            return response.StatusCode;
        }
    }
}
