using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Npgsql;

namespace MCTG
{
    public class TcpServer
    {
        private readonly TcpListener _listener;
        private readonly IUserRepository _userRepository;
        private readonly ICardRepository _cardRepository;
        private readonly ITradeRepository _tradeRepository;
        private readonly IBattleRepository _battleRepository;
        private readonly IJwtService _jwtService;
        private readonly string _connectionString;
        private readonly JwtMiddleware _jwtMiddleware;
        private readonly IPackageRepository _packageRepository;
        private readonly IUserStatsRepository _userStatsRepository;

        public TcpServer(int port, IUserRepository userRepository, ICardRepository cardRepository, 
            ITradeRepository tradeRepository, IBattleRepository battleRepository, 
            IJwtService jwtService, IUserStatsRepository userStatsRepository, IPackageRepository packageRepository)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _userStatsRepository = userStatsRepository;
            _packageRepository = packageRepository;
            _userRepository = userRepository;
            _cardRepository = cardRepository;
            _tradeRepository = tradeRepository;
            _battleRepository = battleRepository;
            _jwtService = jwtService;
            _userStatsRepository = userStatsRepository;
            _jwtMiddleware = new JwtMiddleware(null, jwtService);
        }

        public void Start()
        {
            _listener.Start();
            Console.WriteLine("Server started. Waiting for connections...");

            while (true)
            {
                TcpClient client = _listener.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(HandleClient, client);
            }
        }

       private async void HandleClient(object obj)
{
    TcpClient client = (TcpClient)obj;
    using NetworkStream networkStream = client.GetStream();
    
    try
    {
        // Read request
        byte[] buffer = new byte[4096];
        using var ms = new MemoryStream();
        
        // Read the initial request
        int bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length);
        if (bytesRead == 0) return;
        
        string initialRequest = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        Console.WriteLine("Received request:");
        Console.WriteLine(initialRequest);

        // Parse content length
        var contentLengthMatch = System.Text.RegularExpressions.Regex.Match(initialRequest, @"Content-Length: (\d+)");
        int contentLength = 0;
        if (contentLengthMatch.Success)
        {
            contentLength = int.Parse(contentLengthMatch.Groups[1].Value);
        }

        // Write initial bytes to memory stream
        ms.Write(buffer, 0, bytesRead);
        int totalBytesRead = bytesRead;

        // If we haven't received all the data, keep reading
        int headerEndPosition = initialRequest.IndexOf("\r\n\r\n");
        if (headerEndPosition > 0 && contentLength > 0)
        {
            int remainingBytes = contentLength - (totalBytesRead - headerEndPosition - 4);
            while (remainingBytes > 0)
            {
                bytesRead = await networkStream.ReadAsync(buffer, 0, Math.Min(buffer.Length, remainingBytes));
                if (bytesRead == 0) break;
                ms.Write(buffer, 0, bytesRead);
                remainingBytes -= bytesRead;
                totalBytesRead += bytesRead;
            }
        }

        // Reset position to start
        ms.Position = 0;
        
        // Convert to string for processing
        string fullRequest = Encoding.UTF8.GetString(ms.ToArray());
        string[] requestLines = fullRequest.Split("\r\n");
        
        // Parse the request line
        string[] requestLine = requestLines[0].Split(' ');
        if (requestLine.Length < 2)
        {
            await SendResponseAsync(networkStream, "HTTP/1.1 400 Bad Request", "Invalid request format");
            return;
        }

        string method = requestLine[0];
        string path = requestLine[1];

        Console.WriteLine($"Method: {method}, Path: {path}");

        // Create HttpContext
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;

        // Parse headers
        var headers = new Dictionary<string, string>();
        int i;
        for (i = 1; i < requestLines.Length; i++)
        {
            var line = requestLines[i];
            if (string.IsNullOrEmpty(line)) break;

            var headerParts = line.Split(": ", 2);
            if (headerParts.Length == 2)
            {
                Console.WriteLine($"Header: {headerParts[0]} = {headerParts[1]}");
                context.Request.Headers[headerParts[0]] = headerParts[1];
            }
        }

        // Get body
        string body = "";
        if (i < requestLines.Length - 1)
        {
            body = string.Join("\r\n", requestLines.Skip(i + 1));
        }
        Console.WriteLine($"Body: {body}");

        // Set the request body
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));

        // Invoke JWT middleware
        await _jwtMiddleware.Invoke(context);

        // Use a new MemoryStream for the response
        using var responseStream = new MemoryStream();

        try
        {
            switch (method + " " + path)
            {
                case "POST /users":
                    await HandleRegistrationAsync(responseStream, body);
                    break;
                case "POST /sessions":
                    await HandleLoginAsync(responseStream, body);
                    break;
                case "GET /cards":
                    await HandleGetCardsAsync(responseStream, context);
                    break;
                case "POST /transactions/packages":
                    await HandleBuyPackageAsync(responseStream, body, context);
                    break;
                case "POST /packages":
                    await HandleCreatePackageAsync(responseStream, context, body);
                    break;
                case "POST /battles": 
                    await HandleBattleAsync(responseStream, context, body);
                    break;
                case "GET /deck":
                    await HandleGetDeckAsync(responseStream, context);
                    break;
                case "PUT /deck":
                    await HandleConfigureDeckAsync(responseStream, body, context);
                    break;
                case "GET /stats":
                    await HandleGetStatsAsync(responseStream, context);
                    break;
                case "GET /scoreboard":
                    await HandleGetScoreboardAsync(responseStream);
                    break;
                case "GET /tradings":
                    await HandleGetTradingsAsync(responseStream);
                    break;
                case "POST /tradings":
                    await HandleCreateTradeAsync(responseStream, body, context);
                    break;
                case var tradePath when method == "DELETE" && path.StartsWith("/tradings/"):
                    await HandleDeleteTradeAsync(responseStream, path, context);
                    break;
                case "GET /history":
                    await HandleGetBattleHistoryAsync(responseStream, context);
                    break;
                case var profilePath when path.StartsWith("/users/") && method == "GET":
                    await HandleGetProfileAsync(responseStream, path.Split('/')[2], context);
                    break;
                case var profilePath when path.StartsWith("/users/") && method == "PUT":
                    await HandleUpdateProfileAsync(responseStream, path.Split('/')[2], body, context);
                    break;
                default:
                    await SendResponseAsync(responseStream, "HTTP/1.1 404 Not Found", "Unknown endpoint");
                    break;
            }

            // Send the response
            responseStream.Position = 0;
            await responseStream.CopyToAsync(networkStream);
            await networkStream.FlushAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling request: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            await SendResponseAsync(networkStream, "HTTP/1.1 500 Internal Server Error", 
                $"Internal server error: {ex.Message}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Fatal error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        try
        {
            await SendResponseAsync(networkStream, "HTTP/1.1 500 Internal Server Error", 
                "A fatal error occurred");
        }
        catch
        {
            // Ignore any errors in sending error response
        }
    }
    finally
    {
        client.Close();
    }
}
        
        // To
        public async Task HandleGetProfileAsync(Stream stream, string username, HttpContext context)
        {
            var requestingUser = context.User.Identity.Name;
            var user = _userRepository.GetByUsername(username);
    
            if (user == null)
            {
                await SendResponseAsync(stream, "HTTP/1.1 404 Not Found", "User not found");
                return;
            }

            // Only allow users to view their own profile
            if (requestingUser != username)
            {
                await SendResponseAsync(stream, "HTTP/1.1 403 Forbidden", "Access denied");
                return;
            }

            var profile = _userRepository.GetUserProfile(user.Id);
            var response = JsonSerializer.Serialize(profile);
            await SendResponseAsync(stream, "HTTP/1.1 200 OK", response);
        }

// And update the update profile method similarly
        public async Task HandleUpdateProfileAsync(Stream stream, string username, string body, HttpContext context)
        {
            var requestingUser = context.User.Identity.Name;
            var user = _userRepository.GetByUsername(username);
    
            if (user == null)
            {
                await SendResponseAsync(stream, "HTTP/1.1 404 Not Found", "User not found");
                return;
            }

            // Only allow users to update their own profile
            if (requestingUser != username)
            {
                await SendResponseAsync(stream, "HTTP/1.1 403 Forbidden", "Access denied");
                return;
            }

            var profile = JsonSerializer.Deserialize<UserProfile>(body);
            profile.UserId = user.Id;
    
            _userRepository.UpdateProfile(profile);
            await SendResponseAsync(stream, "HTTP/1.1 200 OK", "Profile updated successfully");
        }

        public async Task HandleUpdateProfileAsync(Stream stream, string body, HttpContext context)
        {
            var username = context.User.Identity.Name;
            var user = _userRepository.GetByUsername(username);
    
            if (user == null)
            {
                await SendResponseAsync(stream, "HTTP/1.1 404 Not Found", "User not found");
                return;
            }

            var profile = JsonSerializer.Deserialize<UserProfile>(body);
            profile.UserId = user.Id;
    
            _userRepository.UpdateProfile(profile);
            await SendResponseAsync(stream, "HTTP/1.1 200 OK", "Profile updated successfully");
        }
        
        
        
public async Task HandleBattleAsync(Stream stream, HttpContext context, string body)
{
    var username = context.User.Identity.Name;
    var user1 = _userRepository.GetByUsername(username);
    var opponent = JsonSerializer.Deserialize<BattleRequest>(body);
    var user2 = _userRepository.GetByUsername(opponent.OpponentUsername);

    if (user1 == null || user2 == null)
    {
        await SendResponseAsync(stream, "HTTP/1.1 404 Not Found", "User not found");
        return;
    }

    // Get user stats
    var stats1 = _userStatsRepository.GetUserStats(user1.Id);
    var stats2 = _userStatsRepository.GetUserStats(user2.Id);
    
    if (stats1 == null || stats2 == null)
    {
        await SendResponseAsync(stream, "HTTP/1.1 500 Internal Server Error", "User stats not found");
        return;
    }

    var cardsInDeck1 = _cardRepository.GetUserDeck(user1.Id).ToList();
    var cardsInDeck2 = _cardRepository.GetUserDeck(user2.Id).ToList();
    
    if (cardsInDeck1.Count < 4 || cardsInDeck2.Count < 4)
    {
        await SendResponseAsync(stream, "HTTP/1.1 400 Bad Request", "Both users must have at least 4 cards in their deck");
        return;
    }

    var deck1 = new Deck(new Stack { Cards = cardsInDeck1 });
    var deck2 = new Deck(new Stack { Cards = cardsInDeck2 });

    var battleLogic = new BattleLogic(user1, user2, deck1, deck2);
    var battleLog = battleLogic.ExecuteBattle();

    // Update stats based on battle result
    stats1.GamesPlayed++;
    stats2.GamesPlayed++;

    if (battleLog.Winner == "User 1")
    {
        stats1.Wins++;
        stats2.Losses++;
        stats1.Elo += 3;
        stats2.Elo -= 5;
    }
    else if (battleLog.Winner == "User 2")
    {
        stats2.Wins++;
        stats1.Losses++;
        stats2.Elo += 3;
        stats1.Elo -= 5;
    }
    else
    {
        stats1.Draws++;
        stats2.Draws++;
    }

    // Update stats once
    _userStatsRepository.UpdateStats(stats1);
    _userStatsRepository.UpdateStats(stats2);

    await SendResponseAsync(stream, "HTTP/1.1 200 OK", JsonSerializer.Serialize(battleLog));
}
        
        
        public async Task HandleGetBattleHistoryAsync(Stream stream, HttpContext context)
        {
            var username = context.User.Identity.Name;
            var user = _userRepository.GetByUsername(username);
    
            if (user == null)
            {
                await SendResponseAsync(stream, "HTTP/1.1 404 Not Found", "User not found");
                return;
            }

            var battles = _battleRepository.GetUserBattleHistory(user.Id);
            var response = JsonSerializer.Serialize(battles);
            await SendResponseAsync(stream, "HTTP/1.1 200 OK", response);
        }
        
        public async Task HandleRegistrationAsync(Stream stream, string body)
        {
            try
            {
                Console.WriteLine($"Attempting registration with body: {body}");
        
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
        
                var user = JsonSerializer.Deserialize<User>(body, options);
                Console.WriteLine($"Deserialized user: {user?.Username}");
        
                if (user == null || string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Password))
                {
                    Console.WriteLine("Invalid user data");
                    await SendResponseAsync(stream, "HTTP/1.1 400 Bad Request", "Invalid user data");
                    return;
                }

                // Add initial coins and ELO
                user.Coins = 20;  // Starting coins
                user.Elo = 100;   // Starting ELO

                _userRepository.Add(user);
                Console.WriteLine($"User {user.Username} registered successfully");

                // Generate token after successful registration
                string token = _jwtService.GenerateToken(user.Username);

                // Send token in response
                await SendResponseAsync(stream, "HTTP/1.1 201 Created", token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Registration error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await SendResponseAsync(stream, "HTTP/1.1 400 Bad Request", $"Registration failed: {ex.Message}");
            }
        }

        public async Task HandleLoginAsync(Stream stream, string body)
        {
            try
            {
                Console.WriteLine($"Login attempt with body: {body}");
        
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
        
                var credentials = JsonSerializer.Deserialize<User>(body, options);
        
                if (credentials == null || string.IsNullOrEmpty(credentials.Username) || string.IsNullOrEmpty(credentials.Password))
                {
                    Console.WriteLine("Invalid credentials format - null or empty values");
                    await SendResponseAsync(stream, "HTTP/1.1 400 Bad Request", "Invalid credentials format");
                    return;
                }
        
                Console.WriteLine($"Attempting login for username: {credentials.Username}");
                Console.WriteLine($"Password length: {credentials.Password?.Length ?? 0}");
        
                bool isValid = _userRepository.ValidateCredentials(credentials.Username, credentials.Password);
                Console.WriteLine($"Credentials valid: {isValid}");
        
                if (isValid)
                {
                    string token = _jwtService.GenerateToken(credentials.Username);
                    await SendResponseAsync(stream, "HTTP/1.1 200 OK", token);
                }
                else
                {
                    await SendResponseAsync(stream, "HTTP/1.1 401 Unauthorized", "Invalid credentials");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await SendResponseAsync(stream, "HTTP/1.1 400 Bad Request", $"Login failed: {ex.Message}");
            }
        }

        public async Task HandleGetCardsAsync(Stream stream, HttpContext context)
        {
            try
            {
                var username = context.Items["Username"] as string;  // Changed from User.Identity.Name
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

public async Task HandleGetDeckAsync(Stream stream, HttpContext context)
{
    try
    {
        var username = context.Items["Username"] as string;
        Console.WriteLine($"Getting deck for user: {username}");

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

        var deck = _cardRepository.GetUserDeck(user.Id);
        Console.WriteLine($"Found {deck.Count()} cards in deck for user {username}");

        var response = JsonSerializer.Serialize(deck);
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

        // Assuming the request body contains a list of newly assigned card IDs 
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

        // No need to check for card ownership since we're using the assigned IDs 

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

        public async Task HandleGetStatsAsync(Stream stream, HttpContext context)
        {
            var username = context.User.Identity.Name;
            var user = _userRepository.GetByUsername(username);
            
            if (user == null)
            {
                await SendResponseAsync(stream, "HTTP/1.1 404 Not Found", "User not found");
                return;
            }

            var stats = new { user.Elo, user.Coins };
            var response = JsonSerializer.Serialize(stats);
            await SendResponseAsync(stream, "HTTP/1.1 200 OK", response);
        }

        public async Task HandleGetScoreboardAsync(Stream stream)
        {
            var users = _userRepository.GetAllUsers();
            var scoreboard = users.OrderByDescending(u => u.Elo)
                                .Select(u => new { u.Username, u.Elo });
            var response = JsonSerializer.Serialize(scoreboard);
            await SendResponseAsync(stream, "HTTP/1.1 200 OK", response);
        }

        public async Task HandleGetTradingsAsync(Stream stream)
        {
            var trades = _tradeRepository.GetAllTrades();
            var response = JsonSerializer.Serialize(trades);
            await SendResponseAsync(stream, "HTTP/1.1 200 OK", response);
        }

        public async Task HandleCreateTradeAsync(Stream stream, string body, HttpContext context)
        {
            var username = context.User.Identity.Name;
            var user = _userRepository.GetByUsername(username);
            
            if (user == null)
            {
                await SendResponseAsync(stream, "HTTP/1.1 404 Not Found", "User not found");
                return;
            }

            var trade = JsonSerializer.Deserialize<Trade>(body);
            trade.UserId = user.Id;
            
            try
            {
                _tradeRepository.CreateTrade(trade);
                await SendResponseAsync(stream, "HTTP/1.1 201 Created", "Trade created successfully");
            }
            catch (Exception ex)
            {
                await SendResponseAsync(stream, "HTTP/1.1 400 Bad Request", ex.Message);
            }
        }

        public async Task HandleDeleteTradeAsync(Stream stream, string path, HttpContext context)
        {
            try 
            {
                // Parse trading ID from path (e.g., /tradings/123)
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

                var username = context.User.Identity.Name;
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

        public async Task SendResponseAsync(Stream stream, string status, string content)
        {
            string response = $"{status}\r\n" +
                              "Content-Type: application/json\r\n" +
                              "Access-Control-Allow-Origin: *\r\n" +
                              $"Content-Length: {Encoding.UTF8.GetByteCount(content)}\r\n" +
                              "\r\n" +
                              content;

            byte[] responseBytes = Encoding.UTF8.GetBytes(response);
            await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
            await stream.FlushAsync();
        }
    }
}