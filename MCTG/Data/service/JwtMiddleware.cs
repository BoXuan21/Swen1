using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MCTG
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IJwtService _jwtService;

        public JwtMiddleware(RequestDelegate next, IJwtService jwtService)
        {
            _next = next;
            _jwtService = jwtService;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
            {
                var token = authorizationHeader.FirstOrDefault()?.Split(" ").Last();

                if (token != null)
                {
                    var username = _jwtService.ValidateToken(token);

                    if (username != null)
                    {
                        // Attach the username to the HttpContext for further use
                        context.Items["Username"] = username;
                    }
                    else
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("Invalid token");
                        return;
                    }
                }
            }

            if (_next != null)
            {
                await _next(context);
            }
        }
    }
}