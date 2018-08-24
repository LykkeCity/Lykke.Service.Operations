using System.Threading.Tasks;
using AzureStorage.Queue;
using Common;
using Lykke.Service.Operations.Workflow.Commands;
using Lykke.Service.Operations.Workflow.Validation;

namespace Lykke.Service.Operations.Services
{
    public interface ISrvSolarCoinCommandProducer
    {
        Task ProduceCashOutCommand(string id, SolarCoinAddress addressTo, decimal amount);
    }

    public class SrvSolarCoinCommandProducer : ISrvSolarCoinCommandProducer
    {
        private readonly IQueueExt _queueExt;

        public SrvSolarCoinCommandProducer(IQueueExt queueExt)
        {
            _queueExt = queueExt;
        }

        public async Task ProduceCashOutCommand(string id, SolarCoinAddress addressTo, decimal amount)
        {
            await _queueExt.PutRawMessageAsync(new SolarCashOutCommand
            {
                Id = id,
                Amount = amount,
                Address = addressTo.Value
            }.ToJson());
        }        
    }
}
