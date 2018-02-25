using System;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Workflow.Extensions;
using Lykke.Workflow;
using Lykke.Workflow.Fluent;

namespace Lykke.Service.Operations.Workflow
{
    [UsedImplicitly]
    public class CashoutWorkflow : OperationWorkflow
    {
        public CashoutWorkflow(
            Operation operation,
            ILog log,            
            IActivityFactory activityFactory) : base(operation, log, activityFactory)
        {
            Configure(cfg => 
                cfg 
                    .Do("Global check").OnFail("Fail operation")
                    .Do("Asset check").OnFail("Fail operation")
                    .Do("Client check").OnFail("Fail operation")
                    .Do("Balance check").OnFail("Fail operation")
                    .Do("Limits check").OnFail("Fail operation")
                    .Do("Payment check").OnFail("Fail operation")
                    .Do("Send to Matching Engine").OnFail("Fail operation")                    
                    .Do("Successful operation finish")
                    .Do("Logging successful operation finish")
                    .ContinueWith("Clear")                    
                    .WithBranch()
                        .Do("Fail operation").ContinueWith("Clear")
                    .WithBranch()
                        .Do("Cancel operation").ContinueWith("Clear")
                    .WithBranch()
                        .Do("Clear")
                    .End()
            );

            ValidationNode<GlobalInput>("Global check")
                .WithInput(context => new GlobalInput
                {
                    CashOutBlocked = (bool)context.OperationValues.GlobalContext.CashOutBlocked
                })
                .MergeFailOutput(output => output);

            ValidationNode<AssetInput>("Asset check")
                .WithInput(context => new AssetInput
                {
                    IsTradable = (bool)context.OperationValues.Asset.IsTradable,
                    IsTrusted = (bool)context.OperationValues.Asset.IsTrusted                    
                })
                .MergeFailOutput(output => output);            

            ValidationNode<ClientInput>("Client check")
                .WithInput(context => new ClientInput
                {

                })
                .MergeFailOutput(output => output);

            ValidationNode<BalanceCheckInput>("Balance check")
                .WithInput(context => new BalanceCheckInput
                {

                })                
                .MergeFailOutput(output => output);

            DelegateNode("Fail operation", context => context.Fail());

            DelegateNode<LimitsCheckInput, string>("Limits check", input => LimitsCheck(input.ClientId, input.AssetId, input.Volume))
                .WithInput(context => new LimitsCheckInput
                {
                    
                })
                .MergeFailOutput(failOutput => new { ErrorMessage = "Limits check fail", ErrorDetails = failOutput.Message });

            DelegateNode<PaymentCheckInput, string>("Payment check", input => PaymentCheck())
                .WithInput(context => new PaymentCheckInput
                {

                })
                .MergeFailOutput(failOutput => new { ErrorMessage = "Limits check fail", ErrorDetails = failOutput.Message });

            DelegateNode<MatchingEngineInput, string>("Send to Matching Engine", input => SendToMatchingEngine())
                .WithInput(context => new MatchingEngineInput
                {

                })
                .MergeFailOutput(failOutput => new { ErrorMessage = "Limits check fail", ErrorDetails = failOutput.Message });

            DelegateNode<MatchingEngineInput, string>("Cancel operation", input => SendToMatchingEngine())
                .WithInput(context => new MatchingEngineInput
                {

                })
                .MergeFailOutput(failOutput => failOutput);

        }

        private string SendToMatchingEngine()
        {
            return "";
        }

        private string LimitsCheck(Guid clientId, string assetId, double volume)
        {
            return "";
        }

        private string PaymentCheck()
        {
            return "";
        }
    }

    internal class MatchingEngineInput
    {
    }

    internal class PaymentCheckInput
    {
    }

    internal class LimitsCheckInput
    {
        public Guid ClientId { get; set; }
        public string AssetId { get; set; }
        public double Volume { get; set; }
    }

    internal class BalanceCheckInput
    {
    }

    public class ClientInput
    {
        public bool TradesBlocked { get; set; }
        public bool BackupDone { get; set; }        
    }

    public class AssetInput
    {
        public string Id { get; set; }
        public bool IsTradable { get; set; }
        public bool IsTrusted { get; set; }
        public string NeededAssetId { get; set; }
        public bool NeededAssetIsTrusted { get; set; }
        public decimal NeededAmount { get; set; }
        public double WalletBalance { get; set; }
        public OrderAction OrderAction { get; set; }
    }

    internal class GlobalInput
    {
        public bool CashOutBlocked { get; set; }
    }
}
