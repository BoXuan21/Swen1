using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace MCTG
{
    public partial class TcpServer
    {
        public async Task HandleGetTradingsAsync(Stream stream)
        {
            try
            {
                var trades = _tradeRepository.GetAllTrades();
                var response = JsonSerializer.Serialize(trades);
                await SendResponseAsync(stream, "HTTP/1.1 200 OK", response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting trades: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await SendResponseAsync(stream, "HTTP/1.1 500 Internal Server Error", 
                    "An error occurred while retrieving trades");
            }
        }

        public async Task HandleCreateTradeAsync(Stream stream, string body, HttpContext context)
        {
            try
            {
                var username = context.Items["Username"] as string;
                Console.WriteLine($"Creating trade for user: {username}");

                if (string.IsNullOrEmpty(username))
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

                var trade = JsonSerializer.Deserialize<Trade>(body);
                trade.UserId = user.Id;

                try
                {
                    _tradeRepository.CreateTrade(trade);
                    Console.WriteLine($"Trade created successfully for user {username}");
                    await SendResponseAsync(stream, "HTTP/1.1 201 Created", "Trade created successfully");
                }
                catch (Exception ex)
                {
                    // Changed this to return 400 Bad Request instead of propagating the error
                    Console.WriteLine($"Error creating trade: {ex.Message}");
                    await SendResponseAsync(stream, "HTTP/1.1 400 Bad Request", ex.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HandleCreateTradeAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await SendResponseAsync(stream, "HTTP/1.1 500 Internal Server Error", 
                    "An error occurred while creating the trade");
            }
        }

        public async Task HandleExecuteTradeAsync(Stream stream, string path, string body, HttpContext context)
        {
            try
            {
                var username = context.Items["Username"] as string;
                Console.WriteLine($"Execute trade request from user: {username}");

                if (string.IsNullOrEmpty(username))
                {
                    await SendResponseAsync(stream, "HTTP/1.1 401 Unauthorized", "Authentication required");
                    return;
                }

                var pathParts = path.Split('/');
                if (pathParts.Length < 3 || !int.TryParse(pathParts[2], out int tradeId))
                {
                    await SendResponseAsync(stream, "HTTP/1.1 400 Bad Request", "Invalid trading ID format");
                    return;
                }

                var offeredCardId = JsonSerializer.Deserialize<int>(body);

                var user = _userRepository.GetByUsername(username);
                if (user == null)
                {
                    await SendResponseAsync(stream, "HTTP/1.1 404 Not Found", "User not found");
                    return;
                }

                var trade = _tradeRepository.GetTradeById(tradeId);
                if (trade == null)
                {
                    await SendResponseAsync(stream, "HTTP/1.1 404 Not Found", "Trade not found");
                    return;
                }

                if (trade.UserId == user.Id)
                {
                    await SendResponseAsync(stream, "HTTP/1.1 403 Forbidden", "Cannot trade with yourself");
                    return;
                }

                _tradeRepository.ExecuteTrade(tradeId, offeredCardId, user.Id);
                await SendResponseAsync(stream, "HTTP/1.1 200 OK", "Trade executed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing trade: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await SendResponseAsync(stream, "HTTP/1.1 500 Internal Server Error", 
                    "An error occurred while executing the trade");
            }
        }

        public async Task HandleDeleteTradeAsync(Stream stream, string path, HttpContext context)
        {
            try 
            {
                var username = context.Items["Username"] as string;
                Console.WriteLine($"Delete trade request from user: {username}");

                if (string.IsNullOrEmpty(username))
                {
                    Console.WriteLine("No username found in context");
                    await SendResponseAsync(stream, "HTTP/1.1 401 Unauthorized", "Authentication required");
                    return;
                }

                var pathParts = path.Split('/');
                if (pathParts.Length < 3)
                {
                    await SendResponseAsync(stream, "HTTP/1.1 400 Bad Request", "Invalid trading ID format");
                    return;
                }

                if (!int.TryParse(pathParts[2], out int tradeId))
                {
                    await SendResponseAsync(stream, "HTTP/1.1 400 Bad Request", "Invalid trading ID format");
                    return;
                }

                var user = _userRepository.GetByUsername(username);
                if (user == null)
                {
                    Console.WriteLine($"User not found: {username}");
                    await SendResponseAsync(stream, "HTTP/1.1 404 Not Found", "User not found");
                    return;
                }

                var trade = _tradeRepository.GetTradeById(tradeId);
                if (trade == null)
                {
                    await SendResponseAsync(stream, "HTTP/1.1 404 Not Found", "Trade not found");
                    return;
                }

                if (trade.UserId != user.Id)
                {
                    await SendResponseAsync(stream, "HTTP/1.1 403 Forbidden", "Cannot delete another user's trade");
                    return;
                }

                _tradeRepository.DeleteTrade(tradeId);
                await SendResponseAsync(stream, "HTTP/1.1 200 OK", "Trade deleted successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HandleDeleteTradeAsync: {ex.Message}");
                await SendResponseAsync(stream, "HTTP/1.1 500 Internal Server Error", "Failed to delete trade");
            }
        }
    }
}