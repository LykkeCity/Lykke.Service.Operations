using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Common.Log;
using Lykke.Contracts.Operations;
using Lykke.Service.Assets.Client;
using Lykke.Service.ClientAccount.Client.AutorestClient;
using Lykke.Service.ClientAccount.Client.AutorestClient.Models;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Models;
using Lykke.Service.Operations.Workflow;
using Lykke.Service.PushNotifications.Client.AutorestClient;
using Lykke.Service.PushNotifications.Client.AutorestClient.Models;
using Lykke.Workflow;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OperationStatus = Lykke.Service.Operations.Contracts.OperationStatus;
using OperationType = Lykke.Service.Operations.Contracts.OperationType;

namespace Lykke.Service.Operations.Controllers
{
    [Route("api/operations")]
    [Produces("application/json")]
    public class OperationsController : Controller
    {        
        private readonly IOperationsRepository _operationsRepository;
        private readonly IClientAccountService _clientAccountService;
        private readonly IPushNotificationsAPI _pushNotificationsApi;
        private readonly IAssetsServiceWithCache _assetsServiceWithCache;
        private readonly Func<string, Operation, OperationWorkflow> _workflowFactory;
        private readonly ILog _log;
        private readonly IMapper _mapper;

        public OperationsController(            
            IOperationsRepository operationsRepository, 
            IClientAccountService clientAccountService, 
            IPushNotificationsAPI pushNotificationsApi, 
            IAssetsServiceWithCache assetsServiceWithCache,
            Func<string, Operation, OperationWorkflow> workflowFactory,
            ILog log,
            IMapper mapper)
        {
            _operationsRepository = operationsRepository;
            _clientAccountService = clientAccountService;
            _pushNotificationsApi = pushNotificationsApi;
            _assetsServiceWithCache = assetsServiceWithCache;
            _workflowFactory = workflowFactory;
            _log = log;
            _mapper = mapper;
        }

        [HttpGet]
        [Route("{id}")]
        [ProducesResponseType(typeof(OperationModel), (int)HttpStatusCode.OK)]
        public async Task<OperationModel> Get(Guid id)
        {
            var operation = await _operationsRepository.Get(id);

            if (operation == null)
                throw new ApiException(HttpStatusCode.NotFound, new ApiResult("id", "Operation not found"));

            var result = _mapper.Map<Operation, OperationModel>(operation);

            return result;
        }

        [HttpGet]
        [Route("{clientId}/list/{status}")]
        [ProducesResponseType(typeof(IEnumerable<OperationModel>), (int)HttpStatusCode.OK)]
        public async Task<IEnumerable<OperationModel>> Get(Guid clientId, OperationStatus status)
        {
            var operations = await _operationsRepository.Get(clientId, status);

            var result = _mapper.Map<IEnumerable<Operation>, IEnumerable<OperationModel>>(operations);

            return result;
        }

        /// <summary>
        /// Registers a new order with attached client order Id.
        /// </summary>
        /// <param name="id">The order Id</param>
        /// <param name="cmd">Order related information</param>
        /// <returns>A path to the new context</returns>
        [HttpPost]
        [Route("newOrder/{id}")]
        [ProducesResponseType(typeof(Guid), (int)HttpStatusCode.Created)]
        public async Task<IActionResult> NewOrder(Guid id, [FromBody]CreateNewOrderCommand cmd)
        {
            if (id == Guid.Empty)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("id", "Operation id must be not empty and has a correct GUID value"));

            if (!ModelState.IsValid)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult(ModelState));

            var operation = await _operationsRepository.Get(id);

            if (operation != null)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("id", "Operation with the id already exists."));

            var context = new NewOrderContext
            {
                ClientOrderId = cmd.ClientOrderId,
            };

            operation = new Operation();
            operation.Create(id, cmd.WalletId, OperationType.NewOrder, JsonConvert.SerializeObject(context));

            await _operationsRepository.Save(operation);

            return Created(Url.Action("Get", new { id }), id);
        }

		[HttpPost]
        [Route("order/{id}/market")]
        [ProducesResponseType(typeof(Guid), (int)HttpStatusCode.Created)]		
        public async Task<IActionResult> MarketOrder(Guid id, [FromBody] CreateMarketOrderCommand command)
		{
		    await HandleOrder(id, command.Client.Id,  OperationType.MarketOrder, "MarketOrderWorkflow", JsonConvert.SerializeObject(command, Formatting.Indented));

		    return Created(Url.Action("Get", new { id }), id);
		}

        [HttpPost]
        [Route("order/{id}/limit")]
        [ProducesResponseType(typeof(Guid), (int)HttpStatusCode.Created)]
        public async Task<IActionResult> LimitOrder(Guid id, [FromBody] CreateLimitOrderCommand command)
        {
            await HandleOrder(id, command.Client.Id, OperationType.LimitOrder, "LimitOrderWorkflow", JsonConvert.SerializeObject(command, Formatting.Indented));

            return Created(Url.Action("Get", new { id }), id);
        }

        private async Task HandleOrder(Guid id, Guid clientId, OperationType operationType, string workflowType, string command)
        {
            if (id == Guid.Empty)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("id", "Operation id must be non empty"));

            if (!ModelState.IsValid)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult(ModelState));

            var operation = await _operationsRepository.Get(id);

            if (operation != null)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("id", "Operation with the id already exists."));

            operation = new Operation();
            operation.Create(id, clientId, operationType, command);

            var wf = _workflowFactory(workflowType, operation);
            var wfResult = wf.Run(operation);

            await _operationsRepository.Save(operation);

            if (wfResult.State == WorkflowState.Corrupted)
            {
                _log.WriteFatalError(nameof(MarketOrderWorkflow), JsonConvert.SerializeObject(wfResult, Formatting.Indented));

                throw new ApiException(HttpStatusCode.InternalServerError, new ApiResult("_", wfResult.Error));
            }

            if (operation.Status == OperationStatus.Failed)
            {
                var modelState = new ModelStateDictionary();
                JArray errors = operation.OperationValues.ValidationErrors;

                if (errors != null)
                    foreach (var error in errors)
                    {
                        modelState.AddModelError(error["PropertyName"].ToString(), error["ErrorMessage"].ToString());
                    }

                string errorMessage = operation.OperationValues.ErrorMessage;

                if (!string.IsNullOrWhiteSpace(errorMessage))
                    modelState.AddModelError("_", errorMessage);

                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult(modelState));
            }
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

            var clientResponse = await _clientAccountService.GetByIdAsync(cmd.ClientId.ToString());
            if (clientResponse is ClientAccount.Client.AutorestClient.Models.ErrorResponse)
            {
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("clientId", "Non-existed client."));
            }
            var clientAccount = (ClientResponseModel)clientResponse;

            var isSourceWalletTrustedResponse = await _clientAccountService.IsTrustedAsync(cmd.SourceWalletId.ToString());
            if (isSourceWalletTrustedResponse is ClientAccount.Client.AutorestClient.Models.ErrorResponse)
            {
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("sourceWalletId", "Non-existed wallet."));
            }
            var isSourceWalletTrusted = (bool?)isSourceWalletTrustedResponse ?? false;

            var isDestinationWalletTrustedResponse = await _clientAccountService.IsTrustedAsync(cmd.WalletId.ToString());
            if (isDestinationWalletTrustedResponse is ClientAccount.Client.AutorestClient.Models.ErrorResponse)
            {
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("walletId", "Non-existed wallet."));
            }
            var isDestinationWalletTrusted = (bool?)isDestinationWalletTrustedResponse ?? false;

            var isSourceAssetTrusted = (await _assetsServiceWithCache.TryGetAssetAsync(cmd.AssetId))?.IsTrusted ?? false;

            var transferType = TransferType.TrustedToTrusted;

            if (!isSourceWalletTrusted)
            {
                transferType = TransferType.TradingToTrusted;
                cmd.SourceWalletId = cmd.ClientId;
            }
            else if (!isDestinationWalletTrusted)
            {
                transferType = TransferType.TrustedToTrading;
                cmd.WalletId = cmd.ClientId;
            }

            if (isSourceAssetTrusted)
                transferType = TransferType.TrustedToTrusted;

            var context = new TransferContext
            {
                AssetId = cmd.AssetId,
                Amount = cmd.Amount,
                SourceWalletId = cmd.SourceWalletId,
                WalletId = cmd.WalletId,
                TransferType = transferType
            };

            operation = new Operation();
            operation.Create(id, cmd.ClientId, OperationType.Transfer, JsonConvert.SerializeObject(context, Formatting.Indented));

            await _operationsRepository.Save(operation);

            await _pushNotificationsApi.SendDataNotificationToAllDevicesAsync(new DataNotificationModel(NotificationType.OperationCreated, new[] { clientAccount.NotificationsId }, "Operation"));

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
