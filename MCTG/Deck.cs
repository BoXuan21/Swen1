namespace MCTG
{
    public class Deck
    {
        public List<Card> Cards { get; private set; }

        // Maximale Anzahl an Karten im Deck
        private const int MaxDeckSize = 10;
        
        public Deck(Stack stack)
        {
            Cards = new List<Card>();

            // Füllt das Deck mit Karten
            FillDeckFromStack(stack);
        }

        // Füllt das Deck aus einem Stack
        private void FillDeckFromStack(Stack stack)
        {
            if (stack == null || stack.Cards.Count == 0)
            {
                Console.WriteLine("Stack is empty or null. Cannot populate the deck.");
                return;
            }

            // Verschiebt Karten vom Stack ins Deck, bis das Deck voll ist oder der Stack leer ist
            while (Cards.Count < MaxDeckSize && stack.Cards.Count > 0)
            {
                var card = stack.Cards[0]; // Erste Karte aus dem Stack
                Cards.Add(card);          // Karte ins Deck hinzufügen
                stack.RemoveCard(card);   // Karte aus dem Stack entfernen
            }

            Console.WriteLine($"Deck populated with {Cards.Count} cards from the stack.");
        }

        // Methode zum Abrufen der obersten Karte des Decks ohne sie zu entfernen
        public Card GetTopCard()
        {
            if (Cards.Count == 0)
                throw new InvalidOperationException("The deck is empty.");
            return Cards[0];
        }

        // Methode zum Entfernen der obersten Karte des Decks
        public void RemoveTopCard()
        {
            if (Cards.Count == 0)
                throw new InvalidOperationException("The deck is empty.");
            Cards.RemoveAt(0); // Entfernt die erste Karte im Deck
        }

        // Gibt alle Karten im Deck aus
        public void DisplayDeck()
        {
            Console.WriteLine("Deck contains the following cards:");
            foreach (var card in Cards)
            {
                Console.WriteLine($"{card.Name} - Damage: {card.Damage} - Element: {card.Element}");
            }
        }

        // Methode zum Abrufen der verbleibenden Karten im Deck
        public int RemainingCards()
        {
            return MaxDeckSize - Cards.Count; // Gibt die verbleibenden Kartenplätze im Deck zurück
        }

        // Methode zum Überprüfen der Kartenzahl im Deck
        public int GetCurrentCardCount()
        {
            return Cards.Count;
        }
    }
}
