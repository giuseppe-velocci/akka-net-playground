using System.Threading.Tasks;

namespace AkkaNetPlayground.Service
{
    public interface ISinkService<TIn>
    {
        Task Execute(TIn apiResponse);
    }
}
