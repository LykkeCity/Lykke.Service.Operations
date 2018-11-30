using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Contracts.Events;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Services;
using Lykke.Service.Operations.Workflow.Commands;
using Lykke.Service.Operations.Workflow.Data;
using Lykke.Service.Operations.Workflow.Events;
using Lykke.Workflow;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Lykke.Service.Operations.Workflow.CommandHandlers
{
    public class WorkflowCommandHandler
    {
        private readonly ILog _log;
        private readonly IOperationsRepository _operationsRepository;
        private readonly IWorkflowService _workflowService;
        private readonly Func<string, Operation, OperationWorkflow> _workflowFactory;

        public WorkflowCommandHandler(
            ILogFactory logFactory,
            IOperationsRepository operationsRepository,
            IWorkflowService workflowService,
            Func<string, Operation, OperationWorkflow> workflowFactory)
        {
            _log = logFactory.CreateLog(this);
            _operationsRepository = operationsRepository;
            _workflowService = workflowService;
            _workflowFactory = workflowFactory;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(ExecuteOperationCommand command, IEventPublisher eventPublisher)
        {
            _log.Info($"ExecuteOperationCommand received. Operation [{command.OperationId}]", command);

            var operation = await _operationsRepository.Get(command.OperationId);
            if (operation == null)
                throw new InvalidOperationException($"Operation with id {command.OperationId} not found");

            var wf = _workflowFactory(operation.Type + "Workflow", operation);
            var wfResult = wf.Run(operation);

            await HandleWorkflow(operation, wfResult, eventPublisher, OperationStatus.Created);

            return CommandHandlingResult.Ok();
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(CompleteActivityCommand command, IEventPublisher eventPublisher)
        {
            _log.Info($"CompleteActivityCommand received. Operation [{command.OperationId}]", command);

            var operation = await _operationsRepository.Get(command.OperationId);

            if (operation == null)
            {
                _log.Warning(nameof(CompleteActivityCommand), context: command, message: $"operation [{command.OperationId}] not found!");

                return CommandHandlingResult.Ok();
            }

            if (operation.Status == OperationStatus.Completed)
            {
                _log.Warning(nameof(CompleteActivityCommand), context: command, message: $"operation [{command.OperationId}] already completed!");

                return CommandHandlingResult.Ok();
            }

            if (operation.ExecutingActivity(command.ActivityType) == null)
            {
                _log.Info($"Executing activity {command.ActivityType} for operation [{operation.Id}] is null. Retrying...");

                return new CommandHandlingResult
                {
                    Retry = true,
                    RetryDelay = (long)TimeSpan.FromSeconds(5).TotalMilliseconds
                };
            }

            var previousStatus = operation.Status;

            var wfResult = await _workflowService.CompleteActivity(operation, command.ActivityId, JObject.Parse(command.Output));

            if (wfResult != null)
                await HandleWorkflow(operation, wfResult, eventPublisher, previousStatus);

            return CommandHandlingResult.Ok();
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(FailActivityCommand command, IEventPublisher eventPublisher)
        {
            _log.Info($"FailActivityCommand received. Operation [{command.OperationId}]", command);

            var operation = await _operationsRepository.Get(command.OperationId);

            if (operation == null)
            {
                _log.Warning(nameof(FailActivityCommand), context: command, message: $"operation [{command.OperationId}] not found!");

                return CommandHandlingResult.Ok();
            }

            var activity = operation.Activities.SingleOrDefault(o => !command.ActivityId.HasValue && o.IsExecuting || o.ActivityId == command.ActivityId);

            if (activity == null)
            {
                _log.Warning("FailActivity", context: new { command.Output }, message: $"Executing activity for operation [{command.OperationId}] not found");

                return CommandHandlingResult.Ok();
            }

            activity.Fail(command.Output);

            await _operationsRepository.Save(operation);

            if (!string.IsNullOrWhiteSpace(command.Output))
            {
                var output = JObject.Parse(command.Output);

                eventPublisher.PublishEvent(new OperationFailedEvent
                {
                    ClientId = operation.ClientId,
                    OperationId = operation.Id,
                    ErrorCode = output.ContainsKey("ErrorCode") ? output["ErrorCode"].ToString() : null,
                    ErrorMessage = output.ContainsKey("ErrorMessage") ? output["ErrorMessage"].ToString() : null
                });
            }

            return CommandHandlingResult.Ok();
        }

        private async Task HandleWorkflow(Operation operation, Execution wfResult, IEventPublisher eventPublisher, OperationStatus previousStatus)
        {
            _log.Info($"Handle workflow result: [{wfResult.State}]. Operation [{operation.Id}]", wfResult);

            if (wfResult.State == WorkflowState.Corrupted)
            {
                _log.Critical(operation.Type + "Workflow", context: wfResult, message: $"Workflow for operation [{operation.Id}] has corrupted!");

                operation.Corrupt();

                await _operationsRepository.Save(operation);

                eventPublisher.PublishEvent(new OperationCorruptedEvent
                {
                    OperationId = operation.Id
                });
            }
            else if (wfResult.State == WorkflowState.Failed || operation.Status == OperationStatus.Failed)
            {
                operation.Fail();

                string errorMessage = operation.OperationValues.ErrorMessage;
                string errorCode = operation.OperationValues.ErrorCode;
                JArray errors = operation.OperationValues.ValidationErrors;

                if (errors != null)
                {
                    errorCode = errors.First()["ErrorCode"].ToString();
                    errorMessage = errors.First()["ErrorMessage"].ToString();
                }

                await _operationsRepository.Save(operation);

                eventPublisher.PublishEvent(new OperationFailedEvent
                {
                    ClientId = operation.ClientId,
                    OperationId = operation.Id,
                    ErrorCode = errorCode,
                    ErrorMessage = errorMessage
                });
            }
            else if (wfResult.State == WorkflowState.InProgress)
            {
                var executingActivity = operation.Activities.Single(a => a.IsExecuting);

                await _operationsRepository.Save(operation);

                eventPublisher.PublishEvent(new ExternalExecutionActivityCreatedEvent
                {
                    OperationId = operation.Id,
                    ActivityId = executingActivity.ActivityId,
                    Type = executingActivity.Type,
                    Input = executingActivity.Input
                });

                // need to fire OperationConfirmed event to notify consumers
                if (previousStatus != operation.Status && operation.Status == OperationStatus.Confirmed)
                    eventPublisher.PublishEvent(new OperationConfirmedEvent
                    {
                        ClientId = operation.ClientId,
                        OperationId = operation.Id
                    });
            }
            else
            {
                await _operationsRepository.Save(operation);

                if (operation.Status == OperationStatus.Confirmed)
                    eventPublisher.PublishEvent(new OperationConfirmedEvent
                    {
                        OperationId = operation.Id,
                        ClientId = operation.ClientId
                    });

                if (operation.Status == OperationStatus.Completed)
                    eventPublisher.PublishEvent(new OperationCompletedEvent
                    {
                        OperationId = operation.Id,
                        ClientId = operation.ClientId
                    });
            }
        }
    }
}
