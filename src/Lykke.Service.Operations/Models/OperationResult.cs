using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Lykke.Service.Operations.Models
{
    public class OperationResult : Dictionary<string, string[]>
    {
        public OperationResult(string field, string message)
        {
            this[field] = new [] { message };
        }

        public OperationResult(ModelStateDictionary modelState)
        {
            foreach (var key in modelState.Keys)
            {
                if (modelState[key].Errors.Any())
                    this[key] = modelState[key].Errors.Select(e => e.ErrorMessage).ToArray();
            }            
        }        
    }
}
