namespace MCTG
{
    public class CardController
    {
        // Moves a single card from one list (source) to another (destination).
        public static bool MoveCard(List<Card> source, List<Card> destination, Card card)
        {
            if (source == null || destination == null || card == null)
            {
                Console.WriteLine("Invalid operation: Null source, destination, or card.");
                return false;
            }

            if (source.Remove(card))
            {
                destination.Add(card);
                Console.WriteLine($"Card {card.Name} moved successfully.");
                return true;
            }
            
            Console.WriteLine($"Card {card.Name} not found in source.");
            return false;
        }


        // Moves multiple cards from one list (source) to another (destination).
        public static void MoveMultipleCards(List<Card> source, List<Card> destination, IEnumerable<Card> cards)
        {
            if (source == null || destination == null || cards == null)
            {
                Console.WriteLine("Invalid operation: Null source, destination, or cards.");
                return;
            }

            foreach (var card in cards.ToList())
            {
                MoveCard(source, destination, card);
            }
        }
        
        // Moves cards from a stack to a deck up to a maximum size.
        public static void FillDeckFromStack(Stack stack, Deck deck)
        {
            if (stack == null || deck == null)
            {
                Console.WriteLine("Invalid operation: Stack or Deck is null.");
                return;
            }

            while (deck.Cards.Count < Deck.MaxDeckSize && stack.Cards.Count > 0)
            {
                var card = stack.Cards[0]; // Take the first card from the stack
                MoveCard(stack.Cards, deck.Cards, card);
            }

            Console.WriteLine($"Deck populated with {deck.Cards.Count} cards from the stack.");
        }
    }
}
