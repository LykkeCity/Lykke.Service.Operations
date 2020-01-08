using Lykke.Service.Kyc.Abstractions.Domain.Verification;

namespace Lykke.Service.Operations.Workflow.Data
{
    public class KycCheckInput
    {
        public KycStatus KycStatus { get; set; }
        public string ClientId { get; set; }
    }
}
