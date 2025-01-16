using System.Text.Json;

namespace MCTG
{
    public partial class TcpServer
    {
        public async Task HandleGetTradingsAsync(CustomHttpContext context)
        {
            try
            {
                var trades = _tradeRepository.GetAllTrades();
                context.Response.StatusCode = 200;
                context.Response.StatusDescription = "OK";
                context.Response.Body = JsonSerializer.Serialize(trades, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting trades: {ex.Message}");
                context.Response.StatusCode = 500;
                context.Response.StatusDescription = "Internal Server Error";
                context.Response.Body = "An error occurred while retrieving trades";
            }
        }

        public async Task HandleCreateTradeAsync(CustomHttpContext context)
        {
            try
            {
                var username = context.Items["Username"]?.ToString();
                Console.WriteLine($"Creating trade for user: {username}");

                if (string.IsNullOrEmpty(username))
                {
                    Console.WriteLine("No username found in context");
                    context.Response.StatusCode = 401;
                    context.Response.StatusDescription = "Unauthorized";
                    context.Response.Body = "Authentication required";
                    return;
                }

                var user = _userRepository.GetByUsername(username);
                if (user == null)
                {
                    Console.WriteLine($"User not found: {username}");
                    context.Response.StatusCode = 404;
                    context.Response.StatusDescription = "Not Found";
                    context.Response.Body = "User not found";
                    return;
                }

                var trade = JsonSerializer.Deserialize<Trade>(context.Request.Body, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                trade.UserId = user.Id;

                try
                {
                    _tradeRepository.CreateTrade(trade);
                    Console.WriteLine($"Trade created successfully for user {username}");
                    context.Response.StatusCode = 201;
                    context.Response.StatusDescription = "Created";
                    context.Response.Body = "Trade created successfully";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating trade: {ex.Message}");
                    context.Response.StatusCode = 400;
                    context.Response.StatusDescription = "Bad Request";
                    context.Response.Body = ex.Message;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HandleCreateTradeAsync: {ex.Message}");
                context.Response.StatusCode = 500;
                context.Response.StatusDescription = "Internal Server Error";
                context.Response.Body = "An error occurred while creating the trade";
            }
        }

        public async Task HandleExecuteTradeAsync(CustomHttpContext context)
        {
            try
            {
                var username = context.Items["Username"]?.ToString();
                Console.WriteLine($"Execute trade request from user: {username}");

                if (string.IsNullOrEmpty(username))
                {
                    context.Response.StatusCode = 401;
                    context.Response.StatusDescription = "Unauthorized";
                    context.Response.Body = "Authentication required";
                    return;
                }

                var pathParts = context.Request.Path.Split('/');
                if (pathParts.Length < 3 || !int.TryParse(pathParts[2], out int tradeId))
                {
                    context.Response.StatusCode = 400;
                    context.Response.StatusDescription = "Bad Request";
                    context.Response.Body = "Invalid trading ID format";
                    return;
                }

                // Parse the offered card ID from the body
                Console.WriteLine($"Parsing card ID from body: {context.Request.Body}");
                int offeredCardId;
                try
                {
                    offeredCardId = int.TryParse(context.Request.Body.Trim(), out int parsed) 
                        ? parsed 
                        : JsonSerializer.Deserialize<int>(context.Request.Body);
                }
                catch (Exception)
                {
                    context.Response.StatusCode = 400;
                    context.Response.StatusDescription = "Bad Request";
                    context.Response.Body = "Invalid card ID format";
                    return;
                }

                var user = _userRepository.GetByUsername(username);
                if (user == null)
                {
                    context.Response.StatusCode = 404;
                    context.Response.StatusDescription = "Not Found";
                    context.Response.Body = "User not found";
                    return;
                }

                var trade = _tradeRepository.GetTradeById(tradeId);
                if (trade == null)
                {
                    context.Response.StatusCode = 404;
                    context.Response.StatusDescription = "Not Found";
                    context.Response.Body = "Trade not found";
                    return;
                }

                if (trade.UserId == user.Id)
                {
                    context.Response.StatusCode = 403;
                    context.Response.StatusDescription = "Forbidden";
                    context.Response.Body = "Cannot trade with yourself";
                    return;
                }

                if (_tradeRepository.ExecuteTrade(tradeId, offeredCardId, user.Id))
                {
                    context.Response.StatusCode = 200;
                    context.Response.StatusDescription = "OK";
                    context.Response.Body = "Trade executed successfully";
                }
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.StatusDescription = "Bad Request";
                    context.Response.Body = "Trade execution failed";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing trade: {ex.Message}");
                context.Response.StatusCode = 500;
                context.Response.StatusDescription = "Internal Server Error";
                context.Response.Body = "An error occurred while executing the trade";
            }
        }

        public async Task HandleDeleteTradeAsync(CustomHttpContext context)
        {
            try 
            {
                var username = context.Items["Username"]?.ToString();
                Console.WriteLine($"Delete trade request from user: {username}");

                if (string.IsNullOrEmpty(username))
                {
                    Console.WriteLine("No username found in context");
                    context.Response.StatusCode = 401;
                    context.Response.StatusDescription = "Unauthorized";
                    context.Response.Body = "Authentication required";
                    return;
                }

                var pathParts = context.Request.Path.Split('/');
                if (pathParts.Length < 3 || !int.TryParse(pathParts[2], out int tradeId))
                {
                    context.Response.StatusCode = 400;
                    context.Response.StatusDescription = "Bad Request";
                    context.Response.Body = "Invalid trading ID format";
                    return;
                }

                var user = _userRepository.GetByUsername(username);
                if (user == null)
                {
                    Console.WriteLine($"User not found: {username}");
                    context.Response.StatusCode = 404;
                    context.Response.StatusDescription = "Not Found";
                    context.Response.Body = "User not found";
                    return;
                }

                var trade = _tradeRepository.GetTradeById(tradeId);
                if (trade == null)
                {
                    context.Response.StatusCode = 404;
                    context.Response.StatusDescription = "Not Found";
                    context.Response.Body = "Trade not found";
                    return;
                }

                if (trade.UserId != user.Id)
                {
                    context.Response.StatusCode = 403;
                    context.Response.StatusDescription = "Forbidden";
                    context.Response.Body = "Cannot delete another user's trade";
                    return;
                }

                _tradeRepository.DeleteTrade(tradeId);
                context.Response.StatusCode = 200;
                context.Response.StatusDescription = "OK";
                context.Response.Body = "Trade deleted successfully";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HandleDeleteTradeAsync: {ex.Message}");
                context.Response.StatusCode = 500;
                context.Response.StatusDescription = "Internal Server Error";
                context.Response.Body = "Failed to delete trade";
            }
        }
    }
}