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
        
                var response = JsonSerializer.Serialize(cards, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
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

        public async Task HandleCreatePackageAsync(Stream stream, HttpContext context, string body)
        {
            try
            {
                var cardDtos = JsonSerializer.Deserialize<List<CardDTO>>(body);
                var cards = new List<Card>();
        
                foreach (var dto in cardDtos)
                {
                    cards.Add(new Card 
                    {
                        Id = Math.Abs(dto.Id.Replace("-", "").GetHashCode()) % 1000000,
                        Name = dto.Name,
                        Damage = dto.Damage 
                    });
                }
        
                _packageRepository.CreatePackage(cards);
                await SendResponseAsync(stream, "HTTP/1.1 201 Created", "Package created successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating package: {ex.Message}");
                await SendResponseAsync(stream, "HTTP/1.1 400 Bad Request", $"Error creating package: {ex.Message}");
            }
        }
    }
}