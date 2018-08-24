using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Lykke.Service.BlockchainCashoutPreconditionsCheck.Client.AutorestClient.Models;

namespace Lykke.Service.Operations.Workflow.Validation
{
    public class BilValidator : AbstractValidator<BilOutput>
    {
        // TODO: extend?
        private readonly Dictionary<ValidationErrorType, (string codeType, string message)> _errors = new Dictionary<ValidationErrorType, (string, string)>()
        {
            { ValidationErrorType.AddressIsNotValid, ("InvalidCashoutAddress", "Invalid Destination Address. Please try again.") },
            { ValidationErrorType.BlackListedAddress, ("InvalidCashoutAddress", "The destination address is not allowed for the withdrawal from the Trading wallet. Please try to send funds to your private wallet first.") }            
        };

        public BilValidator()
        {
            When(m => m.Errors != null, () =>
                RuleFor(m => m.Errors)
                    .Must(errors => !errors.Any())                    
                    .WithState(input =>
                    {
                        var errorType = input.Errors.First().Type;

                        if (_errors.ContainsKey(errorType))
                            return _errors[errorType].codeType;

                        return "RuntimeProblem";
                    })
                    .WithMessage(input =>
                    {
                        var errorType = input.Errors.First().Type;

                        if (_errors.ContainsKey(errorType))
                            return _errors[errorType].message;

                        return input.Errors.First().Value;
                    }));
        }        
    }
}
