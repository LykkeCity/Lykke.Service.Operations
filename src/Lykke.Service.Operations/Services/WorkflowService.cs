using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Cqrs;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Workflow;
using Lykke.Workflow;
using Newtonsoft.Json.Linq;

namespace Lykke.Service.Operations.Services
{
    public class WorkflowService : IWorkflowService
    {
        private readonly ILog _log;
        private readonly IOperationsRepository _operationsRepository;
        private readonly Func<string, Operation, OperationWorkflow> _workflowFactory;
        
        public WorkflowService(ILog log, IOperationsRepository operationsRepository, Func<string, Operation, OperationWorkflow> workflowFactory)
        {
            _log = log;
            _operationsRepository = operationsRepository;
            _workflowFactory = workflowFactory;
        }

        public async Task<WorkflowState> CompleteActivity(Operation operation, Guid? activityId, JObject activityOutput)
        {            
            var activity = operation.Activities.SingleOrDefault(o => !activityId.HasValue && o.IsExecuting || o.ActivityId == activityId);

            if (activity == null)
            {
                _log.WriteWarning("CompleteActivity", context: new { activity, activityOutput }, info: "Executing activity not found");

                return operation.WorkflowState;
            }

            activity.Complete(activityOutput);
            
            await _operationsRepository.Save(operation);

            var wf = _workflowFactory(operation.Type + "Workflow", operation);

            var wfResult = wf.Resume(operation, activity.ActivityId, new ActivityState { NodeName = activity.Name, Status = activity.Status, Values = JObject.Parse(activity.Output) });
            
            await _operationsRepository.Save(operation);

            return wfResult.State;            
        }

        public async Task FailActivity(Operation operation, Guid? activityId, JObject activityOutput)
        {
            var activity = operation.Activities.Single(o => !activityId.HasValue && o.IsExecuting || o.ActivityId == activityId);
            activity.Complete(activityOutput);

            operation.Status = OperationStatus.Failed;

            await _operationsRepository.Save(operation);            
        }
    }
}
