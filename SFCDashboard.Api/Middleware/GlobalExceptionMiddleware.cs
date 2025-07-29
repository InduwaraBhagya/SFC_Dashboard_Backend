using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace SFCDashboard.Middleware
{
    /// <summary>
    /// Global exception handling middleware for production-ready error responses
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            _logger.LogError(exception, "An unhandled exception occurred");

            context.Response.ContentType = "application/json";

            var response = new ProblemDetails();

            switch (exception)
            {
                case ArgumentException _:
                    response.Status = (int)HttpStatusCode.BadRequest;
                    response.Title = "Bad Request";
                    break;
                case UnauthorizedAccessException _:
                    response.Status = (int)HttpStatusCode.Unauthorized;
                    response.Title = "Unauthorized";
                    break;
                case NotImplementedException _:
                    response.Status = (int)HttpStatusCode.NotImplemented;
                    response.Title = "Not Implemented";
                    break;
                case KeyNotFoundException _:
                    response.Status = (int)HttpStatusCode.NotFound;
                    response.Title = "Not Found";
                    break;
                case TimeoutException _:
                    response.Status = (int)HttpStatusCode.RequestTimeout;
                    response.Title = "Request Timeout";
                    break;
                default:
                    response.Status = (int)HttpStatusCode.InternalServerError;
                    response.Title = "Internal Server Error";
                    break;
            }

            // Only include detailed error information in development
            if (_env.IsDevelopment())
            {
                response.Detail = exception.Message;
                response.Extensions["stackTrace"] = exception.StackTrace;
            }
            else
            {
                response.Detail = "An error occurred while processing your request.";
            }

            response.Instance = context.Request.Path;
            response.Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";

            context.Response.StatusCode = response.Status.Value;

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }
}

