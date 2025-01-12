using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace MCTG
{
    public partial class TcpServer
    {
      public async Task HandleGetCardsAsync(Stream stream, HttpContext context)
{
    try
    {
        var username = context.Items["Username"] as string;
        Console.WriteLine($"Attempting to get cards for user: {username}");

        if (string.IsNullOrEmpty(username))
        {
            Console.WriteLine("Username is null or empty");
            await SendResponseAsync(stream, "HTTP/1.1 401 Unauthorized", "Authentication required");
            return;
        }

        var user = _userRepository.GetByUsername(username);
        if (user == null)
        {
            Console.WriteLine($"User not found in database for username: {username}");
            await SendResponseAsync(stream, "HTTP/1.1 404 Not Found", "User not found");
            return;
        }

        var cards = _cardRepository.GetUserCards(user.Id);
        Console.WriteLine($"Found {cards.Count()} cards for user {username}");
        
        var options = new JsonSerializerOptions { WriteIndented = true };
        var response = JsonSerializer.Serialize(cards, options);
        await SendResponseAsync(stream, "HTTP/1.1 200 OK", response);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in HandleGetCardsAsync: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        await SendResponseAsync(stream, "HTTP/1.1 500 Internal Server Error", 
            "An error occurred while retrieving the cards");
    }
}
      
    }
}