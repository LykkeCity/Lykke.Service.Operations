using System;
using System.IO;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Common.ApiLibrary.Middleware;
using Lykke.Common.Log;
using Lykke.Service.Operations.Models;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Lykke.Service.Operations.Middleware
{
    public class GlobalErrorHandlerMiddleware
    {
        private readonly ILog _log;
        private readonly string _componentName;
        private readonly CreateErrorResponse _createErrorResponse;
        private readonly RequestDelegate _next;

        public GlobalErrorHandlerMiddleware(RequestDelegate next, ILogFactory log, string componentName, CreateErrorResponse createErrorResponse)
        {
            _log = log.CreateLog(this) ?? throw new ArgumentNullException(nameof(log));
            _componentName = componentName ?? throw new ArgumentNullException(nameof(componentName));
            _createErrorResponse = createErrorResponse ?? throw new ArgumentNullException(nameof(createErrorResponse));
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next.Invoke(context);
            }
            catch (ApiException ex)
            {
                await LogWarning(context, ex);
                await CreateApiErrorResponse(context, ex);
            }
            catch (Exception ex)
            {
                await LogError(context, ex);
                await CreateErrorResponse(context, ex);
            }
        }

        private async Task LogWarning(HttpContext httpContext, ApiException ex)
        {
            var context = await GetBody(httpContext.Request);
            await _log.WriteWarningAsync(_componentName, httpContext.Request.GetUri().AbsoluteUri, context, ex.Result.ToJson());
        }

        private async Task LogError(HttpContext httpContext, Exception ex)
        {
            var context = await GetBody(httpContext.Request);
            await _log.WriteErrorAsync(_componentName, httpContext.Request.GetUri().AbsoluteUri, context, ex);
        }

        private static async Task<string> GetBody(HttpRequest request)
        {
            if (request.ContentLength != null && request.ContentLength > 0 && request.Body.CanSeek)
            {
                request.Body.Seek(0, SeekOrigin.Begin);
                using (var reader = new StreamReader(request.Body))
                {
                    return await reader.ReadToEndAsync();
                }
            }
            return String.Empty;
        }

        private async Task CreateApiErrorResponse(HttpContext ctx, ApiException ex)
        {
            ctx.Response.ContentType = "application/json";
            ctx.Response.StatusCode = (int)ex.StatusCode;

            var responseJson = JsonConvert.SerializeObject(ex.Result);

            await ctx.Response.WriteAsync(responseJson);
        }

        private async Task CreateErrorResponse(HttpContext ctx, Exception ex)
        {
            ctx.Response.ContentType = "application/json";
            ctx.Response.StatusCode = 500;

            var response = _createErrorResponse(ex);
            var responseJson = JsonConvert.SerializeObject(response);

            await ctx.Response.WriteAsync(responseJson);
        }
    }
}
