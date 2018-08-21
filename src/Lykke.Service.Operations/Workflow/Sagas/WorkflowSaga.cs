using System.Threading.Tasks;
using Lykke.Cqrs;
using Lykke.Service.Operations.Contracts.Events;
using Lykke.Service.Operations.Workflow.Commands;

namespace Lykke.Service.Operations.Workflow.Sagas
{
    public class WorkflowSaga
    {
        public async Task Handle(OperationCreatedEvent evt, ICommandSender commandSender)
        {
            var command = new ExecuteOperationCommand
            {
                OperationId = evt.Id
            };

            commandSender.SendCommand(command, "operations");
        }
    }
}
