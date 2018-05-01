using Lykke.Service.Kyc.Abstractions.Domain.Verification;

namespace Lykke.Service.Operations.Workflow.Data
{
    public class AssetKycInput
    {
        public KycStatus KycStatus { get; set; }
        public string AssetId { get; set; }
        public bool AssetKycNeeded { get; set; }        
    }
}
