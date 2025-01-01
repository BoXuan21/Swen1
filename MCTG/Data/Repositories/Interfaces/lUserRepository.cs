namespace MCTG;

public interface IUserRepository
{
    User GetByUsername(string username);
    void Add(User user);
    bool ValidateCredentials(string username, string password);
    string GenerateToken(string username);
    void Update(User user);  // Add this method
        
    IEnumerable<User> GetAllUsers();  // Add this
}