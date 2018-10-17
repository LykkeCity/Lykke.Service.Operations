using System.Threading.Tasks;

namespace Lykke.Service.Operations.Core.Services
{
    public interface IShutdownManager
    {
        Task StopAsync();
    }
}