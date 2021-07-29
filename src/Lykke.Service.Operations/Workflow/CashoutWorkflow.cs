using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.BlockchainCashoutPreconditionsCheck.Client;
using Lykke.Service.ExchangeOperations.Client;
using Lykke.Service.ExchangeOperations.Client.Models.Fee;
using Lykke.Service.FeeCalculator.AutorestClient.Models;
using Lykke.Service.FeeCalculator.Client;
using Lykke.Service.Limitations.Client;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Services.Blockchain;
using Lykke.Service.Operations.Workflow.Data;
using Lykke.Service.Operations.Workflow.Exceptions;
using Lykke.Service.Operations.Workflow.Extensions;
using Lykke.Workflow;
using Lykke.Workflow.Fluent;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Lykke.Common.ApiLibrary.Exceptions;
using Lykke.MatchingEngine.Connector.Models.Api;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.BlockchainCashoutPreconditionsCheck.Contract.Requests;
using Lykke.Service.ExchangeOperations.Client.Models;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.WindowsAzure.Storage;
using Polly;
using FeeType = Lykke.Service.FeeCalculator.AutorestClient.Models.FeeType;

namespace Lykke.Service.Operations.Workflow
{
    public class CashoutWorkflow : OperationWorkflow
    {
        private readonly ILog _log;
        private readonly IFeeCalculatorClient _feeCalculatorClient;
        private readonly IExchangeOperationsServiceClient _exchangeOperationsServiceClient;
        private readonly IBlockchainCashoutPreconditionsCheckClient _blockchainCashoutPreconditionsCheckClient;

        public CashoutWorkflow(
            Operation operation,
            ILogFactory log,
            IActivityFactory activityFactory,
            BlockchainAddress blockchainAddress,
            IFeeCalculatorClient feeCalculatorClient,
            IExchangeOperationsServiceClient exchangeOperationsServiceClient,
            IBlockchainCashoutPreconditionsCheckClient blockchainCashoutPreconditionsCheckClient) : base(operation, log, activityFactory)
        {
            _log = log.CreateLog(this);
            _feeCalculatorClient = feeCalculatorClient;
            _exchangeOperationsServiceClient = exchangeOperationsServiceClient;
            _blockchainCashoutPreconditionsCheckClient = blockchainCashoutPreconditionsCheckClient;

            Configure(cfg =>
                cfg
                    .Do("Global validation").OnFail("Fail operation")
                    .Do("Client validation").OnFail("Fail operation")
                    .Do("Asset validation").OnFail("Fail operation")
                    .Do("Destination address validation").OnFail("Fail operation")
                    .Do("Disclaimers validation").OnFail("Fail operation")
                    .Do("Balance validation").OnFail("Fail operation")
                    .On("Is sirius integration").DeterminedAs(context => Enum.TryParse<BlockchainIntegrationType>((string)context.OperationValues.Asset.BlockchainIntegrationType, out var type) && type == BlockchainIntegrationType.Sirius)
                        .ContinueWith("Calculate fee")
                    .On("Is not blockchain integration").DeterminedAs(context => string.IsNullOrWhiteSpace((string)context.OperationValues.Asset.BlockchainIntegrationLayerId) && Enum.TryParse<BlockchainIntegrationType>((string)context.OperationValues.Asset.BlockchainIntegrationType, out var type) && type != BlockchainIntegrationType.Sirius)
                        .ContinueWith("Limits validation")
                    .On("Is blockchain integration").DeterminedAs(context => !string.IsNullOrWhiteSpace((string)context.OperationValues.Asset.BlockchainIntegrationLayerId) && Enum.TryParse<BlockchainIntegrationType>((string)context.OperationValues.Asset.BlockchainIntegrationType, out var type) && type != BlockchainIntegrationType.Sirius)
                        .ContinueWith("Merge blockchain address")
                    .WithBranch()
                        .Do("Merge blockchain address").OnFail("Fail operation")
                        .Do("BIL check").OnFail("Fail operation")
                        .Do("BIL check validation").OnFail("Fail operation")
                        .ContinueWith("Limits validation")
                    .WithBranch()
                        .Do("Limits validation").OnFail("Fail operation")
                        .Do("Calculate fee").OnFail("Fail operation")
                        .Do("Accept operation").OnFail("Fail operation")
                        .On("2FA is disabled").DeterminedAs(context => !(bool)context.OperationValues.GlobalSettings.TwoFactorEnabled)
                            .ContinueWith("Send to ME")
                        .On("2FA is enabled").DeterminedAs(context => (bool)context.OperationValues.GlobalSettings.TwoFactorEnabled)
                            .ContinueWith("Start confirmation process")
                    .WithBranch()
                        .Do("Start confirmation process").OnFail("Fail operation")
                        .Do("Request confirmation").OnFail("Fail operation")
                        .Do("Validate confirmation").OnFail("Fail operation")
                        .On("Confirmation invalid").DeterminedAs(context => !(bool)context.OperationValues.Confirmation.IsValid)
                            .ContinueWith("Start confirmation process")
                        .On("Confirmation valid").DeterminedAs(context => (bool)context.OperationValues.Confirmation.IsValid)
                            .ContinueWith("Send to ME")
                    .WithBranch()
                        .Do("Send to ME").OnFail("Fail operation")
                        .Do("Confirm operation")
                        .Do("Wait for results from ME")
                        .Do("Settle on blockchain")
                        .Do("Complete operation")
                        .ContinueWith("Send operation status")
                    .WithBranch()
                        .Do("Fail operation")
                        .ContinueWith("Send operation status")
                    .WithBranch()
                        .Do("Send operation status")
                    .End()
            );

            ValidationNode<GlobalInput>("Global validation")
                .WithInput(context => new GlobalInput
                {
                    CashoutBlocked = context.OperationValues.GlobalSettings.CashOutBlocked
                })
                .MergeFailOutput(output => output);

            ValidationNode<ClientInput>("Client validation")
                .WithInput(context => new ClientInput
                {
                    OperationsBlocked = context.OperationValues.Client.CashOutBlocked,
                })
                .MergeFailOutput(output => output);

            ValidationNode<AddressInput>("Destination address validation")
                .WithInput(context => new AddressInput
                {
                    AssetId = context.OperationValues.Asset.Id,
                    AssetBlockchain = context.OperationValues.Asset.Blockchain,
                    DestinationAddress = context.OperationValues.DestinationAddress,
                    BlockchainIntegrationLayerId = context.OperationValues.Asset.BlockchainIntegrationLayerId,
                })
                .MergeFailOutput(output => output);

            ValidationNode<AssetInput>("Asset validation")
                .WithInput(context => new AssetInput
                {
                    DisplayId = context.OperationValues.Asset.DisplayId,
                    IsTradable = context.OperationValues.Asset.IsTradable,
                    IsTrusted = context.OperationValues.Asset.IsTrusted,
                    SiriusAssetId = context.OperationValues.Asset.SiriusAssetId,
                    BlockchainIntegrationType = context.OperationValues.Asset.BlockchainIntegrationType
                })
                .MergeFailOutput(output => output);

            ValidationNode<BalanceInput>("Balance validation")
                .WithInput(context => new BalanceInput
                {
                    Balance = context.OperationValues.Client.Balance,
                    Volume = context.OperationValues.Volume
                })
                .MergeFailOutput(output => output);

            ValidationNode<DisclaimerInput>("Disclaimers validation")
                .WithInput(context => new DisclaimerInput
                {
                    Type = OperationType.Cashout,
                    ClientId = context.OperationValues.Client.Id,
                    LykkeEntityId1 = context.OperationValues.Asset.LykkeEntityId
                })
                .MergeFailOutput(output => output);

            DelegateNode<BlockchainAddressInput, string>("Merge blockchain address",
                    input => blockchainAddress.MergeAsync(input.DestinationAddress,
                                                          input.DestinationAddressExtension,
                                                          input.BlockchainIntegrationLayerId)
                                                          .ConfigureAwait(false)
                                                        .GetAwaiter()
                                                        .GetResult())
                .WithInput(context => new BlockchainAddressInput
                {
                    DestinationAddress = context.OperationValues.DestinationAddress,
                    DestinationAddressExtension = context.OperationValues.DestinationAddressExtension,
                    BlockchainIntegrationLayerId = context.OperationValues.Asset.BlockchainIntegrationLayerId
                })
                .MergeOutput(output => !string.IsNullOrWhiteSpace(output)
                    ? new
                    {
                        DestinationAddress = output
                    }
                    : (object)new { })
                .MergeFailOutput(e => new { ErrorMessage = e.Message });

            DelegateNode<BilInput, BilOutput>("BIL check", input => BilCheck(input))
                .WithInput(context => new BilInput
                {
                    AssetId = context.OperationValues.Asset.Id,
                    Amount = context.OperationValues.Volume,
                    DestinationAddress = context.OperationValues.DestinationAddress
                })
                .MergeOutput(output => new { BilCheckResult = output })
                .MergeFailOutput(e => new { ErrorMessage = e.Message });

            ValidationNode<BilOutput>("BIL check validation")
                .WithInput(context => ((JObject)context.OperationValues.BilCheckResult).ToObject<BilOutput>())
                .MergeFailOutput(output => output);

            ValidationNode<LimitationInput>("Limits validation")
                .WithInput(context => new LimitationInput
                {
                    AssetId = context.OperationValues.Asset.Id,
                    Volume = context.OperationValues.Volume,
                    ClientId = context.OperationValues.Client.Id,
                    OperationType = CurrencyOperationType.CryptoCashOut
                })
                .MergeFailOutput(output => output);

            DelegateNode<CalculateCashoutFeeInput, object>("Calculate fee", input => CalculateFee(input))
                .WithInput(context => new CalculateCashoutFeeInput
                {
                    AssetId = context.OperationValues.Asset.Id
                })
                .MergeOutput(output => new { Fee = output })
                .MergeFailOutput(e => new { ErrorMessage = e.Message });

            DelegateNode<StartConfirmationInput, object>("Start confirmation process", input => StartConfirmationProcess(input))
                .WithInput(context => new StartConfirmationInput
                {
                    ConfirmationAttemptsCount = (int?)context.OperationValues.Confirmation?.ConfirmationAttemptsCount ?? 0,
                    MaxConfirmationAttempts = context.OperationValues.GlobalSettings.MaxConfirmationAttempts
                })
                .MergeOutput(output => new { Confirmation = output })
                .MergeFailOutput(failOutput => new { ErrorCode = WorkflowException.GetExceptionCode(failOutput), ErrorMessage = failOutput.Message });

            Node("Request confirmation", i => i.RequestConfirmation())
                .WithInput(context => new ConfirmationRequestInput
                {
                    OperationId = context.Id,
                    ClientId = context.ClientId,
                    ConfirmationType = context.OperationValues.Client.ConfirmationType
                })
                .MergeOutput(output => output)
                .MergeFailOutput(e => new { ErrorMessage = e.Message });

            Node("Validate confirmation", i => i.ValidateConfirmation())
                .WithInput(context => new ValidateConfirmationInput
                {
                    OperationId = context.Id,
                    ClientId = context.OperationValues.Client.Id,
                    Confirmation = context.OperationValues.Confirmation.Code,
                    ConfirmationType = context.OperationValues.Client.ConfirmationType
                })
                .MergeOutput(output => output)
                .MergeFailOutput(e => new { ErrorMessage = e.Message });

            DelegateNode<CashoutMeInput>("Send to ME", i => SendToMe(i))
                .WithInput(context =>
                {
                    string clientId = context.OperationValues.Client.Id;

                    try
                    {
                        clientId = context.OperationValues.WalletId;
                    }
                    catch (RuntimeBinderException)
                    {
                        // if WalletId is present in the Context of the Operation, it means we're dealing with API wallet
                        // which means that we should pass its Id as ClientId for the ME
                    }
                    
                    return new CashoutMeInput
                    {
                        OperationId = context.Id,
                        ClientId =  clientId,
                        DestinationAddress = context.OperationValues.DestinationAddress,
                        Volume = context.OperationValues.Volume,
                        AssetId = context.OperationValues.Asset.Id,
                        AssetAccuracy = context.OperationValues.Asset.Accuracy,
                        CashoutTargetClientId = context.OperationValues.GlobalSettings.FeeSettings.TargetClients["Cashout"],
                        FeeSize = context.OperationValues.Fee.Size,
                        FeeType = context.OperationValues.Fee.Type
                    };
                })
                .MergeFailOutput(e => new
                {
                    ErrorCode = "MeError",
                    ErrorMessage = e.Message
                });

            Node("Wait for results from ME", i => i.WaitForResultsFromMe())
                .WithInput(context => new { })
                .MergeOutput(output => new { Me = output })
                .MergeFailOutput(e => new { ErrorMessage = e.Message });

            Node("Settle on blockchain", i => i.SettleOnBlockchain())
                .WithInput(context =>
                {
                    Guid? walletId=default;
                    try
                    {
                        string walletIdStr = context.OperationValues.WalletId;
                        walletId = !string.IsNullOrWhiteSpace(walletIdStr)
                            ? Guid.Parse(walletIdStr)
                            : default;
                    }
                    catch (RuntimeBinderException)
                    {
                        // for backwards compatibility
                    } 
                    
                    return new BlockchainCashoutInput
                        {
                            OperationId = context.Id,
                            ClientId = context.ClientId,
                            AssetId = context.OperationValues.Asset.Id,
                            SiriusAssetId = context.OperationValues.Asset.SiriusAssetId,
                            BlockchainIntegrationType = context.OperationValues.Asset.BlockchainIntegrationType,
                            AssetBlockchain = context.OperationValues.Asset.Blockchain,
                            AssetBlockchainWithdrawal = context.OperationValues.Asset.BlockchainWithdrawal,
                            BlockchainIntegrationLayerId = context.OperationValues.Asset.BlockchainIntegrationLayerId,
                            Amount = context.OperationValues.Volume,
                            ToAddress = context.OperationValues.DestinationAddress,
                            Tag = context.OperationValues.DestinationAddressExtension,
                            WalletId = walletId,
                            EthHotWallet = context.OperationValues.GlobalSettings.EthereumHotWallet
                        };
                })
                .MergeOutput(output => new { Blockchain = output })
                .MergeFailOutput(e => new { ErrorMessage = e.Message });

            DelegateNode("Fail operation", context => context.Fail());
            DelegateNode("Accept operation", context => context.Accept());
            DelegateNode("Confirm operation", context => context.Confirm());
            DelegateNode("Complete operation", context => context.Complete());
        }

        private void SendToMe(CashoutMeInput input)
        {
            var policy = Policy
                .Handle<ClientApiException>(ex =>
                {
                    _log.Error($"Retry on ClientApiException. HTTP status code: {ex.HttpStatusCode}, Error response: {ex.ErrorResponse.ToJson()}", context: input.ToJson(), exception: ex);
                    return true;
                })
                .Or<TaskCanceledException>(exception =>
                {
                    _log.Warning("Retry on TaskCanceledException", context: input.ToJson(), exception: exception);
                    return true;
                }).Or<StorageException>(exception =>
                {
                    _log.Warning("Retry on StorageException", context: input.ToJson(), exception: exception);
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
                var res = _exchangeOperationsServiceClient.ExchangeOperations.CashOutAsync(
                    new CashOutRequestModel
                        {
                            ClientId = input.ClientId,
                            Address = input.DestinationAddress,
                            Amount = (double) input.Volume,
                            AssetId = input.AssetId,
                            TransactionId = input.OperationId.ToString(),
                            FeeClientId = input.CashoutTargetClientId,
                            FeeSize = input.FeeSize,
                            FeeSizeType = input.FeeType == FeeType.Absolute
                                ? FeeSizeType.ABSOLUTE
                                : FeeSizeType.PERCENTAGE,
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

            var message = $"{result.Code}: {result.Message}";
            _log.Warning(message, context: new {input.OperationId, ErrorMessage = message});
            throw new InvalidOperationException(message);
        }

        private BilOutput BilCheck(BilInput input)
        {
            var result = _blockchainCashoutPreconditionsCheckClient.ValidateCashoutAsync(new CheckCashoutValidityModel()
            {
                AssetId = input.AssetId,
                Amount = input.Amount,
                DestinationAddress = input.DestinationAddress
            }).ConfigureAwait(false).GetAwaiter().GetResult();

            return new BilOutput
            {
                IsAllowed = result.isAllowed,
                Errors = result.Item2
            };
        }

        private object CalculateFee(CalculateCashoutFeeInput input)
        {
            return _feeCalculatorClient.GetCashoutFeesAsync(input.AssetId).ConfigureAwait(false).GetAwaiter().GetResult()
                    .FirstOrDefault() ?? new CashoutFee
                    {
                        AssetId = input.AssetId,
                        Size = 0,
                        Type = FeeType.Absolute
                    };
        }

        private object StartConfirmationProcess(StartConfirmationInput input)
        {
            var currentAttemptsCount = input.ConfirmationAttemptsCount + 1;

            if (currentAttemptsCount > input.MaxConfirmationAttempts)
                throw new WorkflowException("ConfirmationFailed", "Number of attempts exceeded");

            return new { ConfirmationAttemptsCount = currentAttemptsCount };
        }
    }
}
