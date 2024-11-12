namespace MCTG_IF23B049
{
    public class SpellCard : Card
    {
        public string SpellType { get; set; }

        // Konstruktor
        public SpellCard(string name, int damage, ElementType element, string spellType)
            : base(name, damage, element)
        {
            SpellType = spellType;
        }
    }
}