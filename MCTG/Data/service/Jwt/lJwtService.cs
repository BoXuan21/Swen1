namespace MCTG;

public interface IJwtService
{
    string GenerateToken(string username);
    string ValidateToken(string token);
}