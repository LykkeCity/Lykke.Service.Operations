using FluentValidation;
using JetBrains.Annotations;
using Lykke.Service.Operations.Workflow.Data;

namespace Lykke.Service.Operations.Workflow.Validation
{
    [UsedImplicitly]
    public class BalanceValidator : AbstractValidator<BalanceInput>
    {
        public BalanceValidator()
        {
            RuleFor(m => m.Volume)
                .Must(volume => volume > 0)
                .WithErrorCode("InvalidField")
                .WithMessage("Volume");

            RuleFor(m => m.Volume)
                .Must((input, volume) => input.Balance >= input.Volume)
                .WithErrorCode("NotEnoughFunds")
                .WithMessage(input => "Not enough funds");
        }
    }
}
