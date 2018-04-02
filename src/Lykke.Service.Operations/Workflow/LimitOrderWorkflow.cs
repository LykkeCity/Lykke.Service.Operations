using System;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.MatchingEngine.Connector.Abstractions.Models;
using Lykke.MatchingEngine.Connector.Abstractions.Services;
using Lykke.Service.FeeCalculator.Client;
using Lykke.Service.Operations.Contracts.Events;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Core.Repositories;
using Lykke.Service.Operations.Workflow.Data;
using Lykke.Service.Operations.Workflow.Extensions;
using Lykke.Workflow;
using Lykke.Workflow.Fluent;
using Newtonsoft.Json.Linq;
using OrderAction = Lykke.Service.Operations.Contracts.OrderAction;

namespace Lykke.Service.Operations.Workflow
{
    [UsedImplicitly]
    public class LimitOrderWorkflow : OrderWorkflow
    {
        private readonly IFeeCalculatorClient _feeCalculatorClient;
        private readonly IOffchainOrdersRepository _offchainOrdersRepository;
        private readonly ILimitOrdersRepository _limitOrdersRepository;
        private readonly IMatchingEngineClient _matchingEngineClient;
        private readonly ICqrsEngine _cqrsEngine;

        public LimitOrderWorkflow(
            Operation operation, 
            ILog log, 
            IActivityFactory activityFactory, 
            IFeeCalculatorClient feeCalculatorClient, 
            IWorkflowService workflowService,
            IOffchainOrdersRepository offchainOrdersRepository,
            ILimitOrdersRepository limitOrdersRepository,
            IMatchingEngineClient matchingEngineClient,
            ICqrsEngine cqrsEngine) : base(operation, log, activityFactory, workflowService)
        {
            _feeCalculatorClient = feeCalculatorClient;
            _offchainOrdersRepository = offchainOrdersRepository;
            _limitOrdersRepository = limitOrdersRepository;
            _matchingEngineClient = matchingEngineClient;
            _cqrsEngine = cqrsEngine;

            DelegateNode<NeededLoAmountInput, object>("Determine needed amount", input => GetNeededAmount(input))
                .WithInput(context => new NeededLoAmountInput
                {                    
                    OrderAction = context.OperationValues.OrderAction,
                    Volume = context.OperationValues.Volume,
                    Price = context.OperationValues.Price,
                    AssetId = context.OperationValues.Asset.Id,
                    BaseAssetId = context.OperationValues.AssetPair.BaseAsset.Id                    
                })
                .MergeOutput(output => new { NeededAmount = output })
                .MergeFailOutput(output => output);

            DelegateNode<CalculateLoFeeInput, LimitOrderFeeModel>("Calculate fee", input => CalculateFee(input))
                .WithInput(context => new CalculateLoFeeInput
                {
                    ClientId = context.OperationValues.Client.Id,
                    AssetPairId = context.OperationValues.AssetPair.Id,                    
                    BaseAssetId = context.OperationValues.AssetPair.BaseAsset.Id,
                    OrderAction = context.OperationValues.OrderAction,
                    TargetClientId = context.OperationValues.GlobalSettings.FeeSettings.TargetClientId
                })
                .MergeOutput(output => new { Fee = output })
                .MergeFailOutput(output => output);

            DelegateNode("Save order", input => SaveOrder(input))
                .MergeFailOutput(output => output);

            DelegateNode<MeLoOrderInput, object>("Send to ME", input => SendToMe(input))
                .WithInput(context => new MeLoOrderInput
                {
                    Id = context.Id.ToString(),                    
                    AssetPairId = context.OperationValues.AssetPair.Id,
                    ClientId = context.OperationValues.Client.Id,                    
                    Volume = (double)context.OperationValues.Volume,
                    Price = (double)context.OperationValues.Price,                    
                    Fee = ((JObject)context.OperationValues.Fee).ToObject<LimitOrderFeeModel>()
                })
                .MergeOutput(output => new { Me = output })
                .MergeFailOutput(output => new { ErrorMessage = output.Message });

            DelegateNode("Create limit order", input => CreateLimitOrder(input));
            DelegateNode("Process limit order after Me", input => PostProcessLimitOrder(input));
        }        

        protected override WorkflowConfiguration<Operation> ConfigurePreMeNodes(WorkflowConfiguration<Operation> configuration)
        {
            return configuration
                .Do("Create limit order").OnFail("Fail operation");
        }

        protected override WorkflowConfiguration<Operation> ConfigurePostMeNodes(WorkflowConfiguration<Operation> configuration)
        {
            return configuration
                .Do("Process limit order after Me").OnFail("Fail operation");
        }

        private void CreateLimitOrder(Operation input)
        {
            _limitOrdersRepository.CreateAsync(LimitOrder.Create(
                input.Id.ToString(), 
                input.ClientId.ToString(), 
                (string)input.OperationValues.AssetPair.Id, 
                (double)input.OperationValues.Volume, 
                (double)input.OperationValues.Price, 
                (double)input.OperationValues.Volume)).ConfigureAwait(false).GetAwaiter().GetResult();

            _cqrsEngine.PublishEvent(new LimitOrderCreatedEvent
            {
                Id = input.Id,
                OrderAction = input.OperationValues.OrderAction,
                AssetPairId = input.OperationValues.AssetPair.Id,
                Volume = input.OperationValues.Volume,
                Price = input.OperationValues.Price
            }, "operations");
        }

        private void PostProcessLimitOrder(Operation input)
        {
            if (input.OperationValues.Me.Status != MeStatusCodes.Ok)
            {
                var order = _limitOrdersRepository.GetOrderAsync(input.Id.ToString()).ConfigureAwait(false).GetAwaiter().GetResult();

                _limitOrdersRepository.FinishAsync(order, (string)input.OperationValues.Me.Status).ConfigureAwait(false).GetAwaiter().GetResult();

                _cqrsEngine.PublishEvent(new LimitOrderRejectedEvent
                {
                    Id = input.Id,
                    OrderAction = input.OperationValues.OrderAction,
                    AssetPairId = input.OperationValues.AssetPair.Id,
                    Volume = input.OperationValues.Volume,
                    Price = input.OperationValues.Price
                }, "operations");
            }
        }

        private object GetNeededAmount(NeededLoAmountInput input)
        {
            var orderAction = input.OrderAction;
            
            if (orderAction == OrderAction.Buy)
            {                
                return new
                {
                    Amount = input.BaseAssetId == input.AssetId ? input.Volume * input.Price.Value : input.Volume / input.Price.Value
                };                                
            }
            else
            {
                return new
                {
                    Amount = Math.Abs(input.Volume)
                };
            }
        }

        private LimitOrderFeeModel CalculateFee(CalculateLoFeeInput input)
        {
            var orderAction = input.OrderAction == OrderAction.Buy
                ? FeeCalculator.AutorestClient.Models.OrderAction.Buy
                : FeeCalculator.AutorestClient.Models.OrderAction.Sell;

            var fee = _feeCalculatorClient.GetLimitOrderFees(input.ClientId, input.AssetPairId, input.BaseAssetId, orderAction).ConfigureAwait(false).GetAwaiter().GetResult();

            return new LimitOrderFeeModel
            {
                MakerSize = (double)fee.MakerFeeSize,
                TakerSize = (double)fee.TakerFeeSize,
                SourceClientId = input.ClientId,
                TargetClientId = input.TargetClientId,
                Type = fee.MakerFeeSize == 0m && fee.TakerFeeSize == 0m ? (int)LimitOrderFeeType.NO_FEE : (int)LimitOrderFeeType.CLIENT_FEE
            };
        }

        private void SaveOrder(Operation context)
        {
            _offchainOrdersRepository.CreateLimitOrder(
                    context.ClientId.ToString(),
                    (string)context.OperationValues.Asset.Id,
                    (string)context.OperationValues.AssetPair.Id,
                    (decimal)context.OperationValues.Volume,
                    (decimal)context.OperationValues.NeededAmount.Amount,
                    (bool)(context.OperationValues.AssetPair.BaseAsset.Id == context.OperationValues.Asset.Id),
                    (decimal)context.OperationValues.Price)
                .ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private object SendToMe(MeLoOrderInput input)
        {
            var limitOrderModel = new LimitOrderModel
            {
                Id = input.Id,
                ClientId = input.ClientId,
                AssetPairId = input.AssetPairId,
                Price = input.Price,
                Volume = Math.Abs(input.Volume),
                Fee = input.Fee,
            };
            
            var response = _matchingEngineClient.PlaceLimitOrderAsync(limitOrderModel).ConfigureAwait(false).GetAwaiter().GetResult();

            if (response == null)
                throw new ApplicationException("Me is not available.");

            if (response.Status != MeStatusCodes.Ok)            
                throw new ApplicationException(response.Status.Format());                        
            
            return new
            {
                Status = response.Status.ToString(),
                response.Message,
                response.TransactionId
            };
        }

        protected override void OnMeFail(Operation context)
        {
            _limitOrdersRepository.RemoveAsync(context.Id.ToString(), context.ClientId.ToString()).ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
