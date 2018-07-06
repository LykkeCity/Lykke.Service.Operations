using System;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.MatchingEngine.Connector.Abstractions.Models;
using Lykke.Service.ExchangeOperations.Client;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Workflow.Data;
using Lykke.Service.Operations.Workflow.Exceptions;
using Lykke.Service.Operations.Workflow.Extensions;
using Lykke.Service.SwiftWithdrawal.Contracts;
using Lykke.Workflow;
using Lykke.Workflow.Fluent;
using AssetInput = Lykke.Service.Operations.Workflow.Data.SwiftCashout.AssetInput;

namespace Lykke.Service.Operations.Workflow
{
    [UsedImplicitly]
    public class SwiftCashoutWorkflow : OperationWorkflow
    {
        private readonly IExchangeOperationsServiceClient _exchangeOperationsServiceClient;
        private readonly ICqrsEngine _cqrsEngine;

        public SwiftCashoutWorkflow(
            Operation operation,
            ILog log,
            IActivityFactory activityFactory,
            IExchangeOperationsServiceClient exchangeOperationsServiceClient, ICqrsEngine cqrsEngine) : base(operation, log, activityFactory)
        {
            _exchangeOperationsServiceClient = exchangeOperationsServiceClient;
            _cqrsEngine = cqrsEngine;

            Configure(cfg =>
                cfg
                    .Do("Swift check").OnFail("Fail operation")
                    .Do("Asset check").OnFail("Fail operation")
                    .Do("Asset kyc validation").OnFail("Fail operation")
                    .Do("Balance check").OnFail("Fail operation")
                    .Do("Disclaimers validation").OnFail("Fail operation")
                    .Do("Limits check").OnFail("Fail operation")
                    .Do("Send to exchange operations").OnFail("Fail operation")
                    .Do("Send create request command").OnFail("Fail operation")
                    .Do("Confirm operation")
                    .End()
                    .WithBranch()
                        .Do("Fail operation")
                        .End()
            );

            ValidationNode<SwiftInput>("Swift check")
                .WithInput(context => new SwiftInput
                {
                    AccHolderAddress = context.OperationValues.Swift.AccHolderAddress,
                    AccHolderCity = context.OperationValues.Swift.AccHolderCity,
                    AccHolderCountry = context.OperationValues.Swift.AccHolderCountry,
                    AccHolderZipCode = context.OperationValues.Swift.AccHolderZipCode,
                    AccName = context.OperationValues.Swift.AccName,
                    AccNumber = context.OperationValues.Swift.AccNumber,
                    BankName = context.OperationValues.Swift.BankName,
                    Bic = context.OperationValues.Swift.Bic,
                })
                .MergeFailOutput(output => output);

            ValidationNode<AssetInput>("Asset check")
                .WithInput(context => new AssetInput
                {
                    SwiftCashoutEnabled = context.OperationValues.Asset.SwiftCashoutEnabled
                })
                .MergeFailOutput(output => output);

            ValidationNode<AssetKycInput>("Asset kyc validation")
                .WithInput(context => new AssetKycInput
                {
                    KycStatus = context.OperationValues.Client.KycStatus,
                    AssetId = context.OperationValues.Asset.Id,
                    AssetKycNeeded = context.OperationValues.Asset.KycNeeded
                })
                .MergeFailOutput(output => output);

            ValidationNode<BalanceCheckInput>("Balance check")
                .WithInput(context => new BalanceCheckInput
                {
                    AssetId = context.OperationValues.Asset.Id,
                    ClientId = context.OperationValues.Client.Id,
                    Volume = context.OperationValues.Volume
                })
                .MergeFailOutput(output => output);

            ValidationNode<DisclaimerInput>("Disclaimers validation")
                .WithInput(context => new DisclaimerInput
                {
                    Type = context.Type,
                    ClientId = context.OperationValues.Client.Id,
                    LykkeEntityId1 = context.OperationValues.Asset.LykkeEntityId
                })
                .MergeFailOutput(output => output);


            ValidationNode<LimitationInput>("Limits check")
                .WithInput(context => new LimitationInput
                {
                    ClientId = context.OperationValues.Client.Id,
                    AssetId = context.OperationValues.Asset.Id,
                    Volume = context.OperationValues.Volume
                })
                .MergeFailOutput(output => output);

            DelegateNode("Fail operation", context => context.Fail());

            DelegateNode("Confirm operation", context => context.Confirm());

            DelegateNode<ExchangeOperationsInput>("Send to exchange operations", input => SendToExchangeOperations(input))
                .WithInput(context => new ExchangeOperationsInput
                {
                    Id = context.Id.ToString(),
                    ClientId = context.OperationValues.Client.Id,
                    AssetId = context.OperationValues.Asset.Id,
                    Volume = context.OperationValues.Volume
                })
                .MergeFailOutput(failOutput => new { ErrorCode = WorkflowException.GetExceptionCode(failOutput), ErrorMessage = failOutput.Message });

            DelegateNode<SendCreateRequestCommandInput>("Send create request command", input => SendCreateRequestCommand(input))
                .WithInput(context => new SendCreateRequestCommandInput
                {
                    Id = context.Id.ToString(),
                    ClientId = context.OperationValues.Client.Id,
                    AssetId = context.OperationValues.Asset.Id,
                    Volume = context.OperationValues.Volume,
                    Swift = new SwiftInput
                    {
                        AccHolderAddress = context.OperationValues.Swift.AccHolderAddress,
                        AccHolderCity = context.OperationValues.Swift.AccHolderCity,
                        AccHolderCountry = context.OperationValues.Swift.AccHolderCountry,
                        AccHolderZipCode = context.OperationValues.Swift.AccHolderZipCode,
                        AccName = context.OperationValues.Swift.AccName,
                        AccNumber = context.OperationValues.Swift.AccNumber,
                        BankName = context.OperationValues.Swift.BankName,
                        Bic = context.OperationValues.Swift.Bic,
                    }
                })
                .MergeFailOutput(failOutput => new { ErrorCode = WorkflowException.GetExceptionCode(failOutput), ErrorMessage = failOutput.Message });
        }

        private void SendToExchangeOperations(ExchangeOperationsInput input)
        {
            var additionalData = new AdditionalData
            {
                SwiftData = new AdditionalData.CashoutRequestData
                {
                    CashOutRequestId = input.Id
                }
            }.ToJsonString();

            var result = _exchangeOperationsServiceClient
                .CashOutAsync(input.ClientId, "SwiftCashout", (double)input.Volume, input.AssetId, additionalData).GetAwaiter()
                .GetResult();

            if (result.IsOk())
                return;

            if (!result.Code.HasValue || !Enum.IsDefined(typeof(MeStatusCodes), result.Code))
                throw new WorkflowException("InternalError", "Exchange operation service failed");

            var meCode = (MeStatusCodes)result.Code;

            throw new WorkflowException(meCode.GetStringCode(), meCode.Format());
        }

        private void SendCreateRequestCommand(SendCreateRequestCommandInput input)
        {
            _cqrsEngine.SendCommand(new SwiftCashoutCreateCommand
            {
                Id = input.Id,
                AssetId = input.AssetId,
                ClientId = input.ClientId,
                State = TransactionState.SettledOffchain,
                TradeSystem = CashoutRequestTradeSystem.Spot,
                Volume = input.Volume,
                SwiftData = new SwiftDataModel
                {
                    AccHolderAddress = input.Swift.AccHolderAddress,
                    AccHolderCity = input.Swift.AccHolderCity,
                    AccHolderCountry = input.Swift.AccHolderCountry,
                    AccHolderZipCode = input.Swift.AccHolderZipCode,
                    AccName = input.Swift.AccName,
                    AccNumber = input.Swift.AccNumber,
                    BankName = input.Swift.BankName,
                    Bic = input.Swift.Bic
                }
            }, SwiftWithdrawalBoundedContext.Name, SwiftWithdrawalBoundedContext.Name);
        }
    }

    internal class ExchangeOperationsInput
    {
        public string Id { get; set; }

        public string ClientId { get; set; }

        public string AssetId { get; set; }

        public decimal Volume { get; set; }
    }

    internal class AdditionalData
    {
        public CashoutRequestData SwiftData { get; set; }

        internal class CashoutRequestData
        {
            public string CashOutRequestId { get; set; }
        }
    }

    internal class SendCreateRequestCommandInput
    {
        public string Id { get; set; }
        
        public string ClientId { get; set; }

        public string AssetId { get; set; }

        public decimal Volume { get; set; }

        public SwiftInput Swift { get; set; }
    }
}
