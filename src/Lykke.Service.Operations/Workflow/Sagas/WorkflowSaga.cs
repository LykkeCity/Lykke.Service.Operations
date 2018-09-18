using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Service.ConfirmationCodes.Contract.Events;
using Lykke.Service.Operations.Contracts.Events;
using Lykke.Service.Operations.Workflow.Commands;

namespace Lykke.Service.Operations.Workflow.Sagas
{
    public class WorkflowSaga
    {
        private ILog _log;

        public WorkflowSaga(ILogFactory logFactory)
        {
            _log = logFactory.CreateLog(this);
        }
        
        public async Task Handle(OperationCreatedEvent evt, ICommandSender commandSender)
        {
            _log.Info($"OperationCreatedEvent for operation [{evt.Id}] received", evt);

            var command = new ExecuteOperationCommand
            {
                OperationId = evt.Id
            };

            commandSender.SendCommand(command, "operations");
        }

        public async Task Handle(ConfirmationValidationPassedEvent evt, ICommandSender commandSender)
        {
            _log.Info($"ConfirmationValidationPassedEvent for operation [{evt.Id}] received", evt);

            var command = new CompleteActivityCommand
            {
                OperationId = Guid.Parse(evt.Id),
                Output = "{}"
            };

            commandSender.SendCommand(command, "operations");
        }

        public async Task Handle(ConfirmationValidationFailedEvent evt, ICommandSender commandSender)
        {
            _log.Info($"ConfirmationValidationFailedEvent for operation [{evt.Id}] received", evt);

            var command = new FailActivityCommand
            {
                OperationId = Guid.Parse(evt.Id),
                Output = new
                {
                    ErrorCode = evt.Reason.ToString(),
                    ErrorMessage = "Confirmation failed"
                }.ToJson()
            };

            commandSender.SendCommand(command, "operations");
        }
    }
}
