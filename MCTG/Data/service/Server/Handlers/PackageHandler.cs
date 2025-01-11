using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace MCTG
{
    public partial class TcpServer
    {
        public async Task HandleBuyPackageAsync(Stream stream, string body, HttpContext context)
        {
            try
            {
                var username = context.Items["Username"] as string;
                Console.WriteLine($"Attempting to buy package for user: {username}");

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

                Console.WriteLine($"User {username} has {user.Coins} coins");
                if (user.Coins < 5)
                {
                    await SendResponseAsync(stream, "HTTP/1.1 403 Forbidden", "Not enough coins");
                    return;
                }

                var package = _packageRepository.GetAvailablePackage();
                if (package == null)
                {
                    await SendResponseAsync(stream, "HTTP/1.1 404 Not Found", "No packages available");
                    return;
                }

                try
                {
                    _packageRepository.MarkPackageAsSold(package.Id, user.Id);
                    user.Coins -= 5;
                    _userRepository.Update(user);
                    
                    Console.WriteLine($"Package {package.Id} successfully purchased by user {username}");
                    await SendResponseAsync(stream, "HTTP/1.1 200 OK", JsonSerializer.Serialize(package.Cards));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during package purchase: {ex.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HandleBuyPackageAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await SendResponseAsync(stream, "HTTP/1.1 500 Internal Server Error", 
                    "An error occurred while processing your request");
            }
        }
    }
}