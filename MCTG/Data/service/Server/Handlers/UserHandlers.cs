using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace MCTG
{
    public partial class TcpServer
    {
        public async Task HandleRegistrationAsync(Stream stream, string body)
        {
            try
            {
                Console.WriteLine($"Attempting registration with body: {body}");
        
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
        
                var user = JsonSerializer.Deserialize<User>(body, options);
                Console.WriteLine($"Deserialized user: {user?.Username}");
        
                if (user == null || string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Password))
                {
                    Console.WriteLine("Invalid user data");
                    await SendResponseAsync(stream, "HTTP/1.1 400 Bad Request", "Invalid user data");
                    return;
                }

                user.Coins = 20;
                user.Elo = 100;

                _userRepository.Add(user);
                Console.WriteLine($"User {user.Username} registered successfully");

                string token = _jwtService.GenerateToken(user.Username);
                await SendResponseAsync(stream, "HTTP/1.1 201 Created", token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Registration error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await SendResponseAsync(stream, "HTTP/1.1 400 Bad Request", $"Registration failed: {ex.Message}");
            }
        }

        public async Task HandleLoginAsync(Stream stream, string body)
        {
            try
            {
                Console.WriteLine($"Login attempt with body: {body}");
        
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
        
                var credentials = JsonSerializer.Deserialize<User>(body, options);
        
                if (credentials == null || string.IsNullOrEmpty(credentials.Username) || string.IsNullOrEmpty(credentials.Password))
                {
                    Console.WriteLine("Invalid credentials format - null or empty values");
                    await SendResponseAsync(stream, "HTTP/1.1 400 Bad Request", "Invalid credentials format");
                    return;
                }
        
                Console.WriteLine($"Attempting login for username: {credentials.Username}");
                Console.WriteLine($"Password length: {credentials.Password?.Length ?? 0}");
        
                bool isValid = _userRepository.ValidateCredentials(credentials.Username, credentials.Password);
                Console.WriteLine($"Credentials valid: {isValid}");
        
                if (isValid)
                {
                    string token = _jwtService.GenerateToken(credentials.Username);
                    await SendResponseAsync(stream, "HTTP/1.1 200 OK", token);
                }
                else
                {
                    await SendResponseAsync(stream, "HTTP/1.1 401 Unauthorized", "Invalid credentials");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await SendResponseAsync(stream, "HTTP/1.1 400 Bad Request", $"Login failed: {ex.Message}");
            }
        }

        public async Task HandleGetProfileAsync(Stream stream, string username, HttpContext context)
        {
            try 
            {
                var requestingUser = context.Items["Username"] as string;
                Console.WriteLine($"Request to view profile of {username} by user {requestingUser}");

                if (string.IsNullOrEmpty(requestingUser))
                {
                    Console.WriteLine("No username found in context");
                    await SendResponseAsync(stream, "HTTP/1.1 401 Unauthorized", "Authentication required");
                    return;
                }

                var user = _userRepository.GetByUsername(username);
                if (user == null)
                {
                    Console.WriteLine($"User not found: {username}");
                    await SendResponseAsync(stream, "HTTP/1.1 404 Not Found", "User not found");
                    return;
                }

                if (requestingUser != username)
                {
                    Console.WriteLine($"Access denied: {requestingUser} tried to view {username}'s profile");
                    await SendResponseAsync(stream, "HTTP/1.1 403 Forbidden", "Access denied");
                    return;
                }

                var profile = _userRepository.GetUserProfile(user.Id);
                Console.WriteLine($"Retrieved profile for user {username}");
                var response = JsonSerializer.Serialize(profile);
                await SendResponseAsync(stream, "HTTP/1.1 200 OK", response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting profile: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await SendResponseAsync(stream, "HTTP/1.1 500 Internal Server Error", 
                    "An error occurred while retrieving the profile");
            }
        }

        public async Task HandleUpdateProfileAsync(Stream stream, string username, string body, HttpContext context)
        {
            try
            {
                var requestingUser = context.Items["Username"] as string;
                Console.WriteLine($"Request to update profile for {username} by user {requestingUser}");

                if (string.IsNullOrEmpty(requestingUser))
                {
                    Console.WriteLine("No username found in context");
                    await SendResponseAsync(stream, "HTTP/1.1 401 Unauthorized", "Authentication required");
                    return;
                }

                var user = _userRepository.GetByUsername(username);
                if (user == null)
                {
                    Console.WriteLine($"User not found: {username}");
                    await SendResponseAsync(stream, "HTTP/1.1 404 Not Found", "User not found");
                    return;
                }

                if (requestingUser != username)
                {
                    Console.WriteLine($"Access denied: {requestingUser} tried to update {username}'s profile");
                    await SendResponseAsync(stream, "HTTP/1.1 403 Forbidden", "Access denied");
                    return;
                }

                var profile = JsonSerializer.Deserialize<UserProfile>(body);
                profile.UserId = user.Id;
    
                _userRepository.UpdateProfile(profile);
                Console.WriteLine($"Profile updated successfully for user {username}");
                await SendResponseAsync(stream, "HTTP/1.1 200 OK", "Profile updated successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating profile: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await SendResponseAsync(stream, "HTTP/1.1 500 Internal Server Error", 
                    "An error occurred while updating the profile");
            }
        }
    }
}