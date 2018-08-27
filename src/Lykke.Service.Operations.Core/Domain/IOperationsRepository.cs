using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Operations.Contracts;

namespace Lykke.Service.Operations.Core.Domain
{
    public interface IOperationsRepository
    {        
        Task<Operation> Get(Guid id);
        Task<IEnumerable<Operation>> Get(Guid? clientId, OperationStatus? status, OperationType? type);
        Task Create(Operation operation);
        Task UpdateStatus(Guid id, OperationStatus status);
        Task Update(Operation operation);
        Task Save(Operation operation);
    }
}
