using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace MCTG
{
    public partial class TcpServer
    {
        public async Task HandleBattleAsync(Stream stream, HttpContext context, string body)
        {
            try 
            {
                var username = context.Items["Username"] as string;
                Console.WriteLine($"Battle request from user: {username}");

                if (string.IsNullOrEmpty(username))
                {
                    Console.WriteLine("No username found in context");
                    await SendResponseAsync(stream, "HTTP/1.1 401 Unauthorized", "Authentication required");
                    return;
                }

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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HandleBattleAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await SendResponseAsync(stream, "HTTP/1.1 500 Internal Server Error", 
                    "An error occurred while processing the battle");
            }
        }

        public async Task HandleGetBattleHistoryAsync(Stream stream, HttpContext context)
        {
            try 
            {
                var username = context.Items["Username"] as string;
                Console.WriteLine($"Getting battle history for user: {username}");

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

                var battles = _battleRepository.GetUserBattleHistory(user.Id);
                var response = JsonSerializer.Serialize(battles);
                await SendResponseAsync(stream, "HTTP/1.1 200 OK", response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting battle history: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await SendResponseAsync(stream, "HTTP/1.1 500 Internal Server Error", 
                    "An error occurred while retrieving battle history");
            }
        }

        public async Task HandleGetStatsAsync(Stream stream, HttpContext context)
        {
            try
            {
                var username = context.Items["Username"] as string;
                Console.WriteLine($"Getting stats for user: {username}");

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

                var stats = new { user.Elo, user.Coins };
                var response = JsonSerializer.Serialize(stats);
                await SendResponseAsync(stream, "HTTP/1.1 200 OK", response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting stats: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await SendResponseAsync(stream, "HTTP/1.1 500 Internal Server Error", 
                    "An error occurred while retrieving stats");
            }
        }

        public async Task HandleGetScoreboardAsync(Stream stream)
        {
            try
            {
                var users = _userRepository.GetAllUsers();
                var scoreboard = users.OrderByDescending(u => u.Elo)
                    .Select(u => new { u.Username, u.Elo });
                var response = JsonSerializer.Serialize(scoreboard);
                await SendResponseAsync(stream, "HTTP/1.1 200 OK", response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting scoreboard: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await SendResponseAsync(stream, "HTTP/1.1 500 Internal Server Error", 
                    "An error occurred while retrieving the scoreboard");
            }
        }
    }
}