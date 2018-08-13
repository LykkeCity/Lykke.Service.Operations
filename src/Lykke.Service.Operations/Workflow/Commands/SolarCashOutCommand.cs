using MessagePack;

namespace Lykke.Service.Operations.Workflow.Commands
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class SolarCashOutCommand
    {
        public string Id { get; set; }
        public string ClientId { get; set; }
        public string Address { get; set; }
        public decimal Amount { get; set; }
    }
}
