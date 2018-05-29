using Lykke.Service.Operations.Contracts;

namespace Lykke.Service.Operations.Workflow.Data
{
    public class DisclaimerInput
    {
        public OperationType Type { get; set; }

        public string ClientId { get; set; }
        public string LykkeEntityId1 { get; set; }
        public string LykkeEntityId2 { get; set; }
    }
}
