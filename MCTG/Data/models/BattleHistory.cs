namespace MCTG;

public class BattleHistory
{
    public int Id { get; set; }
    public int Player1Id { get; set; }
    public int Player2Id { get; set; }
    public int WinnerId { get; set; }
    public string BattleLog { get; set; }
    public int Player1EloChange { get; set; }
    public int Player2EloChange { get; set; }
    public DateTime CreatedAt { get; set; }
}
