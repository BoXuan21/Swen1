namespace MCTG
{
    public class User
    {
        // Benutzername 
        public string Username { get; set; }

        // Passwort
        public string Password { get; set; }

        // Stack
        public Stack Stack { get; set; }

        // Deck 
        public List<Card> Deck { get; set; }

        // Münzen 
        public int Coin { get; set; }

        // Elo-Rang 
        public string Elo { get; set; }
        public User()
        {
            Coin = 20;
            Elo = "Iron"; 
            this.Deck = new List<Card>(); //initializierung
            this.Stack = new Stack(); 
        }

        // Kartenpositionen im Stapel tauschen
        public void MoveCardtoStack(int firstCardPosition, int secondCardPosition)
        {
            //prüft, ob die angegebenen Positionen gültig sind
            if (firstCardPosition >= 0 && firstCardPosition < Stack.Cards.Count && secondCardPosition >= 0 &&
                secondCardPosition < Stack.Cards.Count)
            {
                // Tauscht die Karten an den angegebenen Positionen
                Card temp = Stack.Cards[firstCardPosition];
                Stack.Cards[firstCardPosition] = Stack.Cards[secondCardPosition];
                Stack.Cards[secondCardPosition] = temp;
            }
            else
            {
                // Gibt eine Fehlermeldung aus, wenn position ungültig ist
                Console.WriteLine("Invalid card position");
            }
        }
        
        public void BuyPackage()
        {
            const int cardsPerPackage = 5;
            const int packCost = 5;

            // Überprüft, ob der Benutzer genug Münzen hat, um ein Paket zu kaufen
            if (Coin >= packCost)
            {
                Coin -= packCost; // Zieht die Kosten von den Münzen ab
                Stack.AddRandomCards(cardsPerPackage); // Fügt dem Stapel zufällige Karten hinzu
                Console.WriteLine("Successfully bought pack");
            }
            else
            {
                Console.WriteLine("Insufficient coins");
            }
        }

        // Methode zum Hinzufügen von Karten vom Stapel zum Deck
        public void AddCardsToDeck()
        {
            const int deckSize = 10;

            // Überprüft, ob das Deck noch Platz hat
            if (Deck.Count < deckSize)
            {
                // Verschiebt Karten vom Stapel ins Deck, solange Platz im Deck ist
                foreach (var card in Stack.Cards.ToList())
                {
                    if (Deck.Count < deckSize)
                    {
                        Deck.Add(card);
                        Stack.RemoveCard(card);
                    }
                    else
                    {
                        Console.WriteLine("Deck Full");
                        break;
                    }
                }
            }
            else
            {
                Console.WriteLine("Deck Full!");
            }
        }
    }
}