using System.Threading;
using System.Threading.Tasks;

namespace EventHubs.Canary.Console
{
    public interface IProbe
    {
        Task Begin(CancellationToken token);
    }
}