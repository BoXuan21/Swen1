using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Npgsql;


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

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Get user information
                using var userCmd = new NpgsqlCommand(
                    "SELECT id, coins FROM users WHERE username = @Username",
                    connection,
                    transaction);
                userCmd.Parameters.AddWithValue("@Username", username);

                using var reader = userCmd.ExecuteReader();
                if (!reader.Read())
                {
                    Console.WriteLine($"User not found in database for username: {username}");
                    await SendResponseAsync(stream, "HTTP/1.1 404 Not Found", "User not found");
                    return;
                }

                var userId = reader.GetInt32(reader.GetOrdinal("id"));
                var coins = reader.GetInt32(reader.GetOrdinal("coins"));
                reader.Close();

                Console.WriteLine($"User {username} has {coins} coins");
                if (coins < 5)
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

                // Update user's coins
                using var updateCoinsCmd = new NpgsqlCommand(
                    "UPDATE users SET coins = coins - 5 WHERE id = @UserId",
                    connection,
                    transaction);
                updateCoinsCmd.Parameters.AddWithValue("@UserId", userId);
                updateCoinsCmd.ExecuteNonQuery();

                // Mark package as sold
                _packageRepository.MarkPackageAsSold(package.Id, userId);

                // Add cards to user's collection
                foreach (var card in package.Cards)
                {
                    using var addCardCmd = new NpgsqlCommand(
                        "UPDATE cards SET user_id = @UserId WHERE id = @CardId",
                        connection,
                        transaction);
                    addCardCmd.Parameters.AddWithValue("@UserId", userId);
                    addCardCmd.Parameters.AddWithValue("@CardId", card.Id);
                    addCardCmd.ExecuteNonQuery();
                }

                transaction.Commit();
                Console.WriteLine($"Package {package.Id} successfully purchased by user {username}");
                await SendResponseAsync(stream, "HTTP/1.1 200 OK", JsonSerializer.Serialize(package.Cards));
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database error during package purchase: {ex.Message}");
            await SendResponseAsync(stream, "HTTP/1.1 500 Internal Server Error",
                "Database error occurred while processing the purchase");
            return;
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
      public async Task HandleCreatePackageAsync(Stream stream, string body, HttpContext context)
{
    try
    {
        var username = context.Items["Username"] as string;
        Console.WriteLine($"Attempting to create package by user: {username}");

        if (string.IsNullOrEmpty(username))
        {
            Console.WriteLine("Username is null or empty");
            await SendResponseAsync(stream, "HTTP/1.1 401 Unauthorized", "Authentication required");
            return;
        }

        // Check if user is admin
        if (username != "admin")
        {
            Console.WriteLine($"User {username} is not authorized to create packages");
            await SendResponseAsync(stream, "HTTP/1.1 403 Forbidden", "Only admin can create packages");
            return;
        }

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // First deserialize to dynamic structure to handle the GUID IDs
            var jsonCards = JsonSerializer.Deserialize<List<JsonElement>>(body, options);
            
            if (jsonCards == null || jsonCards.Count == 0)
            {
                await SendResponseAsync(stream, "HTTP/1.1 400 Bad Request", "Invalid card data");
                return;
            }

            // Convert the JSON format to our Card objects
            var cards = jsonCards.Select(jsonCard => new Card
            {
                // We'll leave Id as 0 since it will be generated by the database
                Id = 0,  
                Name = jsonCard.GetProperty("Name").GetString(),
                Damage = (int)jsonCard.GetProperty("Damage").GetInt32()
            }).ToList();

            _packageRepository.CreatePackage(cards);
            Console.WriteLine($"Package created successfully with {cards.Count} cards");
            await SendResponseAsync(stream, "HTTP/1.1 201 Created", "Package created successfully");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON parsing error: {ex.Message}");
            await SendResponseAsync(stream, "HTTP/1.1 400 Bad Request", "Invalid JSON format");
        }
        catch (Npgsql.PostgresException ex)
        {
            Console.WriteLine($"Database error while creating package: {ex.Message}");
            await SendResponseAsync(stream, "HTTP/1.1 500 Internal Server Error", "Database error occurred");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in HandleCreatePackageAsync: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        await SendResponseAsync(stream, "HTTP/1.1 500 Internal Server Error", 
            "An error occurred while processing your request");
    }
}
    }
}