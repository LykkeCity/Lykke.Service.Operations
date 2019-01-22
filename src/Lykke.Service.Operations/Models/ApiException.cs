using System;
using System.Net;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Lykke.Service.Operations.Models
{
    public class ApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public ApiResult Result { get; }

        public ApiException(HttpStatusCode statusCode, string field, string message)
        {
            StatusCode = statusCode;
            Result = new ApiResult(field, message);
        }

        public ApiException(HttpStatusCode statusCode, ModelStateDictionary modelErrors)
        {
            StatusCode = statusCode;
            Result = new ApiResult(modelErrors);
        }
    }
}
