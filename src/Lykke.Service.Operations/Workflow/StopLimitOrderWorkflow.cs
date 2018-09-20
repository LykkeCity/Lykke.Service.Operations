using System;
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
using Lykke.Workflow.Fluent;
using Newtonsoft.Json.Linq;
using FeeType = Lykke.Service.FeeCalculator.AutorestClient.Models.FeeType;
using OrderAction = Lykke.Service.Operations.Contracts.Orders.OrderAction;

namespace Lykke.Service.Operations.Workflow
{
    [UsedImplicitly]
    public class StopLimitOrderWorkflow : OrderWorkflow
    {
        private readonly IFeeCalculatorClient _feeCalculatorClient;
        private readonly IMatchingEngineClient _matchingEngineClient;

        public StopLimitOrderWorkflow(
            Operation operation,
            ILogFactory logFactory,
            IActivityFactory activityFactory,
            IFeeCalculatorClient feeCalculatorClient,
            IMatchingEngineClient matchingEngineClient) : base(operation, logFactory, activityFactory)
        {
            _feeCalculatorClient = feeCalculatorClient;
            _matchingEngineClient = matchingEngineClient;

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

            DelegateNode<MeStopLoOrderInput, object>("Send to ME", input => SendToMe(input))
                .WithInput(context => new MeStopLoOrderInput
                {
                    Id = context.Id.ToString(),
                    AssetPairId = context.OperationValues.AssetPair.Id,
                    ClientId = context.OperationValues.Client.Id,
                    OrderAction = context.OperationValues.OrderAction,
                    Volume = (double)context.OperationValues.Volume,
                    LowerLimitPrice = (double?)context.OperationValues.LowerLimitPrice,
                    LowerPrice = (double?)context.OperationValues.LowerPrice,
                    UpperLimitPrice = (double?)context.OperationValues.UpperLimitPrice,
                    UpperPrice = (double?)context.OperationValues.UpperPrice,
                    Fee = ((JObject)context.OperationValues.Fee)?.ToObject<LimitOrderFeeModel>()
                })
                .MergeOutput(output => new { Me = output })
                .MergeFailOutput(output => new { ErrorMessage = output.Message, ErrorCode = WorkflowException.GetExceptionCode(output) });
        }

        protected override WorkflowConfiguration<Operation> ConfigurePreMeNodes(WorkflowConfiguration<Operation> configuration)
        {
            return configuration
                .Do("Create stop limit order").OnFail("Fail operation");
        }

        protected override WorkflowConfiguration<Operation> ConfigurePostMeNodes(WorkflowConfiguration<Operation> configuration)
        {
            return configuration
                .Do("Process stop limit order after Me").OnFail("Fail operation");
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
                MakerFeeModificator = (double)fee.MakerFeeModificator,
                MakerSizeType = fee.MakerFeeType == FeeType.Absolute
                    ? FeeSizeType.ABSOLUTE
                    : FeeSizeType.PERCENTAGE,
                TakerSizeType = fee.TakerFeeType == FeeType.Absolute
                    ? FeeSizeType.ABSOLUTE
                    : FeeSizeType.PERCENTAGE,
                SourceClientId = input.ClientId,
                TargetClientId = input.TargetClientId,
                Type = fee.MakerFeeSize == 0m && fee.TakerFeeSize == 0m 
                    ? MatchingEngine.Connector.Models.Common.FeeType.NO_FEE 
                    : MatchingEngine.Connector.Models.Common.FeeType.CLIENT_FEE
            };
        }

        private object SendToMe(MeStopLoOrderInput input)
        {
            var stopLimitOrderModel = new StopLimitOrderModel
            {
                Id = input.Id,
                ClientId = input.ClientId,
                AssetPairId = input.AssetPairId,
                OrderAction = input.OrderAction == OrderAction.Buy
                    ? MatchingEngine.Connector.Models.Common.OrderAction.Buy
                    : MatchingEngine.Connector.Models.Common.OrderAction.Sell,
                LowerLimitPrice = input.LowerLimitPrice,
                LowerPrice = input.LowerPrice,
                UpperLimitPrice = input.UpperLimitPrice,
                UpperPrice = input.UpperPrice,
                Volume = Math.Abs(input.Volume),
                Fee = input.Fee,
            };

            var response = _matchingEngineClient.PlaceStopLimitOrderAsync(stopLimitOrderModel).ConfigureAwait(false).GetAwaiter().GetResult();

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
    }
}
