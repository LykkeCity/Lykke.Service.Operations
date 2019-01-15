using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Lykke.Common.Api.Contract.Responses;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Lykke.Service.Operations.Models
{
    public class ApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public ErrorResponse Result { get; }

        public ApiException(HttpStatusCode statusCode, string field, string message)
        {
            StatusCode = statusCode;
            Result = new ErrorResponse
            {
                ErrorMessage = message,
                ModelErrors = new Dictionary<string, List<string>>
                {
                    { field, new List<string> { message } }
                }
            };
        }

        public ApiException(HttpStatusCode statusCode, ModelStateDictionary modelErrors)
        {
            StatusCode = statusCode;
            Result = new ErrorResponse
            {
                ModelErrors = new Dictionary<string, List<string>>(),
            };
            foreach (var pair in modelErrors)
            {
                if (pair.Value.ValidationState != ModelValidationState.Invalid)
                    continue;

                Result.ModelErrors.Add(pair.Key, pair.Value.Errors.Select(i => i.ErrorMessage).ToList());
            }
        }
    }
}
