using Lykke.Service.Kyc.Abstractions.Domain.Verification;

namespace Lykke.Service.Operations.Workflow.Data
{
    public class UsaUsersRestrictionsInput
    {
        public string Country { get; set; }
        public string CountryFromID { get; set; }
        public string CountryFromPOA { get; set; }
        public string AssetId { get; set; }
        public decimal Volume { get; set; }
        public string BaseAssetId { get; set; }
        public string QuotingAssetId { get; set; }
        public KycStatus KycStatus { get; set; }        
    }
}
