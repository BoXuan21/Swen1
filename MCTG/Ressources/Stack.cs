namespace MCTG
{
    public class Stack
    {
        public List<Card> Cards { get; set; }
        
        public Stack()
        {
            Cards = new List<Card>();
        }

        // Fügt zufällige kartenanzahl
        public void AddRandomCards(int count)
        {
            // Initialisierung des Zufallsgenerators
            Random rand = new Random();
            
            // Definierung der möglichen Elemente
            Card.ElementType[] elements = { Card.ElementType.Fire, Card.ElementType.Water, Card.ElementType.Normal };
            
            string[] monsterTypes = { "Goblin", "Wizard", "Dragons", "Orcs", "Kraken", "Knights" };
            
            string[] spellTypes = { "Fireball", "Heal", "Freeze", "lightning" };
            
            for (int i = 0; i < count; i++)
            {
                // Zufälliges Element (Feuer, Wasser, Normal) für die Karte auswählen
                Card.ElementType elementType = elements[rand.Next(elements.Length)];
                
                int damage = rand.Next(10, 50);

                // Mit 50% Wahrscheinlichkeit eine Monsterkarte oder eine Zauberkarte erzeugen
                if (rand.Next(2) == 0)
                {
                    // Zufälligen Namen und Typ für die Monsterkarte generieren
                    string monsterName = "Monster" + rand.Next(1, 15);
                    string monsterType = monsterTypes[rand.Next(monsterTypes.Length)];
                    
                    // Monsterkarte zum stack hinzufügen
                    Cards.Add(new MonsterCard(monsterName, damage, elementType, monsterType));
                }
                else //spellcard
                {
                    // Namen für Spell generieren
                    string spellName = "Spell" + rand.Next(1, 15);
                    string spellType = spellTypes[rand.Next(spellTypes.Length)];
                    
                    // zum stack hinzufügen
                    Cards.Add(new SpellCard(spellName, damage, elementType, spellType));
                }
            }
        }

        //Entfernen einer bestimmten Karte aus dem Stapel
        public bool RemoveCard(Card card)
        {
            return Cards.Remove(card);
        }

        //Hinzufügen einer bestimmten einzelnen Karte zum Stapel
        public void AddCard(Card card)
        {
            // Karte zur Liste hinzufügen
            Cards.Add(card);
        }
    }
}
