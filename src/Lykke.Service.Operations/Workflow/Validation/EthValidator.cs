using FluentValidation;

namespace Lykke.Service.Operations.Workflow.Validation
{
    public class EthValidator : AbstractValidator<EthInput>
    {
        public EthValidator()
        {             
            RuleFor(m => m.Volume)
                .Must((i, volume) => volume < i.AdapterBalance)
                .WithErrorCode("PreviousTransactionsWereNotCompleted")
                .WithMessage("Your previous transactions are not settled yet, please try again later");
         
            RuleFor(m => m.CashoutIsAllowed)
                .Equal(true)
                .WithErrorCode("InvalidInputField")
                .WithMessage("Destination contract address spends too much gas. Please try to withdraw to another address.");
        }
    }
}
