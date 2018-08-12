using System;
using Lykke.Workflow;
using Newtonsoft.Json.Linq;

namespace Lykke.Service.Operations.Workflow.Activities
{
    public class GenericActivity<TInput, TOutput, TFailOutput> : ActivityBase<TInput, TOutput, TFailOutput>
        where TInput : class
        where TOutput : class
        where TFailOutput : class
    {
        private readonly IActivityExecutor _executor;
        private readonly string _activityType;
        private readonly string _nodeName;

        public GenericActivity(IActivityExecutor executor, string activityType, string nodeName)
        {
            _nodeName = nodeName;
            _activityType = activityType;
            _executor = executor;
        }

        public override ActivityResult Execute(Guid activityExecutionId, TInput input, Action<TOutput> processOutput, Action<TFailOutput> processFailOutput)
        {
            return _executor.Execute(activityExecutionId, _activityType, _nodeName, input, processOutput, processFailOutput);
        }

        public override ActivityResult Resume<TClosure>(Guid activityExecutionId, Action<TOutput> processOutput, Action<TFailOutput> processFailOutput, TClosure closure)
        {
            if (closure.GetType() == typeof(ActivityState))
            {
                var state = (ActivityState)(object)closure;

                if (state.NodeName != _nodeName)
                    return ActivityResult.Pending;

                if (state.Status == ActivityResult.Succeeded)
                {
                    var values = (JObject)state.Values;
                    processOutput(values.ToObject<TOutput>());
                    return ActivityResult.Succeeded;
                }

                if (state.Status == ActivityResult.Failed)
                {
                    var values = (JObject)state.Values;
                    processFailOutput(values.ToObject<TFailOutput>());
                    return ActivityResult.Failed;
                }
            }
            return ActivityResult.Pending;
        }

        public override string ToString()
        {
            return _activityType;
        }
    }
}
