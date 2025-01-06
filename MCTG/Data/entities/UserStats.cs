namespace MCTG;

public class UserStats
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int GamesPlayed { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Draws { get; set; }
    public int Elo { get; set; }
    public double WinRate => GamesPlayed > 0 ? (double)Wins / GamesPlayed * 100 : 0;
}