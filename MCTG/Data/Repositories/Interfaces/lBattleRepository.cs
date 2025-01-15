namespace MCTG;

public interface IBattleRepository
{
    void SaveBattleHistory(BattleHistory history);
    IEnumerable<BattleHistory> GetUserBattleHistory(int userId);
}