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
            if (string.IsNullOrEmpty(username))
            {
                Console.WriteLine("GetByUsername called with null or empty username");
                return null;
            }

            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            Console.WriteLine($"Looking up user with username: '{username}'");

            try
            {
                using var cmd = new NpgsqlCommand(
                    "SELECT * FROM users WHERE username = @username", 
                    connection);
                cmd.Parameters.AddWithValue("@username", username);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    var user = new User
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        Username = reader.GetString(reader.GetOrdinal("username")),
                        Password = reader.GetString(reader.GetOrdinal("password")),
                        Coins = reader.GetInt32(reader.GetOrdinal("coins")),
                        Elo = reader.GetInt32(reader.GetOrdinal("elo"))
                    };
                    Console.WriteLine($"Database lookup result: Found user {user.Username}");
                    return user;
                }

                Console.WriteLine("Database lookup result: User not found");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetByUsername: {ex.Message}");
                throw;
            }
        }

        public void Add(User user)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                connection.Open();
                using var cmd = new NpgsqlCommand(
                    "INSERT INTO users (username, password, coins, elo) VALUES (@Username, @Password, @Coins, @Elo)",
                    connection);

                cmd.Parameters.AddWithValue("@Username", user.Username);
                cmd.Parameters.AddWithValue("@Password", user.Password);
                cmd.Parameters.AddWithValue("@Coins", user.Coins);
                cmd.Parameters.AddWithValue("@Elo", user.Elo);

                cmd.ExecuteNonQuery();
                Console.WriteLine($"Successfully added user: {user.Username}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Add: {ex.Message}");
                throw;
            }
        }

        public void Update(User user)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                connection.Open();
                using var cmd = new NpgsqlCommand(@"
                    UPDATE users 
                    SET coins = @Coins, 
                        elo = @Elo,
                        password = @Password
                    WHERE id = @Id",
                    connection);

                cmd.Parameters.AddWithValue("@Id", user.Id);
                cmd.Parameters.AddWithValue("@Coins", user.Coins);
                cmd.Parameters.AddWithValue("@Elo", user.Elo);
                cmd.Parameters.AddWithValue("@Password", user.Password);

                cmd.ExecuteNonQuery();
                Console.WriteLine($"Successfully updated user: {user.Username}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Update: {ex.Message}");
                throw;
            }
        }

        public bool ValidateCredentials(string username, string password)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                connection.Open();
                using var cmd = new NpgsqlCommand(
                    "SELECT COUNT(*) FROM users WHERE username = @username AND password = @password",
                    connection);

                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@password", password);

                int count = Convert.ToInt32(cmd.ExecuteScalar());
                bool isValid = count > 0;
                Console.WriteLine($"ValidateCredentials: {isValid} for username: {username}");
                return isValid;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ValidateCredentials: {ex.Message}");
                return false;
            }
        }

        public IEnumerable<User> GetAllUsers()
        {
            var users = new List<User>();
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                connection.Open();
                using var cmd = new NpgsqlCommand(
                    "SELECT id, username, password, coins, elo FROM users ORDER BY elo DESC",
                    connection);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    users.Add(new User
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        Username = reader.GetString(reader.GetOrdinal("username")),
                        Password = reader.GetString(reader.GetOrdinal("password")),
                        Coins = reader.GetInt32(reader.GetOrdinal("coins")),
                        Elo = reader.GetInt32(reader.GetOrdinal("elo"))
                    });
                }
                return users;
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
                connection.Open();
                using var cmd = new NpgsqlCommand(@"
                    SELECT user_id, name, bio, image 
                    FROM user_profiles 
                    WHERE user_id = @UserId",
                    connection);

                cmd.Parameters.AddWithValue("@UserId", userId);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    var profile = new UserProfile
                    {
                        UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                        Name = reader.GetString(reader.GetOrdinal("name")),
                        Bio = reader.GetString(reader.GetOrdinal("bio")),
                        Image = reader.GetString(reader.GetOrdinal("image"))
                    };
                    Console.WriteLine($"Retrieved profile for user ID: {userId}");
                    return profile;
                }
                return null;
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
                connection.Open();
                using var cmd = new NpgsqlCommand(@"
                    INSERT INTO user_profiles (user_id, name, bio, image)
                    VALUES (@UserId, @Name, @Bio, @Image)
                    ON CONFLICT (user_id) 
                    DO UPDATE SET 
                        name = @Name,
                        bio = @Bio,
                        image = @Image",
                    connection);

                cmd.Parameters.AddWithValue("@UserId", profile.UserId);
                cmd.Parameters.AddWithValue("@Name", profile.Name);
                cmd.Parameters.AddWithValue("@Bio", profile.Bio);
                cmd.Parameters.AddWithValue("@Image", profile.Image);

                cmd.ExecuteNonQuery();
                Console.WriteLine($"Updated profile for user ID: {profile.UserId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateProfile: {ex.Message}");
                throw;
            }
        }

        public void DeleteUser(int userId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                connection.Open();
                using var cmd = new NpgsqlCommand(
                    "DELETE FROM users WHERE id = @Id",
                    connection);

                cmd.Parameters.AddWithValue("@Id", userId);
                cmd.ExecuteNonQuery();
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
                connection.Open();
                using var cmd = new NpgsqlCommand(
                    "SELECT EXISTS(SELECT 1 FROM users WHERE username = @username)",
                    connection);

                cmd.Parameters.AddWithValue("@username", username);
                bool exists = (bool)cmd.ExecuteScalar();
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
                connection.Open();
                using var cmd = new NpgsqlCommand(
                    "SELECT COUNT(*) FROM users",
                    connection);

                return Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUserCount: {ex.Message}");
                return 0;
            }
        }

        
    }
}