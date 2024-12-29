using System;

namespace MCTG
{
    public class Game
    {
        private User user1;
        private User user2;
        private Deck deck1;
        private Deck deck2;

        public Game()
        {
            // Initialize users
            user1 = new User();
            user2 = new User();
            
            // Initialize decks
            deck1 = new Deck(user1.Stack);
            deck2 = new Deck(user2.Stack);
        }

        public void StartGame()
        {
            var battleLogic = new BattleLogic(user1, user2, deck1, deck2);
            var battleLog = battleLogic.ExecuteBattle();

            // Display results
            Console.WriteLine($"\nBattle finished! Winner: {battleLog.Winner}");
            Console.WriteLine($"Final Score - User 1: {battleLog.FinalScore1}, User 2: {battleLog.FinalScore2}");
            Console.WriteLine("\nPress any key to return to the main menu...");
            Console.ReadKey();
        }
    }
}