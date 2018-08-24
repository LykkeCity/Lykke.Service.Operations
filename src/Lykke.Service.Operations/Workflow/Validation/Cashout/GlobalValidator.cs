using FluentValidation;

namespace Lykke.Service.Operations.Workflow.Validation.Cashout
{
    public class GlobalValidator : AbstractValidator<GlobalInput>
    {
        public GlobalValidator()
        {
            RuleFor(i => i.CashoutBlocked)
                .Equal(false)
                .WithErrorCode("InconsistentData")
                .WithMessage("Service temporarily unavailable. Sorry for the inconvenience.");
        }
    }
}
