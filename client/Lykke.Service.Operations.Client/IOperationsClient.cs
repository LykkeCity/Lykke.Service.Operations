using JetBrains.Annotations;

namespace Lykke.Service.Operations.Client
{
    [PublicAPI]
    public interface IOperationsClient
    {
        IOperations Operations { get; }
    }
}
