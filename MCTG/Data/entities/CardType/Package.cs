namespace MCTG;

public class Package
{
    public int Id { get; set; }
    public List<Card> Cards { get; set; }
    public bool IsSold { get; set; }

}

public enum ElementType
{
    Regular,
    Fire,
    Water,
    Normal
}