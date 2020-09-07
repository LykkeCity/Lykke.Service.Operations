using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Core.Domain;

namespace Lykke.Service.Operations.Core.Services
{
    public interface IOperationsCacheService
    {
        Task<Operation> GetAsync(Guid id);
        Task<IEnumerable<Operation>> GetAsync(Guid clientId, OperationStatus? status, OperationType? type, int? skip = 0, int? take = 10);
        Task CreateAsync(Operation operation);
        Task UpdateStatusAsync(Guid id, OperationStatus status);
        Task SaveAsync(Operation operation);
        Task ClearAsync();
    }
}
