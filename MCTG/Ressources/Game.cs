using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MCTG
{
    public class Game
    {
        private readonly DatabaseController _db;
        private User user1;
        private User user2;
        private Deck deck1;
        private Deck deck2;
        private readonly UserController userController;

        public Game()
        {
            _db = new DatabaseController();
            userController = new UserController();
            // Initialize database on startup
            _db.InitializeDatabase().Wait();
        }

        public async Task StartScreen()
        {
            // Get logged in user
            user1 = await userController.Login();
            if (user1 == null)
            {
                Console.WriteLine("Login failed. Exiting game...");
                return;
            }

            bool exitGame = false;
            while (!exitGame)
            {
                Console.Clear();
                Console.WriteLine($"Welcome to MCTG, {user1.Username}!");
                Console.WriteLine($"Your coins: {user1.Coin}");
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
                        await CreateGame();
                        await StartGame();
                        break;

                    case "2":
                        Console.Clear();
                        Console.WriteLine("Welcome to the Shop!");
                        await BuyCards();
                        break;

                    case "3":
                        Console.Clear();
                        // Trading functionality to be implemented
                        Console.WriteLine("Trading feature coming soon!");
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

        public async Task CreateGame()
        {
            // Initialize computer opponent
            user2 = new User { Username = "Computer", Id = -1 };

            // Get user1's cards from database
            var existingCards = await _db.GetUserCards(user1.Id);
            
            // Initialize user1's stack with existing cards or new ones if none exist
            user1.Stack = new Stack();
            if (existingCards.Count == 0)
            {
                Console.WriteLine("Initializing new cards for first-time player...");
                user1.Stack.AddRandomCards(30);
                
                // Save new cards to database
                foreach (var card in user1.Stack.Cards)
                {
                    await _db.AddCardToUser(user1.Id, card);
                }
            }
            else
            {
                Console.WriteLine("Loading existing cards from database...");
                user1.Stack.Cards = existingCards;
            }

            // Initialize AI opponent's stack
            user2.Stack = new Stack();
            user2.Stack.AddRandomCards(30);

            // Create decks from stacks
            deck1 = new Deck(user1.Stack);
            deck2 = new Deck(user2.Stack);

            // Display deck information
            Console.WriteLine($"\n{user1.Username}'s Deck:");
            deck1.DisplayDeck();
            Console.WriteLine($"Current card count in {user1.Username}'s deck: {deck1.GetCurrentCardCount()}");
            Console.WriteLine($"Remaining cards in {user1.Username}'s stack: {user1.Stack.Cards.Count}\n");

            Console.WriteLine($"{user2.Username}'s Deck:");
            deck2.DisplayDeck();
            Console.WriteLine($"Current card count in {user2.Username}'s deck: {deck2.GetCurrentCardCount()}");
            Console.WriteLine($"Remaining cards in {user2.Username}'s stack: {user2.Stack.Cards.Count}\n");
        }

        public async Task StartGame()
        {
            int user1Wins = 0;
            int user2Wins = 0;
            int maxRounds = 100;
            int currentRound = 0;

            while (deck1.GetCurrentCardCount() > 0 && deck2.GetCurrentCardCount() > 0 && currentRound < maxRounds)
            {
                currentRound++;
                Console.WriteLine($"\nRound {currentRound}");

                Card card1 = deck1.GetTopCard();
                Card card2 = deck2.GetTopCard();

                Console.WriteLine($"{user1.Username} plays: {card1.Name} ({card1.Element}, Damage: {card1.Damage})");
                Console.WriteLine($"{user2.Username} plays: {card2.Name} ({card2.Element}, Damage: {card2.Damage})");

                double damage1 = CalculateEffectiveDamage(card1, card2);
                double damage2 = CalculateEffectiveDamage(card2, card1);

                Console.WriteLine($"{user1.Username}'s card damage: {damage1}");
                Console.WriteLine($"{user2.Username}'s card damage: {damage2}");

                if (damage1 > damage2)
                {
                    Console.WriteLine($"{user1.Username} wins this round!");
                    deck2.RemoveTopCard();
                    deck1.AddCard(card2);
                    // Save won card to database
                    await _db.AddCardToUser(user1.Id, card2);
                    user1Wins++;
                }
                else if (damage2 > damage1)
                {
                    Console.WriteLine($"{user2.Username} wins this round!");
                    deck1.RemoveTopCard();
                    deck2.AddCard(card1);
                    user2Wins++;
                }
                else
                {
                    Console.WriteLine("It's a draw! No cards are exchanged.");
                }

                Console.WriteLine($"Current Score -> {user1.Username}: {user1Wins}, {user2.Username}: {user2Wins}");
            }

            // Record battle result
            var winnerId = user1Wins > user2Wins ? user1.Id : user2.Id;
            await _db.RecordBattle(user1.Id, user2.Id, winnerId);

            // Display final result
            if (deck1.GetCurrentCardCount() == 0 || user2Wins > user1Wins)
                Console.WriteLine($"\n{user2.Username} wins the game!");
            else if (deck2.GetCurrentCardCount() == 0 || user1Wins > user2Wins)
                Console.WriteLine($"\n{user1.Username} wins the game!");
            else
                Console.WriteLine("\nIt's a tie!");

            Console.WriteLine("\nGame over! Press any key to return to the main menu...");
            Console.ReadKey();
        }

        public double CalculateEffectiveDamage(Card attackingCard, Card defendingCard)
        {
            double baseDamage = attackingCard.Damage;

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

        public async Task BuyCards()
        {
            if (user1.Coin < 5)
            {
                Console.WriteLine("Insufficient coins. You need 5 coins to buy a package.");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            user1.Coin -= 5;
            await _db.UpdateUserCoins(user1.Id, user1.Coin);

            // Add 5 random cards
            for (int i = 0; i < 5; i++)
            {
                Card newCard = GenerateRandomCard();
                await _db.AddCardToUser(user1.Id, newCard);
                user1.Stack.Cards.Add(newCard);
            }

            Console.WriteLine("Successfully purchased 5 new cards!");
            Console.WriteLine($"Remaining coins: {user1.Coin}");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private Card GenerateRandomCard()
        {
            Random rand = new Random();
            Card.ElementType[] elements = { Card.ElementType.Fire, Card.ElementType.Water, Card.ElementType.Normal };
            string[] monsterTypes = { "Goblin", "Wizard", "Dragon", "Orc", "Kraken", "Knight" };
            string[] spellTypes = { "Fireball", "Heal", "Freeze", "Lightning" };

            Card.ElementType elementType = elements[rand.Next(elements.Length)];
            int damage = rand.Next(10, 50);

            if (rand.Next(2) == 0)
            {
                string monsterName = $"Monster{rand.Next(1, 100)}";
                string monsterType = monsterTypes[rand.Next(monsterTypes.Length)];
                return new MonsterCard(monsterName, damage, elementType, monsterType);
            }
            else
            {
                string spellName = $"Spell{rand.Next(1, 100)}";
                string spellType = spellTypes[rand.Next(spellTypes.Length)];
                return new SpellCard(spellName, damage, elementType, spellType);
            }
        }
    }
}