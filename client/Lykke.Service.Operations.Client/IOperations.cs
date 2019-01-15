using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Contracts.Commands;
using Refit;

namespace Lykke.Service.Operations.Client
{
    [PublicAPI]
    public interface IOperations
    {
        [Get("/api/operations/{id}")]
        Task<OperationModel> Get(Guid id);
        [Get("/api/operations/{clientId}/list/{status}")]
        Task<IEnumerable<OperationModel>> Get(Guid clientId, OperationStatus status);
        [Get("/api/operations")]
        Task<IEnumerable<OperationModel>> Get(
            Guid? clientId,
            OperationStatus? status,
            OperationType? type,
            int? skip = 0,
            int? take = 10);
        [Post("/api/operations/newOrder/{id}")]
        Task<Guid> NewOrder(Guid id, CreateNewOrderCommand cmd);
        [Post("/api/operations/order/{id}/market")]
        Task<Guid> MarketOrder(Guid id, CreateMarketOrderCommand command);
        [Post("/api/operations/order/{id}/limit")]
        Task<Guid> LimitOrder(Guid id, CreateLimitOrderCommand command);
        [Post("/api/operations/order/{id}/stoplimit")]
        Task<Guid> StopLimitOrder(Guid id, CreateStopLimitOrderCommand command);
        [Post("/api/operations/cashout/{id}/swift")]
        Task<Guid> CashoutSwift(Guid id, CreateSwiftCashoutCommand command);
        [Post("/api/operations/cashout/{id}")]
        Task<Guid> Cashout(Guid id, CreateCashoutCommand command);
        [Post("/api/operations/transfer/{id}")]
        Task<Guid> Transfer(Guid id, CreateTransferCommand cmd);
        [Post("/api/operations/cancel/{id}")]
        Task Cancel(Guid id);
        [Post("/api/operations/complete/{id}")]
        Task Complete(Guid id);
        [Post("/api/operations/fail/{id}")]
        Task Fail(Guid id);
        [Post("/api/operations/confirm/{id}")]
        Task Confirm(Guid id);
    }
}
