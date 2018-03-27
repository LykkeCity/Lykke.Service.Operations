using System;
using System.Linq;
using FluentValidation;
using Lykke.Workflow;
using Newtonsoft.Json.Linq;

namespace Lykke.Service.Operations.Workflow.Activities
{
    public class ValidationActivity<TInput> : ActivityBase<TInput, object, ValidationResults> where TInput : class
    {
        private readonly IValidator<TInput> m_Validator;

        public ValidationActivity(IValidator<TInput> validator)
        {
            m_Validator = validator ?? throw new ArgumentNullException("validator");
        }

        public override ActivityResult Execute(Guid activityExecutionId, TInput input, Action<object> processOutput, Action<ValidationResults> processFailOutput)
        {
            var result = m_Validator.Validate<TInput>(input);
            if (!result.IsValid)
            {
                var validationErrors = result.Errors.Select(e => new ValidationError { PropertyName = e.PropertyName, ErrorMessage = e.ErrorMessage }).ToArray();
                processFailOutput(new ValidationResults
                {
                    ErrorMessage = string.Empty,
                    ValidationErrors = validationErrors
                });

                return ActivityResult.Failed;
            }

            processOutput(null);

            return ActivityResult.Succeeded;
        }

        public override string ToString()
        {
            return string.Format("{0} validation", typeof(TInput).Name);
        }
    }

    public class ValidationError
    {
        public string PropertyName { get; set; }
        public string ErrorMessage { get; set; }
    }
}
