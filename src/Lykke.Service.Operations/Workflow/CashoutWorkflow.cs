﻿using System;
using System.Linq;
using Common;
using Common.Log;
using Lykke.Service.BlockchainCashoutPreconditionsCheck.Client;
using Lykke.Service.BlockchainCashoutPreconditionsCheck.Client.Models;
using Lykke.Service.FeeCalculator.AutorestClient.Models;
using Lykke.Service.FeeCalculator.Client;
using Lykke.Service.Limitations.Client;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Repositories;
using Lykke.Service.Operations.Services.Blockchain;
using Lykke.Service.Operations.Workflow.Data;
using Lykke.Service.Operations.Workflow.Extensions;
using Lykke.Service.Operations.Workflow.Validation;
using Lykke.Workflow;
using Lykke.Workflow.Fluent;
using NBitcoin;
using Newtonsoft.Json.Linq;
using FeeType = Lykke.Service.FeeCalculator.AutorestClient.Models.FeeType;

namespace Lykke.Service.Operations.Workflow
{
    public class CashoutWorkflow : OperationWorkflow
    {
        private readonly ILog _log;
        private readonly IRecoveryTokensRepository _recoveryTokensRepository;
        private readonly IEthereumFacade _ethereumFacade;
        private readonly IFeeCalculatorClient _feeCalculatorClient;                
        private readonly IBlockchainCashoutPreconditionsCheckClient _blockchainCashoutPreconditionsCheckClient;

        public CashoutWorkflow(
            Operation operation, 
            ILog log, 
            IRecoveryTokensRepository recoveryTokensRepository,
            IActivityFactory activityFactory,
            IEthereumFacade ethereumFacade,
            BlockchainAddress blockchainAddress,
            IFeeCalculatorClient feeCalculatorClient,
            IBlockchainCashoutPreconditionsCheckClient blockchainCashoutPreconditionsCheckClient) : base(operation, log, activityFactory)
        {
            _log = log;
            _recoveryTokensRepository = recoveryTokensRepository;
            _ethereumFacade = ethereumFacade;
            _feeCalculatorClient = feeCalculatorClient;
            _blockchainCashoutPreconditionsCheckClient = blockchainCashoutPreconditionsCheckClient;
            
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
                        .Do("Create sign challenge").OnFail("Fail operation")
                        .Do("Accept operation").OnFail("Fail operation")
                        .Do("Request confirmation").OnFail("Fail operation")
                        .Do("Validate confirmation").OnFail("Fail operation")
                        .Do("Send to ME").OnFail("Fail operation")
                        .ContinueWith("Confirm operation")
                    .WithBranch()
                        .Do("Fail operation")
                        .ContinueWith("Send operation status")
                    .WithBranch()
                        .Do("Confirm operation")
                        .Do("Settle on blockchain")
                        .Do("Complete operation")
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

            DelegateNode("Create sign challenge", input => new { SignChallenge = Guid.NewGuid() })
                .MergeOutput(output => output)
                .MergeFailOutput(output => output);

            DelegateNode<CalculateCashoutFeeInput, object>("Calculate fee", input => CalculateFee(input))
                .WithInput(context => new CalculateCashoutFeeInput
                {
                    AssetId = context.OperationValues.Asset.Id
                })
                .MergeOutput(output => new { Fee = output })
                .MergeFailOutput(output => output);

            Node("Request confirmation", i => i.RequestConfirmation())
                .WithInput(context => new
                {

                })
                .MergeOutput(output => output)
                .MergeFailOutput(output => output);

            DelegateNode<ValidateConfirmationInput>("Validate confirmation", input => ValidateConfirmation(input))
                .WithInput(context => new ValidateConfirmationInput
                {
                    ClientId = context.OperationValues.Client.Id,
                    PubKeyAddress = context.OperationValues.Client.BitcoinAddress,
                    Challenge = context.OperationValues.SignChallenge,
                    SignedMessage = context.OperationValues.SignedMessage
                })
                .MergeFailOutput(e => new { ErrorMessage = e.Message });

            Node("Send to ME", i => i.SendToMe())
                .WithInput(context => new CashoutMeInput
                {
                    OperationId = context.Id,
                    ClientId = context.OperationValues.Client.Id,                    
                    Volume = context.OperationValues.Volume,
                    AssetId = context.OperationValues.Asset.Id,
                    AssetAccuracy = context.OperationValues.Asset.Accuracy,
                    CashoutTargetClientId = context.OperationValues.GlobalSettings.FeeSettings.TargetClients["Cashout"],
                    FeeSize = context.OperationValues.Fee.Size,
                    FeeType = context.OperationValues.Fee.Type
                })
                .MergeOutput(output => new { Me = output })
                .MergeFailOutput(output => output);

            Node("Settle on blockchain", i => i.SettleOnBlockchain())
                .WithInput(context => new BlockchainCashoutInput
                {
                    OperationId = context.Id,
                    ClientId = context.ClientId,
                    AssetId = context.OperationValues.Asset.Id,
                    AssetBlockchain = context.OperationValues.Asset.Blockchain,
                    AssetBlockchainWithdrawal = context.OperationValues.Asset.BlockchainWithdrawal,
                    BlockchainIntegrationLayerId = context.OperationValues.Asset.BlockchainIntegrationLayerId,
                    Amount = context.OperationValues.Volume,
                    ToAddress = context.OperationValues.DestinationAddress,
                    EthHotWallet = context.OperationValues.GlobalSettings.EthereumHotWallet
                })
                .MergeOutput(output => new { Blockchain = output })
                .MergeFailOutput(output => output);

            DelegateNode("Fail operation", context => context.Fail());
            DelegateNode("Accept operation", context => context.Accept());
            DelegateNode("Confirm operation", context => context.Confirm());
            DelegateNode("Complete operation", context => context.Complete());
        }

        private void ValidateConfirmation(ValidateConfirmationInput input)
        {
            var isOldVerificationMethod = Guid.TryParse(input.SignedMessage, out var guid);

            if (isOldVerificationMethod)
            {
                var accessToken = _recoveryTokensRepository.GetAccessTokenLvl1Async(input.SignedMessage).ConfigureAwait(false).GetAwaiter().GetResult();

                if (accessToken != null &&
                    accessToken.ClientId != input.ClientId &&
                    DateTime.UtcNow - accessToken.IssueDateTime.ToUniversalTime() > TimeSpan.FromMinutes(5))
                    throw new InvalidOperationException("Token is invalid");

                _recoveryTokensRepository.RemoveAccessTokenLvl1Async(input.SignedMessage).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            else
            {
                var address = new BitcoinPubKeyAddress(input.PubKeyAddress);
                var verifyResult = false;
                try
                {
                    verifyResult = address.VerifyMessage(input.Challenge, input.SignedMessage);
                }
                catch { }

                if (!verifyResult)
                    throw new InvalidOperationException("Signature is invalid");
            }
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
