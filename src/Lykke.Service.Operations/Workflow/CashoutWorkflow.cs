using System;
using System.Linq;
using Common;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.BlockchainCashoutPreconditionsCheck.Client;
using Lykke.Service.BlockchainCashoutPreconditionsCheck.Client.Models;
using Lykke.Service.ExchangeOperations.Client;
using Lykke.Service.FeeCalculator.AutorestClient.Models;
using Lykke.Service.FeeCalculator.Client;
using Lykke.Service.Limitations.Client;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Services.Blockchain;
using Lykke.Service.Operations.Workflow.Data;
using Lykke.Service.Operations.Workflow.Extensions;
using Lykke.Service.Operations.Workflow.Validation;
using Lykke.Workflow;
using Lykke.Workflow.Fluent;
using Newtonsoft.Json.Linq;
using FeeType = Lykke.Service.FeeCalculator.AutorestClient.Models.FeeType;

namespace Lykke.Service.Operations.Workflow
{
    public class CashoutWorkflow : OperationWorkflow
    {
        private readonly ILog _log;
        private readonly IEthereumFacade _ethereumFacade;
        private readonly IFeeCalculatorClient _feeCalculatorClient;        
        private readonly IExchangeOperationsServiceClient _exchangeOperationsServiceClient;
        private readonly IBlockchainCashoutPreconditionsCheckClient _blockchainCashoutPreconditionsCheckClient;

        public CashoutWorkflow(
            Operation operation, 
            ILog log, 
            IActivityFactory activityFactory,
            IEthereumFacade ethereumFacade,
            BlockchainAddress blockchainAddress,
            IFeeCalculatorClient feeCalculatorClient,
            IBlockchainCashoutPreconditionsCheckClient blockchainCashoutPreconditionsCheckClient,
            IExchangeOperationsServiceClient exchangeOperationsServiceClient) : base(operation, log, activityFactory)
        {
            _log = log;
            _ethereumFacade = ethereumFacade;
            _feeCalculatorClient = feeCalculatorClient;
            _blockchainCashoutPreconditionsCheckClient = blockchainCashoutPreconditionsCheckClient;
            _exchangeOperationsServiceClient = exchangeOperationsServiceClient;
            Configure(cfg =>
                cfg                    
                    .Do("Global validation").OnFail("Fail operation")
                    .Do("Client validation").OnFail("Fail operation")
                    .Do("Asset validation").OnFail("Fail operation")
                    .Do("Destination address validation").OnFail("Fail operation")
                    .Do("Kyc validation").OnFail("Fail operation")
                    .Do("Disclaimers validation").OnFail("Fail operation")
                    .Do("Balance validation").OnFail("Fail operation")
                    .Do("BTC ajust volume on low remainder").OnFail("Fail operation")
                    .On("Is not eth cashout").DeterminedAs(context => context.OperationValues.Asset.Id != "ETH").ContinueWith("Start blockchain validation")
                    .On("Is eth cashout").DeterminedAs(context => context.OperationValues.Asset.Id == "ETH").ContinueWith("Load eth adapter balance")                    
                    .WithBranch()
                        .Do("Load eth adapter balance").OnFail("Fail operation")
                        .Do("Estimate eth cashout").OnFail("Fail operation")                        
                        .Do("ETH validation").OnFail("Fail operation")
                        .ContinueWith("Start blockchain validation")
                    .WithBranch()
                        .Do("Start blockchain validation")
                    .On("Is not blockchain integration")
                        .DeterminedAs(context => string.IsNullOrWhiteSpace((string)context.OperationValues.Asset.BlockchainIntegrationLayerId))
                        .ContinueWith("Limits validation")
                    .On("Is blockchain integration")
                        .DeterminedAs(context => !string.IsNullOrWhiteSpace((string)context.OperationValues.Asset.BlockchainIntegrationLayerId))
                        .ContinueWith("Merge blockchain address")                    
                    .WithBranch()
                        .Do("Merge blockchain address").OnFail("Fail operation")
                        .Do("BIL check").OnFail("Fail operation")
                        .Do("BIL check validation").OnFail("Fail operation")
                        .ContinueWith("Limits validation")
                    .WithBranch()
                        .Do("Limits validation").OnFail("Fail operation")
                        .Do("Calculate fee").OnFail("Fail operation")                    
                        .Do("Send to ME").OnFail("Fail operation")
                        .ContinueWith("Confirm operation")
                    .WithBranch()
                        .Do("Fail operation")
                        .ContinueWith("Send operation status")
                    .WithBranch()
                        .Do("Confirm operation")
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
                    BackupDone = context.OperationValues.Client.BackupDone
                })
                .MergeFailOutput(output => output);

            ValidationNode<AddressInput>("Destination address validation")
                .WithInput(context => new AddressInput
                {
                    AssetId = context.OperationValues.Asset.Id,
                    AssetBlockchain = context.OperationValues.Asset.Blockchain,
                    DestinationAddress = context.OperationValues.DestinationAddress,
                    BlockchainIntegrationLayerId = context.OperationValues.Asset.BlockchainIntegrationLayerId,
                    Multisig = context.OperationValues.Client.Multisig
                })
                .MergeFailOutput(output => output);

            ValidationNode<AssetInput>("Asset validation")
                .WithInput(context => new AssetInput
                {
                    DisplayId = context.OperationValues.Asset.DisplayId,
                    IsTradable = context.OperationValues.Asset.IsTradable,
                    IsTrusted = context.OperationValues.Asset.IsTrusted                    
                })
                .MergeFailOutput(output => output);

            ValidationNode<BalanceInput>("Balance validation")
                .WithInput(context => new BalanceInput
                {                    
                    Balance = context.OperationValues.Client.Balance,
                    Volume = context.OperationValues.Volume
                })
                .MergeFailOutput(output => output);            

            ValidationNode<AssetKycInput>("Kyc validation")
                .WithInput(context => new AssetKycInput
                {
                    KycStatus = context.OperationValues.Client.KycStatus,
                    AssetId = context.OperationValues.Asset.Id,
                    AssetKycNeeded = context.OperationValues.Asset.KycNeeded
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

            DelegateNode<AjustmentInput, AjustmentOutput>("BTC ajust volume on low remainder", input => AjustBtcVolume(input))
                .WithInput(context => new AjustmentInput
                {
                    AssetId = context.OperationValues.Asset.Id,
                    AssetAccuracy = context.OperationValues.Asset.Accuracy,
                    CashoutMinimalAmount = context.OperationValues.Asset.CashoutMinimalAmount,
                    Balance = context.OperationValues.Client.Balance,                    
                    Volume = context.OperationValues.Volume,                    
                })
                .MergeOutput(output => output);

            DelegateNode<AdapterBalanceInput, AdapterBalanceOutput>("Load eth adapter balance", input => LoadEthAdapterBalance(input))
                .WithInput(context => new AdapterBalanceInput
                {
                    AssetAddress = context.OperationValues.Asset.AssetAddress,
                    AssetMultiplierPower = context.OperationValues.Asset.MultiplierPower,
                    AssetAccuracy = context.OperationValues.Asset.Accuracy,
                    HotWallet = context.OperationValues.GlobalSettings.EthereumHotWallet                    
                })
                .MergeOutput(output => new { EthAdapterBalance = output });

            DelegateNode<EthCashoutEstimationInput, EthCashoutEstimation>("Estimate eth cashout", input => EstimateEthCashout(input))
                .WithInput(context => new EthCashoutEstimationInput
                {
                    OperationId = context.Id.ToString(),
                    AssetAddress = context.OperationValues.Asset.AssetAddress,
                    AssetMultiplierPower = context.OperationValues.Asset.MultiplierPower,
                    AssetAccuracy = context.OperationValues.Asset.Accuracy,
                    FromAddress = context.OperationValues.GlobalSettings.EthereumHotWallet,
                    ToAddress = context.OperationValues.DestinationAddress,
                    Volume = context.OperationValues.Volume
                })
                .MergeOutput(output => new
                {
                    EthCashoutEstimation = output
                });

            ValidationNode<EthInput>("ETH validation")
                .WithInput(context => new EthInput
                {                    
                    Volume = context.OperationValues.Volume,                    
                    AdapterBalance = context.OperationValues.EthAdapterBalance.Balance,
                    CashoutIsAllowed = context.OperationValues.EthCashoutEstimation.IsAllowed
                })
                .MergeFailOutput(output => output);

            DelegateNode<BlockchainAddressInput, string>("Merge blockchain address", input => blockchainAddress.MergeAsync(input.DestinationAddress, input.DestinationAddressExtension, input.BlockchainIntegrationLayerId).ConfigureAwait(false).GetAwaiter().GetResult())
                .WithInput(context => new BlockchainAddressInput
                {
                    DestinationAddress = context.OperationValues.DestinationAddress,
                    DestinationAddressExtension = context.OperationValues.DestinationAddressExtension,
                    BlockchainIntegrationLayerId = context.OperationValues.Asset.BlockchainIntegrationLayerId
                })
                .MergeFailOutput(output => output)
                .MergeOutput(output => !string.IsNullOrWhiteSpace(output)
                    ? new
                    {
                        DestinationAddress = output
                    }
                    : (object)new {});

            DelegateNode<BilInput, BilOutput>("BIL check", input => BilCheck(input))
                .WithInput(context => new BilInput
                {
                    AssetId = context.OperationValues.Asset.Id,
                    Amount = context.OperationValues.Volume,
                    DestinationAddress = context.OperationValues.DestinationAddress
                })
                .MergeOutput(output => new { BilCheckResult = output })
                .MergeFailOutput(output => output);

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
                .MergeFailOutput(output => output);

            DelegateNode<CashoutMeInput, object>("Send to ME", input => SendToMe(input))
                .WithInput(context => new CashoutMeInput
                {
                    OperationId = context.Id.ToString(),
                    ClientId = context.OperationValues.Client.Id,
                    DestinationAddress = context.OperationValues.DestinationAddress,
                    Volume = context.OperationValues.Volume,
                    AssetId = context.OperationValues.Asset.Id,                    
                    CashoutTargetClientId = context.OperationValues.GlobalSettings.FeeSettings.TargetClients["Cashout"],
                    FeeSize = context.OperationValues.Fee.Size,
                    FeeType = context.OperationValues.Fee.Type
                })
                .MergeOutput(output => new { Me = output })
                .MergeFailOutput(output => output);

            DelegateNode("Fail operation", context => context.Fail());
        }

        private AdapterBalanceOutput LoadEthAdapterBalance(AdapterBalanceInput input)
        {
            var balance = _ethereumFacade.GetBalanceOnAdapterAsync(
                    input.AssetAddress,
                    input.AssetMultiplierPower,
                    input.AssetAccuracy,
                    input.HotWallet).ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            return new AdapterBalanceOutput
            {
                Balance = balance
            };
        }

        private EthCashoutEstimation EstimateEthCashout(EthCashoutEstimationInput input)
        {
            return _ethereumFacade.EstimateCashOutAsync(
                    input.OperationId,
                    input.AssetAddress,
                    input.AssetMultiplierPower,
                    input.AssetAccuracy,
                    input.FromAddress,
                    input.ToAddress,
                    input.Volume).ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }

        private BilOutput BilCheck(BilInput input)
        {
            var result = _blockchainCashoutPreconditionsCheckClient.ValidateCashoutAsync(new CashoutValidateModel()
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

        private object SendToMe(CashoutMeInput input)
        {
            var response = _exchangeOperationsServiceClient.CashOutAsync(
                    input.ClientId,
                    input.DestinationAddress,
                    (double) input.Volume,
                    input.AssetId,
                    txId: input.OperationId,
                    feeClientId: input.CashoutTargetClientId,
                    feeSize: input.FeeSize,
                    feeSizeType: input.FeeType == FeeType.Absolute ? FeeSizeType.ABSOLUTE : FeeSizeType.PERCENTAGE)
                .ConfigureAwait(false).GetAwaiter().GetResult();

            if (response == null)
                throw new ApplicationException("Me is not available");

            if (response.Code != 0)
            {
                _log.Warning("ME operation failed", context: response);

                throw new InvalidOperationException("Send cashout to ME has failed");
            }
            
            return response;
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

        private AjustmentOutput AjustBtcVolume(AjustmentInput input)
        {
            var volume = input.Volume;

            if (input.AssetId == "BTC" && input.Balance - input.Volume < input.CashoutMinimalAmount)
                volume = Convert.ToDecimal(input.Balance).TruncateDecimalPlaces(input.AssetAccuracy);

            return new AjustmentOutput
            {
                Volume = volume
            };
        }
    }
}
