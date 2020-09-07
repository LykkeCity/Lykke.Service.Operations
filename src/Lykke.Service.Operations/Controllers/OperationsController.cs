using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Common.Log;
using Lykke.Common.ApiLibrary.Exceptions;
using Lykke.Common.Log;
using Lykke.Contracts.Operations;
using Lykke.Cqrs;
using Lykke.Service.Assets.Client;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.EthereumCore.Client;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Contracts.Api;
using Lykke.Service.Operations.Contracts.Commands;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Core.Services;
using Lykke.Service.Operations.Models;
using Lykke.Service.Operations.Workflow;
using Lykke.Service.Operations.Workflow.Commands;
using Lykke.Service.PushNotifications.Client.AutorestClient;
using Lykke.Service.PushNotifications.Client.AutorestClient.Models;
using Lykke.Workflow;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ApiException = Lykke.Service.Operations.Models.ApiException;
using OperationStatus = Lykke.Service.Operations.Contracts.OperationStatus;
using OperationType = Lykke.Service.Operations.Contracts.OperationType;

namespace Lykke.Service.Operations.Controllers
{
    [Route("api/operations")]
    [Produces("application/json")]
    public class OperationsController : Controller
    {
        private readonly IOperationsRepository _operationsRepository;
        private readonly IClientAccountClient _clientAccountService;
        private readonly IPushNotificationsAPI _pushNotificationsApi;
        private readonly IAssetsServiceWithCache _assetsServiceWithCache;
        private readonly Func<string, Operation, OperationWorkflow> _workflowFactory;
        private readonly ICqrsEngine _cqrsEngine;
        private readonly EthereumServiceClientSettings _ethereumServiceClientSettings;
        private readonly IOperationsCacheService _operationsCacheService;
        private readonly ILog _log;
        private readonly IMapper _mapper;

        public OperationsController(
            IOperationsRepository operationsRepository,
            IClientAccountClient clientAccountService,
            IPushNotificationsAPI pushNotificationsApi,
            IAssetsServiceWithCache assetsServiceWithCache,
            Func<string, Operation, OperationWorkflow> workflowFactory,
            ICqrsEngine cqrsEngine,
            EthereumServiceClientSettings ethereumServiceClientSettings,
            IOperationsCacheService operationsCacheService,
            ILogFactory log,
            IMapper mapper)
        {
            _operationsRepository = operationsRepository;
            _clientAccountService = clientAccountService;
            _pushNotificationsApi = pushNotificationsApi;
            _assetsServiceWithCache = assetsServiceWithCache;
            _workflowFactory = workflowFactory;
            _cqrsEngine = cqrsEngine;
            _ethereumServiceClientSettings = ethereumServiceClientSettings;
            _operationsCacheService = operationsCacheService;
            _log = log.CreateLog(this);
            _mapper = mapper;
        }

        [HttpGet]
        [Route("{id}")]
        [ProducesResponseType(typeof(OperationModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        public async Task<OperationModel> Get(Guid id)
        {
            var operation = await _operationsCacheService.GetAsync(id);

            if (operation == null)
                throw new ApiException(HttpStatusCode.NotFound, new ApiResult("id", "Operation not found"));

            var result = _mapper.Map<Operation, OperationModel>(operation);

            return result;
        }

        [HttpGet]
        [Obsolete()]
        [Route("{clientId}/list/{status}")]
        [ProducesResponseType(typeof(IEnumerable<OperationModel>), (int)HttpStatusCode.OK)]
        public async Task<IEnumerable<OperationModel>> Get(Guid clientId, OperationStatus status)
        {
            var operations = await _operationsCacheService.GetAsync(clientId, status, null);

            var result = _mapper.Map<IEnumerable<Operation>, IEnumerable<OperationModel>>(operations);

            return result;
        }

        [HttpGet]
        [Route("")]
        [ProducesResponseType(typeof(IEnumerable<OperationModel>), (int)HttpStatusCode.OK)]
        public async Task<IEnumerable<OperationModel>> Get(Guid? clientId, OperationStatus? status, OperationType? type, int? skip = 0, int? take = 10)
        {
            var operations = await _operationsCacheService.GetAsync(clientId ?? Guid.Empty, status, type, skip, take);

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

            var operation = await _operationsCacheService.GetAsync(id);

            if (operation != null)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("id", "Operation with the id already exists."));

            var context = new NewOrderContext
            {
                ClientOrderId = cmd.ClientOrderId,
            };

            operation = new Operation();
            operation.Create(id, cmd.WalletId, OperationType.NewOrder, JsonConvert.SerializeObject(context));
            await _operationsCacheService.CreateAsync(operation);

            return Created(Url.Action("Get", new { id }), id);
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

            var operation = await _operationsCacheService.GetAsync(id);

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
            await _operationsCacheService.SaveAsync(operation);

            await HandleWorkflow("MarketOrderWorkflow", operation);

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

            var operation = await _operationsCacheService.GetAsync(id);

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
            await _operationsCacheService.SaveAsync(operation);

            await HandleWorkflow("LimitOrderWorkflow", operation);

            return Created(Url.Action("Get", new { id }), id);
        }

        [HttpPost]
        [Route("order/{id}/stoplimit")]
        [ProducesResponseType(typeof(Guid), (int)HttpStatusCode.Created)]
        public async Task<IActionResult> StopLimitOrder(Guid id, [FromBody] CreateStopLimitOrderCommand command)
        {
            if (id == Guid.Empty)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("id", "Operation id must be non empty"));

            if (!ModelState.IsValid)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult(ModelState));

            var operation = await _operationsCacheService.GetAsync(id);

            if (operation != null)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("id", "Operation with the id already exists."));

            var context = new
            {
                Asset = command.AssetPair.BaseAsset,
                command.AssetPair,
                command.Volume,
                command.LowerLimitPrice,
                command.LowerPrice,
                command.UpperLimitPrice,
                command.UpperPrice,
                command.OrderAction,
                command.Client,
                command.GlobalSettings
            };

            operation = new Operation();
            operation.Create(id, command.Client.Id, OperationType.StopLimitOrder, JsonConvert.SerializeObject(context, Formatting.Indented));
            await _operationsCacheService.SaveAsync(operation);

            await HandleWorkflow("StopLimitOrderWorkflow", operation);

            return Created(Url.Action("Get", new { id }), id);
        }

        [HttpPost]
        [Route("cashout/{id}/swift")]
        [ProducesResponseType(typeof(Guid), (int)HttpStatusCode.Created)]
        public async Task<IActionResult> CashoutSwift(Guid id, [FromBody] CreateSwiftCashoutCommand command)
        {
            if (id == Guid.Empty)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("id", "Operation id must be non empty"));

            if (!ModelState.IsValid)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult(ModelState));

            var operation = await _operationsCacheService.GetAsync(id);

            if (operation != null)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("id", "Operation with the id already exists."));

            var context = new
            {
                Asset = command.Asset,
                command.Volume,
                Client = command.Client,
                Swift = command.Swift,
                CashoutSettings = command.CashoutSettings
            };

            operation = new Operation();
            operation.Create(id, command.Client.Id, OperationType.CashoutSwift, JsonConvert.SerializeObject(context, Formatting.Indented));
            await _operationsCacheService.SaveAsync(operation);

            await HandleWorkflow(OperationType.CashoutSwift + "Workflow", operation);

            return Created(Url.Action("Get", "Operations", new { id }), id);
        }

        [HttpPost]
        [Route("cashout/{id}")]
        [ProducesResponseType(typeof(Guid), (int)HttpStatusCode.Created)]
        public async Task<IActionResult> Cashout(Guid id, [FromBody] CreateCashoutCommand command)
        {
            if (id == Guid.Empty)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("id", "Operation id must be non empty"));

            if (!ModelState.IsValid)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult(ModelState));

            var operation = await _operationsCacheService.GetAsync(id);

            if (operation != null)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("id", "Operation with the id already exists."));

            // TODO: temp, ugly
            command.GlobalSettings.EthereumHotWallet = _ethereumServiceClientSettings.HotwalletAddress;

            operation = new Operation();
            operation.Create(id, command.Client.Id, OperationType.Cashout, JsonConvert.SerializeObject(command, Formatting.Indented));
            await _operationsCacheService.SaveAsync(operation);

            await HandleWorkflow(OperationType.Cashout + "Workflow", operation);

            return Created(Url.Action("Get", "Operations", new { id }), id);
        }

        private async Task HandleWorkflow(string workflowType, Operation operation)
        {
            var wf = _workflowFactory(workflowType, operation);
            var wfResult = wf.Run(operation);

            await _operationsCacheService.SaveAsync(operation);

            if (wfResult.State == WorkflowState.Corrupted)
            {
                _log.Critical(operation.Type + "Workflow", context: wfResult, message: $"Workflow for operation [{operation.Id}] has corrupted!");

                ModelState.AddModelError("InternalError", wfResult.Error);

                throw new ApiException(HttpStatusCode.InternalServerError, new ApiResult(ModelState));
            }

            if (operation.Status == OperationStatus.Failed)
            {
                var modelState = new ModelStateDictionary();
                JArray errors = operation.OperationValues.ValidationErrors;

                if (errors != null)
                    foreach (var error in errors)
                    {
                        modelState.AddModelError(error["ErrorCode"].ToString(), error["ErrorMessage"].ToString());
                    }

                string errorMessage = operation.OperationValues.ErrorMessage;
                string errorCode = operation.OperationValues.ErrorCode;

                if (!string.IsNullOrWhiteSpace(errorMessage))
                    modelState.AddModelError(errorCode ?? "Error", errorMessage);

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

            var operation = await _operationsCacheService.GetAsync(id);

            if (operation != null)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("id", "Operation with the id already exists."));

            var clientAccount = await _clientAccountService.ClientAccountInformation.GetByIdAsync(cmd.ClientId.ToString());

            if (clientAccount == null)
            {
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("clientId", "Non-existed client."));
            }

            bool isSourceWalletTrusted;
            bool isDestinationWalletTrusted;

            try
            {
                isSourceWalletTrusted = await _clientAccountService.ClientAccount.IsTrustedAsync(cmd.SourceWalletId.ToString());
            }
            catch (ValidationApiException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                    throw new ApiException(HttpStatusCode.BadRequest,
                        new ApiResult("sourceWalletId", "Non-existed wallet."));
                throw;
            }

            try
            {
                isDestinationWalletTrusted = await _clientAccountService.ClientAccount.IsTrustedAsync(cmd.WalletId.ToString());
            }
            catch (ValidationApiException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                    throw new ApiException(HttpStatusCode.BadRequest,
                        new ApiResult("walletId", "Non-existed wallet."));
                throw;
            }

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

            await _operationsCacheService.CreateAsync(operation);
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

            var operation = await _operationsCacheService.GetAsync(id);

            if (operation == null)
                throw new ApiException(HttpStatusCode.NotFound, new ApiResult("id", "Operation not found"));

            if (operation.Status == OperationStatus.Canceled)
                return;

            if (operation.Status != OperationStatus.Created)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("id", "An operation in created status could be canceled"));

            await _operationsCacheService.UpdateStatusAsync(id, OperationStatus.Canceled);
        }

        [HttpPost]
        [Route("complete/{id}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task Complete(Guid id)
        {
            if (id == Guid.Empty)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("id", "Operation id must be non empty"));

            var operation = await _operationsCacheService.GetAsync(id);

            if (operation == null || operation.Status == OperationStatus.Completed || operation.Status == OperationStatus.Confirmed)
                return;

            await _operationsCacheService.UpdateStatusAsync(id, OperationStatus.Completed);
        }

        [HttpPost]
        [Route("fail/{id}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task Fail(Guid id)
        {
            if (id == Guid.Empty)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("id", "Operation id must be non empty"));

            var operation = await _operationsCacheService.GetAsync(id);

            if (operation == null || operation.Status == OperationStatus.Failed)
                return;

            await _operationsCacheService.UpdateStatusAsync(id, OperationStatus.Failed);
        }

        [HttpPost]
        [Route("confirm/{id}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task Confirm(Guid id)
        {
            if (id == Guid.Empty)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("id", "Operation id must be non empty"));

            var operation = await _operationsCacheService.GetAsync(id);

            if (operation == null)
                throw new ApiException(HttpStatusCode.NotFound, new ApiResult("id", "Operation not found"));

            if (operation.Status == OperationStatus.Confirmed)
                return;

            if (operation.Status != OperationStatus.Created && operation.Status != OperationStatus.Accepted)
                throw new ApiException(HttpStatusCode.BadRequest, new ApiResult("id", "An operation in created status could be confirmed"));

            switch (operation.Type)
            {
                case OperationType.Cashout:
                    break;
                default:
                    await _operationsCacheService.UpdateStatusAsync(id, OperationStatus.Confirmed);
                    break;
            }
        }

        [HttpPost]
        [Route("{id}/activity/{activityId}/complete")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public IActionResult ManualCompleteActivity(Guid id, Guid activityId, [FromBody] ManualResumeActivityModel model)
        {
            _cqrsEngine.SendCommand(new CompleteActivityCommand
            {
                OperationId = id,
                ActivityId = activityId,
                Output = model.OutputJson
            }, OperationsBoundedContext.Name, OperationsBoundedContext.Name);

            _log.Info($"Operation {id} is manually completed with activity id {activityId}", model);

            return Ok();
        }

        [HttpPost]
        [Route("{id}/activity/{activityId}/fail")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public IActionResult ManualFailActivity(Guid id, Guid activityId, [FromBody] ManualResumeActivityModel model)
        {
            _cqrsEngine.SendCommand(new FailActivityCommand
            {
                OperationId = id,
                ActivityId = activityId,
                Output = model.OutputJson
            }, OperationsBoundedContext.Name, OperationsBoundedContext.Name);

            _log.Info($"Operation {id} is manually failed with activity id {activityId}", model);

            return Ok();
        }
    }
}
