using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SmartParkingSystem.Application.Common.Exceptions;

namespace SmartParkingSystem.Api.Middleware;

/// <summary>
/// Result filter that guarantees any action returning an HTTP 4xx or 5xx ObjectResult
/// conforms to the standardized ErrorResponse JSON contract { success: false, message, errors: [] }.
/// </summary>
public class StandardErrorResultFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult objectResult && objectResult.StatusCode >= StatusCodes.Status400BadRequest)
        {
            if (objectResult.Value is not ErrorResponse)
            {
                string message = "An error occurred while processing your request.";
                var errors = new List<string>();

                if (objectResult.Value is string strVal)
                {
                    message = strVal;
                }
                else if (objectResult.Value != null)
                {
                    var type = objectResult.Value.GetType();
                    var propMessage = type.GetProperty("Message") ?? type.GetProperty("message");
                    if (propMessage != null)
                    {
                        var val = propMessage.GetValue(objectResult.Value)?.ToString();
                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            message = val;
                        }
                    }

                    var propErrors = type.GetProperty("Errors") ?? type.GetProperty("errors");
                    if (propErrors != null)
                    {
                        if (propErrors.GetValue(objectResult.Value) is IEnumerable<string> errList)
                        {
                            errors.AddRange(errList);
                        }
                    }
                }

                objectResult.Value = new ErrorResponse
                {
                    Success = false,
                    Message = message,
                    Errors = errors
                };
            }
        }

        await next();
    }
}
