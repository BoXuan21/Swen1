using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Npgsql;

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
            connection.Open();
            Console.WriteLine($"Looking up user with username: {username}");
    
            var user = connection.QuerySingleOrDefault<User>(
                "SELECT * FROM users WHERE username = @username",
                new { username });
        
            Console.WriteLine($"Database lookup result: {(user != null ? "User found" : "User not found")}");
            return user;
        }

        public void Add(User user)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                var userDto = user.ToDto();
                connection.Execute(@"
                    INSERT INTO users (username, password, coins, elo) 
                    VALUES (@Username, @Password, @Coins, @Elo)",
                    userDto);
                
                Console.WriteLine($"Successfully added user: {user.Username}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Add: {ex.Message}");
                throw;
            }
        }

        public bool ValidateCredentials(string username, string password)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                var userDto = connection.QuerySingleOrDefault<UserDto>(
                    "SELECT * FROM users WHERE username = @username AND password = @password",
                    new { username, password });

                bool isValid = userDto != null;
                Console.WriteLine($"ValidateCredentials: {isValid} for username: {username}");
                return isValid;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ValidateCredentials: {ex.Message}");
                return false;
            }
        }

        public void Update(User user)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                var userDto = user.ToDto();
                connection.Execute(@"
                    UPDATE users 
                    SET coins = @Coins, 
                        elo = @Elo,
                        password = @Password
                    WHERE id = @Id",
                    userDto);
                
                Console.WriteLine($"Successfully updated user: {user.Username}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Update: {ex.Message}");
                throw;
            }
        }

        public IEnumerable<User> GetAllUsers()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                var userDtos = connection.Query<UserDto>(@"
                    SELECT id, username, password, coins, elo 
                    FROM users 
                    ORDER BY elo DESC");

                return userDtos.Select(dto => User.FromDto(dto));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllUsers: {ex.Message}");
                return Enumerable.Empty<User>();
            }
        }

        public UserProfile GetUserProfile(int userId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                var profile = connection.QuerySingleOrDefault<UserProfile>(@"
                    SELECT user_id as UserId, name, bio, image 
                    FROM user_profiles 
                    WHERE user_id = @UserId",
                    new { UserId = userId });

                Console.WriteLine($"Retrieved profile for user ID: {userId}");
                return profile;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUserProfile: {ex.Message}");
                return null;
            }
        }

        public void UpdateProfile(UserProfile profile)
        {
            try
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

                Console.WriteLine($"Updated profile for user ID: {profile.UserId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateProfile: {ex.Message}");
                throw;
            }
        }

        public string GenerateToken(string username)
        {
            try
            {
                string token = $"{username}-mtcgToken";
                _tokens[username] = token;
                Console.WriteLine($"Generated token for user: {username}");
                return token;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GenerateToken: {ex.Message}");
                throw;
            }
        }

        public void DeleteUser(int userId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                connection.Execute("DELETE FROM users WHERE id = @Id", new { Id = userId });
                Console.WriteLine($"Deleted user with ID: {userId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteUser: {ex.Message}");
                throw;
            }
        }

        public bool UserExists(string username)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                var exists = connection.ExecuteScalar<bool>(
                    "SELECT EXISTS(SELECT 1 FROM users WHERE username = @username)",
                    new { username });

                Console.WriteLine($"Checked existence for username {username}: {exists}");
                return exists;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UserExists: {ex.Message}");
                return false;
            }
        }

        public int GetUserCount()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                return connection.ExecuteScalar<int>("SELECT COUNT(*) FROM users");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUserCount: {ex.Message}");
                return 0;
            }
        }
    }
}