using System;
using System.Collections.Generic;
using System.Linq;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Service.Assets.Client;
using Lykke.Service.Balances.AutorestClient.Models;
using Lykke.Service.Balances.Client;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Workflow.Data;
using Lykke.Service.Operations.Workflow.Extensions;
using Lykke.Service.RateCalculator.Client;
using Lykke.Service.RateCalculator.Client.AutorestClient.Models;
using Lykke.Workflow;
using Lykke.Workflow.Fluent;
using Newtonsoft.Json.Linq;

namespace Lykke.Service.Operations.Workflow
{
    [UsedImplicitly]
    public class TradeWorkflow : OperationWorkflow
    {
        private readonly IRateCalculatorClient _rateCalculatorClient;
        private readonly IBalancesClient _balancesClient;
        private readonly IAssetsServiceWithCache _assetsService;

        public TradeWorkflow(Operation operation, ILog log, IActivityFactory activityFactory, IRateCalculatorClient rateCalculatorClient, IBalancesClient balancesClient, IAssetsServiceWithCache assetsService) : base(operation, log, activityFactory)
        {
            _rateCalculatorClient = rateCalculatorClient;
            _balancesClient = balancesClient;
            _assetsService = assetsService;
            Configure(cfg => 
                cfg
                    .Do("Client validation").OnFail("Fail operation")
                    .Do("Get asset").OnFail("Fail operation")
                    .Do("Get asset pair").OnFail("Fail operation")
                    .Do("Determine needed asset").OnFail("Fail operation")
                    .Do("Determine needed amount").OnFail("Fail operation")
                    .Do("Get wallet").OnFail("Fail operation")
                    .Do("Asset validation").OnFail("Fail operation")
                    .Do("Asset pair validation").OnFail("Fail operation")
                    .Do("AssetPair: base asset kyc validation").OnFail("Fail operation")
                    .Do("AssetPair: quoting asset kyc validation").OnFail("Fail operation")                    
                    .Do("USA users restrictions validation").OnFail("Fail operation")
                    .Do("LKK2Y restrictions validation").OnFail("Fail operation")
                    .Do("Disclaimers validation").OnFail("Fail operation")
                    .ContinueWith("Accept operation")
                    .WithBranch()
                        .Do("Fail operation")
                        .ContinueWith("end")
                    .WithBranch()
                        .Do("Accept operation")
                    .End()
            );

            ValidationNode<ClientInput>("Client validation")
                .WithInput(context => new ClientInput
                {
                    TradesBlocked = context.OperationValues.Client.TradesBlocked,
                    BackupDone = context.OperationValues.Client.BackupDone
                })
                .MergeFailOutput(output => output);

            DelegateNode<string, object>("Get asset", assetId => GetAsset(assetId))
                .WithInput(context => (string)context.OperationValues.AssetId)
                .MergeOutput(output => new { Asset = output })
                .MergeFailOutput(output => output);

            DelegateNode<string, object>("Get asset pair", assetPairId => GetAssetPair(assetPairId))
                .WithInput(context => (string)context.OperationValues.AssetPairId)
                .MergeOutput(output => new { AssetPair = output })
                .MergeFailOutput(output => output);

            DelegateNode("Determine needed asset", context => GetNeededAsset(context))
                .MergeOutput(output => output)
                .MergeFailOutput(output => output);

            DelegateNode<NeededAmountInput, object>("Determine needed amount", input => GetNeededAmount(input))
                .WithInput(context => new NeededAmountInput
                {
                    OrderAction = context.OperationValues.OrderAction,
                    Volume = context.OperationValues.Volume,
                    NeededAssetId = context.OperationValues.NeededAssetId,
                    ReceivedAssetId = context.OperationValues.ReceivedAssetId
                })
                .MergeOutput(output => new { NeededAmount = output })
                .MergeFailOutput(output => output);

            DelegateNode("Get wallet", context => GetWalletBalance(context))
                .MergeOutput(output => new
                {
                    Wallet = output
                })
                .MergeFailOutput(output => output);

            ValidationNode<AssetInput>("Asset validation")
                .WithInput(context => new AssetInput
                {
                    Id = context.OperationValues.Asset.Id,
                    IsTradable = context.OperationValues.Asset.IsTradable,
                    IsTrusted = context.OperationValues.Asset.IsTrusted,
                    NeededAssetId = context.OperationValues.NeededAssetId,                    
                    NeededAmount = context.OperationValues.NeededAmount.Amount,
                    NeededConversionResult = context.OperationValues.NeededAmount.ConversionResult[0].Result,
                    WalletBalance = context.OperationValues.Wallet.Balance,
                    OrderAction = context.OperationValues.OrderAction
                })
                .MergeFailOutput(output => output);

            ValidationNode<AssetPairInput>("Asset pair validation")
                .WithInput(context => new AssetPairInput
                {
                    Id = context.OperationValues.AssetPair.Id,
                    BaseAssetId = context.OperationValues.AssetPair.BaseAsset.Id,
                    QuotingAssetId = context.OperationValues.AssetPair.QuotingAsset.Id,
                    MinVolume = context.OperationValues.AssetPair.MinVolume,
                    MinInvertedVolume = context.OperationValues.AssetPair.MinInvertedVolume,
                    AssetId = context.OperationValues.Asset.Id,
                    Volume = context.OperationValues.Volume,
                    BitcoinBlockchainOperationsDisabled = context.OperationValues.GlobalSettings.BitcoinBlockchainOperationsDisabled,
                    BtcOperationsDisabled = context.OperationValues.GlobalSettings.BtcOperationsDisabled,
                    BlockedAssetPairs = ((JArray)context.OperationValues.GlobalSettings.BlockedAssetPairs).Select(t => t.ToString()).ToArray()
                })
                .MergeFailOutput(output => output);

            ValidationNode<AssetKycInput>("AssetPair: base asset kyc validation")
                .WithInput(context => new AssetKycInput
                {
                    KycStatus = context.OperationValues.Client.KycStatus,
                    AssetId = context.OperationValues.AssetPair.BaseAsset.Id,
                    AssetKycNeeded = context.OperationValues.AssetPair.BaseAsset.KycNeeded
                })
                .MergeFailOutput(output => output);

            ValidationNode<AssetKycInput>("AssetPair: quoting asset kyc validation")
                .WithInput(context => new AssetKycInput
                {
                    KycStatus = context.OperationValues.Client.KycStatus,
                    AssetId = context.OperationValues.AssetPair.QuotingAsset.Id,
                    AssetKycNeeded = context.OperationValues.AssetPair.QuotingAsset.KycNeeded
                })
                .MergeFailOutput(output => output);            

            ValidationNode<UsaUsersRestrictionsInput>("USA users restrictions validation")
                .WithInput(context => new UsaUsersRestrictionsInput
                {
                    Country = context.OperationValues.Client.PersonalData.Country,
                    CountryFromID = context.OperationValues.Client.PersonalData.CountryFromID,
                    CountryFromPOA = context.OperationValues.Client.PersonalData.CountryFromPOA,
                    AssetId = context.OperationValues.Asset.Id,
                    Volume = context.OperationValues.Volume,
                    BaseAssetId = context.OperationValues.AssetPair.BaseAsset.Id,
                    QuotingAssetId = context.OperationValues.AssetPair.QuotingAsset.Id,
                    KycStatus = context.OperationValues.Client.KycStatus
                })
                .MergeFailOutput(output => output);

            ValidationNode<Lkk2yRestrictionsInput>("LKK2Y restrictions validation")
                .WithInput(context => new Lkk2yRestrictionsInput
                {
                    CountryFromPOA = context.OperationValues.Client.PersonalData.CountryFromPOA,                    
                    BaseAssetId = context.OperationValues.AssetPair.BaseAsset.Id,
                    QuotingAssetId = context.OperationValues.AssetPair.QuotingAsset.Id,                    
                    IcoSettings = new IcoSettings
                    {
                        LKK2YAssetId = context.OperationValues.GlobalSettings.IcoSettings.LKK2YAssetId,
                        RestrictedCountriesIso3 = ((JArray)context.OperationValues.GlobalSettings.IcoSettings.RestrictedCountriesIso3).Select(t => t.ToString()).ToArray()
                    }
                })
                .MergeFailOutput(output => output);

            ValidationNode<DisclaimerInput>("Disclaimers validation")
                .WithInput(context => new DisclaimerInput
                {

                })
                .MergeFailOutput(output => output);


            DelegateNode("Fail operation", context => context.Fail());
            DelegateNode("Accept operation", context => context.Accept());
        }

        private object GetAssetPair(string assetPairId)
        {
            var assetPair = _assetsService.TryGetAssetPairAsync(assetPairId).ConfigureAwait(false).GetAwaiter().GetResult();

            if (assetPair == null)
                throw new InvalidOperationException($"Asset pair '{assetPairId}' not found");

            var baseAsset = _assetsService.TryGetAssetAsync(assetPair.BaseAssetId).ConfigureAwait(false).GetAwaiter().GetResult();
            var quotingAsset = _assetsService.TryGetAssetAsync(assetPair.QuotingAssetId).ConfigureAwait(false).GetAwaiter().GetResult();

            return new
            {
                Id = assetPairId,
                BaseAsset = new
                {
                    baseAsset.Id,
                    baseAsset.Accuracy,
                    baseAsset.KycNeeded,
                    baseAsset.IsTrusted,
                    baseAsset.LykkeEntityId,
                    Blockchain = baseAsset.Blockchain.ToString()
                },
                QuotingAsset = new
                {
                    quotingAsset.Id,
                    quotingAsset.Accuracy,
                    quotingAsset.KycNeeded,
                    quotingAsset.IsTrusted,
                    quotingAsset.LykkeEntityId,
                    Blockchain = quotingAsset.Blockchain.ToString()
                },
                assetPair.MinVolume,
                assetPair.MinInvertedVolume
            };
        }

        private object GetAsset(string assetId)
        {            
            var asset = _assetsService.TryGetAssetAsync(assetId).ConfigureAwait(false).GetAwaiter().GetResult();

            if (asset == null)
                throw new InvalidOperationException($"Asset '{assetId}' not found");

            return new
            {
                asset.Id,
                asset.IsTradable,
                asset.IsTrusted,
                Blockchain = asset.Blockchain.ToString()
            };
        }

        private object GetNeededAsset(Operation context)
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

        private object GetNeededAmount(NeededAmountInput input)
        {
            var orderAction = (OrderAction) input.OrderAction;
            var neededAmount = 0m;

            if (orderAction == OrderAction.Buy)
            {
                var result = _rateCalculatorClient.GetMarketAmountInBaseAsync(
                        new List<AssetWithAmount>
                        {
                            new AssetWithAmount
                            {
                                AssetId = input.ReceivedAssetId,
                                Amount = (double)input.Volume
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
            else
            {
                return new
                {
                    Amount = Math.Abs(input.Volume)
                };
            }            
        }

        private object GetWalletBalance(Operation context)
        {
            var clientId = (string) context.OperationValues.Client.Id;
            var neededAssetId = (string) context.OperationValues.NeededAssetId;

            return _balancesClient.GetClientBalanceByAssetId(new ClientBalanceByAssetIdModel(neededAssetId, clientId)).ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }

    public enum OrderAction
    {
        Buy, Sell
    }
}
