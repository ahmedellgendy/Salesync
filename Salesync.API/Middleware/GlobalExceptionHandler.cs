using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Salesync.API.Responses;
using System.Net;

namespace Salesync.API.Middleware
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError(exception, exception.Message);

            ApiResponse<object> response;
            int statusCode;

            switch (exception)
            {
                // Handle validation exceptions
                case ValidationException validationException:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    response = ApiResponse<object>.ValidationErrorResponse(
                        validationException.Errors
                        .Select(e => e.ErrorMessage)
                        .Distinct()
                        .ToList(),
                        "Validation Error"
                        );
                    break;

                // Handle not found exceptions
                case KeyNotFoundException:
                    statusCode = (int)HttpStatusCode.NotFound;
                    response = ApiResponse<object>.NotFoundResponse(
                        exception.Message
                        );
                    break;

                // handle bad request exceptions
                case ArgumentException:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    response = ApiResponse<object>.ErrorResponse(
                        exception.Message,
                        statusCode
                        );
                    break;

                // unauthorized access exceptions
                case UnauthorizedAccessException:
                    statusCode = (int)HttpStatusCode.Unauthorized;
                    response = ApiResponse<object>.ErrorResponse(
                        exception.Message,
                        statusCode
                        );
                    break;

                    // internal server errors   
                    default:
                    statusCode = (int)HttpStatusCode.InternalServerError;
                    response = ApiResponse<object>.ErrorResponse(
                        "Internal Server Error.",
                        statusCode
                        );
                    break;
            }

            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = "application/json";

            await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

            return true;
        }
    }
}