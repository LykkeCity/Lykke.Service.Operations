using System;
using System.Collections.Generic;
using System.Linq;
using Lykke.Contracts.Operations;
using Lykke.Service.Operations.Core.Extensions;
using Lykke.Workflow;
using Newtonsoft.Json.Linq;

namespace Lykke.Service.Operations.Core.Domain
{
    public class Operation : IHasId, IExecutionObserver, IWorkflowPersister<Operation>
    {
        private readonly List<OperationActivity> _activities = new List<OperationActivity>();
        
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public Guid ClientId { get; set; }
        public OperationType Type { get; set; }
        public OperationStatus Status { get; set; }
        public string Context { get; set; }

        public dynamic OperationValues { get; set; }
        
        public WorkflowState WorkflowState { get; set; }

        public Operation()
        {            
            WorkflowState = WorkflowState.None;
        }

        public void Fail()
        {
            Status = OperationStatus.Failed;
        }

        public void ActivityStarted(Guid activityExecutionId, string node, string activityType, object inputValues)
        {
            _activities.Add(new OperationActivity(activityExecutionId, node, activityType, inputValues.ToJsonString()));
        }

        public void ActivityFinished(Guid activityExecutionId, string node, string activityType, object outputValues)
        {
            var activity = _activities.Single(om => om.ActivityId == activityExecutionId);
                
            activity.Status = ActivityResult.Succeeded;
            activity.Output = outputValues.ToJsonString();
        }

        public void ActivityFailed(Guid activityExecutionId, string node, string activityType, object outputValues)
        {
            var activity = _activities.Single(om => om.ActivityId == activityExecutionId);

            activity.Output = outputValues.ToJsonString();
            activity.Status = ActivityResult.Failed;
        }

        public void ActivityCorrupted(Guid activityExecutionId, string node, string activityType)
        {
            var activity = _activities.Single(om => om.ActivityId == activityExecutionId);

            activity.Status = ActivityResult.None;
        }

        public void Save(Operation context, Execution<Operation> execution)
        {
            _activities.ForEach(oa => oa.IsExecuting = false);
            
            foreach (var ea in execution.ExecutingActivities)
            {
                _activities.Single(a => a.ActivityId == ea.Id && a.Name == ea.Node).IsExecuting = true;
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
            throw new NotImplementedException();
        }

        public void ApplyValuesChanges()
        {
            
        }
    }
}
