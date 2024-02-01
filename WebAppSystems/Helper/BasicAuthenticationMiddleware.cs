using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace WebAppSystems.Helper
{
    public class BasicAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly BasicAuthenticationFilterAttribute _authenticationFilter;

        public BasicAuthenticationMiddleware(RequestDelegate next, BasicAuthenticationFilterAttribute authenticationFilter)
        {
            _next = next;
            _authenticationFilter = authenticationFilter;
        }

        public async Task Invoke(HttpContext context)
        {
            if (IsApiRequest(context.Request))
            {
                if (!_authenticationFilter.IsUserAuthenticated(context))
                {
                    context.Response.Headers["WWW-Authenticate"] = "Basic";
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized");
                    return;
                }
            }

            await _next(context);
        }

        private bool IsApiRequest(HttpRequest request)
        {
            return request.Path.HasValue && request.Path.Value.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);
        }
    }
}
