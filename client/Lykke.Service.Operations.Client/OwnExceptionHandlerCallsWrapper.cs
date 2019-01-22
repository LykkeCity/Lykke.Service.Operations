using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Lykke.HttpClientGenerator.Infrastructure;
using Microsoft.Rest;
using ApiException = Refit.ApiException;

namespace Lykke.Service.Operations.Client
{
    public class OwnExceptionHandlerCallsWrapper : ICallsWrapper
    {
        /// <inheritdoc />
        public async Task<object> HandleMethodCall(MethodInfo targetMethod, object[] args, Func<Task<object>> innerHandler)
        {
            try
            {
                return await innerHandler();
            }
            catch (ApiException ex)
            {
                var errResponse = ex.GetContentAs<Dictionary<string, string[]>>();
                if (errResponse != null)
                {
                    throw new HttpOperationException
                    {
                        Request = new HttpRequestMessageWrapper(ex.RequestMessage, ex.RequestMessage.Content.ToString()),
                        Response = new HttpResponseMessageWrapper(new HttpResponseMessage(ex.StatusCode), ex.Content),
                    };
                }

                throw;
            }
        }
    }
}
