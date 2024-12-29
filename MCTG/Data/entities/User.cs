namespace MCTG
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public Stack Stack { get; set; }
        public List<Card> Deck { get; set; }
        public int Coins { get; set; } = 20;
        public int Elo { get; set; } = 100;  // Changed from string to int for easier calculation
        
        // Constructor for new users
        public User()
        {
            Deck = new List<Card>();
            Stack = new Stack();
        }
        

        // Constructor for database mapping
        public User(string username, string password)
        {
            Username = username;
            Password = password;
            Coins = 20;
            Deck = new List<Card>();
            Stack = new Stack();
        }

        public void BuyPackage()
        {
            const int cardsPerPackage = 5;
            const int packCost = 5;

            if (Coins >= packCost)
            {
                Coins -= packCost;
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
        
        // For database mapping
        public User ToDbModel()
        {
            return new User
            {
                Id = this.Id,
                Username = this.Username,
                Password = this.Password,
                Coins = this.Coins,
                Elo = this.Elo
            };
        }
    }
}