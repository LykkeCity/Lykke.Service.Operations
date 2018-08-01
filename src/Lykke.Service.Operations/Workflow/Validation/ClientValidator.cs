using FluentValidation;
using JetBrains.Annotations;
using Lykke.Service.Operations.Workflow.Data;

namespace Lykke.Service.Operations.Workflow.Validation
{
    [UsedImplicitly]
    public class ClientValidator : AbstractValidator<ClientInput>
    {
        public ClientValidator()
        {
            RuleFor(m => m.OperationsBlocked)
                .Equal(false)
                .WithErrorCode("InconsistentData")
                .WithMessage("Operations are blocked");

            RuleFor(m => m.BackupDone)                
                .Equal(true)
                .WithErrorCode("BackupRequired")
                .WithMessage("To ensure the safety of your funds, you must back up your private key before proceeding");
        }
    }
}
