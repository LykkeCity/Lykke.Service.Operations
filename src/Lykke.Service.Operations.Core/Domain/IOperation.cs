using System;
using System.Threading.Tasks;
using Lykke.Contracts.Operations;

namespace Lykke.Service.Operations.Core.Domain
{
    public interface IOperation
    {
        Guid Id { get; }
        DateTime Created { get; }
        Guid ClientId { get; }
        OperationType Type { get; }
        OperationStatus Status { get; }
        string AssetId { get; }
        decimal Amount { get; }
        Guid WalletId { get; }
    }

    public interface IOperationsRepository
    {
        Task CreateTransfer(Guid id, Guid clientId, string assetId, decimal amount, Guid walletId);
        Task<IOperation> Get(Guid id);
    }
}
