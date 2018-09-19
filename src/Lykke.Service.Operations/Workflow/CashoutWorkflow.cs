using Common;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.BlockchainCashoutPreconditionsCheck.Client;
using Lykke.Service.BlockchainCashoutPreconditionsCheck.Client.Models;
using Lykke.Service.ExchangeOperations.Client;
using Lykke.Service.ExchangeOperations.Contracts.Fee;
using Lykke.Service.FeeCalculator.AutorestClient.Models;
using Lykke.Service.FeeCalculator.Client;
using Lykke.Service.Limitations.Client;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Services.Blockchain;
using Lykke.Service.Operations.Workflow.Data;
using Lykke.Service.Operations.Workflow.Extensions;
using Lykke.Workflow;
using Lykke.Workflow.Fluent;
using NBitcoin;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
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
                    .Do("Kyc validation").OnFail("Fail operation")
                    .Do("Disclaimers validation").OnFail("Fail operation")
                    .Do("Balance validation").OnFail("Fail operation")
                    .Do("BTC ajust volume on low remainder").OnFail("Fail operation")
                    .On("Is not blockchain integration").DeterminedAs(context => string.IsNullOrWhiteSpace((string)context.OperationValues.Asset.BlockchainIntegrationLayerId))
                        .ContinueWith("Limits validation")
                    .On("Is blockchain integration").DeterminedAs(context => !string.IsNullOrWhiteSpace((string)context.OperationValues.Asset.BlockchainIntegrationLayerId))
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
                            .ContinueWith("Create sign challenge")
                    .WithBranch()
                        .Do("Create sign challenge").OnFail("Fail operation")
                        .Do("Request confirmation").OnFail("Fail operation")
                        .Do("Validate confirmation").OnFail("Fail operation").ContinueWith("Send to ME")
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
                    : (object)new {})
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

            DelegateNode("Create sign challenge", input => new { SignChallenge = Guid.NewGuid() })
                .MergeFailOutput(e => new { ErrorMessage = e.Message });

            DelegateNode<CalculateCashoutFeeInput, object>("Calculate fee", input => CalculateFee(input))
                .WithInput(context => new CalculateCashoutFeeInput
                {
                    AssetId = context.OperationValues.Asset.Id
                })
                .MergeOutput(output => new { Fee = output })
                .MergeFailOutput(e => new { ErrorMessage = e.Message });

            Node("Request confirmation", i => i.RequestConfirmation())
                .WithInput(context => new { })
                .MergeOutput(output => output)
                .MergeFailOutput(e => new { ErrorMessage = e.Message });

            DelegateNode<ValidateConfirmationInput>("Validate confirmation", input => ValidateConfirmation(input))
                .WithInput(context => new ValidateConfirmationInput
                {
                    ClientId = context.OperationValues.Client.Id,
                    PubKeyAddress = context.OperationValues.Client.BitcoinAddress,
                    Challenge = context.OperationValues.SignChallenge,
                    Confirmation = context.OperationValues.Confirmation
                })
                .MergeFailOutput(e => new { ErrorMessage = e.Message });

            DelegateNode<CashoutMeInput>("Send to ME", i => SendToMe(i))
                .WithInput(context => new CashoutMeInput
                {
                    OperationId = context.Id,                    
                    ClientId = context.OperationValues.Client.Id,
                    DestinationAddress = context.OperationValues.DestinationAddress,
                    Volume = context.OperationValues.Volume,
                    AssetId = context.OperationValues.Asset.Id,
                    AssetAccuracy = context.OperationValues.Asset.Accuracy,
                    CashoutTargetClientId = context.OperationValues.GlobalSettings.FeeSettings.TargetClients["Cashout"],
                    FeeSize = context.OperationValues.Fee.Size,
                    FeeType = context.OperationValues.Fee.Type
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
                .MergeFailOutput(e => new { ErrorMessage = e.Message });

            DelegateNode("Fail operation", context => context.Fail());
            DelegateNode("Accept operation", context => context.Accept());
            DelegateNode("Confirm operation", context => context.Confirm());
            DelegateNode("Complete operation", context => context.Complete());
        }

        private void SendToMe(CashoutMeInput input)
        {
            var res = _exchangeOperationsServiceClient.CashOutAsync(
                input.ClientId,
                input.DestinationAddress,
                (double)input.Volume,
                input.AssetId,
                txId: input.OperationId.ToString(),
                feeClientId: input.CashoutTargetClientId,
                feeSize: input.FeeSize,
                feeSizeType: input.FeeType == FeeType.Absolute ? FeeSizeType.ABSOLUTE : FeeSizeType.PERCENTAGE).GetAwaiter().GetResult();
            
            if (!res.IsOk())
            {
                var message = $"{res.Code}: {res.Message}";

                _log.Warning(message, context: new { input.OperationId, ErrorMessage = message });

                throw new InvalidOperationException(message);
            }
        }

        private void ValidateConfirmation(ValidateConfirmationInput input)
        {
            var address = new BitcoinPubKeyAddress(input.PubKeyAddress);
            var verifyResult = false;
            try
            {
                verifyResult = address.VerifyMessage(input.Challenge, input.Confirmation);
            }
            catch { }

            if (!verifyResult)
                throw new InvalidOperationException("Signature is invalid");            
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
