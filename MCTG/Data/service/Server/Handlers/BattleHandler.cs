using System.Text.Json;

namespace MCTG
{
    public partial class TcpServer
    {
        public async Task HandleBattleAsync(CustomHttpContext context)
        {
            try 
            {
                var username = context.Items["Username"]?.ToString();
                Console.WriteLine($"Battle request from user: {username}");

                if (string.IsNullOrEmpty(username))
                {
                    Console.WriteLine("No username found in context");
                    context.Response.StatusCode = 401;
                    context.Response.StatusDescription = "Unauthorized";
                    context.Response.Body = "Authentication required";
                    return;
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var user1 = _userRepository.GetByUsername(username);
                var opponent = JsonSerializer.Deserialize<BattleRequest>(context.Request.Body, options);
                var user2 = _userRepository.GetByUsername(opponent.OpponentUsername);

                if (user1 == null || user2 == null)
                {
                    context.Response.StatusCode = 404;
                    context.Response.StatusDescription = "Not Found";
                    context.Response.Body = "User not found";
                    return;
                }

                var cardsInDeck1 = _cardRepository.GetUserDeck(user1.Id).ToList();
                var cardsInDeck2 = _cardRepository.GetUserDeck(user2.Id).ToList();
                
                if (cardsInDeck1.Count < 4 || cardsInDeck2.Count < 4)
                {
                    context.Response.StatusCode = 400;
                    context.Response.StatusDescription = "Bad Request";
                    context.Response.Body = "Both users must have at least 4 cards in their deck";
                    return;
                }

                var deck1 = new Deck(new Stack { Cards = cardsInDeck1 });
                var deck2 = new Deck(new Stack { Cards = cardsInDeck2 });

                var battleLogic = new BattleLogic(user1, user2, deck1, deck2);
                var battleLog = battleLogic.ExecuteBattle();

                // Update user stats
                int user1EloChange = 0;
                int user2EloChange = 0;
                int? winnerId = null;

                if (battleLog.Winner == "User 1")
                {
                    user1EloChange = 3;
                    user2EloChange = -5;
                    user1.Elo += user1EloChange;
                    user2.Elo += user2EloChange;
                    winnerId = user1.Id;
                }
                else if (battleLog.Winner == "User 2")
                {
                    user1EloChange = -5;
                    user2EloChange = 3;
                    user1.Elo += user1EloChange;
                    user2.Elo += user2EloChange;
                    winnerId = user2.Id;
                }

                _userRepository.Update(user1);
                _userRepository.Update(user2);

                var history = new BattleHistory
                {
                    Player1Id = user1.Id,
                    Player2Id = user2.Id,
                    WinnerId = winnerId,
                    BattleLog = JsonSerializer.Serialize(battleLog),
                    Player1EloChange = user1EloChange,
                    Player2EloChange = user2EloChange
                };

                _battleRepository.SaveBattleHistory(history);
                
                context.Response.StatusCode = 200;
                context.Response.StatusDescription = "OK";
                context.Response.Body = JsonSerializer.Serialize(battleLog);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HandleBattleAsync: {ex.Message}");
                context.Response.StatusCode = 500;
                context.Response.StatusDescription = "Internal Server Error";
                context.Response.Body = "An error occurred while processing the battle";
            }
        }

        public async Task HandleGetStatsAsync(CustomHttpContext context)
        {
            try
            {
                var username = context.Items["Username"]?.ToString();
                Console.WriteLine($"Getting stats for user: {username}");

                if (string.IsNullOrEmpty(username))
                {
                    context.Response.StatusCode = 401;
                    context.Response.StatusDescription = "Unauthorized";
                    context.Response.Body = "Authentication required";
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

                var stats = new { user.Elo, user.Coins };
                context.Response.StatusCode = 200;
                context.Response.StatusDescription = "OK";
                context.Response.Body = JsonSerializer.Serialize(stats);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting stats: {ex.Message}");
                context.Response.StatusCode = 500;
                context.Response.StatusDescription = "Internal Server Error";
                context.Response.Body = "An error occurred while retrieving stats";
            }
        }

        private async Task HandleGetScoreboardAsync(CustomHttpContext context)
        {
            try
            {
                var users = _userRepository.GetAllUsers();
                var scoreboard = users.OrderByDescending(u => u.Elo)
                    .Select(u => new { u.Username, u.Elo });
                
                context.Response.StatusCode = 200;
                context.Response.StatusDescription = "OK";
                context.Response.Body = JsonSerializer.Serialize(scoreboard);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting scoreboard: {ex.Message}");
                context.Response.StatusCode = 500;
                context.Response.StatusDescription = "Internal Server Error";
                context.Response.Body = "An error occurred while retrieving the scoreboard";
            }
        }
    }
}