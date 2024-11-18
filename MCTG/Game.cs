using System;

namespace MCTG
{
    public class Game
    {
        private User user1;
        private User user2;
        private Deck deck1;
        private Deck deck2;

        public void CreateGame()
        {
            user1 = new User();
            user2 = new User();
            
            user1.Stack.AddRandomCards(50);
            user2.Stack.AddRandomCards(50);
            
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

            while (deck1.GetCurrentCardCount() > 0 && deck2.GetCurrentCardCount() > 0)
            {
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
                    user1Wins++;
                }
                else if (damage2 > damage1)
                {
                    Console.WriteLine("User 2 wins this round!");
                    deck1.RemoveTopCard(); // Remove defeated card from deck1
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
            else
                Console.WriteLine("It's a tie!");
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
