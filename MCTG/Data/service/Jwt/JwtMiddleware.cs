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
                Console.WriteLine($"Processing token: {token}");

                if (token != null)
                {
                    string username;
            
                    if (token.EndsWith("-mtcgToken"))
                    {
                        username = token.Replace("-mtcgToken", "");
                        Console.WriteLine($"Simple token format detected. Username: {username}");
                    }
                    else
                    {
                        username = _jwtService.ValidateToken(token);
                        Console.WriteLine($"JWT validation result. Username: {username}");
                    }

                    if (username != null)
                    {
                        context.Items["Username"] = username;
                        Console.WriteLine($"Username {username} attached to context");
                    }
                    else
                    {
                        Console.WriteLine("Token validation failed");
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("Invalid token");
                        return;
                    }
                }
            }
            else
            {
                Console.WriteLine("No Authorization header found");
            }

            if (_next != null)
            {
                await _next(context);
            }
        }
    }
}