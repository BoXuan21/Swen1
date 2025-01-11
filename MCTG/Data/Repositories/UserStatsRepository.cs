using Dapper;
using Npgsql;
namespace MCTG;

public class UserStatsRepository : IUserStatsRepository
{
    private readonly string _connectionString;

    public UserStatsRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public UserStats GetUserStats(int userId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
    
        var stats = connection.QuerySingleOrDefault<UserStats>(
            "SELECT * FROM user_stats WHERE user_id = @UserId",
            new { UserId = userId });

        if (stats == null)
        {
            // Create default stats if none exist
            stats = new UserStats
            {
                UserId = userId,
                GamesPlayed = 0,
                Wins = 0,
                Losses = 0,
                Draws = 0,
                Elo = 100
            };
        
            // Save the default stats
            connection.Execute(@"
            INSERT INTO user_stats (user_id, games_played, wins, losses, draws, elo)
            VALUES (@UserId, @GamesPlayed, @Wins, @Losses, @Draws, @Elo)",
                stats);
        }
    
        return stats;
    }

    public void UpdateStats(UserStats stats)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Execute(@"
            UPDATE user_stats 
            SET games_played = @GamesPlayed,
                wins = @Wins,
                losses = @Losses,
                draws = @Draws,
                elo = @Elo
            WHERE user_id = @UserId",
            stats);
    }

    public void CreateStats(int userId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Execute(@"
            INSERT INTO user_stats (user_id, games_played, wins, losses, draws, elo)
            VALUES (@UserId, 0, 0, 0, 0, 100)",
            new { UserId = userId });
    }
    
}