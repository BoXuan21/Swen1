using Npgsql;
using Dapper;
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
        connection.Execute(@"
            INSERT INTO battle_history 
                (player1_id, player2_id, winner_id, battle_log, player1_elo_change, player2_elo_change)
            VALUES 
                (@Player1Id, @Player2Id, @WinnerId, @BattleLog, @Player1EloChange, @Player2EloChange)",
            history);
    }

    public IEnumerable<BattleHistory> GetUserBattleHistory(int userId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return connection.Query<BattleHistory>(@"
            SELECT * FROM battle_history 
            WHERE player1_id = @userId OR player2_id = @userId 
            ORDER BY created_at DESC",
            new { userId });
    }

    public BattleHistory GetBattleById(int battleId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return connection.QuerySingleOrDefault<BattleHistory>(@"
            SELECT * FROM battle_history WHERE id = @battleId",
            new { battleId });
    }
}