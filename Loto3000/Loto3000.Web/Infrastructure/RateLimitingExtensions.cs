using System.IdentityModel.Tokens.Jwt;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Loto3000.Web.Infrastructure;

/// <summary>Limits credential attempts by IP and writes by authenticated subject within this process.</summary>
internal static class RateLimitingExtensions
{
    /// <summary>Registers fixed-window policies with no queue and problem-JSON rejections.</summary>
    /// <param name="services">The application's service collection.</param>
    /// <returns>The same collection for chained registrations.</returns>
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            options.AddPolicy("writes", context => RateLimitPartition.GetFixedWindowLimiter(
                context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = "60";
                await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too many requests",
                    Detail = "Please wait a minute before trying again."
                }, options: null, contentType: "application/problem+json", cancellationToken: cancellationToken);
            };
        });
        return services;
    }
}
