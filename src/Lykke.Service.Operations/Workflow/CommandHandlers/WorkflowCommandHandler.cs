using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Workflow.Commands;
using Lykke.Service.Operations.Workflow.Events;
using Lykke.Workflow;
using Newtonsoft.Json.Linq;

namespace Lykke.Service.Operations.Services
{
    public class WorkflowCommandHandler
    {
        private readonly ILog _log;
        private readonly IOperationsRepository _operationsRepository;
        private readonly IWorkflowService _workflowService;

        public WorkflowCommandHandler(ILog log, IOperationsRepository operationsRepository, IWorkflowService workflowService)
        {
            _log = log;
            _operationsRepository = operationsRepository;
            _workflowService = workflowService;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(CompleteActivityCommand cmd, IEventPublisher eventPublisher)
        {
            var operation = await _operationsRepository.Get(cmd.OperationId);            

            if (operation == null)
                return CommandHandlingResult.Ok();

            var wfResult = await _workflowService.CompleteActivity(operation, cmd.ActivityId, JObject.Parse(cmd.Output));

            if (wfResult == WorkflowState.Corrupted)
            {
                _log.WriteError("Workflow had corrupted", operation);
            }

            if (wfResult == WorkflowState.InProgress)
            {
                var executingActivity = operation.Activities.Single(a => a.IsExecuting);

                eventPublisher.PublishEvent(new ExternalExecutionActivityCreatedEvent
                {
                    Id = executingActivity.ActivityId,
                    Type = executingActivity.Type,
                    Input = executingActivity.Input
                });
            }

            return CommandHandlingResult.Ok();
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(FailActivityCommand cmd, IEventPublisher eventPublisher)
        {
            return CommandHandlingResult.Ok();
        }
    }
}
