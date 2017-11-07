using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Contracts.Operations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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
        Guid SourceWalletId { get; }
        Guid WalletId { get; }        
        TransferType TransferType { get; set; }
    }

    public interface IOperationsRepository
    {        
        Task<IOperation> Get(Guid id);
        Task<IEnumerable<IOperation>> Get(Guid clientId, OperationStatus status);
        Task CreateTransfer(Guid id, TransferType transferType, Guid clientId, string assetId, decimal amount, Guid sourceWalletId, Guid walletId);
        Task UpdateStatus(Guid id, OperationStatus status);        
    }
}
