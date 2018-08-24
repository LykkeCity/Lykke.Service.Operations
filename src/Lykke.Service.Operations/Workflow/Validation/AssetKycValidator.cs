using FluentValidation;
using JetBrains.Annotations;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;

namespace Lykke.Service.Operations.Workflow.Data.Validation
{
    [UsedImplicitly]
    public class AssetKycValidator : AbstractValidator<AssetKycInput>
    {
        public AssetKycValidator()
        {
            RuleFor(m => m.AssetKycNeeded)
                .Must((input, kycNeeded) => input.KycStatus.IsKycOkOrReviewDone() || !kycNeeded)
                .WithMessage(input => $"Asset: kyc needed. Client kyc status is {input.GetMappedKycStatus().ToString()}. Asset kyc required: {input.AssetKycNeeded}");
        }
    }
}
