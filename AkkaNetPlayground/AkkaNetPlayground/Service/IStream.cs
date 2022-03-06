using System.Threading.Tasks;

namespace AkkaNetPlayground.Service
{
    public interface IStream
    {
        Task RunStream();
    }
}
