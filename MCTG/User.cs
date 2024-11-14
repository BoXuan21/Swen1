namespace MCTG
{
    public class User
    {
        // Benutzername des Benutzers
        public string Username { get; set; }

        // Passwort des Benutzers
        public string Password { get; set; }

        // Stack
        public Stack Stack { get; set; }

        // Deck des Benutzers, das bis zu 10 Karten enthalten kann
        public List<Card> Deck { get; set; }

        // Münzen des Benutzers
        public int Coin { get; set; }

        // Elo-Rang des Benutzers
        public string Elo { get; set; }

        // Konstruktor initialisiert ein neues Benutzerobjekt mit Standardwerten
        public User()
        {
            Coin = 20; // Startmünzen
            Elo = "Iron"; // Start-Elo-Rang
            this.Deck = new List<Card>(); // Initialisiert das Deck als leere Liste
            this.Stack = new Stack(); // Initialisiert den Stapel als neuen Stapel
        }

        // Methode, um Kartenpositionen im Stapel zu tauschen
        public void MoveCardtoStack(int firstCardPosition, int secondCardPosition)
        {
            // Überprüft, ob die angegebenen Positionen gültig sind
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
                // Gibt eine Fehlermeldung aus, wenn die Positionen ungültig sind
                Console.WriteLine("Invalid card position");
            }
        }

        // Methode zum Kauf eines Kartenpakets
        public void BuyPackage()
        {
            const int cardsPerPackage = 5; // Anzahl der Karten pro Paket
            const int packCost = 5; // Kosten eines Pakets

            // Überprüft, ob der Benutzer genug Münzen hat, um ein Paket zu kaufen
            if (Coin >= packCost)
            {
                Coin -= packCost; // Zieht die Kosten von den Münzen des Benutzers ab
                Stack.AddRandomCards(cardsPerPackage); // Fügt dem Stapel zufällige Karten hinzu
                Console.WriteLine("Successfully bought pack");
            }
            else
            {
                // Gibt eine Fehlermeldung aus, wenn der Benutzer nicht genug Münzen hat
                Console.WriteLine("Insufficient coins");
            }
        }

        // Methode zum Hinzufügen von Karten vom Stapel zum Deck
        public void AddCardsToDeck()
        {
            const int deckSize = 10; // Maximale Größe des Decks

            // Überprüft, ob das Deck noch Platz für weitere Karten hat
            if (Deck.Count < deckSize)
            {
                // Verschiebt Karten vom Stapel ins Deck, solange Platz im Deck ist
                foreach (var card in Stack.Cards.ToList())
                {
                    if (Deck.Count < deckSize)
                    {
                        Deck.Add(card); // Fügt die Karte dem Deck hinzu
                        Stack.RemoveCard(card); // Entfernt die Karte vom Stapel
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
                // Gibt eine Fehlermeldung aus, wenn das Deck voll ist
                Console.WriteLine("Deck Full!");
            }
        }
    }
}