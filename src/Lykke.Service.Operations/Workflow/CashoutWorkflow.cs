using System;
using System.Collections.Generic;
using System.Diagnostics;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Workflow.Activities;
using Lykke.Service.Operations.Workflows;
using Lykke.Workflow;
using Lykke.Workflow.Fluent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Lykke.Service.Operations.Workflow
{
    public static class WorkflowFrontActivities
    {        
        public static WorkflowConfiguration<TContext> FallbackToFront<TContext>(this WorkflowConfiguration<TContext> config)
        {
            return config.OnFail("FallBackToFront");
        }
    }

    internal static class WorkflowConfigurationExtensions
    {
        public static IActivitySlot<TContext, TInput, TOutput, TFailOutput> MergeFailOutput<TContext, TInput, TOutput,
            TFailOutput>(
            this IActivitySlot<TContext, TInput, TOutput, TFailOutput> slot,
            Func<TFailOutput, object> getContextMapFromFailOutput
        )
            where TContext : Operation
        {
            return slot.MergeFailOutput((context, output) => getContextMapFromFailOutput(output));
        }

        public static IActivitySlot<TContext, TInput, TOutput, TFailOutput> MergeFailOutput<TContext, TInput, TOutput, TFailOutput>(
            this IActivitySlot<TContext, TInput, TOutput, TFailOutput> slot,
            Func<TContext, TFailOutput, object> getContextMapFromOutput
        )
            where TContext : Operation
        {
            return slot.ProcessFailOutput((context, output) => JsonStringExtensions.Merge(((JObject)context.OperationValues), JObject.FromObject(getContextMapFromOutput(context, output))));
        }
    }

    public class OperationWorkflow : Workflow<Operation>
    {
        protected ILog Log { get; }

        public OperationWorkflow(Operation operation, ILog log, IActivityFactory activityFactory) 
            : base(operation, activityFactory, operation)
        {
            Log = log;
        }

        public override Execution<Operation> Run(Operation operation)
        {
            Log.WriteInfo(nameof(CashoutWorkflow), null, $"Operation [{operation.Id}] Run - running operation of type '{operation.Type}'");

            var state = operation.WorkflowState;
            var result = base.Run(operation);
            operation.ApplyValuesChanges();

            Log.WriteInfo(nameof(CashoutWorkflow), null, $"Operation [{operation.Id}] of type '{operation.Type}' Run - state changed from {state} to {operation.WorkflowState}");
            
            return result;
        }

        protected ISlotCreationHelper<Operation, ValidationActivity<TInput>> ValidationNode<TInput>(string name)
            where TInput : class
        {
            return Node<ValidationActivity<TInput>>(name, null, string.Format("{0} validation", typeof(TInput).Name));
        }
    }

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
                    .Do("Client check").FallbackToFront()
                    .Do("Balance check").FallbackToFront()
                    .Do("Limits check").FallbackToFront()
                    .Do("Payment check").FallbackToFront()
                    .Do("Send to Matching Engine").OnFail("Fail operation")                    
                    .Do("Successful operation finish")
                    .Do("Logging successful operation finish")
                    .ContinueWith("Clear")
                    .WithBranch()
                        .Do("FallBackToFront").OnFail("Cancel operation")
                    .WithBranch()
                        .Do("Fail operation").ContinueWith("Clear")
                    .WithBranch()
                        .Do("Cancel operation").ContinueWith("Clear")
                    .WithBranch()
                        .Do("Clear")
                    .End()
            );

            ValidationNode<GlobalCheckInput>("Global check")
                .WithInput(context => new GlobalCheckInput
                {
                    CashOutBlocked = (bool)context.OperationValues.GlobalContext.CashOutBlocked
                })
                .MergeFailOutput(output => output);

            ValidationNode<AssetCheckInput>("Asset check")
                .WithInput(context => new AssetCheckInput
                {
                    IsTradable = (bool)context.OperationValues.Asset.IsTradable,
                    IsTrusted = (bool)context.OperationValues.Asset.IsTrusted                    
                })
                .MergeFailOutput(output => output);            

            ValidationNode<ClientCheckInput>("Client check")
                .WithInput(context => new ClientCheckInput
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

    internal class ClientCheckInput
    {
    }

    public class AssetCheckInput
    {
        public bool IsTradable { get; set; }
        public bool IsTrusted { get; set; }
    }

    internal class GlobalCheckInput
    {
        public bool CashOutBlocked { get; set; }
    }
}
