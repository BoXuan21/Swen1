
using NUnit.Framework;

namespace MCTG
{
    public class BattleLogicTests
    {
        private User user1;
        private User user2;
        private Deck deck1;
        private Deck deck2;
        private BattleLogic battleLogic;

        [SetUp]
        public void Setup()
        {
            // Create users with initial ELO
            user1 = new User { Username = "Player1", Elo = 100 };
            user2 = new User { Username = "Player2", Elo = 100 };
            
            deck1 = new Deck(user1.Stack);
            deck2 = new Deck(user2.Stack);
        }

        [Test]
        public void SpecialRule_GoblinVsDragon_GoblinCantWin()
        {
            // Arrange
            var goblin = new Card("Goblin", 100, ElementType.Normal) { CardType = "Monster" };
            var dragon = new Card("Dragon", 50, ElementType.Fire) { CardType = "Monster" };
            
            deck1.AddCard(goblin);
            deck2.AddCard(dragon);
            
            battleLogic = new BattleLogic(user1, user2, deck1, deck2);

            // Act
            var result = battleLogic.ExecuteBattle();

            // Assert
            Assert.That(result.Winner, Is.EqualTo("User 2")); // Dragon should win
            Assert.That(user1.Elo, Is.EqualTo(95)); // Lost -5
            Assert.That(user2.Elo, Is.EqualTo(103)); // Won +3
        }
        [Test]
        public void MonsterFight_ElementalEffectsNotApplied()
        {
            // Arrange
            var waterMonster = new Card("Kraken", 50, ElementType.Water) { CardType = "Monster" };
            var fireMonster = new Card("Dragon", 40, ElementType.Fire) { CardType = "Monster" };
            
            deck1.AddCard(waterMonster);
            deck2.AddCard(fireMonster);
            
            battleLogic = new BattleLogic(user1, user2, deck1, deck2);

            // Act
            var result = battleLogic.ExecuteBattle();

            // Assert
            Assert.That(result.Winner, Is.EqualTo("User 1")); // Higher damage should win without elemental effects
            Assert.That(result.FinalScore1, Is.EqualTo(1));
        }

        [Test]
        public void KrakenVsSpell_KrakenImmune()
        {
            // Arrange
            var kraken = new Card("Kraken", 30, ElementType.Water) { CardType = "Monster" };
            var powerfulSpell = new Card("FireSpell", 100, ElementType.Fire) { CardType = "Spell" };
            
            deck1.AddCard(kraken);
            deck2.AddCard(powerfulSpell);
            
            battleLogic = new BattleLogic(user1, user2, deck1, deck2);

            // Act
            var result = battleLogic.ExecuteBattle();

            // Assert
            Assert.That(result.Winner, Is.EqualTo("User 1")); // Kraken should win despite lower damage
        }
    }
}