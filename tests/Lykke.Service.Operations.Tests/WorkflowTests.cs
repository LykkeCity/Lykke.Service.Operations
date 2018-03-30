using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Autofac;
using Common.Log;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Contracts.Events;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Core.Settings;
using Lykke.Service.Operations.Core.Settings.Assets;
using Lykke.Service.Operations.Core.Settings.ServiceSettings;
using Lykke.Service.Operations.Core.Settings.SlackNotifications;
using Lykke.Service.Operations.Modules;
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
        [Theory]
        [InlineData(OperationType.LimitOrder, "LimitOrderWorkflow")]
        [InlineData(OperationType.MarketOrder, "MarketOrderWorkflow")]
        public async Task RunWorkflow(OperationType type, string workflowType)
        {
            var builder = new ContainerBuilder();

            var reloadingManager = new Mock<IReloadingManager<AppSettings>>();

            reloadingManager.Setup(m => m.CurrentValue).Returns(new AppSettings
            {
                Transports = new TransportSettings
                {
                    ClientRabbitMqConnectionString = "amqp://lykke.user:123qwe123qwe123@rabbit-registration.lykke-service.svc.cluster.local:5672",
                    MeRabbitMqConnectionString = "amqp://lykke.history:lykke.history@rabbit-me.lykke-me.svc.cluster.local:5672"
                },
                OperationsService = new OperationsSettings
                {
                    Db = new DbSettings
                    {
                        OffchainConnString = "DefaultEndpointsProtocol=https;AccountName=lkedevmain;AccountKey=l0W0CaoNiRZQIqJ536sIScSV5fUuQmPYRQYohj/UjO7+ZVdpUiEsRLtQMxD+1szNuAeJ351ndkOsdWFzWBXmdw==",
                        HMarketOrdersConnString = "DefaultEndpointsProtocol=https;AccountName=lkedevmain;AccountKey=l0W0CaoNiRZQIqJ536sIScSV5fUuQmPYRQYohj/UjO7+ZVdpUiEsRLtQMxD+1szNuAeJ351ndkOsdWFzWBXmdw=="
                    },
                    Services = new ServicesSettings
                    {
                        ClientAccountUrl = "http://client-account.lykke-service.svc.cluster.local",
                        PushNotificationsUrl = "http://push-notifications.lykke-service.svc.cluster.local"
                    }
                },
                Assets = new AssetsSettings
                {
                    ServiceUrl = "http://assets.lykke-service.svc.cluster.local"
                },               
                RateCalculatorServiceClient = new RateCalculatorSettings
                {
                    ServiceUrl = "http://rate-calculator.lykke-service.svc.cluster.local"
                },
                BalancesServiceClient = new BalancesSettings
                {
                    ServiceUrl = "http://balances.lykke-service.svc.cluster.local"
                },
                FeeCalculatorServiceClient = new FeeCalculatorSettings
                {
                    ServiceUrl = "http://fee-calculator.lykke-service.svc.cluster.local"
                },
                MatchingEngineClient = new MatchingEngineSettings
                {
                    IpEndpoint = new IpEndpointSettings
                    {
                        Port = 8888,
                        Host = "me.lykke-me.svc.cluster.local",
                        InternalHost = "me.lykke-me.svc.cluster.local"                       
                    }
                },
                SlackNotifications = new SlackNotificationsSettings()
            });

            builder.RegisterModule(new ServiceModule(reloadingManager.Object, new LogToConsole()));
            builder.RegisterModule(new ClientsModule(reloadingManager.Object, new LogToConsole()));
            builder.Register(ctx => new InMemoryCqrsEngine(Register.BoundedContext("operations")
                .PublishingEvents(typeof(LimitOrderCreatedEvent), typeof(LimitOrderRejectedEvent)).With("events"))).As<ICqrsEngine>().SingleInstance();
            //builder.RegisterModule(new CqrsModule(reloadingManager.Object, new LogToConsole()));
            builder.RegisterModule(new WorkflowModule());            
            
            var container = builder.Build();

            var context = new CreateLimitOrderCommand
            {
                AssetPairId = "BTCUSD",
                AssetId = "BTC",
                Volume = 0.001,
                Price = 6000,
                Asset = new AssetShortModel
                {
                    Id = "BTC",
                    IsTradable = true,
                    IsTrusted = true,
                    Blockchain = "Bitcoin"
                },
                AssetPair = new AssetPairModel
                {
                    Id = "BTCUSD",
                    BaseAsset = new AssetModel
                    {
                        Id = "BTC",
                        Accuracy = 8,
                        KycNeeded = false,
                        IsTrusted = true,
                        LykkeEntityId = "LYKKEUK",
                        Blockchain = "Bitcoin"
                    },
                    QuotingAsset = new AssetModel
                    {
                        Id = "USD",
                        Accuracy = 2,
                        KycNeeded = true,
                        IsTrusted = true,
                        LykkeEntityId = "LYKKEUK",
                        Blockchain = "Bitcoin"
                    },
                    MinVolume = 0.001m,
                    MinInvertedVolume = 1.0m
                },
                Client = new ClientModel
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
                GlobalSettings = new GlobalSettingsModel
                {
                    BlockedAssetPairs = new[] {"BTCEUR"},
                    BitcoinBlockchainOperationsDisabled = false,
                    BtcOperationsDisabled = false,
                    IcoSettings = new IcoSettingsModel
                    {
                        RestrictedCountriesIso3 = new[] {"USA"},
                        LKK2YAssetId = "LKK2Y"
                    },
                    FeeSettings = new FeeSettingsModel
                    {
                        FeeEnabled = true,
                        TargetClientId = "e3fa1d1e-8e7a-44e0-a666-a442bc35515c"
                    }
                }
            };

            Trace.WriteLine(JsonConvert.SerializeObject(context, Formatting.Indented));

            var operation = new Operation();

            var inputValues = JsonConvert.SerializeObject(context);

            operation.Create(Guid.NewGuid(), new Guid("27fe9c28-a18b-4939-8ebf-a70061fbfa05"), type, inputValues);                        

            var wf = container.ResolveNamed<OperationWorkflow>(workflowType, new TypedParameter(typeof(Operation), operation));            

            var result = wf.Run(operation);

            Trace.WriteLine(JsonConvert.SerializeObject(operation, Formatting.Indented));
            Trace.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
            Assert.Equal(WorkflowState.Complete, result.State);
            Assert.Equal(OperationStatus.Accepted, operation.Status);
        }
    }
}

