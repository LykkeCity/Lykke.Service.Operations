using System;
using System.Collections.Generic;
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
        string Context { get; set; }
    }

    public interface IOperationsRepository
    {        
        Task<IOperation> Get(Guid id);
        Task<IEnumerable<IOperation>> Get(Guid clientId, OperationStatus status);
        Task Create(Guid id, Guid clientId, OperationType operationType, string context);
        Task UpdateStatus(Guid id, OperationStatus status);        
    }
}
