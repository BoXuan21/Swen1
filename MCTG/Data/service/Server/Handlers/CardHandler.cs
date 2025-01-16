using System.Text.Json;

namespace MCTG
{
    public partial class TcpServer
    {
        public async Task HandleGetCardsAsync(CustomHttpContext context)
        {
            try
            {
                var username = context.Items["Username"]?.ToString();
                Console.WriteLine($"Attempting to get cards for user: {username}");

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

                var cards = _cardRepository.GetUserCards(user.Id);
                Console.WriteLine($"Found {cards.Count()} cards for user {username}");
                
                var options = new JsonSerializerOptions { WriteIndented = true };
                context.Response.StatusCode = 200;
                context.Response.StatusDescription = "OK";
                context.Response.Body = JsonSerializer.Serialize(cards, options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HandleGetCardsAsync: {ex.Message}");
                context.Response.StatusCode = 500;
                context.Response.StatusDescription = "Internal Server Error";
                context.Response.Body = "An error occurred while retrieving the cards";
            }
        }
    }
}