
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Contracts.Operations;
using Lykke.Service.Operations.Contracts;

namespace Lykke.Service.Operations.Client
{
    public interface IOperationsClient
    {
        Task<OperationModel> Get(Guid id);
        Task<IEnumerable<OperationModel>> Get(Guid clientId, OperationStatus status);
        Task<Guid> Transfer(Guid id, CreateTransferCommand transferCommand);
        Task Cancel(Guid id);
        Task Complete(Guid id);
        Task Confirm(Guid id);
        Task Fail(Guid id);
    }
}
