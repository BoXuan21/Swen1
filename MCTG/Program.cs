using System;
using System.Threading.Tasks;

namespace MCTG
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            // Create a user
            User user = new User();

            // Add random cards to the user's stack
            user.Stack.AddRandomCards(50); // Add 50 cards to the stack

            // Create a deck using the user's stack
            Deck deck = new Deck(user.Stack);

            // Display the deck
            deck.DisplayDeck();
            
            //Display current card amount
            Console.WriteLine($"Current card count in deck: {deck.GetCurrentCardCount()}");
            
            // Display remaining cards in the stack
            Console.WriteLine($"Remaining cards in stack: {user.Stack.Cards.Count}");
            
            //var controller = new HttpsController();
            //await controller.StartServer();
        }

    }
}