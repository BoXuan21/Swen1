using System.Text.Json;

namespace MCTG
{
    public partial class TcpServer
    {
        public async Task HandleRegistrationAsync(CustomHttpContext context)
        {
            try
            {
                Console.WriteLine($"Attempting registration with body: {context.Request.Body}");
                
                // Konfiguriere JSON-Optionen (ignoriert Groß-/Kleinschreibung)
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                // Konvertiert JSON-Request in User-Objekt
                var user = JsonSerializer.Deserialize<User>(context.Request.Body, options);
                Console.WriteLine($"Deserialized user: {user?.Username}");
                
                //ob felder vorhanden sind
                if (user == null || string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Password))
                {
                    Console.WriteLine("Invalid user data");
                    context.Response.StatusCode = 400;
                    context.Response.StatusDescription = "Bad Request";
                    context.Response.Body = "Invalid user data";
                    return;
                }

                // Check if username da ist
                var existingUser = _userRepository.GetByUsername(user.Username);
                if (existingUser != null)
                {
                    Console.WriteLine($"User {user.Username} already exists");
                    context.Response.StatusCode = 409;
                    context.Response.StatusDescription = "Conflict";
                    context.Response.Body = "Username already taken";
                    return;
                }

                // start werete
                user.Coins = 20;
                user.Elo = 100;

                try
                { // user wird hinzugefügt
                    _userRepository.Add(user);
                    Console.WriteLine($"User {user.Username} registered successfully");

                    string token = _jwtService.GenerateToken(user.Username);
                    context.Response.StatusCode = 201;
                    context.Response.StatusDescription = "Created";
                    context.Response.Body = token;
                }
                catch (Npgsql.PostgresException ex)
                {
                    Console.WriteLine($"Database error during registration: {ex.Message}");
                    context.Response.StatusCode = 500;
                    context.Response.StatusDescription = "Internal Server Error";
                    context.Response.Body = "Registration failed due to database error";
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON parsing error: {ex.Message}");
                context.Response.StatusCode = 400;
                context.Response.StatusDescription = "Bad Request";
                context.Response.Body = "Invalid JSON format";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Registration error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                context.Response.StatusCode = 500;
                context.Response.StatusDescription = "Internal Server Error";
                context.Response.Body = "An unexpected error occurred";
            }
        }

        public async Task HandleLoginAsync(CustomHttpContext context)
        {
            try
            {
                Console.WriteLine($"Login attempt with body: {context.Request.Body}");
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                // Deserialisiert Login-Daten im request body
                var credentials = JsonSerializer.Deserialize<User>(context.Request.Body, options);
                
                if (credentials == null || string.IsNullOrEmpty(credentials.Username) || string.IsNullOrEmpty(credentials.Password))
                {
                    Console.WriteLine("Invalid credentials format - null or empty values");
                    context.Response.StatusCode = 400;
                    context.Response.StatusDescription = "Bad Request";
                    context.Response.Body = "Invalid credentials format";
                    return;
                }
                
                Console.WriteLine($"Attempting login for username: {credentials.Username}");
                
                try 
                {
                    bool isValid = _userRepository.ValidateCredentials(credentials.Username, credentials.Password);
                    Console.WriteLine($"Credentials valid: {isValid}");
                
                    if (isValid)
                    {
                        string token = _jwtService.GenerateToken(credentials.Username);
                        context.Response.StatusCode = 200;
                        context.Response.StatusDescription = "OK";
                        context.Response.Body = token;
                    }
                    else
                    {
                        context.Response.StatusCode = 401;
                        context.Response.StatusDescription = "Unauthorized";
                        context.Response.Body = "Invalid username or password";
                    }
                }
                catch (Npgsql.PostgresException ex)
                {
                    Console.WriteLine($"Database error during login: {ex.Message}");
                    context.Response.StatusCode = 500;
                    context.Response.StatusDescription = "Internal Server Error";
                    context.Response.Body = "Login failed due to database error";
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON parsing error: {ex.Message}");
                context.Response.StatusCode = 400;
                context.Response.StatusDescription = "Bad Request";
                context.Response.Body = "Invalid JSON format";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                context.Response.StatusCode = 500;
                context.Response.StatusDescription = "Internal Server Error";
                context.Response.Body = "An unexpected error occurred";
            }
        }

        public async Task HandleGetProfileAsync(CustomHttpContext context)
        {
            try 
            {    // Holt authentifizierten User aus JWT und angeforderten User aus URL
                var requestingUser = context.Items["Username"]?.ToString();
                var requestedUsername = context.Request.Path.Split('/')[2];
                Console.WriteLine($"Request to view profile of {requestedUsername} by user {requestingUser}");

                if (string.IsNullOrEmpty(requestingUser))
                    //ob jwt vorhanden war
                {
                    Console.WriteLine("No username found in context");
                    context.Response.StatusCode = 401;
                    context.Response.StatusDescription = "Unauthorized";
                    context.Response.Body = "Authentication required";
                    return;
                }

                var user = _userRepository.GetByUsername(requestedUsername);
                if (user == null)
                {
                    Console.WriteLine($"User not found: {requestedUsername}");
                    context.Response.StatusCode = 404;
                    context.Response.StatusDescription = "Not Found";
                    context.Response.Body = "User not found";
                    return;
                }

                if (requestingUser != requestedUsername)
                {
                    Console.WriteLine($"Access denied: {requestingUser} tried to view {requestedUsername}'s profile");
                    context.Response.StatusCode = 403;
                    context.Response.StatusDescription = "Forbidden";
                    context.Response.Body = "Access denied";
                    return;
                }

                var profile = _userRepository.GetUserProfile(user.Id);
                if (profile == null)
                {
                    Console.WriteLine($"Profile not found for user {requestedUsername}");
                    context.Response.StatusCode = 404;
                    context.Response.StatusDescription = "Not Found";
                    context.Response.Body = "Profile not found";
                    return;
                }

                Console.WriteLine($"Retrieved profile for user {requestedUsername}");
                context.Response.StatusCode = 200;
                context.Response.StatusDescription = "OK";
                context.Response.Body = JsonSerializer.Serialize(profile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting profile: {ex.Message}");
                context.Response.StatusCode = 500;
                context.Response.StatusDescription = "Internal Server Error";
                context.Response.Body = "An unexpected error occurred while retrieving the profile";
            }
        }

        public async Task HandleUpdateProfileAsync(CustomHttpContext context)
        {
            try
            { // selbe wie oben
                var requestingUser = context.Items["Username"]?.ToString();
                var requestedUsername = context.Request.Path.Split('/')[2];
                Console.WriteLine($"Request to update profile for {requestedUsername} by user {requestingUser}");

                if (string.IsNullOrEmpty(requestingUser))
                {
                    Console.WriteLine("No username found in context");
                    context.Response.StatusCode = 401;
                    context.Response.StatusDescription = "Unauthorized";
                    context.Response.Body = "Authentication required";
                    return;
                }

                var user = _userRepository.GetByUsername(requestedUsername);
                if (user == null)
                {
                    Console.WriteLine($"User not found: {requestedUsername}");
                    context.Response.StatusCode = 404;
                    context.Response.StatusDescription = "Not Found";
                    context.Response.Body = "User not found";
                    return;
                }

                if (requestingUser != requestedUsername)
                {
                    Console.WriteLine($"Access denied: {requestingUser} tried to update {requestedUsername}'s profile");
                    context.Response.StatusCode = 403;
                    context.Response.StatusDescription = "Forbidden";
                    context.Response.Body = "Access denied";
                    return;
                }

                try
                {
                    var profile = JsonSerializer.Deserialize<UserProfile>(context.Request.Body);
                    if (profile == null)
                    {
                        context.Response.StatusCode = 400;
                        context.Response.StatusDescription = "Bad Request";
                        context.Response.Body = "Invalid profile data";
                        return;
                    }

                    profile.UserId = user.Id;
    
                    _userRepository.UpdateProfile(profile);
                    Console.WriteLine($"Profile updated successfully for user {requestedUsername}");
                    
                    context.Response.StatusCode = 200;
                    context.Response.StatusDescription = "OK";
                    context.Response.Body = "Profile updated successfully";
                }
                catch (JsonException)
                {
                    context.Response.StatusCode = 400;
                    context.Response.StatusDescription = "Bad Request";
                    context.Response.Body = "Invalid JSON format";
                }
                catch (Npgsql.PostgresException)
                {
                    context.Response.StatusCode = 500;
                    context.Response.StatusDescription = "Internal Server Error";
                    context.Response.Body = "Database error occurred";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating profile: {ex.Message}");
                context.Response.StatusCode = 500;
                context.Response.StatusDescription = "Internal Server Error";
                context.Response.Body = "An unexpected error occurred while updating the profile";
            }
        }
    }
}