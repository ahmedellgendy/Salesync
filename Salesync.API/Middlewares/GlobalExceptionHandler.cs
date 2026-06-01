using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
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
                // Handle validation exceptions - 400 Bad Request
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

                // Handle not found exceptions - 404 Not Found
                case KeyNotFoundException:
                    statusCode = (int)HttpStatusCode.NotFound;
                    response = ApiResponse<object>.NotFoundResponse(
                        exception.Message
                        );
                    break;

                // handle bad request exceptions - 400 Bad Request
                case ArgumentException:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    response = ApiResponse<object>.ErrorResponse(
                        exception.Message,
                        statusCode
                        );
                    break;

                // unauthorized access exceptions - 401 Unauthorized
                case UnauthorizedAccessException:
                    statusCode = (int)HttpStatusCode.Unauthorized;
                    response = ApiResponse<object>.ErrorResponse(
                        exception.Message,
                        statusCode
                        );
                    break;

                // handle conflict exceptions - 409 Conflict
                case InvalidOperationException:
                    statusCode = (int)HttpStatusCode.Conflict; 
                    response = ApiResponse<object>.ErrorResponse(
                        exception.Message,
                        statusCode
                    );
                    break;

                // internal server errors - 500 Internal Server Error    
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