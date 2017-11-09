using FluentValidation;

namespace Lykke.Service.Operations.Models
{
    public class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
    {
        public CreatePaymentCommandValidator()
        {
            RuleFor(m => m.ClientId)
                .NotNull();

            RuleFor(m => m.AssetId)
                .NotEmpty();

            RuleFor(m => m.Amount)
                .GreaterThan(0);            
        }
    }
}
