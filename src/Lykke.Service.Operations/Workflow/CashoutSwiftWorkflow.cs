﻿using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.ApiLibrary.Exceptions;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.MatchingEngine.Connector.Models.Api;
using Lykke.Service.ExchangeOperations.Client;
using Lykke.Service.ExchangeOperations.Client.Models;
using Lykke.Service.FeeCalculator.Client;
using Lykke.Service.Limitations.Client;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Workflow.Data;
using Lykke.Service.Operations.Workflow.Exceptions;
using Lykke.Service.Operations.Workflow.Extensions;
using Lykke.Service.SwiftWithdrawal.Contracts;
using Lykke.Workflow;
using Lykke.Workflow.Fluent;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json.Linq;
using Polly;
using AssetInput = Lykke.Service.Operations.Workflow.Data.SwiftCashout.AssetInput;

namespace Lykke.Service.Operations.Workflow
{
    [UsedImplicitly]
    public class CashoutSwiftWorkflow : OperationWorkflow
    {
        private readonly IExchangeOperationsServiceClient _exchangeOperationsServiceClient;
        private readonly IFeeCalculatorClient _feeCalculatorClient;
        private readonly ICqrsEngine _cqrsEngine;
        private readonly ILog _log;

        public CashoutSwiftWorkflow(
            Operation operation,
            ILogFactory logFactory,
            IActivityFactory activityFactory,
            IExchangeOperationsServiceClient exchangeOperationsServiceClient,
            IFeeCalculatorClient feeCalculatorClient,
            ICqrsEngine cqrsEngine
            ) : base(operation, logFactory, activityFactory)
        {
            _log = logFactory.CreateLog(this);
            _exchangeOperationsServiceClient = exchangeOperationsServiceClient;
            _feeCalculatorClient = feeCalculatorClient;
            _cqrsEngine = cqrsEngine;

            Configure(cfg =>
                cfg
                    .Do("Swift check").OnFail("Fail operation")
                    .Do("Asset check").OnFail("Fail operation")
                    .Do("Balance check").OnFail("Fail operation")
                    .Do("Disclaimers validation").OnFail("Fail operation")
                    .Do("Limits check").OnFail("Fail operation")
                    .Do("Calculate fee").OnFail("Fail operation")
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
                    Volume = context.OperationValues.Volume,
                    OperationType = CurrencyOperationType.SwiftTransferOut
                })
                .MergeFailOutput(output => output);

            DelegateNode("Fail operation", context => context.Fail());

            DelegateNode("Confirm operation", context => context.Confirm());

            DelegateNode<CalculateSwiftCashoutFeeInput, ExchangeOperations.Client.Models.Fee.FeeModel>("Calculate fee", input => CalculateFee(input))
                .WithInput(context => new CalculateSwiftCashoutFeeInput
                {
                    ClientId = context.OperationValues.Client.Id,
                    AssetId = context.OperationValues.Asset.Id,
                    Bic = context.OperationValues.Swift.Bic,
                    FeeTargetId = context.OperationValues.CashoutSettings.FeeTargetId
                })
                .MergeOutput(output => new { Fee = output })
                .MergeFailOutput(output => output);

            DelegateNode<ExchangeOperationsInput>("Send to exchange operations", input => SendToExchangeOperations(input))
                .WithInput(context => new ExchangeOperationsInput
                {
                    Id = context.Id.ToString(),
                    ClientId = context.OperationValues.Client.Id,
                    HotwalletId = context.OperationValues.CashoutSettings.HotwalletTargetId,
                    AssetId = context.OperationValues.Asset.Id,
                    Volume = context.OperationValues.Volume,
                    Fee = ((JObject)context.OperationValues.Fee).ToObject<ExchangeOperations.Client.Models.Fee.FeeModel>()
                })
                .MergeFailOutput(failOutput => new { ErrorCode = WorkflowException.GetExceptionCode(failOutput), ErrorMessage = failOutput.Message });

            DelegateNode<SendCreateRequestCommandInput>("Send create request command", input => SendCreateRequestCommand(input))
                .WithInput(context => new SendCreateRequestCommandInput
                {
                    Id = context.Id.ToString(),
                    ClientId = context.OperationValues.Client.Id,
                    AssetId = context.OperationValues.Asset.Id,
                    Volume = context.OperationValues.Volume,
                    FeeSize = context.OperationValues.Fee.Size,
                    Swift = new SwiftInput
                    {
                        AccHolderAddress = context.OperationValues.Swift.AccHolderAddress,
                        AccHolderCity = context.OperationValues.Swift.AccHolderCity,
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
            var operationFee = Math.Abs(input.Fee.Size) > 0 ? input.Fee : null;

            var policy = Policy
                .Handle<ClientApiException>(exception =>
                {
                    _log.Warning("Retry on ClientApiException", context: input.ToJson());
                    return true;
                })
                .Or<TaskCanceledException>(exception =>
                {
                    _log.Warning("Retry on TaskCanceledException", context: input.ToJson());
                    return true;
                }).Or<StorageException>(exception =>
                {
                    _log.Warning("Retry on StorageException", context: input.ToJson());
                    return true;
                })
                .OrResult<ExchangeOperationResult>(r =>
                {
                    _log.Info(message: "Response from ME", r.ToJson());
                    return r.Code == 500;
                }) //ME runtime error
                .WaitAndRetry(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            var result = policy.Execute(() =>
            {
                var res = _exchangeOperationsServiceClient.ExchangeOperations.TransferAsync(
                        new TransferRequestModel
                        {
                            DestClientId = input.HotwalletId,
                            SourceClientId = input.ClientId,
                            Amount = (double)input.Volume,
                            AssetId = input.AssetId,
                            OperationId = input.Id,
                            Fee = operationFee,
                        })
                    .GetAwaiter().GetResult();

                return res;
            });

            if (result.IsOk())
                return;

            if (result.Code == (int) MeStatusCodes.Duplicate)
            {
                _log.Warning("Duplicate status from ME", context: result.ToJson());
                return;
            }

            if (!Enum.IsDefined(typeof(MeStatusCodes), result.Code))
                throw new WorkflowException("InternalError", $"Exchange operation service failed, code {result.Code}");

            var meCode = (MeStatusCodes)result.Code;

            throw new WorkflowException(meCode.GetStringCode(), meCode.Format());
        }

        private void SendCreateRequestCommand(SendCreateRequestCommandInput input)
        {
            var countryCode = input.Swift.Bic.GetCountryCode();

            _cqrsEngine.SendCommand(new SwiftCashoutCreateCommand
            {
                Id = input.Id,
                AssetId = input.AssetId,
                ClientId = input.ClientId,
                State = TransactionState.SettledOffchain,
                TradeSystem = CashoutRequestTradeSystem.Spot,
                Volume = input.Volume,
                FeeSize = input.FeeSize,
                SwiftData = new SwiftDataModel
                {
                    AccHolderAddress = input.Swift.AccHolderAddress,
                    AccHolderCity = input.Swift.AccHolderCity,
                    AccHolderCountry = CountryManager.GetCountryNameByIso2(countryCode),
                    AccHolderCountryCode = countryCode,
                    AccHolderZipCode = input.Swift.AccHolderZipCode,
                    AccName = input.Swift.AccName,
                    AccNumber = input.Swift.AccNumber,
                    BankName = input.Swift.BankName,
                    Bic = input.Swift.Bic
                }
            }, SwiftWithdrawalBoundedContext.Name, SwiftWithdrawalBoundedContext.Name);
        }

        private ExchangeOperations.Client.Models.Fee.FeeModel CalculateFee(CalculateSwiftCashoutFeeInput input)
        {
            var fee = _feeCalculatorClient.GetWithdrawalFeeAsync(input.AssetId, input.Bic.GetCountryCode()).ConfigureAwait(false).GetAwaiter().GetResult();

            return new ExchangeOperations.Client.Models.Fee.FeeModel()
            {
                Type = ExchangeOperations.Client.Models.Fee.FeeType.CLIENT_FEE,
                Size = fee.Size,
                SizeType = ExchangeOperations.Client.Models.Fee.FeeSizeType.ABSOLUTE,
                SourceClientId = input.ClientId,
                TargetClientId = input.FeeTargetId,
                ChargingType = ExchangeOperations.Client.Models.Fee.FeeChargingType.SUBTRACT_FROM_AMOUNT
            };
        }
    }


    internal class ExchangeOperationsInput
    {
        public string Id { get; set; }

        public string ClientId { get; set; }

        public string HotwalletId { get; set; }

        public string AssetId { get; set; }

        public decimal Volume { get; set; }

        public ExchangeOperations.Client.Models.Fee.FeeModel Fee { get; set; }
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

        public decimal FeeSize { get; set; }

        public SwiftInput Swift { get; set; }
    }

    internal class CalculateSwiftCashoutFeeInput
    {
        public string ClientId { get; set; }

        public string AssetId { get; set; }

        public string Bic { get; set; }

        public string FeeTargetId { get; set; }
    }


}
