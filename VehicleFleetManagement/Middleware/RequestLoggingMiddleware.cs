using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace VehicleFleetManagement.Middleware
{
    /// <summary>
    /// Middleware that logs incoming HTTP requests, response status codes,
    /// and elapsed execution duration. Satisfies INT 402 middleware requirement.
    /// </summary>
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var method = context.Request.Method;
            var path = context.Request.Path;

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                var statusCode = context.Response.StatusCode;
                var elapsedMs = stopwatch.ElapsedMilliseconds;

                // Log performance metric and request lifecycle
                _logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {Elapsed}ms",
                    method, path, statusCode, elapsedMs);
            }
        }
    }
}
