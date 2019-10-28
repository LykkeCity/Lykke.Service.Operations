using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using JetBrains.Annotations;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;

namespace Lykke.Service.Operations.Workflow.Data.Validation
{
    [UsedImplicitly]
    public class KycValidator : AbstractValidator<KycCheckInput>
    {
        private readonly IClientAccountClient _clientAccountClient;

        public KycValidator(
            IClientAccountClient clientAccountClient
            )
        {
            _clientAccountClient = clientAccountClient;

            RuleFor(m => m).MustAsync(IsKycNotNeeded).WithMessage("KYC needed").WithErrorCode("AssetKycNeeded");
        }

        private async Task<bool> IsKycNotNeeded(KycCheckInput input, CancellationToken cancellationToken)
        {
            var client = await _clientAccountClient.ClientAccountInformation.GetByIdAsync(input.ClientId);

            return !(client.Tier == AccountTier.Beginner && input.KycStatus != KycStatus.Ok);
        }
    }
}
