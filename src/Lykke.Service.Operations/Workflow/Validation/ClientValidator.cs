using FluentValidation;
using JetBrains.Annotations;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;

namespace Lykke.Service.Operations.Workflow.Validation
{
    [UsedImplicitly]
    public class ClientValidator : AbstractValidator<ClientInput>
    {
        public ClientValidator()
        {
            RuleFor(m => m.TradesBlocked)
                .Equal(false)
                .WithMessage("Trades are blocked.");

            RuleFor(m => m.BackupDone)                
                .Equal(true)
                .WithMessage("Wallet requires backup.");            
        }
    }
}
