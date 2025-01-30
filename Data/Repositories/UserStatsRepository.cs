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
        using var transaction = connection.BeginTransaction();

        try
        {
            using var cmd = new NpgsqlCommand(
                "SELECT * FROM user_stats WHERE user_id = @UserId",
                connection,
                transaction);
            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new UserStats
                {
                    UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                    GamesPlayed = reader.GetInt32(reader.GetOrdinal("games_played")),
                    Wins = reader.GetInt32(reader.GetOrdinal("wins")),
                    Losses = reader.GetInt32(reader.GetOrdinal("losses")),
                    Draws = reader.GetInt32(reader.GetOrdinal("draws")),
                    Elo = reader.GetInt32(reader.GetOrdinal("elo"))
                };
            }

            reader.Close();

            // Create default stats if none exist
            var stats = new UserStats
            {
                UserId = userId,
                GamesPlayed = 0,
                Wins = 0,
                Losses = 0,
                Draws = 0,
                Elo = 100
            };
    
            // Save the default stats
            using var insertCmd = new NpgsqlCommand(@"
                INSERT INTO user_stats (user_id, games_played, wins, losses, draws, elo)
                VALUES (@UserId, @GamesPlayed, @Wins, @Losses, @Draws, @Elo)",
                connection,
                transaction);

            insertCmd.Parameters.AddWithValue("@UserId", stats.UserId);
            insertCmd.Parameters.AddWithValue("@GamesPlayed", stats.GamesPlayed);
            insertCmd.Parameters.AddWithValue("@Wins", stats.Wins);
            insertCmd.Parameters.AddWithValue("@Losses", stats.Losses);
            insertCmd.Parameters.AddWithValue("@Draws", stats.Draws);
            insertCmd.Parameters.AddWithValue("@Elo", stats.Elo);

            insertCmd.ExecuteNonQuery();
            transaction.Commit();
            return stats;
        }
        catch (Exception)
        {
            transaction.Rollback();
            throw;
        }
    }

    public void UpdateStats(UserStats stats)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            using var cmd = new NpgsqlCommand(@"
                UPDATE user_stats 
                SET games_played = @GamesPlayed,
                    wins = @Wins,
                    losses = @Losses,
                    draws = @Draws,
                    elo = @Elo
                WHERE user_id = @UserId",
                connection,
                transaction);

            cmd.Parameters.AddWithValue("@UserId", stats.UserId);
            cmd.Parameters.AddWithValue("@GamesPlayed", stats.GamesPlayed);
            cmd.Parameters.AddWithValue("@Wins", stats.Wins);
            cmd.Parameters.AddWithValue("@Losses", stats.Losses);
            cmd.Parameters.AddWithValue("@Draws", stats.Draws);
            cmd.Parameters.AddWithValue("@Elo", stats.Elo);

            int rowsAffected = cmd.ExecuteNonQuery();

            // If no rows were affected, the user_stats entry doesn't exist yet, so create it
            if (rowsAffected == 0)
            {
                using var insertCmd = new NpgsqlCommand(@"
                    INSERT INTO user_stats (user_id, games_played, wins, losses, draws, elo)
                    VALUES (@UserId, @GamesPlayed, @Wins, @Losses, @Draws, @Elo)",
                    connection,
                    transaction);

                insertCmd.Parameters.AddWithValue("@UserId", stats.UserId);
                insertCmd.Parameters.AddWithValue("@GamesPlayed", stats.GamesPlayed);
                insertCmd.Parameters.AddWithValue("@Wins", stats.Wins);
                insertCmd.Parameters.AddWithValue("@Losses", stats.Losses);
                insertCmd.Parameters.AddWithValue("@Draws", stats.Draws);
                insertCmd.Parameters.AddWithValue("@Elo", stats.Elo);

                insertCmd.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch (Exception)
        {
            transaction.Rollback();
            throw;
        }
    }

    public void CreateStats(int userId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            using var cmd = new NpgsqlCommand(@"
                INSERT INTO user_stats (user_id, games_played, wins, losses, draws, elo)
                VALUES (@UserId, 0, 0, 0, 0, 100)",
                connection,
                transaction);

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.ExecuteNonQuery();
            transaction.Commit();
        }
        catch (Exception)
        {
            transaction.Rollback();
            throw;
        }
    }
}