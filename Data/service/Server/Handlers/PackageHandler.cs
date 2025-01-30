using System.Text.Json;

namespace MCTG
{
    public partial class TcpServer
    {
        public async Task HandleBuyPackageAsync(CustomHttpContext context)
        {
            try
            {   //checkt context ob jwt authorization passt
                var username = context.Items["Username"]?.ToString();
                Console.WriteLine($"Attempting to buy package for user: {username}");

                if (string.IsNullOrEmpty(username))
                {
                    Console.WriteLine("Username is null or empty");
                    context.Response.StatusCode = 401;
                    context.Response.StatusDescription = "Unauthorized";
                    context.Response.Body = "Authentication required";
                    return;
                }

                var user = _userRepository.GetByUsername(username);
                if (user == null)
                {
                    Console.WriteLine($"User not found in database for username: {username}");
                    context.Response.StatusCode = 404;
                    context.Response.StatusDescription = "Not Found";
                    context.Response.Body = "User not found";
                    return;
                }

                Console.WriteLine($"User {username} has {user.Coins} coins");
                if (user.Coins < 5)
                {
                    context.Response.StatusCode = 403;
                    context.Response.StatusDescription = "Forbidden";
                    context.Response.Body = "Not enough coins";
                    return;
                }

                var package = _packageRepository.GetAvailablePackage();
                if (package == null)
                {
                    context.Response.StatusCode = 404;
                    context.Response.StatusDescription = "Not Found";
                    context.Response.Body = "No packages available";
                    return;
                }

                // Update user's coins
                user.Coins -= 5;
                _userRepository.Update(user);

                // Mark package as sold and update card ownership
                _packageRepository.MarkPackageAsSold(package.Id, user.Id);

                context.Response.StatusCode = 200;
                context.Response.StatusDescription = "OK";
                context.Response.Body = JsonSerializer.Serialize(package.Cards);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HandleBuyPackageAsync: {ex.Message}");
                context.Response.StatusCode = 500;
                context.Response.StatusDescription = "Internal Server Error";
                context.Response.Body = "An error occurred while processing your request";
            }
        }

        public async Task HandleCreatePackageAsync(CustomHttpContext context)
        {
            try
            {
                var username = context.Items["Username"]?.ToString();
                Console.WriteLine($"Attempting to create package by user: {username}");

                if (string.IsNullOrEmpty(username))
                {
                    Console.WriteLine("Username is null or empty");
                    context.Response.StatusCode = 401;
                    context.Response.StatusDescription = "Unauthorized";
                    context.Response.Body = "Authentication required";
                    return;
                }

                // Check if user is admin
                if (username != "admin")
                {
                    Console.WriteLine($"User {username} is not authorized to create packages");
                    context.Response.StatusCode = 403;
                    context.Response.StatusDescription = "Forbidden";
                    context.Response.Body = "Only admin can create packages";
                    return;
                }

                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    // json body lesen
                    var jsonCards = JsonSerializer.Deserialize<List<JsonElement>>(context.Request.Body, options);
                    
                    if (jsonCards == null || jsonCards.Count == 0)
                    {
                        context.Response.StatusCode = 400;
                        context.Response.StatusDescription = "Bad Request";
                        context.Response.Body = "Invalid card data";
                        return;
                    }

                    // Convert the JSON format to our Card objects
                    var cards = jsonCards.Select(jsonCard => new Card
                    {
                        Id = 0,  // will be added with add card db
                        Name = jsonCard.GetProperty("Name").GetString(),
                        Damage = jsonCard.GetProperty("Damage").GetInt32()
                    }).ToList();

                    _packageRepository.CreatePackage(cards);
                    
                    context.Response.StatusCode = 201;
                    context.Response.StatusDescription = "Created";
                    context.Response.Body = "Package created successfully";
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
                Console.WriteLine($"Error in HandleCreatePackageAsync: {ex.Message}");
                context.Response.StatusCode = 500;
                context.Response.StatusDescription = "Internal Server Error";
                context.Response.Body = "An error occurred while processing your request";
            }
        }
    }
}