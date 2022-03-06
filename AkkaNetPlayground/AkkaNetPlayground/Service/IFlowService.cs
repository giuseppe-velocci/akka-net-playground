using System.Threading.Tasks;

namespace AkkaNetPlayground.Service
{
    public interface IFlowService<T>
    {
        Task<T> Execute();
    }
}
