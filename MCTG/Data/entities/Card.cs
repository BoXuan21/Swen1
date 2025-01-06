namespace MCTG
{
    public enum ElementType
    {
        Fire,
        Water,
        Normal
    }
    
    public class Card
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Damage { get; set; }
        public ElementType Element { get; set; }
        public string CardType { get; set; }
        public int? UserId { get; set; }

        // Original constructor
        public Card(string name, int damage, ElementType element)
        {
            Name = name;
            Damage = damage;
            Element = element;
            CardType = "Monster";
        }

        // Default constructor for database mapping
        public Card() { }

        // Helper method to get ElementType as string for database
        public string GetElementTypeString()
        {
            return Element.ToString();
        }

        // Helper method to set ElementType from string (for database)
        public void SetElementTypeFromString(string elementType)
        {
            if (Enum.TryParse<ElementType>(elementType, out ElementType result))
            {
                Element = result;
            }
        }
    }   
}