using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Contracts.Operations;
using Lykke.Cqrs;
using Lykke.Service.Assets.Client;
using Lykke.Service.ClientAccount.Client.AutorestClient;
using Lykke.Service.ClientAccount.Client.AutorestClient.Models;
using Lykke.Service.EthereumCore.Client;
using Lykke.Service.Operations.Client;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Contracts.Api;
using Lykke.Service.Operations.Contracts.Commands;
using Lykke.Service.Operations.Core.Domain;
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
using OperationStatus = Lykke.Service.Operations.Contracts.OperationStatus;
using OperationType = Lykke.Service.Operations.Contracts.OperationType;

namespace Lykke.Service.Operations.Controllers
{
    [Route("api/operations")]
    [Produces("application/json")]
    public class OperationsController : Controller, IOperations
    {
        private readonly IOperationsRepository _operationsRepository;
        private readonly IClientAccountService _clientAccountService;
        private readonly IPushNotificationsAPI _pushNotificationsApi;
        private readonly IAssetsServiceWithCache _assetsServiceWithCache;
        private readonly Func<string, Operation, OperationWorkflow> _workflowFactory;
        private readonly ICqrsEngine _cqrsEngine;
        private readonly EthereumServiceClientSettings _ethereumServiceClientSettings;
        private readonly ILog _log;
        private readonly IMapper _mapper;

        public OperationsController(
            IOperationsRepository operationsRepository,
            IClientAccountService clientAccountService,
            IPushNotificationsAPI pushNotificationsApi,
            IAssetsServiceWithCache assetsServiceWithCache,
            Func<string, Operation, OperationWorkflow> workflowFactory,
            ICqrsEngine cqrsEngine,
            EthereumServiceClientSettings ethereumServiceClientSettings,
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
            _log = log.CreateLog(this);
            _mapper = mapper;
        }

        [HttpGet]
        [Route("{id}")]
        [ProducesResponseType(typeof(OperationModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        public async Task<OperationModel> Get(Guid id)
        {
            var operation = await _operationsRepository.Get(id);

            if (operation == null)
                throw new ApiException(HttpStatusCode.NotFound, "id", "Operation not found");

            var result = _mapper.Map<Operation, OperationModel>(operation);

            return result;
        }

        [HttpGet]
        [Obsolete()]
        [Route("{clientId}/list/{status}")]
        [ProducesResponseType(typeof(IEnumerable<OperationModel>), (int)HttpStatusCode.OK)]
        public async Task<IEnumerable<OperationModel>> Get(Guid clientId, OperationStatus status)
        {
            var operations = await _operationsRepository.Get(clientId, status, null);

            var result = _mapper.Map<IEnumerable<Operation>, IEnumerable<OperationModel>>(operations);

            return result;
        }

        [HttpGet]
        [Route("")]
        [ProducesResponseType(typeof(IEnumerable<OperationModel>), (int)HttpStatusCode.OK)]
        public async Task<IEnumerable<OperationModel>> Get(Guid? clientId, OperationStatus? status, OperationType? type, int? skip = 0, int? take = 10)
        {
            var operations = await _operationsRepository.Get(clientId, status, type, skip, take);

            var result = _mapper.Map<IEnumerable<Operation>, IEnumerable<OperationModel>>(operations);

            return result;
        }

        /// <summary>
        /// Registers a new order with attached client order Id.
        /// </summary>
        /// <param name="id">The order Id</param>
        /// <param name="cmd">Order related information</param>
        /// <returns>Guid of created order.</returns>
        [HttpPost]
        [Route("newOrder/{id}")]
        [ProducesResponseType(typeof(Guid), (int)HttpStatusCode.OK)]
        public async Task<Guid> NewOrder(Guid id, [FromBody]CreateNewOrderCommand cmd)
        {
            if (id == Guid.Empty)
                throw new ApiException(HttpStatusCode.BadRequest, "id", "Operation id must be not empty and has a correct GUID value");

            if (!ModelState.IsValid)
                throw new ApiException(HttpStatusCode.BadRequest, ModelState);

            var operation = await _operationsRepository.Get(id);

            if (operation != null)
                throw new ApiException(HttpStatusCode.BadRequest, "id", "Operation with the id already exists.");

            var context = new NewOrderContext
            {
                ClientOrderId = cmd.ClientOrderId,
            };

            operation = new Operation();
            operation.Create(id, cmd.WalletId, OperationType.NewOrder, JsonConvert.SerializeObject(context));
            await _operationsRepository.Create(operation);

            return id;
        }

        [HttpPost]
        [Route("order/{id}/market")]
        [ProducesResponseType(typeof(Guid), (int)HttpStatusCode.OK)]
        public async Task<Guid> MarketOrder(Guid id, [FromBody] CreateMarketOrderCommand command)
        {
            if (id == Guid.Empty)
                throw new ApiException(HttpStatusCode.BadRequest, "id", "Operation id must be non empty");

            if (!ModelState.IsValid)
                throw new ApiException(HttpStatusCode.BadRequest, ModelState);

            var operation = await _operationsRepository.Get(id);

            if (operation != null)
                throw new ApiException(HttpStatusCode.BadRequest, "id", "Operation with the id already exists.");

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
            await _operationsRepository.Save(operation);

            await HandleWorkflow("MarketOrderWorkflow", operation);

            return id;
        }

        [HttpPost]
        [Route("order/{id}/limit")]
        [ProducesResponseType(typeof(Guid), (int)HttpStatusCode.OK)]
        public async Task<Guid> LimitOrder(Guid id, [FromBody] CreateLimitOrderCommand command)
        {
            if (id == Guid.Empty)
                throw new ApiException(HttpStatusCode.BadRequest, "id", "Operation id must be non empty");

            if (!ModelState.IsValid)
                throw new ApiException(HttpStatusCode.BadRequest, ModelState);

            var operation = await _operationsRepository.Get(id);

            if (operation != null)
                throw new ApiException(HttpStatusCode.BadRequest, "id", "Operation with the id already exists.");

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
            await _operationsRepository.Save(operation);

            await HandleWorkflow("LimitOrderWorkflow", operation);

            return id;
        }

        [HttpPost]
        [Route("order/{id}/stoplimit")]
        [ProducesResponseType(typeof(Guid), (int)HttpStatusCode.OK)]
        public async Task<Guid> StopLimitOrder(Guid id, [FromBody] CreateStopLimitOrderCommand command)
        {
            if (id == Guid.Empty)
                throw new ApiException(HttpStatusCode.BadRequest, "id", "Operation id must be non empty");

            if (!ModelState.IsValid)
                throw new ApiException(HttpStatusCode.BadRequest, ModelState);

            var operation = await _operationsRepository.Get(id);

            if (operation != null)
                throw new ApiException(HttpStatusCode.BadRequest, "id", "Operation with the id already exists.");

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
            await _operationsRepository.Save(operation);

            await HandleWorkflow("StopLimitOrderWorkflow", operation);

            return id;
        }

        [HttpPost]
        [Route("cashout/{id}/swift")]
        [ProducesResponseType(typeof(Guid), (int)HttpStatusCode.OK)]
        public async Task<Guid> CashoutSwift(Guid id, [FromBody] CreateSwiftCashoutCommand command)
        {
            if (id == Guid.Empty)
                throw new ApiException(HttpStatusCode.BadRequest, "id", "Operation id must be non empty");

            if (!ModelState.IsValid)
                throw new ApiException(HttpStatusCode.BadRequest, ModelState);

            var operation = await _operationsRepository.Get(id);

            if (operation != null)
                throw new ApiException(HttpStatusCode.BadRequest, "id", "Operation with the id already exists.");

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
            await _operationsRepository.Save(operation);

            await HandleWorkflow(OperationType.CashoutSwift + "Workflow", operation);

            return id;
        }

        [HttpPost]
        [Route("cashout/{id}")]
        [ProducesResponseType(typeof(Guid), (int)HttpStatusCode.OK)]
        public async Task<Guid> Cashout(Guid id, [FromBody] CreateCashoutCommand command)
        {
            if (id == Guid.Empty)
                throw new ApiException(HttpStatusCode.BadRequest, "id", "Operation id must be non empty");

            if (!ModelState.IsValid)
                throw new ApiException(HttpStatusCode.BadRequest, ModelState);

            var operation = await _operationsRepository.Get(id);

            if (operation != null)
                throw new ApiException(HttpStatusCode.BadRequest, "id", "Operation with the id already exists.");

            // TODO: temp, ugly
            command.GlobalSettings.EthereumHotWallet = _ethereumServiceClientSettings.HotwalletAddress;

            operation = new Operation();
            operation.Create(id, command.Client.Id, OperationType.Cashout, JsonConvert.SerializeObject(command, Formatting.Indented));
            await _operationsRepository.Save(operation);

            await HandleWorkflow(OperationType.Cashout + "Workflow", operation);

            return id;
        }

        private async Task HandleWorkflow(string workflowType, Operation operation)
        {
            var wf = _workflowFactory(workflowType, operation);
            var wfResult = wf.Run(operation);

            await _operationsRepository.Save(operation);

            if (wfResult.State == WorkflowState.Corrupted)
            {
                _log.Critical(operation.Type + "Workflow", context: wfResult, message: $"Workflow for operation [{operation.Id}] has corrupted!");

                ModelState.AddModelError("InternalError", wfResult.Error);

                throw new ApiException(HttpStatusCode.InternalServerError, ModelState);
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

                throw new ApiException(HttpStatusCode.BadRequest, ModelState);
            }
        }

        [HttpPost]
        [Route("transfer/{id}")]
        [ProducesResponseType(typeof(Guid), (int)HttpStatusCode.OK)]
        public async Task<Guid> Transfer(Guid id, [FromBody]CreateTransferCommand cmd)
        {
            if (id == Guid.Empty)
                throw new ApiException(HttpStatusCode.BadRequest, "id", "Operation id must be non empty");

            if (!ModelState.IsValid)
                throw new ApiException(HttpStatusCode.BadRequest, ModelState);

            var operation = await _operationsRepository.Get(id);

            if (operation != null)
                throw new ApiException(HttpStatusCode.BadRequest, "id", "Operation with the id already exists.");

            var clientResponse = await _clientAccountService.GetByIdAsync(cmd.ClientId.ToString());
            if (clientResponse is ClientAccount.Client.AutorestClient.Models.ErrorResponse)
            {
                throw new ApiException(HttpStatusCode.BadRequest, "clientId", "Non-existed client.");
            }
            var clientAccount = (ClientResponseModel)clientResponse;

            var isSourceWalletTrustedResponse = await _clientAccountService.IsTrustedAsync(cmd.SourceWalletId.ToString());
            if (isSourceWalletTrustedResponse is ClientAccount.Client.AutorestClient.Models.ErrorResponse)
            {
                throw new ApiException(HttpStatusCode.BadRequest, "sourceWalletId", "Non-existed wallet.");
            }
            var isSourceWalletTrusted = (bool?)isSourceWalletTrustedResponse ?? false;

            var isDestinationWalletTrustedResponse = await _clientAccountService.IsTrustedAsync(cmd.WalletId.ToString());
            if (isDestinationWalletTrustedResponse is ClientAccount.Client.AutorestClient.Models.ErrorResponse)
            {
                throw new ApiException(HttpStatusCode.BadRequest, "walletId", "Non-existed wallet.");
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

            return id;
        }

        [HttpPost]
        [Route("cancel/{id}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task Cancel(Guid id)
        {
            if (id == Guid.Empty)
                throw new ApiException(HttpStatusCode.BadRequest, "id", "Operation id must be non empty");

            var operation = await _operationsRepository.Get(id);

            if (operation == null)
                throw new ApiException(HttpStatusCode.NotFound, "id", "Operation not found");

            if (operation.Status == OperationStatus.Canceled)
                return;

            if (operation.Status != OperationStatus.Created)
                throw new ApiException(HttpStatusCode.BadRequest, "id", "An operation in created status could be canceled");

            await _operationsRepository.UpdateStatus(id, OperationStatus.Canceled);
        }

        [HttpPost]
        [Route("complete/{id}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task Complete(Guid id)
        {
            if (id == Guid.Empty)
                throw new ApiException(HttpStatusCode.BadRequest, "id", "Operation id must be non empty");

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
                throw new ApiException(HttpStatusCode.BadRequest, "id", "Operation id must be non empty");

            var operation = await _operationsRepository.Get(id);

            if (operation == null || operation.Status == OperationStatus.Failed)
                return;

            await _operationsRepository.UpdateStatus(id, OperationStatus.Failed);
        }

        [HttpPost]
        [Route("confirm/{id}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task Confirm(Guid id)
        {
            if (id == Guid.Empty)
                throw new ApiException(HttpStatusCode.BadRequest, "id", "Operation id must be non empty");

            var operation = await _operationsRepository.Get(id);

            if (operation == null)
                throw new ApiException(HttpStatusCode.NotFound, "id", "Operation not found");

            if (operation.Status == OperationStatus.Confirmed)
                return;

            if (operation.Status != OperationStatus.Created && operation.Status != OperationStatus.Accepted)
                throw new ApiException(HttpStatusCode.BadRequest, "id", "An operation could be confirmed only in created or accepted status ");

            switch (operation.Type)
            {
                case OperationType.Cashout:
                    break;
                default:
                    await _operationsRepository.UpdateStatus(id, OperationStatus.Confirmed);
                    break;
            }
        }

        [HttpPost]
        [Route("{id}/activity/{activityId}/complete")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public Task ManualCompleteActivity(Guid id, Guid activityId, [FromBody] ManualResumeActivityModel model)
        {
            _cqrsEngine.SendCommand(new CompleteActivityCommand
            {
                OperationId = id,
                ActivityId = activityId,
                Output = model.OutputJson
            }, OperationsBoundedContext.Name, OperationsBoundedContext.Name);

            _log.Info($"Operation {id} is manually completed with activity id {activityId}", model);

            return Task.CompletedTask;
        }

        [HttpPost]
        [Route("{id}/activity/{activityId}/fail")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public Task ManualFailActivity(Guid id, Guid activityId, [FromBody] ManualResumeActivityModel model)
        {
            _cqrsEngine.SendCommand(new FailActivityCommand
            {
                OperationId = id,
                ActivityId = activityId,
                Output = model.OutputJson
            }, OperationsBoundedContext.Name, OperationsBoundedContext.Name);

            _log.Info($"Operation {id} is manually failed with activity id {activityId}", model);

            return Task.CompletedTask;
        }
    }
}
