using System.Threading.Tasks;

namespace AkkaAzureServiceBusCleaner.Service
{
    public interface ISourceService<TOut>
    {
        Task<TOut> Execute();
    }
}
