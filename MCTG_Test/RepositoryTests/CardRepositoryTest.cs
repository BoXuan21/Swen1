using NUnit.Framework;
using System.Linq;
using MCTG;
using Npgsql;
using Dapper;

namespace MCTG
{
    public class CardRepositoryTests
    {
        private readonly string _connectionString = "Host=localhost;Database=MCTG;Username=postgres;Password=postgres;";
        private ICardRepository _cardRepository;
        private IUserRepository _userRepository;
        private int _testUserId;

        [SetUp]
        public void Setup()
        {
            _cardRepository = new CardRepository(_connectionString);
            _userRepository = new UserRepository(_connectionString);
        
            // Create test user
            var testUser = new User 
            { 
                Username = "testuser", 
                Password = "test" 
            };
            _userRepository.Add(testUser);
            _testUserId = _userRepository.GetByUsername("testuser").Id;
        }

        [Test]
        public void AddCard_ShouldStoreCardInDatabase()
        {
            // Arrange
            var card = new Card("TestCard", 50, ElementType.Fire)
            {
                CardType = "Monster",  // Make sure CardType is set
                UserId = _testUserId   // Set the UserId
            };

            // Act
            _cardRepository.AddCard(card, _testUserId);
            var userCards = _cardRepository.GetUserCards(_testUserId).ToList();

            // Assert
            Assert.That(userCards, Has.Count.GreaterThan(0));
            var storedCard = userCards.FirstOrDefault(c => c.Name == "TestCard");
            Assert.That(storedCard, Is.Not.Null);
            Assert.That(storedCard.Damage, Is.EqualTo(50));
        }

        [TearDown]
        public void Cleanup()
        {
            // Clean up test data
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Execute("DELETE FROM cards WHERE user_id = @userId", new { userId = _testUserId });
            connection.Execute("DELETE FROM users WHERE id = @userId", new { userId = _testUserId });
        }
    }
}