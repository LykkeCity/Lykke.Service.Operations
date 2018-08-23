using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Cqrs;
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
    }
}
