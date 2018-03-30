using System;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Workflow;
using Lykke.Workflow.Fluent;
using Newtonsoft.Json.Linq;

namespace Lykke.Service.Operations.Workflow.Extensions
{
    internal static class WorkflowConfigurationExtensions
    {
        public static IActivitySlot<TContext, TInput, TOutput, TFailOutput> MergeOutput<TContext, TInput, TOutput, TFailOutput>(this IActivitySlot<TContext, TInput, TOutput, TFailOutput> slot)
            where TContext : Operation
        {
            return slot.MergeOutput((context, output) => output);
        }

        public static IActivitySlot<TContext, TInput, TOutput, TFailOutput> MergeOutput<TContext, TInput, TOutput, TFailOutput>(
            this IActivitySlot<TContext, TInput, TOutput, TFailOutput> slot,
            Func<TContext, TOutput, object> getContextMapFromOutput)
            where TContext : Operation
        {
            return slot.ProcessOutput(
                delegate (TContext context, TOutput output)
                {
                    object contextMapFromOutput = getContextMapFromOutput(context, output);
                    JObject childContent = JObject.FromObject(contextMapFromOutput);
                    JsonStringExtensions.Merge(context.OperationValuesJObject, childContent);
                });
        }

        public static IActivitySlot<TContext, TInput, TOutput, TFailOutput> MergeOutput<TContext, TInput, TOutput, TFailOutput>(this IActivitySlot<TContext, TInput, TOutput, TFailOutput> slot,
            Func<TOutput, object> getContextMapFromOutput)
            where TContext : Operation
        {
            return slot.MergeOutput((context, output) => getContextMapFromOutput(output));
        }

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

        public static WorkflowConfiguration<TContext> SubConfigure<TContext>(this WorkflowConfiguration<TContext> configuration,
            Func<WorkflowConfiguration<TContext>, WorkflowConfiguration<TContext>> configure)
        {
            return configure(configuration);
        }
    }
}
