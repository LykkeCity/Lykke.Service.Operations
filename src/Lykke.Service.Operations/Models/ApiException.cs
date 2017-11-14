using System;
using System.Net;

namespace Lykke.Service.Operations.Models
{
    public class ApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public ApiResult Result { get; }

        public ApiException(HttpStatusCode statusCode, ApiResult result)
        {
            StatusCode = statusCode;
            Result = result;
        }
    }
}
