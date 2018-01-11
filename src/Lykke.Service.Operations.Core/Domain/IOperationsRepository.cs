using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Contracts.Operations;

namespace Lykke.Service.Operations.Core.Domain
{
    public interface IOperationsRepository
    {        
        Task<Operation> Get(Guid id);
        Task<IEnumerable<Operation>> Get(Guid clientId, OperationStatus status);
        Task Create(Guid id, Guid clientId, OperationType operationType, string context);
        Task UpdateStatus(Guid id, OperationStatus status);        
    }
}