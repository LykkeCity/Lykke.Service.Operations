using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Contracts.Commands;
using Lykke.Service.Operations.Contracts.Events;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Workflow.Events;
using Newtonsoft.Json;

namespace Lykke.Service.Operations.Workflow.CommandHandlers
{
    public class CommandHandler
    {
        private readonly ILog _log;
        private readonly IOperationsRepository _operationsRepository;
        private readonly string _ethereumHotWallet;

        public CommandHandler(
            ILogFactory logFactory,
            IOperationsRepository operationsRepository,
            string ethereumHotWallet)
        {
            _log = logFactory.CreateLog(this);
            _operationsRepository = operationsRepository;
            _ethereumHotWallet = ethereumHotWallet;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(CreateCashoutCommand command, IEventPublisher eventPublisher)
        {
            _log.Info($"CreateCashoutCommand received. Operation [{command.OperationId}]", command);

            // TODO: obsolete
            command.GlobalSettings.EthereumHotWallet = _ethereumHotWallet;

            var operation = await _operationsRepository.Get(command.OperationId);

            if (operation != null)
            {
                _log.Warning($"CreateCashoutCommand with id [{command.OperationId}] received, but operation already exists!", context: command);

                eventPublisher.PublishEvent(new OperationFailedEvent
                {
                    ClientId = command.Client.Id,
                    OperationId = command.OperationId,
                    ErrorCode = "DuplicatedOperation",
                    ErrorMessage = "Operation with same id alredy exists"
                });

                return CommandHandlingResult.Ok();
            }

            operation = new Operation();
            operation.Create(command.OperationId, command.Client.Id, OperationType.Cashout, JsonConvert.SerializeObject(command, Formatting.Indented));
            await _operationsRepository.Save(operation);

            eventPublisher.PublishEvent(new OperationCreatedEvent { Id = command.OperationId, ClientId = command.Client.Id });

            return CommandHandlingResult.Ok();
        }
    }
}
