namespace MCTG
{
    public class Stack
    {
        // Liste der Karten im Stapel
        public List<Card> Cards { get; set; }

        // Konstruktor initialisiert den Stapel als leere Liste
        public Stack()
        {
            Cards = new List<Card>();
        }

        // Methode zum Zufügen von zufälligen Karten in den Stack
        public void AddRandomCards(int count)
        {
            Random rand = new Random();
            Card.ElementType[] elements = { Card.ElementType.Fire, Card.ElementType.Water, Card.ElementType.Normal };
            string[] monsterTypes = { "Beast", "Wizard", "Warrior" }; // Beispiel-Monstertypen
            string[] spellTypes = { "Fireball", "Heal", "Freeze" }; // Beispiel-Zaubertypen

            for (int i = 0; i < count; i++)
            {
                Card.ElementType elementType = elements[rand.Next(elements.Length)];
                int damage = rand.Next(10, 100);

                if (rand.Next(2) == 0) // 50% Chance für MonsterCard
                {
                    string monsterName = "Monster" + rand.Next(1, 100);
                    string monsterType = monsterTypes[rand.Next(monsterTypes.Length)];
                    Cards.Add(new MonsterCard(monsterName, damage, elementType, monsterType));
                }
                else // 50% Chance für SpellCard
                {
                    string spellName = "Spell" + rand.Next(1, 100);
                    string spellType = spellTypes[rand.Next(spellTypes.Length)];
                    Cards.Add(new SpellCard(spellName, damage, elementType, spellType));
                }
            }
        }

        // Methode zum Entfernen einer Karte aus dem Stapel
        public bool RemoveCard(Card card)
        {
            return Cards.Remove(card);
        }

        // Methode zum Hinzufügen einer einzelnen Karte zum Stapel
        public void AddCard(Card card)
        {
            Cards.Add(card);
        }
    }
}