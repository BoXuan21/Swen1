using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace MCTG
{
    public class JwtService : IJwtService
    {
        private readonly string _secretKey;  // Fixed the asterisk to underscore

        public JwtService(string secretKey)
        {
            if (secretKey.Length < 16)
            {
                throw new ArgumentException("Secret key must be at least 16 characters long.");
            }
            _secretKey = secretKey;  // Fixed the asterisk to underscore
        }

        public string GenerateToken(string username)
        {
            return $"{username}-mtcgToken";
        }

        public string ValidateToken(string token)
        {
            try
            {
                // If it's in the simple format (username-mtcgToken)
                if (token.EndsWith("-mtcgToken"))
                {
                    return token.Replace("-mtcgToken", "");
                }

                // Fallback to JWT validation if needed
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_secretKey);
                
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                return jwtToken.Claims.First(x => x.Type == ClaimTypes.Name).Value;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token validation error: {ex.Message}");
                return null;
            }
        }
    }
}