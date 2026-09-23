namespace Loto3000.Web.Middleware;

/// <summary>Applies browser security policies to successful and error responses.</summary>
/// <param name="next">The remaining HTTP pipeline.</param>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    /// <summary>Registers final response headers before forwarding the request.</summary>
    /// <param name="context">The current HTTP exchange.</param>
    /// <returns>The downstream pipeline task.</returns>
    public Task InvokeAsync(HttpContext context)
    {
        // Delay header assignment until the final response, including responses cleared by error handling.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.XContentTypeOptions = "nosniff";
            context.Response.Headers.XFrameOptions = "DENY";
            context.Response.Headers["Referrer-Policy"] = "same-origin";
            context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            var inlinePolicy = context.Request.Path.StartsWithSegments("/swagger") ? " 'unsafe-inline'" : string.Empty;
            context.Response.Headers.ContentSecurityPolicy =
                $"default-src 'self'; script-src 'self'{inlinePolicy}; style-src 'self'{inlinePolicy}; img-src 'self' data:; font-src 'self'; connect-src 'self'; base-uri 'none'; object-src 'none'; frame-ancestors 'none'; form-action 'self'";
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.Headers.CacheControl = "no-store";
            }

            return Task.CompletedTask;
        });

        return next(context);
    }
}
