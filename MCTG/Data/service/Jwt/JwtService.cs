namespace MCTG;
    public class JwtService : IJwtService
    {
        private readonly string _secretKey;

        public JwtService(string secretKey)
        {
            if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 16)
            {
                throw new ArgumentException("Secret key must be at least 16 characters long.");
            }
            _secretKey = secretKey;
        }

        public string GenerateToken(string username)
        {
            // For simplicity, we'll use the basic token format as specified
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

                // For a more secure implementation, you could add additional validation here
                // For example, checking token expiration, signature validation, etc.
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token validation error: {ex.Message}");
                return null;
            }
        }
    }