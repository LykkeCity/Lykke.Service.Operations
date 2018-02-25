using System;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Workflow;
using Newtonsoft.Json.Linq;

namespace Lykke.Service.Operations.Workflow.Extensions
{
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
}
