using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using JetBrains.Annotations;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Tier.Client;

namespace Lykke.Service.Operations.Workflow.Data.Validation
{
    [UsedImplicitly]
    public class KycValidator : AbstractValidator<KycCheckInput>
    {
        private readonly ITierClient _tierClient;

        public KycValidator(
            ITierClient tierClient)
        {
            _tierClient = tierClient;
            RuleFor(m => m).MustAsync(IsKycNotNeeded).WithMessage("KYC needed").WithErrorCode("KycNeeded");
        }

        private async Task<bool> IsKycNotNeeded(KycCheckInput input, CancellationToken cancellationToken)
        {
            switch (input.KycStatus)
            {
                case KycStatus.NeedToFillData:
                case KycStatus.RestrictedArea:
                case KycStatus.Rejected:
                case KycStatus.Complicated:
                case KycStatus.JumioInProgress:
                case KycStatus.JumioOk:
                case KycStatus.JumioFailed:
                    return true;
                case KycStatus.Pending:
                    var tierInfo = await _tierClient.Tiers.GetClientTierInfoAsync(input.ClientId);
                    return tierInfo.CurrentTier.Current > tierInfo.CurrentTier.MaxLimit;
                case KycStatus.ReviewDone:
                case KycStatus.Ok:
                    return false;
                default:
                    return false;
            }
        }
    }
}

