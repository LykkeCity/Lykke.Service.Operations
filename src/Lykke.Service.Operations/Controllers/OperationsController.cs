﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Lykke.Contracts.Operations;
using Lykke.Service.ClientAccount.Client.AutorestClient;
using Lykke.Service.ClientAccount.Client.AutorestClient.Models;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Models;
using Lykke.Service.PushNotifications.Client.AutorestClient;
using Lykke.Service.PushNotifications.Client.AutorestClient.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Lykke.Service.Operations.Controllers
{
    [Route("api/operations")]
    public class OperationsController : Controller
    {
        private readonly IOperationsRepository _operationsRepository;
        private readonly IClientAccountService _clientAccountService;
        private readonly IPushNotificationsAPI _pushNotificationsApi;

        public OperationsController(IOperationsRepository operationsRepository, IClientAccountService clientAccountService, IPushNotificationsAPI pushNotificationsApi)
        {
            _operationsRepository = operationsRepository;
            _clientAccountService = clientAccountService;
            _pushNotificationsApi = pushNotificationsApi;
        }

        [HttpGet]
        [Route("{id}")]
        [ProducesResponseType(typeof(OperationModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(OperationResult), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Get(Guid? id)
        {
            if (!id.HasValue)
                return BadRequest(new OperationResult("id", "Operation id is required"));

            var operation = await _operationsRepository.Get(id.Value);

            if (operation == null)
                return NotFound();

            return Ok(new OperationModel
            {
                Id = id.Value,
                Created = operation.Created,
                Type = operation.Type,
                Status = operation.Status,
                ClientId = operation.ClientId,
                Context = JObject.FromObject(new
                {
                    operation.AssetId,
                    operation.Amount,
                    operation.SourceWalletId,
                    operation.WalletId,
                    TransferType = operation.TransferType.ToString()
                })
            });
        }
        
        [HttpGet]
        [Route("{clientId}/list/{status}")]
        [ProducesResponseType(typeof(IEnumerable<OperationModel>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Get(Guid clientId, OperationStatus status)
        {
            var operations = await _operationsRepository.Get(clientId, status);

            var result = operations.Select(o => new OperationModel
            {
                Id = o.Id,
                Created = o.Created,
                Type = o.Type,
                Status = o.Status,
                ClientId = o.ClientId,
                Context = JObject.FromObject(new
                {
                    o.AssetId,
                    o.Amount,
                    o.SourceWalletId,
                    o.WalletId,
                    TransferType = o.TransferType.ToString()
                })
            });

            return Ok(result);
        }
        
        [HttpPost]
        [Route("transfer/{id}")]
        [ProducesResponseType(typeof(Guid), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(OperationResult), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(Guid), (int)HttpStatusCode.Created)]
        public async Task<IActionResult> Transfer([FromBody]CreateTransferCommand cmd, Guid? id)
        {
            if (!id.HasValue)
                return BadRequest(new OperationResult("id", "Operation id is required"));

            if (!ModelState.IsValid)
                return BadRequest(new OperationResult(ModelState));

            var operation = await _operationsRepository.Get(id.Value);

            if (operation != null)
                return BadRequest(new OperationResult("id", "Operation with the id already exists."));

            var clientAccount = (ClientResponseModel)await _clientAccountService.GetByIdAsync(cmd.ClientId.ToString());
            var isSourceWalletIsTrusted = await _clientAccountService.IsTrustedAsync(cmd.SourceWalletId.ToString()) ?? false;
            var isDestinationWalletIsTrusted = await _clientAccountService.IsTrustedAsync(cmd.WalletId.ToString()) ?? false;

            var transferType = TransferType.TrustedToTrusted;

            if (!isSourceWalletIsTrusted)
            {
                transferType = TransferType.TradingToTrusted;
            }
            else if (!isDestinationWalletIsTrusted)
            {
                transferType = TransferType.TrustedToTrading;
            }

            await _operationsRepository.CreateTransfer(id.Value, transferType, cmd.ClientId, cmd.AssetId, cmd.Amount, cmd.SourceWalletId, cmd.WalletId);            

            await _pushNotificationsApi.SendDataNotificationToAllDevicesAsync(new DataNotificationModel(NotificationType.OperationCreated, new[] { clientAccount.NotificationsId } ));
            
            return Created(Url.Action("Get", new { id }), id);
        }

        [HttpPost]
        [Route("cancel/{id}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(OperationResult), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Cancel(Guid id)
        {            
            var operation = await _operationsRepository.Get(id);

            if (operation == null)
                return NotFound();

            if (operation.Status != OperationStatus.Created)
                return BadRequest(new OperationResult("id", "An operation in created status could be canceled"));

            await _operationsRepository.UpdateStatus(id, OperationStatus.Canceled);

            return Ok();
        }

        [HttpPost]
        [Route("complete/{id}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(OperationResult), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Complete(Guid id)
        {
            var operation = await _operationsRepository.Get(id);

            if (operation == null)
                return NotFound();
            
            await _operationsRepository.UpdateStatus(id, OperationStatus.Completed);

            return Ok();
        }

        [HttpPost]
        [Route("fail/{id}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(OperationResult), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Fail(Guid id)
        {
            var operation = await _operationsRepository.Get(id);

            if (operation == null)
                return NotFound();

            await _operationsRepository.UpdateStatus(id, OperationStatus.Failed);

            return Ok();
        }
    }
}
