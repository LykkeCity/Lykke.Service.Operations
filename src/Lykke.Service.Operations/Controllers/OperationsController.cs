using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Common.Log;
using Lykke.Contracts.Operations;
using Lykke.Cqrs;
using Lykke.Service.Assets.Client;
using Lykke.Service.ClientAccount.Client.AutorestClient;
using Lykke.Service.ClientAccount.Client.AutorestClient.Models;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Contracts.Events;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Models;
using Lykke.Service.Operations.Settings.ServiceSettings;
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
        private readonly Guid _paymentsHotWalletId;
        private readonly ICqrsEngine _cqrsEngine;
        private readonly ILog _log;
        private readonly IMapper _mapper;

        public OperationsController(            
            IOperationsRepository operationsRepository, 
            IClientAccountService clientAccountService, 
            IPushNotificationsAPI pushNotificationsApi, 
            IAssetsServiceWithCache assetsServiceWithCache,
            Func<string, Operation, OperationWorkflow> workflowFactory,
            PaymentsSettings paymentsSettings,
            ICqrsEngine cqrsEngine,
            ILog log,
            IMapper mapper)
        {
            _operationsRepository = operationsRepository;
            _clientAccountService = clientAccountService;
            _pushNotificationsApi = pushNotificationsApi;
            _assetsServiceWithCache = assetsServiceWithCache;
            _paymentsHotWalletId = Guid.Parse(paymentsSettings.HotWalletId);
            _workflowFactory = workflowFactory;
            _cqrsEngine = cqrsEngine;
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

            await _operationsRepository.Create(operation);

            return Created(Url.Action("Get", new { id }), id);
        }

        [HttpPost]
        [Route("payment/{id}")]
        [ProducesResponseType(typeof(Guid), (int) HttpStatusCode.Created)]
        public async Task<IActionResult> Payment(Guid id, [FromBody] CreatePaymentCommand command)
        {
            if (id == Guid.Empty)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("id", "Operation id must be non empty"));
            
            if (!ModelState.IsValid)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult(ModelState));

            var operation = await _operationsRepository.Get(id);

            if (operation != null)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("id", "Operation with the id already exists."));
            
            if (await _assetsServiceWithCache.TryGetAssetAsync(command.AssetId) == null)
                throw new ApiException(HttpStatusCode.NotFound, new ApiResult("assetId", "Asset doesn't exist"));
            
            var newOperation = new Operation();
            
            var context = new PaymentContext
            {
                AssetId = command.AssetId,
                Amount = command.Amount,
                WalletId = _paymentsHotWalletId
            };
            
            newOperation.Create(id, null, OperationType.Payment, JsonConvert.SerializeObject(context));

            await _operationsRepository.Create(newOperation);
            
            return Created(Url.Action("Get", new { id }), id);
        }
        
        [HttpPut]
        [Route("payment/{id}")]
        [ProducesResponseType(typeof(bool), (int) HttpStatusCode.OK)]
        public async Task<bool> SetPaymentFrom(Guid id, [FromBody] SetPaymentClientIdCommand cmd)
        {
            if (id == Guid.Empty)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("id", "Operation id must be non empty"));
            
            if (cmd.ClientId == Guid.Empty)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("id", "Client id must be non empty"));
            
            if(await _clientAccountService.GetClientByIdAsync(cmd.ClientId.ToString()) == null)
                throw new ApiException(HttpStatusCode.NotFound, new ApiResult("clientId", "Client doesn't exist"));
            
            var operation = await _operationsRepository.Get(id);

            if (operation == null)
                throw new ApiException(HttpStatusCode.NotFound, new ApiResult("id", "Operation not found"));

            var setSuccessfully = await _operationsRepository.SetClientId(id, cmd.ClientId);

            return setSuccessfully;
        }


		[HttpPost]
        [Route("order/{id}/market")]
        [ProducesResponseType(typeof(Guid), (int)HttpStatusCode.Created)]		
        public async Task<IActionResult> MarketOrder(Guid id, [FromBody] CreateMarketOrderCommand command)
		{
		    if (id == Guid.Empty)
		        throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("id", "Operation id must be non empty"));

		    if (!ModelState.IsValid)
		        throw new ApiException(HttpStatusCode.BadRequest, new ApiResult(ModelState));

		    var operation = await _operationsRepository.Get(id);

		    if (operation != null)
		        throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("id", "Operation with the id already exists."));

            var context = new
            {
                Asset = command.AssetPair.BaseAsset.Id == command.AssetId ? command.AssetPair.BaseAsset : command.AssetPair.QuotingAsset,
                command.AssetPair,
                command.Volume,
                command.OrderAction,
                command.Client,
                command.GlobalSettings
            };

		    operation = new Operation();
		    operation.Create(id, command.Client.Id, OperationType.MarketOrder, JsonConvert.SerializeObject(context, Formatting.Indented));

		    _cqrsEngine.PublishEvent(new OperationCreatedEvent { Id = id, ClientId = command.Client.Id }, "operations");
            
		    await HandleOrder("MarketOrderWorkflow", operation);
		    
		    return Created(Url.Action("Get", new { id }), id);
		}

        [HttpPost]
        [Route("order/{id}/limit")]
        [ProducesResponseType(typeof(Guid), (int)HttpStatusCode.Created)]
        public async Task<IActionResult> LimitOrder(Guid id, [FromBody] CreateLimitOrderCommand command)
        {           
            if (id == Guid.Empty)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("id", "Operation id must be non empty"));

            if (!ModelState.IsValid)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult(ModelState));

            var operation = await _operationsRepository.Get(id);

            if (operation != null)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("id", "Operation with the id already exists."));

            var context = new
            {
                Asset = command.AssetPair.BaseAsset,
                command.AssetPair,
                command.Volume,
                command.Price,
                command.OrderAction,
                command.Client,
                command.GlobalSettings
            };

            operation = new Operation();
            operation.Create(id, command.Client.Id, OperationType.LimitOrder, JsonConvert.SerializeObject(context, Formatting.Indented));

            _cqrsEngine.PublishEvent(new OperationCreatedEvent { Id = id, ClientId = command.Client.Id }, "operations");
            
            await HandleOrder("LimitOrderWorkflow", operation);
            
            return Created(Url.Action("Get", new { id }), id);
        }

        private async Task HandleOrder(string workflowType, Operation operation)
        {           
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

            await _operationsRepository.Create(operation);

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

            operation.OperationValues = JObject.Parse(operation.Context);

            switch (operation.Type)
            {
                case OperationType.MarketOrder:
                    await HandleOrder("MarketOrderWorkflow", operation);
                    break;
                case OperationType.LimitOrder:
                    await HandleOrder("LimitOrderWorkflow", operation);
                    break;
                default:
                    await _operationsRepository.UpdateStatus(id, OperationStatus.Confirmed);
                    break;
            }            
        }
    }
}
