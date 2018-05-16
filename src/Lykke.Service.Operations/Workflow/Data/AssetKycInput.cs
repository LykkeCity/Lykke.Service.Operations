using Lykke.Service.Kyc.Abstractions.Domain.Verification;

namespace Lykke.Service.Operations.Workflow.Data
{
    public class AssetKycInput
    {
        public KycStatus KycStatus { get; set; }
        public string AssetId { get; set; }
        public bool AssetKycNeeded { get; set; }

        public KycStatus GetMappedKycStatus()
        {
            switch (KycStatus)
            {
                case KycStatus.NeedToFillData:
                    return KycStatus.NeedToFillData;
                case KycStatus.Ok:
                case KycStatus.ReviewDone:
                    return KycStatus.Ok;
                case KycStatus.RestrictedArea:
                    return KycStatus.RestrictedArea;
                default:
                    return KycStatus.Pending;
            }
        }
    }
}
