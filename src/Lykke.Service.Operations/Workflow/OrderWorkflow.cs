﻿using System.Linq;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Workflow.Data;
using Lykke.Service.Operations.Workflow.Extensions;
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
            ILogFactory logFactory,
            IActivityFactory activityFactory) : base(operation, logFactory, activityFactory)
        {
            Configure(cfg =>
                cfg
                    .Do("Client validation").OnFail("Fail operation")
                    .Do("Asset validation").OnFail("Fail operation")
                    .Do("Asset pair validation").OnFail("Fail operation")
                    .Do("Kyc validation").OnFail("Fail operation")
                    .Do("LKK2Y restrictions validation").OnFail("Fail operation")
                    .Do("Disclaimers validation").OnFail("Fail operation")
                    .On("Fee enabled").DeterminedAs(context => (bool)context.OperationValues.GlobalSettings.FeeSettings.FeeEnabled).ContinueWith("Calculate fee")
                    .On("Fee disabled").DeterminedAs(context => !(bool)context.OperationValues.GlobalSettings.FeeSettings.FeeEnabled).ContinueWith("Prepare to send to ME")
                    .WithBranch()
                        .Do("Calculate fee").OnFail("Fail operation")
                        .ContinueWith("Prepare to send to ME").OnFail("Fail operation")
                    .WithBranch()
                        .Do("Prepare to send to ME")
                        .SubConfigure(ConfigurePreMeNodes)
                        .Do("Send to ME").OnFail("Fail operation on Me fail")
                        .SubConfigure(ConfigurePostMeNodes)
                        .ContinueWith("Confirm operation")
                    .WithBranch()
                        .Do("Fail operation on Me fail")
                        .ContinueWith("Fail operation")
                    .WithBranch()
                        .Do("Fail operation")
                        .ContinueWith("end")
                    .WithBranch()
                        .Do("Confirm operation")
                    .End()
            );

            ValidationNode<ClientInput>("Client validation")
                .WithInput(context => new ClientInput
                {
                    OperationsBlocked = context.OperationValues.Client.TradesBlocked
                })
                .MergeFailOutput(output => output);

            ValidationNode<AssetInput>("Asset validation")
                .WithInput(context => new AssetInput
                {
                    Id = context.OperationValues.Asset.Id,
                    DisplayId = context.OperationValues.Asset.DisplayId,
                    IsTradable = context.OperationValues.Asset.IsTradable,
                    IsTrusted = context.OperationValues.Asset.IsTrusted,
                    OrderAction = context.OperationValues.OrderAction
                })
                .MergeFailOutput(output => output);

            ValidationNode<AssetPairInput>("Asset pair validation")
                .WithInput(context => new AssetPairInput
                {
                    Id = context.OperationValues.AssetPair.Id,
                    BaseAssetId = context.OperationValues.AssetPair.BaseAsset.Id,
                    BaseAssetDisplayId = context.OperationValues.AssetPair.BaseAsset.DisplayId,
                    QuotingAssetId = context.OperationValues.AssetPair.QuotingAsset.Id,
                    QuotingAssetDisplayId = context.OperationValues.AssetPair.QuotingAsset.DisplayId,
                    MinVolume = context.OperationValues.AssetPair.MinVolume,
                    MinInvertedVolume = context.OperationValues.AssetPair.MinInvertedVolume,
                    AssetId = context.OperationValues.Asset.Id,
                    Volume = context.OperationValues.Volume,
                    BitcoinBlockchainOperationsDisabled = context.OperationValues.GlobalSettings.BitcoinBlockchainOperationsDisabled,
                    BtcOperationsDisabled = context.OperationValues.GlobalSettings.BtcOperationsDisabled,
                    BlockedAssetPairs = ((JArray)context.OperationValues.GlobalSettings.BlockedAssetPairs).Select(t => t.ToString()).ToArray()
                })
                .MergeFailOutput(output => output);

            ValidationNode<KycCheckInput>("Kyc validation")
                .WithInput(context => new KycCheckInput
                {
                    KycStatus = context.OperationValues.Client.KycStatus,
                    ClientId = context.OperationValues.Client.Id
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
                .WithInput(context =>
                {
                    string baseEntityId = context.OperationValues.AssetPair.BaseAsset.LykkeEntityId;
                    string quotingEntityId = context.OperationValues.AssetPair.QuotingAsset.LykkeEntityId;

                    return new DisclaimerInput
                    {
                        Type = context.Type,
                        ClientId = context.OperationValues.Client.Id,
                        LykkeEntityId1 = baseEntityId ?? quotingEntityId,
                        LykkeEntityId2 = quotingEntityId ?? baseEntityId
                    };
                })
                .MergeFailOutput(output => output);

            DelegateNode("Fail operation on Me fail", context => OnMeFail(context));
            DelegateNode("Fail operation", context => context.Fail());
            DelegateNode("Confirm operation", context => context.Confirm());
        }

        protected virtual void OnMeFail(Operation context)
        {

        }

        protected virtual WorkflowConfiguration<Operation> ConfigurePostMeNodes(WorkflowConfiguration<Operation> configuration)
        {
            return configuration;
        }

        protected virtual WorkflowConfiguration<Operation> ConfigurePreMeNodes(WorkflowConfiguration<Operation> configuration)
        {
            return configuration;
        }
    }
}
