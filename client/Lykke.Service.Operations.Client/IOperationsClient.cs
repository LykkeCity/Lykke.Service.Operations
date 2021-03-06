﻿
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Contracts.Commands;

namespace Lykke.Service.Operations.Client
{
    public interface IOperationsClient
    {
        Task<OperationModel> Get(Guid id);
        Task<IEnumerable<OperationModel>> Get(Guid clientId, OperationStatus status);
        Task<IEnumerable<OperationModel>> Get(Guid? clientId, OperationStatus? status, OperationType? type, int? skip = 0, int? take = 10);
        Task<Guid> Transfer(Guid id, CreateTransferCommand transferCommand);
        Task Cancel(Guid id);
        Task Complete(Guid id);
        Task Confirm(Guid id, ConfirmCommand confirmCommand = null);
        Task Fail(Guid id);

        /// <summary>
        /// Registers a new order with attached client order Id.
        /// </summary>
        /// <param name="id">The order Id</param>
        /// <param name="newOrderCommand">Order related information</param>
        /// <returns>A path to the new context</returns>
        Task<Guid> NewOrder(Guid id, CreateNewOrderCommand newOrderCommand);
        
        Task<Guid> PlaceMarketOrder(Guid id, CreateMarketOrderCommand marketOrderCommand);
        Task<Guid> PlaceLimitOrder(Guid id, CreateLimitOrderCommand limitOrderCommand);
        Task<Guid> PlaceStopLimitOrder(Guid id, CreateStopLimitOrderCommand stopLimitOrderCommand);

        Task<Guid> CreateSwiftCashout(Guid id, CreateSwiftCashoutCommand createSwiftCashoutCommand);
        Task<Guid> CreateCashout(Guid id, CreateCashoutCommand createSwiftCashoutCommand);
    }
}
