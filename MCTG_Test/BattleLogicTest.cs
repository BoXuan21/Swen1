
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
        
        [Test]
        public void SpecialRule_OrkVsWizard_WizardControls()
        {
            // Arrange
            var ork = new Card("Ork", 100, ElementType.Normal) { CardType = "Monster" };
            var wizard = new Card("Wizard", 50, ElementType.Normal) { CardType = "Monster" };
    
            deck1.AddCard(ork);
            deck2.AddCard(wizard);
    
            battleLogic = new BattleLogic(user1, user2, deck1, deck2);

            // Act
            var result = battleLogic.ExecuteBattle();

            // Assert
            Assert.That(result.Winner, Is.EqualTo("User 2")); // Wizard should win
            Assert.That(user1.Elo, Is.EqualTo(95)); // Lost -5
            Assert.That(user2.Elo, Is.EqualTo(103)); // Won +3
        }

        [Test]
        public void SpecialRule_KnightVsWaterSpell_KnightDrowns()
        {
            // Arrange
            var knight = new Card("Knight", 100, ElementType.Normal) { CardType = "Monster" };
            var waterSpell = new Card("WaterSpell", 20, ElementType.Water) { CardType = "Spell" };
    
            deck1.AddCard(knight);
            deck2.AddCard(waterSpell);
    
            battleLogic = new BattleLogic(user1, user2, deck1, deck2);

            // Act
            var result = battleLogic.ExecuteBattle();

            // Assert
            Assert.That(result.Winner, Is.EqualTo("User 2")); // WaterSpell should win
            Assert.That(user1.Elo, Is.EqualTo(95)); // Lost -5 
            Assert.That(user2.Elo, Is.EqualTo(103)); // Won +3
        }

        [Test]
        public void SpecialRule_DragonVsFireElves_FireElvesEvade()
        {
            // Arrange
            var dragon = new Card("Dragon", 100, ElementType.Fire) { CardType = "Monster" };
            var fireElves = new Card("FireElves", 30, ElementType.Fire) { CardType = "Monster" };
    
            deck1.AddCard(dragon);
            deck2.AddCard(fireElves);
    
            battleLogic = new BattleLogic(user1, user2, deck1, deck2);

            // Act
            var result = battleLogic.ExecuteBattle();

            // Assert
            Assert.That(result.Winner, Is.EqualTo("User 2")); // FireElves should win
            Assert.That(user1.Elo, Is.EqualTo(95)); // Lost -5
            Assert.That(user2.Elo, Is.EqualTo(103)); // Won +3
        }
    }
}