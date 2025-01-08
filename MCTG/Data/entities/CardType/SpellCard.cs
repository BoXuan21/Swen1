namespace MCTG
{
    public class SpellCard : Card
    {
        public string SpellType { get; set; }

        public SpellCard(string name, int damage, ElementType element, string spellType)
            : base(name, damage, element)
        {
            SpellType = spellType;
            CardType = "Spell";
        }
    }
}