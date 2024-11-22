namespace MCTG
{
    public class User
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public Stack Stack { get; set; }
        public List<Card> Deck { get; set; }
        public int Coin { get; set; }
        public string Elo { get; set; }

        public User()
        {
            Coin = 20;
            Elo = "Iron";
            Deck = new List<Card>();
            Stack = new Stack();
        }

        public void BuyPackage()
        {
            const int cardsPerPackage = 5;
            const int packCost = 5;

            if (Coin >= packCost)
            {
                Coin -= packCost;
                Stack.AddRandomCards(cardsPerPackage);
                Console.WriteLine("Successfully bought a pack.");
            }
            else
            {
                Console.WriteLine("Insufficient coins.");
            }
        }

        public void AddCardsToDeck()
        {
            CardController.MoveMultipleCards(Stack.Cards, Deck, Stack.Cards.Take(Deck.Count < 10 ? 10 - Deck.Count : 0));
        }
    }
}