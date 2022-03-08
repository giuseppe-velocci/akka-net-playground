using System.Threading.Tasks;

namespace AkkaAzureServiceBusCleaner.Services
{
    public interface IStream
    {
        Task Stream();
    }
}
