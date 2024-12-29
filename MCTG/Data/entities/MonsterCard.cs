namespace MCTG
{
    public class MonsterCard : Card
    {
        public string MonsterType { get; set; }

        public MonsterCard(string name, int damage, ElementType element, string monsterType)
            : base(name, damage, element)
        {
            MonsterType = monsterType;
            CardType = "Monster";
        }
    }
}