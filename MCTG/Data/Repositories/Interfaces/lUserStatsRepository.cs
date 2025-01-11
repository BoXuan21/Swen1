namespace MCTG;

public interface IUserStatsRepository
{
    UserStats GetUserStats(int userId);
    void UpdateStats(UserStats stats);
}