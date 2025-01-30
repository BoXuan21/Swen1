namespace MCTG
{
    public class Deck
    {
        public List<Card> Cards { get; private set; }
        public static int MaxDeckSize => 10;

        public Deck(Stack stack)
        {
            Cards = new List<Card>();
            CardController.FillDeckFromStack(stack, this);
        }

        public Card GetTopCard()
        {
            if (Cards.Count == 0)
                throw new InvalidOperationException("The deck is empty.");
            return Cards[0];
        }

        public void RemoveTopCard()
        {
            if (Cards.Count == 0)
                throw new InvalidOperationException("The deck is empty.");
            Cards.RemoveAt(0);
        }

        public void DisplayDeck()
        {
            Console.WriteLine("Deck contains the following cards:");
            foreach (var card in Cards)
            {
                Console.WriteLine($"{card.Name} - Damage: {card.Damage} - Element: {card.ElementType}");
            }
        }

        public int RemainingCards()
        {
            return MaxDeckSize - Cards.Count;
        }

        public int GetCurrentCardCount()
        {
            return Cards.Count;
        }

        public void AddCard(Card card)
        {
            if (card != null)
            {
                Cards.Add(card);
                Console.WriteLine($"{card.Name} has been added to the deck.");
            }
            else
            {
                Console.WriteLine("Cannot add a null card to the deck.");
            }
        }
    }
}