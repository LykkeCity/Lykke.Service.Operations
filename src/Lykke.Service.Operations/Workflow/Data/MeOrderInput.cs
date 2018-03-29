using Lykke.Service.Operations.Contracts;

namespace Lykke.Service.Operations.Workflow.Data
{
    public class MeOrderInput
    {
        public string Id { get; set; }
        public OperationType OperationType { get; set; }
        public string AssetPairId { get; set; }
        public string ClientId { get; set; }
        public bool Straight { get; set; }
        public double Volume { get; set; }
        public double? Price { get; set; }
        public OrderAction OrderAction { get; set; }
        public object Fee { get; set; }        
    }
}
