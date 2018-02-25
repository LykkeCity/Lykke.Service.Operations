using FluentValidation;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;

namespace Lykke.Service.Operations.Workflow.Validation
{
    public class ClientValidator : AbstractValidator<ClientInput>
    {
        public ClientValidator()
        {
            RuleFor(m => m.TradesBlocked)
                .Equal(false);

            RuleFor(m => m.BackupDone)
                .Equal(true);            
        }
    }
}
