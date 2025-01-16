namespace MCTG
{
    public class JwtMiddleware
    {
        private readonly IJwtService _jwtService;

        public JwtMiddleware(IJwtService jwtService)
        {
            _jwtService = jwtService;
        }

        public bool Invoke(CustomHttpContext context)
        {
            if (context.Request.TryGetHeader("Authorization", out var authorizationHeader))
            {
                var token = authorizationHeader?.Split(" ").Last();
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
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("Token validation failed");
                        context.Response.StatusCode = 401;
                        context.Response.StatusDescription = "Unauthorized";
                        context.Response.Body = "Invalid token";
                        return false;
                    }
                }
            }
            
            Console.WriteLine("No Authorization header found");
            context.Response.StatusCode = 401;
            context.Response.StatusDescription = "Unauthorized";
            context.Response.Body = "Authorization header missing";
            return false;
        }
    }
}