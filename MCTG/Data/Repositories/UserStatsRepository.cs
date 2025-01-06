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
        return connection.QuerySingleOrDefault<UserStats>(
            "SELECT * FROM user_stats WHERE user_id = @UserId",
            new { UserId = userId });
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

    public IEnumerable<UserStats> GetLeaderboard()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return connection.Query<UserStats>(@"
            SELECT us.*, u.username 
            FROM user_stats us
            JOIN users u ON u.id = us.user_id
            ORDER BY us.elo DESC");
    }
}