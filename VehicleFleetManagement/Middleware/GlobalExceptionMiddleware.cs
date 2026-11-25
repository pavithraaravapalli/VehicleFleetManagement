using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace VehicleFleetManagement.Middleware
{
    /// <summary>
    /// Global exception handler that captures uncaught exceptions.
    /// Differentiates between Web API calls (returning clean JSON error contracts)
    /// and MVC browser requests (redirecting to friendly error views).
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger,
            IWebHostEnvironment env)
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
                _logger.LogError(ex, "Unhandled exception captured by GlobalExceptionMiddleware: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var isApiRequest = context.Request.Path.StartsWithSegments("/api") ||
                               (context.Request.Headers.Accept.ToString().Contains("application/json") &&
                               !context.Request.Headers.Accept.ToString().Contains("text/html"));

            if (isApiRequest)
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var apiResponse = new
                {
                    success = false,
                    statusCode = context.Response.StatusCode,
                    message = "An unexpected internal server error occurred.",
                    detail = _env.IsDevelopment() ? exception.Message : null,
                    traceId = context.TraceIdentifier
                };

                var json = JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await context.Response.WriteAsync(json);
            }
            else
            {
                // For Razor MVC browser navigation, redirect to user-friendly Error view
                context.Response.Redirect("/Home/Error");
            }
        }
    }
}
