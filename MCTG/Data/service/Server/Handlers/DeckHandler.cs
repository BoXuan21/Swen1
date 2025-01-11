using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace MCTG
{
    public partial class TcpServer
    {
        public async Task HandleGetDeckAsync(Stream stream, HttpContext context)
        {
            try
            {
                var username = context.Items["Username"] as string;
                Console.WriteLine($"Getting deck for user: {username}");

                Console.WriteLine($"Request path and query: {context.Request.Path}{context.Request.QueryString}");

                if (string.IsNullOrEmpty(username))
                {
                    Console.WriteLine("No username found in context");
                    await SendResponseAsync(stream, "HTTP/1.1 401 Unauthorized", "Authentication required");
                    return;
                }

                var user = _userRepository.GetByUsername(username);
                if (user == null)
                {
                    Console.WriteLine($"User not found for username: {username}");
                    await SendResponseAsync(stream, "HTTP/1.1 404 Not Found", "User not found");
                    return;
                }

                var deck = _cardRepository.GetUserDeck(user.Id).ToList();
                Console.WriteLine($"Found {deck.Count} cards in deck for user {username}");

                var requestLines = context.Request.Path.Value?.Split('?');
                var isPlainFormat = requestLines?.Length > 1 && requestLines[1].Contains("format=plain");
                
                Console.WriteLine($"Format is plain: {isPlainFormat}");

                string response;
                if (isPlainFormat)
                {
                    var plainResponse = new StringBuilder();
                    plainResponse.AppendLine($"Deck of user {username}:");
                    foreach (var card in deck)
                    {
                        plainResponse.AppendLine($"- {card.Name}: {card.Damage} damage");
                    }
                    response = plainResponse.ToString();
                    Console.WriteLine("Sending plain format response");
                }
                else
                {
                    response = JsonSerializer.Serialize(deck);
                    Console.WriteLine("Sending JSON format response");
                }

                await SendResponseAsync(stream, "HTTP/1.1 200 OK", response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting deck: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await SendResponseAsync(stream, "HTTP/1.1 500 Internal Server Error", 
                    "An error occurred while retrieving the deck");
            }
        }

        public async Task HandleConfigureDeckAsync(Stream stream, string body, HttpContext context)
        {
            try
            {
                var username = context.Items["Username"] as string;
                Console.WriteLine($"Configuring deck for user: {username}");

                if (string.IsNullOrEmpty(username))
                {
                    Console.WriteLine("No username found in context");
                    await SendResponseAsync(stream, "HTTP/1.1 401 Unauthorized", "Authentication required");
                    return;
                }

                var cardIds = JsonSerializer.Deserialize<List<int>>(body); 

                if (cardIds == null || cardIds.Count != 4)
                {
                    await SendResponseAsync(stream, "HTTP/1.1 400 Bad Request", "Deck must contain exactly 4 cards");
                    return;
                }

                var user = _userRepository.GetByUsername(username);
                if (user == null)
                {
                    Console.WriteLine($"User not found for username: {username}");
                    await SendResponseAsync(stream, "HTTP/1.1 404 Not Found", "User not found");
                    return;
                }

                _cardRepository.UpdateDeck(user.Id, cardIds);
                await SendResponseAsync(stream, "HTTP/1.1 200 OK", "Deck configured successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error configuring deck: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await SendResponseAsync(stream, "HTTP/1.1 500 Internal Server Error",
                    "An error occurred while configuring the deck");
            }
        }
    }
}