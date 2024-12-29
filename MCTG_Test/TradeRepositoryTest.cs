using NUnit.Framework;
using MCTG;
using Npgsql;
using Dapper;

namespace MCTG_Test
{
    public class TradeRepositoryTests
    {
        private string _connectionString = "Host=localhost;Database=MCTG;Username=postgres;Password=postgres;";
        private ITradeRepository _tradeRepository;
        private ICardRepository _cardRepository;
        private IUserRepository _userRepository;
        private User _testUser1;
        private User _testUser2;
        private Card _testCard;

        [SetUp]
        public void Setup()
        {
            _cardRepository = new CardRepository(_connectionString);
            _userRepository = new UserRepository(_connectionString);
            _tradeRepository = new TradeRepository(_connectionString, _cardRepository);

            // Clean up any prior state
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Execute("DELETE FROM trades");
            connection.Execute("DELETE FROM cards");
            connection.Execute("DELETE FROM users WHERE username LIKE 'trader%'");

            // Create test users
            _userRepository.Add(new User { Username = "trader1", Password = "test" });
            _userRepository.Add(new User { Username = "trader2", Password = "test" });

            // Retrieve created users
            _testUser1 = _userRepository.GetByUsername("trader1");
            _testUser2 = _userRepository.GetByUsername("trader2");

            // Create and persist a test card
            _testCard = new Card("TestCard", 50, ElementType.Fire)
            {
                CardType = "Monster",
                UserId = _testUser1.Id
            };
            _cardRepository.AddCard(_testCard, _testUser1.Id);

            // Validate that the card was persisted
            var persistedCard = _cardRepository.GetCard(_testCard.Id);
            Assert.That(persistedCard, Is.Not.Null, "The test card was not correctly added to the database.");
        }

        [Test]
        public void CreateTrade_ShouldCreateNewTrade()
        {
            // Arrange
            var trade = new Trade
            {
                CardId = _testCard.Id,
                UserId = _testUser1.Id,
                RequiredType = "Spell",
                MinimumDamage = 30
            };

            // Act
            _tradeRepository.CreateTrade(trade);
            var trades = _tradeRepository.GetAllTrades();

            // Assert
            Assert.That(trades, Is.Not.Empty);
            var createdTrade = trades.FirstOrDefault(t => t.CardId == _testCard.Id);
            Assert.That(createdTrade, Is.Not.Null);
            Assert.That(createdTrade.MinimumDamage, Is.EqualTo(30));
        }

        [Test]
        public void ExecuteTrade_WithValidCard_ShouldSucceed()
        {
            // Arrange
            var tradeCard = new Card("TradeCard", 50, ElementType.Water) { CardType = "Monster" };
            var offeredCard = new Card("OfferedCard", 60, ElementType.Fire) { CardType = "Spell" };
            
            _cardRepository.AddCard(tradeCard, _testUser1.Id);
            _cardRepository.AddCard(offeredCard, _testUser2.Id);

            var trade = new Trade
            {
                CardId = tradeCard.Id,
                UserId = _testUser1.Id,
                RequiredType = "Spell",
                MinimumDamage = 50
            };
            _tradeRepository.CreateTrade(trade);

            // Act
            bool result = _tradeRepository.ExecuteTrade(trade.Id, offeredCard.Id, _testUser2.Id);

            // Assert
            Assert.That(result, Is.True);
            var tradedCard = _cardRepository.GetCard(tradeCard.Id);
            Assert.That(tradedCard.UserId, Is.EqualTo(_testUser2.Id));
        }

        [Test]
        public void ExecuteTrade_WithInvalidCard_ShouldFail()
        {
            // Arrange
            var tradeCard = new Card("TradeCard", 50, ElementType.Water) { CardType = "Monster" };
            var offeredCard = new Card("WeakCard", 20, ElementType.Fire) { CardType = "Spell" };
            
            _cardRepository.AddCard(tradeCard, _testUser1.Id);
            _cardRepository.AddCard(offeredCard, _testUser2.Id);

            var trade = new Trade
            {
                CardId = tradeCard.Id,
                UserId = _testUser1.Id,
                RequiredType = "Spell",
                MinimumDamage = 50  // Offered card is too weak
            };
            _tradeRepository.CreateTrade(trade);

            // Act
            bool result = _tradeRepository.ExecuteTrade(trade.Id, offeredCard.Id, _testUser2.Id);

            // Assert
            Assert.That(result, Is.False);
            var unchangedCard = _cardRepository.GetCard(tradeCard.Id);
            Assert.That(unchangedCard.UserId, Is.EqualTo(_testUser1.Id));
        }

        [TearDown]
        public void Cleanup()
        {
            // Clean up test data
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Execute("DELETE FROM trades");
            connection.Execute("DELETE FROM cards");
            connection.Execute("DELETE FROM users WHERE username LIKE 'trader%'");
        }
    }
}