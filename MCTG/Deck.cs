namespace MCTG
{
    public class Deck
    {
        // Liste der Karten im Deck
        public List<Card> Cards { get; private set; }

        // Maximale Anzahl an Karten im Deck
        private const int MaxDeckSize = 20;

        // Konstruktor
        public Deck(Stack stack)
        {
            Cards = new List<Card>();

            // Füllt das Deck mit Karten aus dem Stack bis zur maximalen Größe
            FillDeckFromStack(stack);
        }

        // Methode zum Füllen des Decks aus einem Stack
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
                var card = stack.Cards[0]; // Nimmt die erste Karte im Stack
                Cards.Add(card); // Fügt sie dem Deck hinzu
                stack.RemoveCard(card); // Entfernt die Karte aus dem Stack
            }

            Console.WriteLine($"Deck populated with {Cards.Count} cards from the stack.");
        }

        // Methode zum Anzeigen des Decks
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
            return MaxDeckSize - Cards.Count;  // Gibt die verbleibenden Kartenplätze im Deck zurück
        }

        // Methode zum Überprüfen der Kartenzahl im Deck
        public int GetCurrentCardCount()
        {
            return Cards.Count;
        }
    }
}
