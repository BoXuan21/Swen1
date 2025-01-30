using Npgsql;
namespace MCTG;

public class BattleRepository : IBattleRepository
{
    private readonly string _connectionString;

    public BattleRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void SaveBattleHistory(BattleHistory history)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            using var cmd = new NpgsqlCommand(@"
            INSERT INTO battle_history 
                (player1_id, player2_id, winner_id, battle_log, player1_elo_change, player2_elo_change)
            VALUES 
                (@Player1Id, @Player2Id, @WinnerId, @BattleLog, @Player1EloChange, @Player2EloChange)",
                connection, 
                transaction);

            cmd.Parameters.AddWithValue("@Player1Id", history.Player1Id);
            cmd.Parameters.AddWithValue("@Player2Id", history.Player2Id);
            if (history.WinnerId.HasValue)
                cmd.Parameters.AddWithValue("@WinnerId", history.WinnerId.Value);
            else
                cmd.Parameters.AddWithValue("@WinnerId", DBNull.Value);
            cmd.Parameters.AddWithValue("@BattleLog", history.BattleLog);
            cmd.Parameters.AddWithValue("@Player1EloChange", history.Player1EloChange);
            cmd.Parameters.AddWithValue("@Player2EloChange", history.Player2EloChange);

            cmd.ExecuteNonQuery();
            transaction.Commit();
        }
        catch (Exception)
        {
            transaction.Rollback();
            throw;
        }
    }

    public IEnumerable<BattleHistory> GetUserBattleHistory(int userId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        using var cmd = new NpgsqlCommand(@"
            SELECT * FROM battle_history 
            WHERE player1_id = @UserId OR player2_id = @UserId 
            ORDER BY created_at DESC",
            connection);

        cmd.Parameters.AddWithValue("@UserId", userId);

        var battles = new List<BattleHistory>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            battles.Add(new BattleHistory
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Player1Id = reader.GetInt32(reader.GetOrdinal("player1_id")),
                Player2Id = reader.GetInt32(reader.GetOrdinal("player2_id")),
                WinnerId = reader.GetInt32(reader.GetOrdinal("winner_id")),
                BattleLog = reader.GetString(reader.GetOrdinal("battle_log")),
                Player1EloChange = reader.GetInt32(reader.GetOrdinal("player1_elo_change")),
                Player2EloChange = reader.GetInt32(reader.GetOrdinal("player2_elo_change")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
            });
        }

        return battles;
    }
    
}