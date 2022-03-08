using System.Net;
using System.Web.Http;

namespace DummyServerApi.Controllers
{
    public class ValuesController : ApiController
    {
        // GET api/values
        public string Get()
        {
            return HttpStatusCode.OK.ToString();
        }
    }
}
