﻿using System;
using Common.Log;
using JetBrains.Annotations;
using Lykke.MatchingEngine.Connector.Abstractions.Models;
using Lykke.MatchingEngine.Connector.Abstractions.Services;
using Lykke.Service.FeeCalculator.Client;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Workflow.Data;
using Lykke.Service.Operations.Workflow.Extensions;
using Lykke.Workflow;
using Newtonsoft.Json.Linq;
using FeeType = Lykke.Service.FeeCalculator.AutorestClient.Models.FeeType;
using OrderAction = Lykke.Service.Operations.Contracts.OrderAction;

namespace Lykke.Service.Operations.Workflow
{
    [UsedImplicitly]
    public class MarketOrderWorkflow : OrderWorkflow
    {
        private readonly IFeeCalculatorClient _feeCalculatorClient;
        private readonly IMatchingEngineClient _matchingEngineClient;
        
        public MarketOrderWorkflow(
            Operation operation, 
            ILog log, 
            IActivityFactory activityFactory,
            IFeeCalculatorClient feeCalculatorClient,
            IMatchingEngineClient matchingEngineClient) : base(operation, log, activityFactory)
        {
            _feeCalculatorClient = feeCalculatorClient;
            _matchingEngineClient = matchingEngineClient;
            
            DelegateNode<CalculateMoFeeInput, MarketOrderFeeModel>("Calculate fee", input => CalculateFee(input))
                .WithInput(context => new CalculateMoFeeInput
                {
                    ClientId = context.OperationValues.Client.Id,                    
                    AssetPairId = context.OperationValues.AssetPair.Id,                    
                    AssetId = context.OperationValues.Asset.Id,
                    OrderAction = context.OperationValues.OrderAction,
                    TargetClientId = context.OperationValues.GlobalSettings.FeeSettings.TargetClientId
                })
                .MergeOutput(output => new { Fee = output })
                .MergeFailOutput(output => output);
            
            DelegateNode<MeMoOrderInput, object>("Send to ME", input => SendToMe(input))
                .WithInput(context => new MeMoOrderInput
                {
                    Id = context.Id.ToString(),                    
                    AssetPairId = context.OperationValues.AssetPair.Id,
                    ClientId = context.OperationValues.Client.Id,
                    Straight = (string)context.OperationValues.Asset.Id == (string)context.OperationValues.AssetPair.BaseAsset.Id,
                    Volume = context.OperationValues.Volume,                    
                    OrderAction = context.OperationValues.OrderAction,
                    Fee = ((JObject)context.OperationValues.Fee).ToObject<MarketOrderFeeModel>()
                })
                .MergeOutput(output => new { Me = output })
                .MergeFailOutput(output => new { ErrorMessage = output.Message });
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