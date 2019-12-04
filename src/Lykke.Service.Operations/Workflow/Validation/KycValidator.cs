using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using JetBrains.Annotations;
using Lykke.Service.Kyc.Abstractions.Services;

namespace Lykke.Service.Operations.Workflow.Data.Validation
{
    [UsedImplicitly]
    public class KycValidator : AbstractValidator<KycCheckInput>
    {
        private readonly IKycStatusService _kycStatusService;

        public KycValidator(
            IKycStatusService kycStatusService)
        {
            _kycStatusService = kycStatusService;
            RuleFor(m => m).MustAsync(IsKycNotNeeded).WithMessage("KYC needed").WithErrorCode("KycNeeded");
        }

        private async Task<bool> IsKycNotNeeded(KycCheckInput input, CancellationToken cancellationToken)
        {
            bool isKycNeeded = await _kycStatusService.IsKycNeededAsync(input.ClientId, input.KycStatus);
            return !isKycNeeded;
        }
    }
}

