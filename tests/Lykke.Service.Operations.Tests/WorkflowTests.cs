using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using Autofac;
using Common.Log;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Contracts.Events;
using Lykke.Service.Operations.Contracts.Orders;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Modules;
using Lykke.Service.Operations.Settings;
using Lykke.Service.Operations.Workflow;
using Lykke.SettingsReader;
using Lykke.Workflow;
using Moq;
using Newtonsoft.Json;
using Xunit;
using OperationType = Lykke.Service.Operations.Contracts.OperationType;

namespace Lykke.Service.Operations.Tests
{
    public class WorkflowTests
    {
        private readonly IContainer _container;

        public WorkflowTests()
        {
            var builder = new ContainerBuilder();
            var reloadingManager = new Mock<IReloadingManager<AppSettings>>();

            AppSettings settings;
            using (var client = new HttpClient())
            { 
                var response = client.GetAsync("http://settings.lykke-settings.svc.cluster.local/755d5e98-fe7e-480d-8f14-23e55a90d485_Operations").GetAwaiter().GetResult();
                settings = JsonConvert.DeserializeObject<AppSettings>(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            }

            reloadingManager.Setup(m => m.CurrentValue).Returns(settings);

            builder.RegisterModule(new ServiceModule(reloadingManager.Object, new LogToConsole()));
            builder.RegisterModule(new ClientsModule(reloadingManager.Object, new LogToConsole()));
            builder.Register(ctx => new InMemoryCqrsEngine(Register.BoundedContext("operations")
                .PublishingEvents(typeof(LimitOrderCreatedEvent), typeof(LimitOrderRejectedEvent)).With("events"))).As<ICqrsEngine>().SingleInstance();
            //builder.RegisterModule(new CqrsModule(reloadingManager.Object, new LogToConsole()));
            builder.RegisterModule(new WorkflowModule());

            _container = builder.Build();
        }

        //[Fact]
        public void CashoutWorkflow()
        {
            var context = new
            {
                DestinationAddress = "0x3d893aa6c1baa63f5d29a1108bb63a9b76eab425",
                DestinationAddressExtension = (string)null,
                Volume = 0.003m,
                Asset = new Contracts.Cashout.AssetCashoutModel
                {
                    Id = "ETH",                    
                    DisplayId = "ETH",
                    MultiplierPower = 18,
                    AssetAddress = "0x1c4ca817d1c61f9c47ce2bec9d7106393ff981ce",
                    Accuracy = 6,
                    Blockchain = "Ethereum",
                    Type = "Erc20Token",
                    IsTradable = true,
                    IsTrusted = true,
                    KycNeeded = true,                    
                    BlockchainIntegrationLayerId = "",
                    CashoutMinimalAmount = 0.0001m,
                    LowVolumeAmount = 0.0001m
                },
                Client = new Contracts.Cashout.ClientCashoutModel
                {
                    Id = new Guid("27fe9c28-a18b-4939-8ebf-a70061fbfa05"),
                    Multisig = "2Mu5mgwReDeKhAd2queTNAhTBMCro1UEyVp",
                    Balance = 13m,
                    CashOutBlocked = false,
                    BackupDone = true,
                    KycStatus = "Ok"
                },
                GlobalSettings = new Contracts.Cashout.GlobalSettingsCashoutModel
                {
                    CashOutBlocked = false,
                    EthereumHotWallet = "0x406561F72e25af10fD28b41200FA3D52badC5A21",                    
                    FeeSettings = new Contracts.Cashout.FeeSettingsCashoutModel
                    {
                        TargetClients = new Dictionary<string, string>()
                        { 
                            { "Cashout", "27fe9c28-a18b-4939-8ebf-a70061fbfa05" }                            
                        }
                    }
                }
            };

            var inputValues = JsonConvert.SerializeObject(context);
            Trace.WriteLine(JsonConvert.SerializeObject(context, Formatting.Indented));

            var operation = new Operation();
            operation.Create(Guid.NewGuid(), new Guid("27fe9c28-a18b-4939-8ebf-a70061fbfa05"), OperationType.Cashout, inputValues);

            var wf = _container.ResolveNamed<OperationWorkflow>("CashoutWorkflow", new TypedParameter(typeof(Operation), operation));

            var result = wf.Run(operation);

            Trace.WriteLine(JsonConvert.SerializeObject(operation, Formatting.Indented));
            Trace.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
            Assert.Equal(WorkflowState.Complete, result.State);
            Assert.Equal(OperationStatus.Created, operation.Status);
        }

        [Theory]
        [InlineData(OperationType.LimitOrder, "LimitOrderWorkflow")]
        [InlineData(OperationType.MarketOrder, "MarketOrderWorkflow")]
        public void RunWorkflow(OperationType type, string workflowType)
        {
            var context = new
            {
                Asset = new
                {
                    Id = "BTC",
                    Accuracy = 8,
                    KycNeeded = false,
                    IsTrusted = true,
                    IsTradable = true,
                    LykkeEntityId = "LYKKEUK",
                    Blockchain = "Bitcoin"
                },
                AssetPair = new
                {
                    Id = "BTCUSD",
                    BaseAsset = new
                    {
                        Id = "BTC",
                        Accuracy = 8,
                        KycNeeded = false,
                        IsTrusted = true,
                        IsTradable = true,
                        LykkeEntityId = "LYKKEUK",
                        Blockchain = "Bitcoin"
                    },
                    QuotingAsset = new
                    {
                        Id = "USD",
                        Accuracy = 2,
                        KycNeeded = true,
                        IsTrusted = true,
                        IsTradable = true,
                        LykkeEntityId = "LYKKEUK",
                        Blockchain = "Bitcoin"
                    },
                    MinVolume = 0.001,
                    MinInvertedVolume = 1.0
                },
                Volume = 1000000000,
                Price = 6000,
                OrderAction = OrderAction.Buy,
                Client = new
                {
                    Id = new Guid("27fe9c28-a18b-4939-8ebf-a70061fbfa05"),
                    TradesBlocked = false,
                    BackupDone = true,
                    KycStatus = KycStatus.Ok.ToString(),
                    PersonalData = new PersonalDataModel
                    {
                        Country = "RUS",
                        CountryFromID = "RUS",
                        CountryFromPOA = "RUS",
                    }
                },
                GlobalSettings = new
                {
                    BlockedAssetPairs = new[] { "BTCEUR" },
                    BitcoinBlockchainOperationsDisabled = false,
                    BtcOperationsDisabled = false,
                    IcoSettings = new
                    {
                        RestrictedCountriesIso3 = new[] { "USA" },
                        LKK2YAssetId = "LKK2Y"
                    },
                    FeeSettings = new
                    {
                        FeeEnabled = true,
                        TargetClientId = "e3fa1d1e-8e7a-44e0-a666-a442bc35515c"
                    }
                }
            };

            var inputValues = JsonConvert.SerializeObject(context);
            Trace.WriteLine(JsonConvert.SerializeObject(context, Formatting.Indented));

            var operation = new Operation();
            operation.Create(Guid.NewGuid(), new Guid("27fe9c28-a18b-4939-8ebf-a70061fbfa05"), type, inputValues);

            var wf = _container.ResolveNamed<OperationWorkflow>(workflowType, new TypedParameter(typeof(Operation), operation));

            var result = wf.Run(operation);

            Trace.WriteLine(JsonConvert.SerializeObject(operation, Formatting.Indented));
            Trace.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
            Assert.Equal(WorkflowState.Complete, result.State);
            Assert.Equal(OperationStatus.Accepted, operation.Status);
        }
    }    
}

