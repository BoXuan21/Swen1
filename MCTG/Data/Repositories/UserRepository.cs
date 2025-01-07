using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dapper; // For Dapper methods like QuerySingleOrDefault and Execute
using Npgsql; // For NpgsqlConnection

namespace MCTG
{
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;
        private readonly Dictionary<string, string> _tokens = new Dictionary<string, string>();

        public UserRepository(string connectionString)
        {
            _connectionString = connectionString;
        }
        
        public User GetByUsername(string username)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return connection.QuerySingleOrDefault<User>("SELECT * FROM users WHERE username = @username",
                new { username });
        }

        public void Add(User user)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Execute(
                "INSERT INTO users (username, password, coins, elo) VALUES (@Username, @Password, @Coins, @Elo)", 
                user);
        }

        public bool ValidateCredentials(string username, string password)
        {
            var user = GetByUsername(username);
            return user != null && user.Password == password;
        }

        public string GenerateToken(string username)
        {
            string token = $"{username}-mtcgToken";
            _tokens[username] = token;
            return token;
        }
        
        public void Update(User user)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Execute(@"
        UPDATE users 
        SET coins = @Coins, elo = @Elo 
        WHERE id = @Id",
                user);
        }
        
        public IEnumerable<User> GetAllUsers()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return connection.Query<User>(@"
        SELECT id, username, password, coins, elo 
        FROM users 
        ORDER BY elo DESC");
        }
        
        public UserProfile GetUserProfile(int userId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return connection.QuerySingleOrDefault<UserProfile>(@"
            SELECT user_id as UserId, name, bio, image 
            FROM user_profiles 
            WHERE user_id = @UserId",
                new { UserId = userId });
        } 
        public void UpdateProfile(UserProfile profile)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Execute(@"
            INSERT INTO user_profiles (user_id, name, bio, image)
            VALUES (@UserId, @Name, @Bio, @Image)
            ON CONFLICT (user_id) 
            DO UPDATE SET 
                name = @Name,
                bio = @Bio,
                image = @Image",
                profile);
        }
        
        
    }
}