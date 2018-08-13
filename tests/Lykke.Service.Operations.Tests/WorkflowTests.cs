using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using Lykke.Cqrs;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Modules;
using Lykke.Service.Operations.Services;
using Lykke.Service.Operations.Settings;
using Lykke.Service.Operations.Workflow;
using Lykke.Service.Operations.Workflow.Events;
using Lykke.Service.Operations.Workflow.Sagas;
using Lykke.SettingsReader;
using Lykke.Workflow;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Assert = Xunit.Assert;
using OperationType = Lykke.Service.Operations.Contracts.OperationType;

namespace Lykke.Service.Operations.Tests
{
    [TestFixture]
    public class WorkflowTests : IDisposable
    {
        private IContainer _container;

        [NCrunch.Framework.Timeout(500000)]
        [SetUp]
        public void SetUp()
        {
            var builder = new ContainerBuilder();
            var reloadingManager = new Mock<IReloadingManager<AppSettings>>();

            AppSettings settings;
            using (var client = new HttpClient())
            { 
                var response = client.GetAsync("http://settings.lykke-settings.svc.cluster.local/755d5e98-fe7e-480d-8f14-23e55a90d485_Operations").GetAwaiter().GetResult();
                settings = JsonConvert.DeserializeObject<AppSettings>(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            }
            
            builder.RegisterType<MeHandler>().SingleInstance();
            builder.RegisterType<WorkflowCommandHandler>().SingleInstance();
            builder.RegisterType<MeSaga>().SingleInstance();                   

            reloadingManager.Setup(m => m.CurrentValue).Returns(settings);

            builder.RegisterModule(new ClientsModule(reloadingManager.Object, new LogToConsole()));
            builder.RegisterModule(new ServiceModule(reloadingManager.Object, new LogToConsole()));            
            builder.RegisterModule(new DbModule(reloadingManager.Object.Nested(x => x.OperationsService.Db), new LogToConsole()));            
            builder.RegisterModule(new CqrsModule(reloadingManager.Object, new LogToConsole()));
            builder.RegisterModule(new WorkflowModule());

            _container = builder.Build();
        }

        [NCrunch.Framework.Timeout(500000)]
        [Test]
        public async Task Test()
        {
            var cqrsEngine = _container.Resolve<ICqrsEngine>();

            await Task.Delay(60000);
        }

        [NCrunch.Framework.Timeout(500000)]
        [Test]
        public async Task CashoutWorkflow()
        {            
            var context = new
            {
                DestinationAddress = "mv1nGjtU1UYiCD2gg8Vhr3AgboGCnn4KmB",
                DestinationAddressExtension = (string)null,
                Volume = 0.005m,
                Asset = new Contracts.Cashout.AssetCashoutModel
                {
                    Id = "663a1d65-cb66-4e8c-b51a-5b7f0f4817ec",                    
                    DisplayId = "LTC",
                    MultiplierPower = 8,
                    AssetAddress = "0x1c4ca817d1c61f9c47ce2bec9d7106393ff981ce",
                    Accuracy = 8,
                    Blockchain = "Ethereum",
                    Type = "",
                    IsTradable = true,
                    IsTrusted = true,
                    KycNeeded = true,
                    BlockchainWithdrawal = false,
                    BlockchainIntegrationLayerId = "LiteCoin",
                    CashoutMinimalAmount = 0.005m,
                    LowVolumeAmount = 0.002m
                },
                Client = new Contracts.Cashout.ClientCashoutModel
                {
                    Id = new Guid("1a5f673a-c8a9-4cef-bcc8-3f5aa30a12bb"),
                    BitcoinAddress = "16DVQXimrww3tRmEJwFN6kwrBygK753zEj",
                    Multisig = "2N1bCpdgw75iA9KMuH7SpdETsn7iaLNrbfN",
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
            
            var operation = new Operation();
            operation.Create(Guid.NewGuid(), new Guid("1a5f673a-c8a9-4cef-bcc8-3f5aa30a12bb"), OperationType.Cashout, inputValues);

            var wf = _container.ResolveNamed<OperationWorkflow>("CashoutWorkflow", new TypedParameter(typeof(Operation), operation));
            var wfService = _container.Resolve<IWorkflowService>();
            var cqrsEngine = _container.Resolve<ICqrsEngine>();
            
            var result = wf.Run(operation);

            if (result.State == WorkflowState.Corrupted || result.State == WorkflowState.Failed)
            {
                Trace.WriteLine(result.ToJson());
                return;
            }

            //Trace.WriteLine(JsonConvert.SerializeObject(operation, Formatting.Indented));
            //Trace.WriteLine("--- WF result ---");
            //Trace.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
            
            await wfService.CompleteActivity(operation, operation.GetConfirmationActivity().ActivityId, JObject.FromObject(new { Confirmation = "111" }));

            var executingActivity = operation.Activities.Single(a => a.IsExecuting);

            cqrsEngine.PublishEvent(new ExternalExecutionActivityCreatedEvent
            {
                Id = executingActivity.ActivityId,
                Type = executingActivity.Type,
                Input = executingActivity.Input
            }, "operations");

            //Trace.WriteLine(JsonConvert.SerializeObject(operation, Formatting.Indented));

            await Task.Delay(100000);

            Assert.Equal(OperationStatus.Accepted, operation.Status);            
        }

        //[Theory]
        //[InlineData(OperationType.LimitOrder, "LimitOrderWorkflow")]
        //[InlineData(OperationType.MarketOrder, "MarketOrderWorkflow")]
        //public void RunWorkflow(OperationType type, string workflowType)
        //{
        //    var context = new
        //    {
        //        Asset = new
        //        {
        //            Id = "BTC",
        //            Accuracy = 8,
        //            KycNeeded = false,
        //            IsTrusted = true,
        //            IsTradable = true,
        //            LykkeEntityId = "LYKKEUK",
        //            Blockchain = "Bitcoin"
        //        },
        //        AssetPair = new
        //        {
        //            Id = "BTCUSD",
        //            BaseAsset = new
        //            {
        //                Id = "BTC",
        //                Accuracy = 8,
        //                KycNeeded = false,
        //                IsTrusted = true,
        //                IsTradable = true,
        //                LykkeEntityId = "LYKKEUK",
        //                Blockchain = "Bitcoin"
        //            },
        //            QuotingAsset = new
        //            {
        //                Id = "USD",
        //                Accuracy = 2,
        //                KycNeeded = true,
        //                IsTrusted = true,
        //                IsTradable = true,
        //                LykkeEntityId = "LYKKEUK",
        //                Blockchain = "Bitcoin"
        //            },
        //            MinVolume = 0.001,
        //            MinInvertedVolume = 1.0
        //        },
        //        Volume = 1000000000,
        //        Price = 6000,
        //        OrderAction = OrderAction.Buy,
        //        Client = new
        //        {
        //            Id = new Guid("27fe9c28-a18b-4939-8ebf-a70061fbfa05"),
        //            TradesBlocked = false,
        //            BackupDone = true,
        //            KycStatus = KycStatus.Ok.ToString(),
        //            PersonalData = new PersonalDataModel
        //            {
        //                Country = "RUS",
        //                CountryFromID = "RUS",
        //                CountryFromPOA = "RUS",
        //            }
        //        },
        //        GlobalSettings = new
        //        {
        //            BlockedAssetPairs = new[] { "BTCEUR" },
        //            BitcoinBlockchainOperationsDisabled = false,
        //            BtcOperationsDisabled = false,
        //            IcoSettings = new
        //            {
        //                RestrictedCountriesIso3 = new[] { "USA" },
        //                LKK2YAssetId = "LKK2Y"
        //            },
        //            FeeSettings = new
        //            {
        //                FeeEnabled = true,
        //                TargetClientId = "e3fa1d1e-8e7a-44e0-a666-a442bc35515c"
        //            }
        //        }
        //    };

        //    var inputValues = JsonConvert.SerializeObject(context);
        //    Trace.WriteLine(JsonConvert.SerializeObject(context, Formatting.Indented));

        //    var operation = new Operation();
        //    operation.Create(Guid.NewGuid(), new Guid("27fe9c28-a18b-4939-8ebf-a70061fbfa05"), type, inputValues);

        //    var wf = _container.ResolveNamed<OperationWorkflow>(workflowType, new TypedParameter(typeof(Operation), operation));

        //    var result = wf.Run(operation);

        //    Trace.WriteLine(JsonConvert.SerializeObject(operation, Formatting.Indented));
        //    Trace.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
        //    Assert.Equal(WorkflowState.Complete, result.State);
        //    Assert.Equal(OperationStatus.Accepted, operation.Status);
        //}

        [TearDown]
        public void Dispose()
        {
            _container?.Dispose();
        }
    }    
}

