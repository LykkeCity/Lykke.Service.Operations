using System;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Cqrs;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Workflow;
using Lykke.Workflow;
using Newtonsoft.Json.Linq;

namespace Lykke.Service.Operations.Services
{
    public class WorkflowService : IWorkflowService
    {
        private readonly IOperationsRepository _operationsRepository;
        private readonly Func<string, Operation, OperationWorkflow> _workflowFactory;
        
        public WorkflowService(IOperationsRepository operationsRepository, Func<string, Operation, OperationWorkflow> workflowFactory)
        {
            _operationsRepository = operationsRepository;
            _workflowFactory = workflowFactory;
        }

        public async Task<WorkflowState> CompleteActivity(Operation operation, Guid? activityId, JObject activityOutput)
        {            
            var activity = operation.Activities.Single(o => !activityId.HasValue && o.IsExecuting || o.ActivityId == activityId);
            activity.Complete(activityOutput);
            
            await _operationsRepository.Save(operation);

            var wf = _workflowFactory(operation.Type + "Workflow", operation);

            var wfResult = wf.Resume(operation, activity.ActivityId, new ActivityState { NodeName = activity.Name, Status = activity.Status, Values = JObject.Parse(activity.Output) });
            
            await _operationsRepository.Save(operation);

            return wfResult.State;            
        }
    }

    public class ExternalExecutionActivityCreatedEvent
    {
        public Guid Id { get; set; }
        public string Type { get; set; }
        public string Input { get; set; }
    }
}
