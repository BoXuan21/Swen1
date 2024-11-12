namespace MCTG_IF23B049
{
    public class Card
    {
        public string Name { get; set; }
        
        public int Damage { get; set; }
        
        public ElementType Element { get; set; }

        public enum ElementType
        {
            Fire,
            Water,
            Normal
        }

        // Konstruktor
        public Card(string name, int damage, ElementType element)
        {
            Name = name;
            Damage = damage;
            Element = element;
        }
    }
}