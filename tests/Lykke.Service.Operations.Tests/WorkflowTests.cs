using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Autofac;
using Common.Log;
using Lykke.Contracts.Operations;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.ClientAccount.Client.AutorestClient.Models;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Core.Settings.Assets;
using Lykke.Service.Operations.Core.Settings.ServiceSettings;
using Lykke.Service.Operations.Modules;
using Lykke.Service.Operations.Workflow;
using Lykke.Workflow;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Lykke.Service.Operations.Tests
{
    public class WorkflowTests
    {
        [Fact]
        public async Task RunWorkflow()
        {
            var builder = new ContainerBuilder();

            builder.RegisterModule(new ServiceModule(new LogToConsole()));
            builder.RegisterModule(new WorkflowModule());
            
            var container = builder.Build();
            
            var context = new
            {
                Volume = 2.1m,
                Asset = new
                {
                    Id = "LKK",
                    IsTradable = true,
                    IsTrusted = true,
                    Blockchain = Blockchain.Bitcoin
                },
                AssetPair = new
                {
                    Id = "LKKBTC",
                    BaseAsset = new { Id = "LKK", KycNeeded = false, LykkeEntityId = "Lykke UK", Blockchain = Blockchain.Bitcoin },
                    QuotingAsset = new { Id = "BTC", KycNeeded = true, LykkeEntityId = "Lykke UK", Blockchain = Blockchain.Bitcoin },
                    MinVolume = 2m,
                    MinInvertedVolume = 0.00001m
                },   
                NeededAsset = new
                {
                    Id = "BTC",
                    IsTrusted = true,
                    NeededAmount = 1000m
                },
                Wallet = new
                {
                    Balance = 10000m
                },
                Client = new
                {                   
                    Id = Guid.NewGuid(),
                    TradesBlocked = false,                    
                    BackupDone = true,                 
                    KycStatus = KycStatus.Ok,                    
                    PersonalData = new
                    {
                        Country = "RUS",
                        CountryFromID = "RUS",
                        CountryFromPOA = "RUS",
                    }
                },
                GlobalSettings = new
                {
                    BlockedAssetPairs = new[] { "BTCUSD" },
                    BitcoinBlockchainOperationsDisabled = false,
                    BtcOperationsDisabled = false
                },
                IcoSettings = new
                {
                    RestrictedCountriesIso3 = new[] { "USA" },
                    LKK2YAssetId = "LKK2Y"
                }
            };
            
            var operation = new Operation
            {
                Id = Guid.NewGuid(),
                ClientId = new Guid("27fe9c28-a18b-4939-8ebf-a70061fbfa05"),
                Context = JsonConvert.SerializeObject(context),
                OperationValues = JObject.FromObject(context),
                Created = DateTime.UtcNow,
                Status = OperationStatus.Created,
                Type = OperationType.Cashout
            };

            var wf = container.ResolveNamed<OperationWorkflow>("TradeWorkflow", new TypedParameter(typeof(Operation), operation));            

            var result = wf.Run(operation);

            Trace.WriteLine(JsonConvert.SerializeObject(operation, Formatting.Indented));
            Trace.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
            Assert.Equal(WorkflowState.Complete, result.State);
            Assert.Equal(OperationStatus.Accepted, operation.Status);
        }
    }
}

