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
        {    // Authorization Header existiert
            if (context.Request.TryGetHeader("Authorization", out var authorizationHeader))
            {     
                // Extrahiert Token aus "Bearer token"
                var token = authorizationHeader?.Split(" ").Last();
                Console.WriteLine($"Processing token: {token}");

                    
                // 2 way validierung
                if (token != null)
                {
                    string username;
            
                    if (token.EndsWith("-mtcgToken")) //direkte prüfung
                    {
                        username = token.Replace("-mtcgToken", "");
                        Console.WriteLine($"Simple token format detected. Username: {username}");
                    }
                    else
                    {  // Oder über den JwtService
                        username = _jwtService.ValidateToken(token);
                        Console.WriteLine($"JWT validation result. Username: {username}");
                    }

                    if (username != null)
                    { 
                        // Speichert Username im Context für spätere Handler
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