using System;
using System.Collections.Generic;
using System.Linq;
using Common.Log;
using JetBrains.Annotations;
using Lykke.MatchingEngine.Connector.Abstractions.Models;
using Lykke.MatchingEngine.Connector.Abstractions.Services;
using Lykke.Service.FeeCalculator.Client;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Core.Repositories;
using Lykke.Service.Operations.Workflow.Data;
using Lykke.Service.Operations.Workflow.Extensions;
using Lykke.Service.RateCalculator.Client;
using Lykke.Service.RateCalculator.Client.AutorestClient.Models;
using Lykke.Workflow;
using Lykke.Workflow.Fluent;
using Newtonsoft.Json.Linq;
using FeeType = Lykke.Service.FeeCalculator.AutorestClient.Models.FeeType;
using OrderAction = Lykke.Service.Operations.Contracts.OrderAction;

namespace Lykke.Service.Operations.Workflow
{
    [UsedImplicitly]
    public class MarketOrderWorkflow : OrderWorkflow
    {
        private readonly IFeeCalculatorClient _feeCalculatorClient;
        private readonly IOffchainOrdersRepository _offchainOrdersRepository;
        private readonly IMatchingEngineClient _matchingEngineClient;
        private readonly IRateCalculatorClient _rateCalculatorClient;

        public MarketOrderWorkflow(
            Operation operation, 
            ILog log, 
            IActivityFactory activityFactory, 
            IWorkflowService workflowService, 
            IFeeCalculatorClient feeCalculatorClient,
            IOffchainOrdersRepository offchainOrdersRepository,
            IMatchingEngineClient matchingEngineClient,
            IRateCalculatorClient rateCalculatorClient) : base(operation, log, activityFactory, workflowService)
        {
            _feeCalculatorClient = feeCalculatorClient;
            _offchainOrdersRepository = offchainOrdersRepository;
            _matchingEngineClient = matchingEngineClient;
            _rateCalculatorClient = rateCalculatorClient;

            DelegateNode<NeededMoAmountInput, object>("Determine needed amount", input => GetNeededAmount(input))
                .WithInput(context => new NeededMoAmountInput
                {                    
                    OrderAction = context.OperationValues.OrderAction,
                    Volume = context.OperationValues.Volume,
                    Price = context.OperationValues.Price,
                    AssetId = context.OperationValues.AssetId,
                    BaseAssetId = context.OperationValues.AssetPair.BaseAsset.Id,
                    NeededAssetId = context.OperationValues.NeededAssetId,
                    ReceivedAssetId = context.OperationValues.ReceivedAssetId
                })
                .MergeOutput(output => new { NeededAmount = output })
                .MergeFailOutput(output => output);

            DelegateNode<CalculateMoFeeInput, MarketOrderFeeModel>("Calculate fee", input => CalculateFee(input))
                .WithInput(context => new CalculateMoFeeInput
                {
                    ClientId = context.OperationValues.Client.Id,                    
                    AssetPairId = context.OperationValues.AssetPairId,                    
                    AssetId = context.OperationValues.AssetId,
                    OrderAction = context.OperationValues.OrderAction,
                    TargetClientId = context.OperationValues.GlobalSettings.FeeSettings.TargetClientId
                })
                .MergeOutput(output => new { Fee = output })
                .MergeFailOutput(output => output);

            DelegateNode("Save order", input => SaveOrder(input))
                .MergeFailOutput(output => output);

            DelegateNode<MeMoOrderInput, object>("Send to ME", input => SendToMe(input))
                .WithInput(context => new MeMoOrderInput
                {
                    Id = context.Id.ToString(),                    
                    AssetPairId = context.OperationValues.AssetPairId,
                    ClientId = context.OperationValues.Client.Id,
                    Straight = (string)context.OperationValues.AssetId == (string)context.OperationValues.AssetPair.BaseAsset.Id,
                    Volume = context.OperationValues.Volume,                    
                    OrderAction = context.OperationValues.OrderAction,
                    Fee = ((JObject)context.OperationValues.Fee).ToObject<MarketOrderFeeModel>()
                })
                .MergeOutput(output => new { Me = output })
                .MergeFailOutput(output => output);

            DelegateNode<UpdatePriceInput>("Update order price", input => UpdateOrderPrice(input))
                .WithInput(context => new UpdatePriceInput
                {
                    Id = context.Id,
                    Price = context.OperationValues.Price
                })
                .MergeFailOutput(output => new { ErrorMessage = output.Message });
        }

        private void UpdateOrderPrice(UpdatePriceInput input)
        {
            _offchainOrdersRepository.UpdatePrice(input.Id.ToString(), input.Price).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        protected override WorkflowConfiguration<Operation> ConfigurePostMeNodes(WorkflowConfiguration<Operation> configuration)
        {
            return configuration.Do("Update order price").OnFail("Fail operation");
        }

        private object GetNeededAmount(NeededMoAmountInput input)
        {
            var orderAction = input.OrderAction;
            var neededAmount = 0m;

            if (orderAction == OrderAction.Buy)
            {
                var result = _rateCalculatorClient.GetMarketAmountInBaseAsync(
                        new List<AssetWithAmount>
                        {
                            new AssetWithAmount
                            {
                                AssetId = input.ReceivedAssetId,
                                Amount = (double) input.Volume
                            }
                        },
                        input.NeededAssetId,
                        orderAction == OrderAction.Buy
                            ? RateCalculator.Client.AutorestClient.Models.OrderAction.Buy
                            : RateCalculator.Client.AutorestClient.Models.OrderAction.Sell)
                    .ConfigureAwait(false).GetAwaiter().GetResult().ToArray();

                var item = result.FirstOrDefault();

                if (item != null)
                    neededAmount = (decimal)(item.To?.Amount ?? 0);

                return new
                {
                    Amount = neededAmount,
                    ConversionResult = result
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

        private MarketOrderFeeModel CalculateFee(CalculateMoFeeInput input)
        {
            var orderAction = input.OrderAction == OrderAction.Buy
                ? FeeCalculator.AutorestClient.Models.OrderAction.Buy
                : FeeCalculator.AutorestClient.Models.OrderAction.Sell;

            var fee = _feeCalculatorClient
                .GetMarketOrderAssetFee(input.ClientId, input.AssetPairId, input.AssetId, orderAction)
                .ConfigureAwait(false).GetAwaiter().GetResult();

            return new MarketOrderFeeModel
            {
                Size = (double)fee.Amount,
                SizeType = fee.Type == FeeType.Absolute
                    ? (int)FeeSizeType.ABSOLUTE
                    : (int)FeeSizeType.PERCENTAGE,
                SourceClientId = input.ClientId,
                TargetClientId = fee.TargetWalletId ?? input.TargetClientId,
                Type = fee.Amount == 0m
                    ? (int)MarketOrderFeeType.NO_FEE
                    : (int)MarketOrderFeeType.CLIENT_FEE,
                AssetId = string.IsNullOrEmpty(fee.TargetAssetId)
                    ? Array.Empty<string>()
                    : new[] { fee.TargetAssetId }
            };
        }

        private void SaveOrder(Operation context)
        {
            _offchainOrdersRepository.CreateOrder(
                    context.ClientId.ToString(),
                    (string)context.OperationValues.AssetId,
                    (string)context.OperationValues.AssetPairId,
                    (decimal)context.OperationValues.Volume,
                    (decimal)context.OperationValues.NeededAmount.Amount,
                    (bool)(context.OperationValues.AssetPair.BaseAsset.Id == context.OperationValues.AssetId))
                .ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private object SendToMe(MeMoOrderInput input)
        {
            var marketOrderModel = new MarketOrderModel
            {
                Id = input.Id,
                AssetPairId = input.AssetPairId,
                ClientId = input.ClientId,
                ReservedLimitVolume = null,
                Straight = input.Straight,
                Volume = Math.Abs(input.Volume),
                Fee = input.Fee,
                OrderAction = input.OrderAction == OrderAction.Buy ? MatchingEngine.Connector.Abstractions.Models.OrderAction.Buy : MatchingEngine.Connector.Abstractions.Models.OrderAction.Sell
            };            

            var response = _matchingEngineClient.HandleMarketOrderAsync(marketOrderModel).ConfigureAwait(false).GetAwaiter().GetResult();

            if (response == null)
                throw new ApplicationException("Me is not available");

            if (response.Status != MeStatusCodes.Ok)
                throw new ApplicationException(response.Status.Format());

            return new
            {
                Status = response.Status.ToString(),
                response.Price
            };           
        }        
    }
}
