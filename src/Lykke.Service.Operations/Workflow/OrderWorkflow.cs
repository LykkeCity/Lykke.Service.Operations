using System.Linq;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Workflow.Data;
using Lykke.Service.Operations.Workflow.Extensions;
using Lykke.Service.RateCalculator.Client.AutorestClient.Models;
using Lykke.Workflow;
using Lykke.Workflow.Fluent;
using Newtonsoft.Json.Linq;

namespace Lykke.Service.Operations.Workflow
{
    [UsedImplicitly]
    public class OrderWorkflow : OperationWorkflow
    {
        public OrderWorkflow(
            Operation operation, 
            ILog log, 
            IActivityFactory activityFactory, 
            IWorkflowService workflowService) : base(operation, log, activityFactory)
        {            
            Configure(cfg => 
                cfg
                    .Do("Client validation").OnFail("Fail operation")                                        
                    .Do("Determine needed asset").OnFail("Fail operation")
                    .Do("Determine needed amount").OnFail("Fail operation")
                    .Do("Get wallet").OnFail("Fail operation")
                    .Do("Asset validation").OnFail("Fail operation")
                    .Do("Adjust needed amount").OnFail("Fail operation")
                    .Do("Asset pair validation").OnFail("Fail operation")
                    .Do("AssetPair: base asset kyc validation").OnFail("Fail operation")
                    .Do("AssetPair: quoting asset kyc validation").OnFail("Fail operation")                    
                    .Do("USA users restrictions validation").OnFail("Fail operation")
                    .Do("LKK2Y restrictions validation").OnFail("Fail operation")
                    .Do("Disclaimers validation").OnFail("Fail operation")                    
                    .On("Fee enabled").DeterminedAs(context => (bool)context.OperationValues.GlobalSettings.FeeSettings.FeeEnabled).ContinueWith("Calculate fee")
                    .On("Fee disabled").DeterminedAs(context => !(bool)context.OperationValues.GlobalSettings.FeeSettings.FeeEnabled).ContinueWith("Save order")
                    .WithBranch()
                        .Do("Calculate fee").OnFail("Fail operation")
                        .ContinueWith("Save order").OnFail("Fail operation")
                    .WithBranch()
                        .Do("Save order").OnFail("Fail operation")
                        .Do("Send to ME").OnFail("Fail operation")
                    .ContinueWith("Accept operation")
                    .WithBranch()
                        .Do("Fail operation")
                        .ContinueWith("end")
                    .WithBranch()
                        .Do("Accept operation")
                    .End()
            );

            DelegateNode("Save order", input => workflowService.SaveOrder(input))
                .MergeFailOutput(output => output);

            DelegateNode<CalculateFeeInput, object>("Calculate fee", input => workflowService.CalculateFee(input))
                .WithInput(context => new CalculateFeeInput
                {
                    ClientId = context.OperationValues.Client.Id,
                    OperationType = context.Type,
                    AssetPairId = context.OperationValues.AssetPairId,
                    BaseAssetId = context.OperationValues.AssetPair.BaseAsset.Id,
                    AssetId = context.OperationValues.AssetId,
                    OrderAction = context.OperationValues.OrderAction,
                    TargetClientId = context.OperationValues.GlobalSettings.FeeSettings.TargetClientId
                })
                .MergeOutput(output => new { Fee = output })
                .MergeFailOutput(output => output);

            DelegateNode<MeOrderInput, object>("Send to ME", input => workflowService.SendToMe(input))
                .WithInput(context => new MeOrderInput
                {
                    Id = context.Id.ToString(),
                    OperationType = context.Type,
                    AssetPairId = context.OperationValues.AssetPairId,
                    ClientId = context.OperationValues.Client.Id,                    
                    Straight = (string)context.OperationValues.AssetId == (string)context.OperationValues.AssetPair.BaseAsset.Id,
                    Volume = (double)context.OperationValues.Volume,
                    Price = (double?)context.OperationValues.Price,
                    OrderAction = (OrderAction)context.OperationValues.OrderAction,
                    Fee = context.OperationValues.Fee
                })
                .MergeOutput(output => new { Me = output })
                .MergeFailOutput(output => output);

            ValidationNode<ClientInput>("Client validation")
                .WithInput(context => new ClientInput
                {
                    TradesBlocked = context.OperationValues.Client.TradesBlocked,
                    BackupDone = context.OperationValues.Client.BackupDone
                })
                .MergeFailOutput(output => output);
            
            DelegateNode("Determine needed asset", context => workflowService.GetNeededAsset(context))
                .MergeOutput(output => output)
                .MergeFailOutput(output => output);

            DelegateNode<NeededAmountInput, object>("Determine needed amount", input => workflowService.GetNeededAmount(input))
                .WithInput(context => new NeededAmountInput
                {
                    OperationType = context.Type,
                    OrderAction = context.OperationValues.OrderAction,
                    Volume = context.OperationValues.Volume,
                    Price = context.OperationValues.Price,
                    AssetId = context.OperationValues.AssetId,
                    BaseAssetId = context.OperationValues.AssetPair.BaseAsset.Id,
                    NeededAssetId = context.OperationValues.NeededAssetId,
                    ReceivedAssetId = context.OperationValues.ReceivedAssetId
                })
                .MergeOutput(output => new { NeededAmount = output })
                .MergeFailOutput(output => output);

            DelegateNode("Get wallet", context => workflowService.GetWalletBalance(context))
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
                    NeededConversionResult = ((JArray)context.OperationValues.NeededAmount?.ConversionResult)?.Select(v => v.ToObject<ConversionResult>()).Select(r => r.Result).ToArray(),
                    WalletBalance = context.OperationValues.Wallet.Balance,
                    OrderAction = context.OperationValues.OrderAction
                })
                .MergeFailOutput(output => output);

            DelegateNode("Adjust needed amount", context => workflowService.AdjustNeededAmount(context))
                .MergeOutput(output => output)
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
    }

    public enum OrderAction
    {
        Buy, Sell
    }
}
