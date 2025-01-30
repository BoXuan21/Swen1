using System.Text;
using System.Text.Json;

namespace MCTG
{
    public partial class TcpServer
    {
        public async Task HandleGetDeckAsync(CustomHttpContext context)
        {
            try
            {
                var username = context.GetUsername();
                Console.WriteLine($"Getting deck for user: {username}");

                if (string.IsNullOrEmpty(username))
                {
                    Console.WriteLine("No username found in context");
                    context.Response.SetUnauthorized();
                    return;
                }

                var user = _userRepository.GetByUsername(username);
                if (user == null)
                {
                    Console.WriteLine($"User not found for username: {username}");
                    context.Response.StatusCode = 404;
                    context.Response.StatusDescription = "Not Found";
                    context.Response.Body = "User not found";
                    return;
                }

                var deck = _cardRepository.GetUserDeck(user.Id).ToList();
                Console.WriteLine($"Found {deck.Count} cards in deck for user {username}");

                var isPlainFormat = context.Request.Path.Contains("?format=plain");
                Console.WriteLine($"Format is plain: {isPlainFormat}");

                context.Response.StatusCode = 200;
                context.Response.StatusDescription = "OK";

                if (isPlainFormat)
                {
                    var plainResponse = new StringBuilder();
                    plainResponse.AppendLine($"Deck of user {username}:");
                    foreach (var card in deck)
                    {
                        plainResponse.AppendLine($"- {card.Name}: {card.Damage} damage");
                    }
                    context.Response.Body = plainResponse.ToString();
                }
                else
                {
                    context.Response.Body = JsonSerializer.Serialize(deck);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting deck: {ex.Message}");
                context.Response.StatusCode = 500;
                context.Response.StatusDescription = "Internal Server Error";
                context.Response.Body = "An error occurred while retrieving the deck";
            }
        }

        public async Task HandleConfigureDeckAsync(CustomHttpContext context)
        {
            try
            {
                var username = context.GetUsername();
                Console.WriteLine($"Configuring deck for user: {username}");

                if (string.IsNullOrEmpty(username))
                {
                    Console.WriteLine("No username found in context");
                    context.Response.SetUnauthorized();
                    return;
                }

                var cardIds = JsonSerializer.Deserialize<List<int>>(context.Request.Body);

                if (cardIds == null || cardIds.Count != 4)
                {
                    context.Response.StatusCode = 400;
                    context.Response.StatusDescription = "Bad Request";
                    context.Response.Body = "Deck must contain exactly 4 cards";
                    return;
                }

                var user = _userRepository.GetByUsername(username);
                if (user == null)
                {
                    Console.WriteLine($"User not found for username: {username}");
                    context.Response.StatusCode = 404;
                    context.Response.StatusDescription = "Not Found";
                    context.Response.Body = "User not found";
                    return;
                }

                _cardRepository.UpdateDeck(user.Id, cardIds);
                context.Response.StatusCode = 200;
                context.Response.StatusDescription = "OK";
                context.Response.Body = "Deck configured successfully";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error configuring deck: {ex.Message}");
                context.Response.StatusCode = 500;
                context.Response.StatusDescription = "Internal Server Error";
                context.Response.Body = "An error occurred while configuring the deck";
            }
        }
    }
}