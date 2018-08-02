using System.Threading.Tasks;

namespace EventHubs.Canary.Console
{
    public interface IClient
    {
        Task SendAsync(byte[] data, string partitionKey = null);
    }
}