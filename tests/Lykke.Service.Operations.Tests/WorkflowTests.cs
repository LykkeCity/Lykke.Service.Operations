using System;
using System.Diagnostics;
using Autofac;
using Common.Log;
using Lykke.Contracts.Operations;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.ClientAccount.Client.AutorestClient.Models;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Operations.Core.Domain;
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
        public void RunWorkflow()
        {
            var builder = new ContainerBuilder();

            builder.RegisterModule(new ServiceModule(new LogToConsole()));
            builder.RegisterModule(new WorkflowModule());
            var container = builder.Build();

            var context = new
            {
                AssetId = "BTC",
                Volume = 0.01m,
                DestinationAddress = "",
                Asset = new Asset
                {
                    IsTradable = true,
                    IsTrusted = false
                },
                Client = new
                {
                    KycStatus = KycStatus.Ok.ToString(),
                    CashOutBlockedSettings = (CashOutBlockSettings)null,
                    BackupSettings = (BackupSettings)null,
                    Balance = 1000
                },
                GlobalContext = new
                {
                    CashOutBlocked = false,
                    LowCashOutLimit = 0.01m,
                    LowCashOutTimeoutMins = 2
                },
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

            var wf = container.ResolveNamed<OperationWorkflow>("CashoutWorkflow", new TypedParameter(typeof(Operation), operation));            

            var result = wf.Run(operation);

            Trace.WriteLine(result.Error);
            Assert.Equal(WorkflowState.Complete, result.State);
        }
    }
}

