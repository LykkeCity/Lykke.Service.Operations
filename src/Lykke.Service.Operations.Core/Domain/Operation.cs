using System;
using System.Collections.Generic;
using System.Linq;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Core.Extensions;
using Lykke.Service.Operations.Workflow.Activities;
using Lykke.Workflow;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OperationType = Lykke.Service.Operations.Contracts.OperationType;

namespace Lykke.Service.Operations.Core.Domain
{
    public class Operation : IHasId, IExecutionObserver, IActivityExecutor, IWorkflowPersister<Operation>
    {
        private dynamic _operationValues;
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public Guid ClientId { get; set; }
        public OperationType Type { get; set; }
        public OperationStatus Status { get; set; }
        public string Context { get; set; }

        [BsonIgnore]
        public dynamic OperationValues
        {
            get
            {
                if (_operationValues == null)
                    _operationValues = JObject.Parse(Context);

                return _operationValues;
            }
            set { _operationValues = value; }
        }

        [BsonIgnore]
        public JObject OperationValuesJObject
        {
            get => (JObject)OperationValues;            
        }

        public List<OperationActivity> Activities { get; set; } = new List<OperationActivity>();

        public WorkflowState WorkflowState { get; set; }
        public string InputValues { get; set; }

        public Operation()
        {            
            WorkflowState = WorkflowState.None;
            InputValues = Context = "{}";
        }

        public void Create(Guid id, Guid clientId, OperationType type, string inputValues)
        {
            Id = id;
            Created = DateTime.UtcNow;
            ClientId = clientId;
            Status = OperationStatus.Created;
            Type = type;
            InputValues = inputValues;
            Context = inputValues;
            OperationValues = JObject.Parse(inputValues);
        }

        public void Fail()
        {
            Status = OperationStatus.Failed;
        }

        public void ActivityStarted(Guid activityExecutionId, string node, string activityType, object inputValues)
        {
            Activities.Add(new OperationActivity(activityExecutionId, node, activityType, inputValues.ToJsonString()));
        }

        public void ActivityFinished(Guid activityExecutionId, string node, string activityType, object outputValues)
        {
            var activity = Activities.Single(om => om.ActivityId == activityExecutionId);
                
            activity.Status = ActivityResult.Succeeded;
            activity.Finished = DateTime.UtcNow;            
            activity.Output = outputValues.ToJsonString();
        }

        public void ActivityFailed(Guid activityExecutionId, string node, string activityType, object outputValues)
        {
            var activity = Activities.Single(om => om.ActivityId == activityExecutionId);

            activity.Output = outputValues.ToJsonString();
            activity.Status = ActivityResult.Failed;
        }

        public void ActivityCorrupted(Guid activityExecutionId, string node, string activityType)
        {
            var activity = Activities.Single(om => om.ActivityId == activityExecutionId);

            activity.Status = ActivityResult.None;
        }

        public void Save(Operation context, Execution<Operation> execution)
        {
            Activities.ForEach(oa => oa.IsExecuting = false);
            
            foreach (var ea in execution.ExecutingActivities)
            {
                Activities.Single(a => a.ActivityId == ea.Id && a.Name == ea.Node).IsExecuting = true;
            }

            var workflowState = WorkflowState;

            if (workflowState != execution.State)
            {
                switch (execution.State)
                {
                    case WorkflowState.InProgress:
                        WorkflowState = WorkflowState.InProgress;
                        break;
                    case WorkflowState.Corrupted:
                        WorkflowState = WorkflowState.Corrupted;
                        break;
                }                
            }
        }

        public Execution<Operation> Load(Operation context)
        {
            var executingActivities = Activities.Where(a => a.IsExecuting)
                .Select(a => new ActivityExecution(a.Name, a.ActivityId))
                .ToList();

            var execution = new Execution<Operation>
            {
                State = WorkflowState
            };

            execution.ExecutingActivities.AddRange(executingActivities);
            return execution;
        }

        public void ApplyValuesChanges()
        {
            Context = OperationValuesJObject.ToString(Formatting.Indented);
        }

        public void Confirm()
        {
            Status = OperationStatus.Confirmed;
        }

        public ActivityResult Execute<TInput, TOutput, TFailOutput>(Guid activityExecutionId, string activityType, string nodeName,
            TInput input, Action<TOutput> processOutput, Action<TFailOutput> processFailOutput)
        {
            return ActivityResult.Pending;
        }

        public void Accept()
        {
            Status = OperationStatus.Accepted;
        }

        public void Complete()
        {
            Status = OperationStatus.Completed;
        }

        public void Corrupt()
        {
            Status = OperationStatus.Corrupted;
        }

        public OperationActivity GetConfirmationActivity()
        {
            return Activities.LastOrDefault(a => a.Type == "RequestConfirmation" && a.Status == ActivityResult.None);
        }

        public void CompleteActivity(Guid activityId, object output)
        {
            var activity = Activities.Single(a => a.ActivityId == activityId && a.Status == ActivityResult.None);

            activity.Complete(JObject.FromObject(output));
        }
    }
}
