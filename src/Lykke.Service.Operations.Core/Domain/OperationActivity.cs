using System;
using Lykke.Workflow;
using Newtonsoft.Json.Linq;

namespace Lykke.Service.Operations.Core.Domain
{
    public class OperationActivity
    {
        public OperationActivity(Guid activityId, string name, string type, string input)
        {
            ActivityId = activityId;
            Name = name;
            Type = type;
            Input = input;
            Status = ActivityResult.None;
            Created = DateTime.UtcNow;
        }

        public Guid ActivityId { get; private set; }
        public string Name { get; private set; }
        public string Input { get; private set; }
        public string Output { get; internal set; }
        public string Type { get; private set; }
        public DateTime Created { get; set; }
        public DateTime Finished { get; set; }
        public ActivityResult Status { get; internal set; }
        public bool IsExecutedExternally { get; internal set; }
        public bool IsExecuting { get; set; }

        public void Complete(JObject outputValues)
        {
            Output = outputValues.ToString();
            IsExecutedExternally = true;
            Status = ActivityResult.Succeeded;
        }

        public void Fail(string outputValues)
        {
            Output = outputValues;
            IsExecutedExternally = true;
            Status = ActivityResult.Failed;
            IsExecuting = false;
        }
    }
}
