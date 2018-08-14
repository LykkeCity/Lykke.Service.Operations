using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Cqrs;
using Lykke.Service.Operations.Services;
using Lykke.Service.Operations.Workflow.Commands;
using Lykke.Service.Operations.Workflow.Events;
using Lykke.Service.Operations.Workflow.Validation;

namespace Lykke.Service.Operations.Workflow.CommandHandlers
{
    public class SolarCoinCommandHandler
    {
        private readonly ILog _log;
        private readonly ISrvSolarCoinCommandProducer _solarCoinCommandProducer;

        public SolarCoinCommandHandler(ILog log, ISrvSolarCoinCommandProducer solarCoinCommandProducer)
        {
            _log = log;
            _solarCoinCommandProducer = solarCoinCommandProducer;
        }

        public async Task<CommandHandlingResult> Handle(SolarCashOutCommand command, IEventPublisher eventPublisher)
        {
            _log.WriteInfo(nameof(SolarCoinCommandHandler), command.ToJson(), "SolarCashOutCommand received");

            var slrAddress = new SolarCoinAddress(command.Address);

            await _solarCoinCommandProducer.ProduceCashOutCommand(command.Id, slrAddress, command.Amount);            

            eventPublisher.PublishEvent(new SolarCashOutCompletedEvent
            {
                OperationId = command.Id,
                ClientId = command.ClientId,
                Address = command.Address,
                Amount = command.Amount
            });

            return CommandHandlingResult.Ok();
        }
    }
}
