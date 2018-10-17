using System.Threading.Tasks;
using Lykke.Sdk;

namespace Lykke.Service.Operations.Services
{
    public class ShutdownManager : IShutdownManager
    {
        public Task StopAsync()
        {
            return Task.CompletedTask;
        }
    }
}
