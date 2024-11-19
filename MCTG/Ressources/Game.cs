using System;

namespace MCTG
{
    public class Game
    {
        //user und deck initialisieren
        private User user1;
        private User user2;
        private Deck deck1;
        private Deck deck2;

        public void StartScreen()
        {
            bool exitGame = false;

            while (!exitGame)
            {
                Console.Clear();
                Console.WriteLine("Welcome to MCTG!");
                Console.WriteLine("Please select an option:");
                Console.WriteLine("1. Start Game (1vs1)");
                Console.WriteLine("2. Shop");
                Console.WriteLine("3. Trade Cards");
                Console.WriteLine("4. Exit");

                Console.Write("Enter your choice: ");
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        Console.Clear();
                        Console.WriteLine("Starting a new game...");
                        CreateGame();
                        StartGame();
                        break;

                    case "2":
                        Console.Clear();
                        Console.WriteLine("Welcome to the Shop! (Placeholder)");
                        Console.WriteLine("Here you can buy cards. (Feature coming soon!)");
                        Console.WriteLine("Press any key to return to the main menu...");
                        Console.ReadKey();
                        break;

                    case "3":
                        Console.Clear();
                        Console.WriteLine("Trade Cards! (Placeholder)");
                        Console.WriteLine("Here you can trade cards with other players. (Feature coming soon!)");
                        Console.WriteLine("Press any key to return to the main menu...");
                        Console.ReadKey();
                        break;

                    case "4":
                        Console.Clear();
                        Console.WriteLine("Exiting the game. Goodbye!");
                        exitGame = true;
                        break;

                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        Console.WriteLine("Press any key to return to the menu...");
                        Console.ReadKey();
                        break;
                }
            }
        }
        public void CreateGame()
        {
            user1 = new User();
            user2 = new User();
            
            //stack wird mit karten gefüllt
            user1.Stack.AddRandomCards(40);
            user2.Stack.AddRandomCards(40);
            
            deck1 = new Deck(user1.Stack);
            deck2 = new Deck(user2.Stack);
            
            //display deck
            Console.WriteLine("User 1's Deck:");
            deck1.DisplayDeck();
            Console.WriteLine($"Current card count in User 1's deck: {deck1.GetCurrentCardCount()}");
            Console.WriteLine($"Remaining cards in User 1's stack: {user1.Stack.Cards.Count}\n");

            Console.WriteLine("User 2's Deck:");
            deck2.DisplayDeck();
            Console.WriteLine($"Current card count in User 2's deck: {deck2.GetCurrentCardCount()}");
            Console.WriteLine($"Remaining cards in User 2's stack: {user2.Stack.Cards.Count}\n");
        }

        public void StartGame()
{
    int user1Wins = 0;
    int user2Wins = 0;
    int maxRounds = 100; // Maximum number of rounds
    int currentRound = 0; // Tracks the current round

    while (deck1.GetCurrentCardCount() > 0 && deck2.GetCurrentCardCount() > 0 && currentRound < maxRounds)
    {
        currentRound++;
        Console.WriteLine($"Round {currentRound}");

        // Retrieve the top cards from each deck
        Card card1 = deck1.GetTopCard();
        Card card2 = deck2.GetTopCard();

        Console.WriteLine($"User 1 plays: {card1.Name} ({card1.Element}, Damage: {card1.Damage})");
        Console.WriteLine($"User 2 plays: {card2.Name} ({card2.Element}, Damage: {card2.Damage})");

        // Calculate damage with type effectiveness
        double damage1 = CalculateEffectiveDamage(card1, card2);
        double damage2 = CalculateEffectiveDamage(card2, card1);

        Console.WriteLine($"User 1's card damage: {damage1}");
        Console.WriteLine($"User 2's card damage: {damage2}");

        // Determine the round winner
        if (damage1 > damage2)
        {
            Console.WriteLine("User 1 wins this round!");
            deck2.RemoveTopCard(); // Remove defeated card from deck2
            deck1.AddCard(card2);  // Add defeated card to deck1
            user1Wins++;
        }
        else if (damage2 > damage1)
        {
            Console.WriteLine("User 2 wins this round!");
            deck1.RemoveTopCard(); // Remove defeated card from deck1
            deck2.AddCard(card1);  // Add defeated card to deck2
            user2Wins++;
        }
        else
        {
            Console.WriteLine("It's a draw! No cards are removed.");
        }

        Console.WriteLine($"Current Score -> User 1: {user1Wins}, User 2: {user2Wins}\n");
    }

    // Determine the winner
    if (deck1.GetCurrentCardCount() == 0)
        Console.WriteLine("User 2 wins the game!");
    else if (deck2.GetCurrentCardCount() == 0)
        Console.WriteLine("User 1 wins the game!");
    else if (currentRound >= maxRounds)
    {
        Console.WriteLine("Maximum rounds reached!");
        if (user1Wins > user2Wins)
            Console.WriteLine("User 1 wins by score!");
        else if (user2Wins > user1Wins)
            Console.WriteLine("User 2 wins by score!");
        else
            Console.WriteLine("It's a tie!");
    }

    // Pause before returning to the main menu
    Console.WriteLine("\nGame over! Press any key to return to the main menu...");
    Console.ReadKey();
}


        private double CalculateEffectiveDamage(Card attackingCard, Card defendingCard)
        {
            double baseDamage = attackingCard.Damage;

            // Apply type effectiveness
            if (attackingCard.Element == Card.ElementType.Water && defendingCard.Element == Card.ElementType.Fire)
                baseDamage *= 2.0;
            else if (attackingCard.Element == Card.ElementType.Fire && defendingCard.Element == Card.ElementType.Normal)
                baseDamage *= 2.0;
            else if (attackingCard.Element == Card.ElementType.Normal && defendingCard.Element == Card.ElementType.Water)
                baseDamage *= 2.0;
            else if (attackingCard.Element == Card.ElementType.Fire && defendingCard.Element == Card.ElementType.Water)
                baseDamage *= 0.5;
            else if (attackingCard.Element == Card.ElementType.Normal && defendingCard.Element == Card.ElementType.Fire)
                baseDamage *= 0.5;
            else if (attackingCard.Element == Card.ElementType.Water && defendingCard.Element == Card.ElementType.Normal)
                baseDamage *= 0.5;

            return baseDamage;
        }
    }
}
