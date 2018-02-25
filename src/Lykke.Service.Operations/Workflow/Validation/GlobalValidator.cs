using FluentValidation;

namespace Lykke.Service.Operations.Workflow.Validation
{
    internal class GlobalValidator : AbstractValidator<GlobalInput>
    {
        public GlobalValidator()
        {
            RuleFor(t => t.CashOutBlocked)
                .Equal(false)
                .WithMessage("Cashout is globally locked");
        }
    }
}
