using System;
using Lykke.Workflow;

namespace Lykke.Service.Operations.Workflow.Activities
{
    public interface IActivityExecutor
    {
        ActivityResult Execute<TInput, TOutput, TFailOutput>(Guid activityExecutionId, string activityType, string nodeName, TInput input, Action<TOutput> processOutput, Action<TFailOutput> processFailOutput);
    }
}
