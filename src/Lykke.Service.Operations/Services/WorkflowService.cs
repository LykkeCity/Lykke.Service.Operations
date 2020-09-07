using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Core.Services;
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
        private readonly IOperationsCacheService _operationsCacheService;

        public WorkflowService(
            ILogFactory log,
            IOperationsRepository operationsRepository,
            Func<string, Operation, OperationWorkflow> workflowFactory,
            IOperationsCacheService operationsCacheService
            )
        {
            _log = log.CreateLog(this);
            _operationsRepository = operationsRepository;
            _workflowFactory = workflowFactory;
            _operationsCacheService = operationsCacheService;
        }

        public async Task<Execution> CompleteActivity(Operation operation, Guid? activityId, JObject activityOutput)
        {
            var activity = operation.Activities.SingleOrDefault(o => !activityId.HasValue && o.IsExecuting || o.ActivityId == activityId);

            if (activity == null)
            {
                _log.Warning("CompleteActivity", context: new { activityOutput }, message: $"Executing activity for operation [{operation.Id}] not found");

                return null;
            }

            activity.Complete(activityOutput);

            await _operationsCacheService.SaveAsync(operation);

            var wf = _workflowFactory(operation.Type + "Workflow", operation);

            return wf.Resume(operation, activity.ActivityId, new ActivityState { NodeName = activity.Name, Status = activity.Status, Values = JObject.Parse(activity.Output) });
        }

        public async Task FailActivity(Operation operation, Guid? activityId, JObject activityOutput)
        {
            var activity = operation.Activities.Single(o => !activityId.HasValue && o.IsExecuting || o.ActivityId == activityId);
            activity.Complete(activityOutput);

            operation.Status = OperationStatus.Failed;

            await _operationsCacheService.SaveAsync(operation);
        }
    }
}
