namespace MCTG;

public class Battle
{
    public int Id { get; set; }
    public int User1Id { get; set; }
    public int User2Id { get; set; }
    public string Winner { get; set; }
    public DateTime Timestamp { get; set; }
}