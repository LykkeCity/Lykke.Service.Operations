using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Contracts.Commands;
using Lykke.Service.Operations.Contracts.Events;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Services;
using Lykke.Service.Operations.Workflow.Commands;
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
        private readonly string _ethereumHotWallet;

        public WorkflowCommandHandler(
            ILogFactory logFactory, 
            IOperationsRepository operationsRepository, 
            IWorkflowService workflowService,
            Func<string, Operation, OperationWorkflow> workflowFactory,
            string ethereumHotWallet)
        {
            _log = logFactory.CreateLog(this);
            _operationsRepository = operationsRepository;
            _workflowService = workflowService;
            _workflowFactory = workflowFactory;
            _ethereumHotWallet = ethereumHotWallet;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(CreateCashoutCommand command, IEventPublisher eventPublisher)
        {
            // TODO: obsolete
            command.GlobalSettings.EthereumHotWallet = _ethereumHotWallet;

            var operation = await _operationsRepository.Get(command.OperationId);

            if (operation != null)
                return CommandHandlingResult.Ok();

            operation = new Operation();
            operation.Create(command.OperationId, command.Client.Id, OperationType.Cashout, JsonConvert.SerializeObject(command, Formatting.Indented));
            await _operationsRepository.Save(operation);

            eventPublisher.PublishEvent(new OperationCreatedEvent { Id = command.OperationId, ClientId = command.Client.Id });

            return CommandHandlingResult.Ok();
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(ExecuteOperationCommand cmd, IEventPublisher eventPublisher)
        {
            var operation = await _operationsRepository.Get(cmd.OperationId);

            var wf = _workflowFactory(operation.Type + "Workflow", operation);
            var wfResult = wf.Run(operation);            

            await HandleWorkflow(operation, wfResult, eventPublisher);

            return CommandHandlingResult.Ok();
        }

        private async Task HandleWorkflow(Operation operation, Execution<Operation> wfResult, IEventPublisher eventPublisher)
        {
            if (wfResult.State == WorkflowState.Corrupted)
            {
                _log.Critical(operation.Type + "Workflow", context: wfResult);

                operation.Corrupt();

                await _operationsRepository.Save(operation);

                eventPublisher.PublishEvent(new OperationCorruptedEvent
                {
                    OperationId = operation.Id
                });
            }
            else if (wfResult.State == WorkflowState.Failed)
            {
                operation.Fail();

                await _operationsRepository.Save(operation);

                string errorMessage = operation.OperationValues.ErrorMessage;
                string errorCode = operation.OperationValues.ErrorCode;
                JArray errors = operation.OperationValues.ValidationErrors;

                if (errors != null)
                {
                    errorCode = errors.First()["ErrorCode"].ToString();
                    errorMessage = errors.First()["ErrorMessage"].ToString();
                }

                eventPublisher.PublishEvent(new OperationFailedEvent
                {
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
                    Id = executingActivity.ActivityId,
                    Type = executingActivity.Type,
                    Input = executingActivity.Input
                });
            }
            else
            {
                await _operationsRepository.Save(operation);
            }
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(CompleteActivityCommand cmd, IEventPublisher eventPublisher)
        {
            var operation = await _operationsRepository.Get(cmd.OperationId);

            if (operation == null)
            {
                _log.Warning(nameof(CompleteActivityCommand), context: cmd, message: $"operation [{cmd.OperationId}] not found!");

                return CommandHandlingResult.Ok();
            }

            if (operation.Status == OperationStatus.Completed)
            {
                _log.Warning(nameof(CompleteActivityCommand), context: cmd, message: $"operation [{cmd.OperationId}] already completed!");

                return CommandHandlingResult.Ok();
            }

            var wfResult = await _workflowService.CompleteActivity(operation, cmd.ActivityId, JObject.Parse(cmd.Output));

            if (wfResult != null)
                await HandleWorkflow(operation, wfResult, eventPublisher);

            return CommandHandlingResult.Ok();
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(FailActivityCommand cmd, IEventPublisher eventPublisher)
        {
            var operation = await _operationsRepository.Get(cmd.OperationId);
            var activity = operation.Activities.SingleOrDefault(o => !cmd.ActivityId.HasValue && o.IsExecuting || o.ActivityId == cmd.ActivityId);

            if (activity == null)
            {
                _log.Warning("FailActivity", context: new { activity, cmd.Output }, message: "Executing activity not found");

                return CommandHandlingResult.Ok();
            }

            activity.Fail(cmd.Output);

            await _operationsRepository.Save(operation);

            return CommandHandlingResult.Ok();
        }
    }
}
