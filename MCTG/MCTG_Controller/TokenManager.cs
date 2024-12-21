namespace MCTG;

public class TokenManager
{
    private readonly Dictionary<string, string> _tokens; // username -> token
    private readonly DatabaseController _db;

    public TokenManager()
    {
        _tokens = new Dictionary<string, string>();
        _db = new DatabaseController();
    }

    public string CreateToken(string username)
    {
        string token = $"{username}-mtcgToken";
        _tokens[username] = token;
        return token;
    }

    public bool ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token)) return false;
        return _tokens.ContainsValue(token);
    }

    public string GetUsernameFromToken(string token)
    {
        if (string.IsNullOrEmpty(token)) return null;
        return _tokens.FirstOrDefault(x => x.Value == token).Key;
    }
}