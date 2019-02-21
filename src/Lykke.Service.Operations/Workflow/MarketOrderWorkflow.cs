using System;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.MatchingEngine.Connector.Abstractions.Services;
using Lykke.MatchingEngine.Connector.Models.Api;
using Lykke.MatchingEngine.Connector.Models.Common;
using Lykke.Service.FeeCalculator.Client;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Workflow.Data;
using Lykke.Service.Operations.Workflow.Exceptions;
using Lykke.Service.Operations.Workflow.Extensions;
using Lykke.Workflow;
using Newtonsoft.Json.Linq;
using FeeType = Lykke.Service.FeeCalculator.AutorestClient.Models.FeeType;
using OrderAction = Lykke.Service.Operations.Contracts.Orders.OrderAction;

namespace Lykke.Service.Operations.Workflow
{
    [UsedImplicitly]
    public class MarketOrderWorkflow : OrderWorkflow
    {
        private readonly IFeeCalculatorClient _feeCalculatorClient;
        private readonly IMatchingEngineClient _matchingEngineClient;
        private readonly ILog _log;

        public MarketOrderWorkflow(
            Operation operation,
            ILogFactory logFactory,
            IActivityFactory activityFactory,
            IFeeCalculatorClient feeCalculatorClient,
            IMatchingEngineClient matchingEngineClient) : base(operation, logFactory, activityFactory)
        {
            _log = logFactory.CreateLog(this);
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
                    Fee = ((JObject)context.OperationValues.Fee)?.ToObject<MarketOrderFeeModel>()
                })
                .MergeOutput(output => new { Me = output })
                .MergeFailOutput(output => new { ErrorMessage = output.Message, ErrorCode = WorkflowException.GetExceptionCode(output) });
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
                    ? FeeSizeType.ABSOLUTE
                    : FeeSizeType.PERCENTAGE,
                SourceClientId = input.ClientId,
                TargetClientId = fee.TargetWalletId ?? input.TargetClientId,
                Type = fee.Amount == 0m
                    ? Lykke.MatchingEngine.Connector.Models.Common.FeeType.NO_FEE
                    : Lykke.MatchingEngine.Connector.Models.Common.FeeType.CLIENT_FEE,
                AssetId = string.IsNullOrEmpty(fee.TargetAssetId)
                    ? Array.Empty<string>()
                    : new[] { fee.TargetAssetId }
            };
        }

        private object SendToMe(MeMoOrderInput input)
        {
            try
            {
                var marketOrderModel = new MarketOrderModel
                {
                    Id = input.Id,
                    AssetPairId = input.AssetPairId,
                    ClientId = input.ClientId,
                    ReservedLimitVolume = null,
                    Straight = input.Straight,
                    Volume = Math.Abs(input.Volume),
                    OrderAction = input.OrderAction == OrderAction.Buy
                        ? MatchingEngine.Connector.Models.Common.OrderAction.Buy
                        : MatchingEngine.Connector.Models.Common.OrderAction.Sell
                };

                if (input.Fee != null)
                    marketOrderModel.Fees = new[] { input.Fee };

                var response = _matchingEngineClient.HandleMarketOrderAsync(marketOrderModel).ConfigureAwait(false).GetAwaiter().GetResult();

                if (response == null)
                    throw new ApplicationException("Me is not available");

                if (response.Status != MeStatusCodes.Ok)
                {
                    _log.Warning($"ME returned invalid status code: [{response.Status}]", context: response);

                    throw new ApplicationException(response.Status.Format());
                }

                return new
                {
                    Status = response.Status.ToString(),
                    response.Price
                };
            }
            catch (Exception e)
            {
                _log.Error(e);
                throw;
            }
        }
    }
}
