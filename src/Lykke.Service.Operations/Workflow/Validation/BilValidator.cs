using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Lykke.Service.BlockchainCashoutPreconditionsCheck.Contract.Responses;

namespace Lykke.Service.Operations.Workflow.Validation
{
    public class BilValidator : AbstractValidator<BilOutput>
    {
        // TODO: extend?
        //implicit maps error codes from bil to lykke wallet ResponseModel.ErrorCodeType
        private readonly Dictionary<ValidationErrorType, string> _codeTypes = new Dictionary<ValidationErrorType, string>()
        {
            { ValidationErrorType.AddressIsNotValid, "InvalidCashoutAddress" },
            { ValidationErrorType.BlackListedAddress, "InvalidCashoutAddress" },
            { ValidationErrorType.LessThanMinCashout, "AmountIsLessThanLimit" }
        };

        private readonly Dictionary<ValidationErrorType, string> _messages = new Dictionary<ValidationErrorType, string>()
        {
            { ValidationErrorType.AddressIsNotValid, "Invalid Destination Address. Please try again." },
            { ValidationErrorType.BlackListedAddress, "The destination address is not allowed for the withdrawal from the Trading wallet. Please try to send funds to your private wallet first." }
        };

        public BilValidator()
        {
            When(m => m.Errors != null, () =>
                RuleFor(m => m.Errors)
                    .Must(errors => !errors.Any())                    
                    .WithState(input =>
                    {
                        var errorType = input.Errors.First().Type;

                        if (_codeTypes.ContainsKey(errorType))
                            return _codeTypes[errorType];

                        return "RuntimeProblem";
                    })
                    .WithMessage(input =>
                    {
                        var errorType = input.Errors.First().Type;

                        if (_messages.ContainsKey(errorType))
                            return _messages[errorType];

                        return input.Errors.First().Value;
                    }));
        }        
    }
}
