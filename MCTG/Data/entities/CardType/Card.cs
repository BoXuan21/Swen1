namespace MCTG
{
    public class Card
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Damage { get; set; }
        public ElementType ElementType { get; set; } 
        
        public string CardType { get; set; }
        public int? UserId { get; set; }
        
        public Card(string name, int damage, ElementType element, string originalId = null)
        {
            Name = name;
            Damage = damage;
            ElementType = element;
            CardType = "Monster";
        }

        // Default constructor for database mapping
        public Card() { }

        // Helper method to get ElementType as string for database
        public string GetElementTypeString()
        {
            return ElementType.ToString();
        }

        // Helper method to set ElementType from string (for database)
        public void SetElementTypeFromString(string elementType)
        {
            if (Enum.TryParse<ElementType>(elementType, out ElementType result))
            {
                ElementType = result;
            }
        }
    }   
}