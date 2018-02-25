using FluentValidation;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;

namespace Lykke.Service.Operations.Workflow.Validation
{
    public class AssetKycValidator : AbstractValidator<AssetKycInput>
    {
        public AssetKycValidator()
        {
            RuleFor(m => m.AssetKycNeeded)
                .Must((input, kycNeeded) => input.KycStatus.IsKycOkOrReviewDone() || !kycNeeded)
                .WithMessage(input => $"Asset: kyc needed. Client kyc status is {input.KycStatus.ToString()}. Asset kyc required: {input.AssetKycNeeded}");
        }
    }
}
