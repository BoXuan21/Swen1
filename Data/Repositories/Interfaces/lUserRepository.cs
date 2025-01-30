namespace MCTG
{
    public interface IUserRepository
    {
        User GetByUsername(string username);
        void Add(User user);
        bool ValidateCredentials(string username, string password);
        void Update(User user);
        IEnumerable<User> GetAllUsers();
        UserProfile GetUserProfile(int userId);
        void UpdateProfile(UserProfile profile);
    }
}