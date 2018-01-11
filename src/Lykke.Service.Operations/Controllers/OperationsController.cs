﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Lykke.Contracts.Operations;
using Lykke.Service.Assets.Client;
using Lykke.Service.ClientAccount.Client.AutorestClient;
using Lykke.Service.ClientAccount.Client.AutorestClient.Models;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Models;
using Lykke.Service.PushNotifications.Client.AutorestClient;
using Lykke.Service.PushNotifications.Client.AutorestClient.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Lykke.Service.Operations.Controllers
{
    [Route("api/operations")]
    public class OperationsController : Controller
    {
        private readonly IOperationsRepository _operationsRepository;
        private readonly IClientAccountService _clientAccountService;
        private readonly IPushNotificationsAPI _pushNotificationsApi;
        private readonly IAssetsServiceWithCache _assetsServiceWithCache;

        public OperationsController(IOperationsRepository operationsRepository, IClientAccountService clientAccountService, IPushNotificationsAPI pushNotificationsApi, IAssetsServiceWithCache assetsServiceWithCache)
        {
            _operationsRepository = operationsRepository;
            _clientAccountService = clientAccountService;
            _pushNotificationsApi = pushNotificationsApi;
            _assetsServiceWithCache = assetsServiceWithCache;
        }

        [HttpGet]
        [Route("{id}")]
        [ProducesResponseType(typeof(OperationModel), (int)HttpStatusCode.OK)]
        public async Task<OperationModel> Get(Guid id)
        {
            var operation = await _operationsRepository.Get(id);

            if (operation == null)
                throw new ApiException(HttpStatusCode.NotFound, new ApiResult("id", "Operation not found"));

            return new OperationModel
            {
                Id = id,
                Created = operation.Created,
                Type = operation.Type,
                Status = operation.Status,
                ClientId = operation.ClientId,
                Context = JObject.Parse(operation.Context)
            };
        }
        
        [HttpGet]
        [Route("{clientId}/list/{status}")]
        [ProducesResponseType(typeof(IEnumerable<OperationModel>), (int)HttpStatusCode.OK)]
        public async Task<IEnumerable<OperationModel>> Get(Guid clientId, OperationStatus status)
        {
            var operations = await _operationsRepository.Get(clientId, status);

            var result = operations.Select(o => new OperationModel
            {
                Id = o.Id,
                Created = o.Created,
                Type = o.Type,
                Status = o.Status,
                ClientId = o.ClientId,
                Context = JObject.Parse(o.Context)
            });

            return result;
        }
        
        [HttpPost]
        [Route("transfer/{id}")]         
        [ProducesResponseType(typeof(Guid), (int)HttpStatusCode.Created)]
        public async Task<IActionResult> Transfer(Guid id, [FromBody]CreateTransferCommand cmd)
        {
            if (id == Guid.Empty)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("id", "Operation id must be non empty"));

            if (!ModelState.IsValid)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult(ModelState));                

            var operation = await _operationsRepository.Get(id);

            if (operation != null)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("id", "Operation with the id already exists."));
            
            var clientAccount = (ClientResponseModel)await _clientAccountService.GetByIdAsync(cmd.ClientId.ToString());
            var isSourceWalletIsTrusted = (bool?)await _clientAccountService.IsTrustedAsync(cmd.SourceWalletId.ToString()) ?? false;
            var isSourceAssetIsTrusted = (await _assetsServiceWithCache.TryGetAssetAsync(cmd.AssetId))?.IsTrusted ?? false;
            var isDestinationWalletIsTrusted = (bool?)await _clientAccountService.IsTrustedAsync(cmd.WalletId.ToString()) ?? false;

            var transferType = TransferType.TrustedToTrusted;

            if (!isSourceWalletIsTrusted)
            {
                transferType = TransferType.TradingToTrusted;
                cmd.SourceWalletId = cmd.ClientId;
            }
            else if (!isDestinationWalletIsTrusted)
            {
                transferType = TransferType.TrustedToTrading;
                cmd.WalletId = cmd.ClientId;
            }

            if (isSourceAssetIsTrusted)
                transferType = TransferType.TrustedToTrusted;

            var context = new TransferContext
            {
                AssetId = cmd.AssetId,
                Amount = cmd.Amount,
                SourceWalletId = cmd.SourceWalletId,
                WalletId = cmd.WalletId,
                TransferType = transferType
            };

            await _operationsRepository.Create(id, cmd.ClientId, OperationType.Transfer, JsonConvert.SerializeObject(context));            

            await _pushNotificationsApi.SendDataNotificationToAllDevicesAsync(new DataNotificationModel(NotificationType.OperationCreated, new[] { clientAccount.NotificationsId }, "Operation" ));
            
            return Created(Url.Action("Get", new { id }), id);
        }
   
        [HttpPost]
        [Route("cancel/{id}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task Cancel(Guid id)
        {
            if (id == Guid.Empty)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("id", "Operation id must be non empty"));

            var operation = await _operationsRepository.Get(id);

            if (operation == null)
                throw new ApiException(HttpStatusCode.NotFound, new ApiResult("id", "Operation not found"));

            if (operation.Status == OperationStatus.Canceled)
                return;

            if (operation.Status != OperationStatus.Created)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("id", "An operation in created status could be canceled"));

            await _operationsRepository.UpdateStatus(id, OperationStatus.Canceled);            
        }

        [HttpPost]
        [Route("complete/{id}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task Complete(Guid id)
        {
            if (id == Guid.Empty)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("id", "Operation id must be non empty"));

            var operation = await _operationsRepository.Get(id);

            if (operation == null || operation.Status == OperationStatus.Completed || operation.Status == OperationStatus.Confirmed)
                return;
            
            await _operationsRepository.UpdateStatus(id, OperationStatus.Completed);            
        }

        [HttpPost]
        [Route("fail/{id}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task Fail(Guid id)
        {
            if (id == Guid.Empty)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("id", "Operation id must be non empty"));

            var operation = await _operationsRepository.Get(id);

            if (operation == null || operation.Status == OperationStatus.Failed)
                return;

            await _operationsRepository.UpdateStatus(id, OperationStatus.Failed);            
        }

        [HttpPost]
        [Route("confirm/{id}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task Confirm(Guid id)
        {
            if (id == Guid.Empty)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("id", "Operation id must be non empty"));

            var operation = await _operationsRepository.Get(id);

            if (operation == null)
                throw new ApiException(HttpStatusCode.NotFound, new ApiResult("id", "Operation not found"));

            if (operation.Status == OperationStatus.Confirmed)
                return;

            if (operation.Status != OperationStatus.Created) // todo: accepted?
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("id", "An operation in created status could be confirmed"));

            await _operationsRepository.UpdateStatus(id, OperationStatus.Confirmed);
        }
    }
}
