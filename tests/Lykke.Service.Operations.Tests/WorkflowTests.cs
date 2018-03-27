using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Autofac;
using Common.Log;
using Lykke.Contracts.Operations;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Operations.Contracts;
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
        [Fact]
        public async Task RunWorkflow()
        {
            var builder = new ContainerBuilder();

            var reloadingManager = new Mock<IReloadingManager<AppSettings>>();

            reloadingManager.Setup(m => m.CurrentValue).Returns(new AppSettings
            {
                OperationsService = new OperationsSettings
                {
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
                SlackNotifications = new SlackNotificationsSettings()
            });

            builder.RegisterModule(new ServiceModule(new LogToConsole()));
            builder.RegisterModule(new ClientsModule(reloadingManager.Object, new LogToConsole()));
            builder.RegisterModule(new WorkflowModule());            
            
            var container = builder.Build();
            
            var context = new CreateTradeCommand
            {
                AssetPairId = "BTCUSD",
                AssetId = "BTC",
                Volume = 10m,                                                              
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
                    BlockedAssetPairs = new[] { "BTCEUR" },
                    BitcoinBlockchainOperationsDisabled = false,
                    BtcOperationsDisabled = false,
                    IcoSettings = new IcoSettingsModel
                    {
                        RestrictedCountriesIso3 = new[] { "USA" },
                        LKK2YAssetId = "LKK2Y"
                    }
                }                
            };

            Trace.WriteLine(JsonConvert.SerializeObject(context, Formatting.Indented));

            var operation = new Operation();

            var inputValues = JsonConvert.SerializeObject(context);

            operation.Create(Guid.NewGuid(), new Guid("27fe9c28-a18b-4939-8ebf-a70061fbfa05"), OperationType.Trade, inputValues);                        

            var wf = container.ResolveNamed<OperationWorkflow>("TradeWorkflow", new TypedParameter(typeof(Operation), operation));            

            var result = wf.Run(operation);

            Trace.WriteLine(JsonConvert.SerializeObject(operation, Formatting.Indented));
            Trace.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
            Assert.Equal(WorkflowState.Complete, result.State);
            Assert.Equal(OperationStatus.Accepted, operation.Status);
        }
    }
}

