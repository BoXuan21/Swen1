using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Text.Json;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

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
                byte[] buffer = new byte[1024];
                int bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length);
                string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        
                Console.WriteLine("Received request:");
                Console.WriteLine(request);

                // Parse the HTTP request
                string[] requestLines = request.Split("\r\n");
                string[] requestLine = requestLines[0].Split(' ');
                string method = requestLine[0];
                string path = requestLine[1];

                Console.WriteLine($"Method: {method}, Path: {path}");

                // Get request body
                string body = "";
                bool readingBody = false;
                foreach (string line in requestLines)
                {
                    if (readingBody)
                    {
                        body += line;
                    }
                    else if (string.IsNullOrEmpty(line))
                    {
                        readingBody = true;
                    }
                }

                Console.WriteLine($"Body: {body}");

                // Create HttpContext for authentication
                var context = new DefaultHttpContext();
                context.Request.Method = method;
                context.Request.Path = path;
                context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));

                await _jwtMiddleware.Invoke(context);

                // Convert NetworkStream to MemoryStream for testing purposes
                var stream = new MemoryStream();
                await networkStream.CopyToAsync(stream);
                stream.Seek(0, SeekOrigin.Begin);

                switch (method + " " + path)
                {
                    // Auth endpoints
                    case "POST /users":
                        await HandleRegistrationAsync(stream, body);
                        break;
                    case "POST /sessions":
                        await HandleLoginAsync(stream, body);
                        break;

                    // Cards endpoints
                    case "GET /cards":
                        await HandleGetCardsAsync(stream, context);
                        break;
                    case "POST /packages":
                        await HandleBuyPackageAsync(stream, body, context);
                        break;
                    
                    case "POST/battle":
                        await HandleBattleAsync(stream, context, body);
                        break;

                    // Deck endpoints
                    case "GET /deck":
                        await HandleGetDeckAsync(stream, context);
                        break;
                    case "PUT /deck":
                        await HandleConfigureDeckAsync(stream, body, context);
                        break;

                    // Stats endpoints
                    case "GET /stats":
                        await HandleGetStatsAsync(stream, context);
                        break;
                    case "GET /score":
                        await HandleGetScoreboardAsync(stream);
                        break;

                    // Trading endpoints
                    case "GET /tradings":
                        await HandleGetTradingsAsync(stream);
                        break;
                    case "POST /tradings":
                        await HandleCreateTradeAsync(stream, body, context);
                        break;
                    case "DELETE /tradings":
                        await HandleDeleteTradeAsync(stream, path, context);
                        break;
                    
                    case "GET /history":
                        await HandleGetBattleHistoryAsync(stream, context);
                        break;
                    

                    default:
                        await SendResponseAsync(stream, "HTTP/1.1 404 Not Found", "Unknown endpoint");
                        break;
                }

                // Copy the response from MemoryStream back to NetworkStream
                stream.Seek(0, SeekOrigin.Begin);
                await stream.CopyToAsync(networkStream);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                await SendResponseAsync(networkStream, "HTTP/1.1 500 Internal Server Error", ex.Message);
            }
            finally
            {
                client.Close();
            }
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

    _userStatsRepository.UpdateStats(stats1);
    _userStatsRepository.UpdateStats(stats2);

    await SendResponseAsync(stream, "HTTP/1.1 200 OK", JsonSerializer.Serialize(battleLog));
    
    Console.WriteLine($"Updating stats for user1: {JsonSerializer.Serialize(stats1)}");
    _userStatsRepository.UpdateStats(stats1);

    Console.WriteLine($"Updating stats for user2: {JsonSerializer.Serialize(stats2)}");
    _userStatsRepository.UpdateStats(stats2);
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
                Console.WriteLine($"Attempting to deserialize: {body}");
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                var user = JsonSerializer.Deserialize<User>(body, options);
                
                if (user == null || string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Password))
                {
                    await SendResponseAsync(stream, "HTTP/1.1 400 Bad Request", "Invalid user data");
                    return;
                }

                _userRepository.Add(user);
                await SendResponseAsync(stream, "HTTP/1.1 201 Created", "User created successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Registration error: {ex.Message}");
                await SendResponseAsync(stream, "HTTP/1.1 400 Bad Request", $"Registration failed: {ex.Message}");
            }
        }

        public async Task HandleLoginAsync(Stream stream, string body)
        {
            try
            {
                var credentials = JsonSerializer.Deserialize<User>(body);
                if (_userRepository.ValidateCredentials(credentials.Username, credentials.Password))
                {
                    string token = _jwtService.GenerateToken(credentials.Username);
                    await SendResponseAsync(stream, "HTTP/1.1 200 OK", token);
                }
                else
                {
                    await SendResponseAsync(stream, "HTTP/1.1 401 Unauthorized", "Invalid credentials");
                }
            }
            catch (Exception)
            {
                await SendResponseAsync(stream, "HTTP/1.1 400 Bad Request", "Login failed");
            }
        }

        public async Task HandleGetCardsAsync(Stream stream, HttpContext context)
        {
            var username = context.User.Identity.Name;
            var user = _userRepository.GetByUsername(username);
            
            if (user == null)
            {
                await SendResponseAsync(stream, "HTTP/1.1 404 Not Found", "User not found");
                return;
            }

            var cards = _cardRepository.GetUserCards(user.Id);
            var response = JsonSerializer.Serialize(cards);
            await SendResponseAsync(stream, "HTTP/1.1 200 OK", response);
        }
        
        public async Task HandleCreatePackageAsync(Stream stream, HttpContext context, string body)
        {
            var username = context.User.Identity.Name;
            var user = _userRepository.GetByUsername(username);
    
            if (user == null || user.Username != "admin") // Simple admin check
            {
                await SendResponseAsync(stream, "HTTP/1.1 403 Forbidden", "Admin access required");
                return;
            }

            var cards = JsonSerializer.Deserialize<List<Card>>(body);
            _packageRepository.CreatePackage(cards);
            await SendResponseAsync(stream, "HTTP/1.1 201 Created", "Package created successfully");
        }

        public async Task HandleBuyPackageAsync(Stream stream, string body, HttpContext context)
        {
            var username = context.User.Identity.Name;
            var user = _userRepository.GetByUsername(username);
    
            if (user == null)
            {
                await SendResponseAsync(stream, "HTTP/1.1 404 Not Found", "User not found");
                return;
            }

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

            _packageRepository.MarkPackageAsSold(package.Id, user.Id);
            user.Coins -= 5;
            _userRepository.Update(user);
    
            await SendResponseAsync(stream, "HTTP/1.1 200 OK", JsonSerializer.Serialize(package.Cards));
        }

        public async Task HandleGetDeckAsync(Stream stream, HttpContext context)
        {
            var username = context.User.Identity.Name;
            var user = _userRepository.GetByUsername(username);
            
            if (user == null)
            {
                await SendResponseAsync(stream, "HTTP/1.1 404 Not Found", "User not found");
                return;
            }

            var deck = _cardRepository.GetUserDeck(user.Id);
            var response = JsonSerializer.Serialize(deck);
            await SendResponseAsync(stream, "HTTP/1.1 200 OK", response);
        }

        public async Task HandleConfigureDeckAsync(Stream stream, string body, HttpContext context)
        {
            var username = context.User.Identity.Name;
            var user = _userRepository.GetByUsername(username);
    
            if (user == null)
            {
                await SendResponseAsync(stream, "HTTP/1.1 404 Not Found", "User not found");
                return;
            }

            var cardIds = JsonSerializer.Deserialize<List<int>>(body);
    
            // Validate deck size
            if (cardIds == null || cardIds.Count != 4)
            {
                await SendResponseAsync(stream, "HTTP/1.1 400 Bad Request", "Deck must contain exactly 4 cards");
                return;
            }

            // Validate card ownership
            var userCards = _cardRepository.GetUserCards(user.Id).ToList();
            var invalidCards = cardIds.Where(id => !userCards.Any(c => c.Id == id)).ToList();
    
            if (invalidCards.Any())
            {
                await SendResponseAsync(stream, "HTTP/1.1 403 Forbidden", "Deck contains cards you don't own");
                return;
            }

            // Validate cards aren't in trades
            var tradedCards = _tradeRepository.GetAllTrades()
                .Where(t => cardIds.Contains(t.CardId))
                .ToList();
    
            if (tradedCards.Any())
            {
                await SendResponseAsync(stream, "HTTP/1.1 403 Forbidden", "Cannot add cards that are currently being traded");
                return;
            }

            _cardRepository.UpdateDeck(user.Id, cardIds);
            await SendResponseAsync(stream, "HTTP/1.1 200 OK", "Deck updated successfully");
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
            var tradeId = int.Parse(path.Split('/').Last());
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

        public async Task SendResponseAsync(Stream stream, string status, string content)
        {
            string response = $"{status}\r\n" +
                            "Content-Type: application/json\r\n" +
                            $"Content-Length: {content.Length}\r\n" +
                            "\r\n" +
                            content;

            byte[] responseBytes = Encoding.UTF8.GetBytes(response);
            await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
        }
    }
}