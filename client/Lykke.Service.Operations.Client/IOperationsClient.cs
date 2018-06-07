
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        /// <summary>
        /// Registers a new order with attached client order Id.
        /// </summary>
        /// <param name="id">The order Id</param>
        /// <param name="newOrderCommand">Order related information</param>
        /// <returns>A path to the new context</returns>
        Task<Guid> NewOrder(Guid id, CreateNewOrderCommand newOrderCommand);
        
        Task<Guid> PlaceMarketOrder(Guid id, CreateMarketOrderCommand marketOrderCommand);
        Task<Guid> PlaceLimitOrder(Guid id, CreateLimitOrderCommand marketOrderCommand);

        Task<Guid> CreateSwiftCashout(Guid id, CreateSwiftCashoutCommand createSwiftCashoutCommand);
    }
}
