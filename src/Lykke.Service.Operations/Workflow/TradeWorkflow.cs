using System.Linq;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Workflow.Extensions;
using Lykke.Workflow;
using Lykke.Workflow.Fluent;
using Newtonsoft.Json.Linq;

namespace Lykke.Service.Operations.Workflow
{
    [UsedImplicitly]
    public class TradeWorkflow : OperationWorkflow
    {
        public TradeWorkflow(Operation operation, ILog log, IActivityFactory activityFactory) : base(operation, log, activityFactory)
        {
            Configure(cfg => 
                cfg
                    .Do("Client validation").OnFail("Fail operation")
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
                    TradesBlocked = (bool)context.OperationValues.Client.TradesBlocked,
                    BackupDone = (bool)context.OperationValues.Client.BackupDone
                })
                .MergeFailOutput(output => output);

            ValidationNode<AssetInput>("Asset validation")
                .WithInput(context => new AssetInput
                {
                    Id = (string)context.OperationValues.Asset.Id,
                    IsTradable = (bool)context.OperationValues.Asset.IsTradable,
                    IsTrusted = (bool)context.OperationValues.Asset.IsTrusted,
                    NeededAssetId = (string)context.OperationValues.NeededAsset.Id,
                    NeededAssetIsTrusted = (bool)context.OperationValues.NeededAsset.IsTrusted,
                    NeededAmount = (decimal)context.OperationValues.NeededAsset.NeededAmount,
                    WalletBalance = (double)context.OperationValues.Wallet.Balance,
                    OrderAction = (decimal)context.OperationValues.Volume > 0 ? OrderAction.Buy : OrderAction.Sell
                })
                .MergeFailOutput(output => output);

            ValidationNode<AssetPairInput>("Asset pair validation")
                .WithInput(context => new AssetPairInput
                {
                    Id = (string)context.OperationValues.AssetPair.Id,
                    BaseAssetId = (string)context.OperationValues.AssetPair.BaseAsset.Id,
                    QuotingAssetId = (string)context.OperationValues.AssetPair.QuotingAsset.Id,
                    MinVolume = (decimal)context.OperationValues.AssetPair.MinVolume,
                    MinInvertedVolume = (decimal)context.OperationValues.AssetPair.MinInvertedVolume,
                    AssetId = (string)context.OperationValues.Asset.Id,
                    Volume = (decimal)context.OperationValues.Volume,
                    BitcoinBlockchainOperationsDisabled = (bool)context.OperationValues.GlobalSettings.BitcoinBlockchainOperationsDisabled,
                    BtcOperationsDisabled = (bool)context.OperationValues.GlobalSettings.BtcOperationsDisabled,
                    BlockedAssetPairs = ((JArray)context.OperationValues.GlobalSettings.BlockedAssetPairs).Select(t => t.ToString()).ToArray()
                })
                .MergeFailOutput(output => output);

            ValidationNode<AssetKycInput>("AssetPair: base asset kyc validation")
                .WithInput(context => new AssetKycInput
                {
                    KycStatus = (KycStatus)context.OperationValues.Client.KycStatus,
                    AssetId = (string)context.OperationValues.AssetPair.BaseAsset.Id,
                    AssetKycNeeded = (bool)context.OperationValues.AssetPair.BaseAsset.KycNeeded
                })
                .MergeFailOutput(output => output);

            ValidationNode<AssetKycInput>("AssetPair: quoting asset kyc validation")
                .WithInput(context => new AssetKycInput
                {
                    KycStatus = (KycStatus)context.OperationValues.Client.KycStatus,
                    AssetId = (string)context.OperationValues.AssetPair.QuotingAsset.Id,
                    AssetKycNeeded = (bool)context.OperationValues.AssetPair.QuotingAsset.KycNeeded
                })
                .MergeFailOutput(output => output);

            ValidationNode<UsaUsersRestrictionsInput>("USA users restrictions validation")
                .WithInput(context => new UsaUsersRestrictionsInput
                {
                    Country = (string)context.OperationValues.Client.PersonalData.Country,
                    CountryFromID = (string)context.OperationValues.Client.PersonalData.CountryFromID,
                    CountryFromPOA = (string)context.OperationValues.Client.PersonalData.CountryFromPOA,
                    AssetId = (string)context.OperationValues.Asset.Id,
                    Volume = (decimal)context.OperationValues.Volume,
                    BaseAssetId = (string)context.OperationValues.AssetPair.BaseAsset.Id,
                    QuotingAssetId = (string)context.OperationValues.AssetPair.QuotingAsset.Id,
                    KycStatus = (KycStatus)context.OperationValues.Client.KycStatus
                })
                .MergeFailOutput(output => output);

            ValidationNode<Lkk2yRestrictionsInput>("LKK2Y restrictions validation")
                .WithInput(context => new Lkk2yRestrictionsInput
                {
                    CountryFromPOA = (string)context.OperationValues.Client.PersonalData.CountryFromPOA,                    
                    BaseAssetId = (string)context.OperationValues.AssetPair.BaseAsset.Id,
                    QuotingAssetId = (string)context.OperationValues.AssetPair.QuotingAsset.Id,                    
                    IcoSettings = new IcoSettings
                    {
                        LKK2YAssetId = (string)context.OperationValues.IcoSettings.LKK2YAssetId,
                        RestrictedCountriesIso3 = ((JArray)context.OperationValues.IcoSettings.RestrictedCountriesIso3).Select(t => t.ToString()).ToArray()
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
    }

    public enum OrderAction
    {
        Buy, Sell
    }

    public class DisclaimerInput
    {
    }

    public class AssetPairInput
    {
        public string Id { get; set; }
        public string BaseAssetId { get; set; }
        public Blockchain BaseAssetBlockain { get; set; }
        public string QuotingAssetId { get; set; }
        public Blockchain QuotingAssetBlockchain { get; set; }
        public decimal MinVolume { get; set; }
        public decimal MinInvertedVolume { get; set; }
        public string AssetId { get; set; }
        public decimal Volume { get; set; }
        public bool BitcoinBlockchainOperationsDisabled { get; set; }
        public bool BtcOperationsDisabled { get; set; }
        public string[] BlockedAssetPairs { get; set; }        
    }

    public class Lkk2yRestrictionsInput
    {
        public string CountryFromPOA { get; set; }        
        public string BaseAssetId { get; set; }
        public string QuotingAssetId { get; set; }
        public IcoSettings IcoSettings { get; set; }        
    }

    public class UsaUsersRestrictionsInput
    {
        public string Country { get; set; }
        public string CountryFromID { get; set; }
        public string CountryFromPOA { get; set; }
        public string AssetId { get; set; }
        public decimal Volume { get; set; }
        public string BaseAssetId { get; set; }
        public string QuotingAssetId { get; set; }
        public KycStatus KycStatus { get; set; }        
    }

    public class AssetKycInput
    {
        public KycStatus KycStatus { get; set; }
        public string AssetId { get; set; }
        public bool AssetKycNeeded { get; set; }        
    }
}
