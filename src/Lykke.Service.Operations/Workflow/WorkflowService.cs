using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Lykke.MatchingEngine.Connector.Abstractions.Models;
using Lykke.MatchingEngine.Connector.Abstractions.Services;
using Lykke.Service.Assets.Client;
using Lykke.Service.Balances.AutorestClient.Models;
using Lykke.Service.Balances.Client;
using Lykke.Service.FeeCalculator.Client;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Core.Repositories;
using Lykke.Service.Operations.Workflow.Data;
using Lykke.Service.RateCalculator.Client;
using Lykke.Service.RateCalculator.Client.AutorestClient.Models;
using Newtonsoft.Json.Linq;
using FeeType = Lykke.Service.FeeCalculator.AutorestClient.Models.FeeType;

namespace Lykke.Service.Operations.Workflow
{
    public interface IWorkflowService
    {
        object CalculateFee(CalculateFeeInput input);
        object SendToMe(MeOrderInput input);               
        object GetNeededAsset(Operation context);
        object GetNeededAmount(NeededAmountInput input);
        object AdjustNeededAmount(Operation context);
        object GetWalletBalance(Operation context);
        void SaveOrder(Operation context);
    }

    public class WorkflowService : IWorkflowService
    {
        private readonly IFeeCalculatorClient _feeCalculatorClient;
        private readonly IMatchingEngineClient _matchingEngineClient;        
        private readonly IBalancesClient _balancesClient;
        private readonly IRateCalculatorClient _rateCalculatorClient;
        private readonly IOffchainOrdersRepository _offchainOrdersRepository;

        public WorkflowService(
            IFeeCalculatorClient feeCalculatorClient, 
            IMatchingEngineClient matchingEngineClient,             
            IBalancesClient balancesClient,
            IRateCalculatorClient rateCalculatorClient,
            IOffchainOrdersRepository offchainOrdersRepository)
        {
            _feeCalculatorClient = feeCalculatorClient;
            _matchingEngineClient = matchingEngineClient;            
            _balancesClient = balancesClient;
            _rateCalculatorClient = rateCalculatorClient;
            _offchainOrdersRepository = offchainOrdersRepository;
        }

        public object CalculateFee(CalculateFeeInput input)
        {
            var orderAction = input.OrderAction == OrderAction.Buy
                ? FeeCalculator.AutorestClient.Models.OrderAction.Buy
                : FeeCalculator.AutorestClient.Models.OrderAction.Sell;

            if (input.OperationType == OperationType.MarketOrder)
            {
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

            if (input.OperationType == OperationType.LimitOrder)
            {
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

            return new { };
        }

        public object SendToMe(MeOrderInput input)
        {
            if (input.OperationType == OperationType.MarketOrder)
            {
                var marketOrderModel = new MarketOrderModel
                {
                    Id = input.Id,
                    AssetPairId = input.AssetPairId,
                    ClientId = input.ClientId,
                    ReservedLimitVolume = null,
                    Straight = input.Straight,
                    Volume = Math.Abs(input.Volume),
                    OrderAction = input.OrderAction == OrderAction.Buy ? MatchingEngine.Connector.Abstractions.Models.OrderAction.Buy : MatchingEngine.Connector.Abstractions.Models.OrderAction.Sell
                };

                if (input.Fee != null)
                {
                    marketOrderModel.Fee = ((JObject)input.Fee).ToObject<MarketOrderFeeModel>();
                }

                var response = _matchingEngineClient.HandleMarketOrderAsync(marketOrderModel).ConfigureAwait(false).GetAwaiter().GetResult();

                if (response == null)
                    throw new ApplicationException("Me is not available");

                if (response.Status != MeStatusCodes.Ok)
                    throw new ApplicationException(response.Status.ToString());

                return new
                {
                    Status = response.Status.ToString(),
                    response.Price
                };
            }

            if (input.OperationType == OperationType.LimitOrder)
            {
                var limitOrderModel = new LimitOrderModel
                {
                    Id = input.Id,
                    ClientId = input.ClientId,
                    AssetPairId = input.AssetPairId,
                    Price = input.Price.Value,
                    Volume = Math.Abs(input.Volume)
                };

                if (input.Fee != null)
                {
                    limitOrderModel.Fee = ((JObject)input.Fee).ToObject<LimitOrderFeeModel>();
                }

                var response = _matchingEngineClient.PlaceLimitOrderAsync(limitOrderModel).ConfigureAwait(false).GetAwaiter().GetResult();

                if (response == null)
                    throw new ApplicationException("Me is not available");

                if (response.Status != MeStatusCodes.Ok)
                    throw new ApplicationException(response.Status.ToString());

                return new
                {
                    Status = response.Status.ToString(),
                    response.Message,
                    response.TransactionId
                };
            }

            return new { };
        }
        
        public object GetNeededAsset(Operation context)
        {
            var orderAction = (decimal)context.OperationValues.Volume > 0 ? OrderAction.Buy : OrderAction.Sell;
            string assetId = context.OperationValues.Asset.Id;
            string baseAssetId = context.OperationValues.AssetPair.BaseAsset.Id;
            string quotingAssetId = context.OperationValues.AssetPair.QuotingAsset.Id;

            if (orderAction == OrderAction.Buy)
            {
                return new
                {
                    OrderAction = orderAction.ToString(),
                    NeededAssetId = baseAssetId == assetId
                        ? quotingAssetId
                        : baseAssetId,
                    ReceivedAssetId = baseAssetId == assetId
                        ? baseAssetId
                        : quotingAssetId
                };
            }
            else
            {
                return new
                {
                    OrderAction = orderAction.ToString(),
                    NeededAssetId = baseAssetId == assetId
                        ? baseAssetId
                        : quotingAssetId
                };
            }
        }

        public object GetNeededAmount(NeededAmountInput input)
        {
            var orderAction = input.OrderAction;
            var neededAmount = 0m;

            if (orderAction == OrderAction.Buy)
            {
                if (input.OperationType == OperationType.MarketOrder)
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

                if (input.OperationType == OperationType.LimitOrder)
                {
                    return new
                    {
                        Amount = input.BaseAssetId == input.AssetId ? input.Volume * input.Price.Value : input.Volume / input.Price.Value
                    };
                }

                return new { Amount = 0 };
            }
            else
            {
                return new
                {
                    Amount = Math.Abs(input.Volume)
                };
            }
        }

        public object AdjustNeededAmount(Operation context)
        {
            var orderAction = (OrderAction)context.OperationValues.OrderAction;

            if (orderAction == OrderAction.Buy)
            {
                var balance = (decimal)context.OperationValues.Wallet.Balance;
                int neededAssetAccuracy =
                    (string)context.OperationValues.AssetPair.BaseAsset.Id ==
                    (string)context.OperationValues.NeededAssetId
                        ? context.OperationValues.AssetPair.BaseAsset.Accuracy
                        : context.OperationValues.AssetPair.QuotingAsset.Accuracy;

                var neededAmount = ((decimal)context.OperationValues.NeededAmount.Amount).TruncateDecimalPlaces(neededAssetAccuracy, true);

                return new
                {
                    NeededAmount = new
                    {
                        Amount = context.Type == OperationType.MarketOrder ? Math.Min(balance, neededAmount) : neededAmount
                    }
                };
            }

            return new { };
        }

        public object GetWalletBalance(Operation context)
        {
            var clientId = (string)context.OperationValues.Client.Id;
            var neededAssetId = (string)context.OperationValues.NeededAssetId;

            return _balancesClient.GetClientBalanceByAssetId(new ClientBalanceByAssetIdModel(neededAssetId, clientId)).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public void SaveOrder(Operation context)
        {
            if (context.Type == OperationType.MarketOrder)
            {
                _offchainOrdersRepository.CreateOrder(
                        context.ClientId.ToString(),
                        (string) context.OperationValues.AssetId,
                        (string) context.OperationValues.AssetPairId,
                        (decimal) context.OperationValues.Volume,
                        (decimal) context.OperationValues.NeededAmount.Amount,
                        (bool) (context.OperationValues.AssetPair.BaseAsset.Id == context.OperationValues.AssetId))
                    .ConfigureAwait(false).GetAwaiter().GetResult();
            }

            if (context.Type == OperationType.LimitOrder)
            {
                _offchainOrdersRepository.CreateLimitOrder(
                        context.ClientId.ToString(),
                        (string)context.OperationValues.AssetId,
                        (string)context.OperationValues.AssetPairId,
                        (decimal)context.OperationValues.Volume,
                        (decimal)context.OperationValues.NeededAmount.Amount,
                        (bool)(context.OperationValues.AssetPair.BaseAsset.Id == context.OperationValues.AssetId),
                        (decimal)context.OperationValues.Price)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }
    }
}
