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

            // Clean up
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Execute("DELETE FROM trades");
            connection.Execute("DELETE FROM cards");
            connection.Execute("DELETE FROM users WHERE username LIKE 'trader%'");

            // Create and verify users
            _userRepository.Add(new User { Username = "trader1", Password = "test" });
            _userRepository.Add(new User { Username = "trader2", Password = "test" });

            _testUser1 = _userRepository.GetByUsername("trader1");
            _testUser2 = _userRepository.GetByUsername("trader2");

            Console.WriteLine($"Created users: ID1={_testUser1?.Id}, ID2={_testUser2?.Id}");

            // Create test card
            _testCard = new Card("TestCard", 50, ElementType.Fire)
            {
                CardType = "Monster"  // Set this before adding
            };

            if (_testUser1 != null)
            {
                var cardId = _cardRepository.AddCard(_testCard, _testUser1.Id);
                _testCard.Id = cardId;
                _testCard.UserId = _testUser1.Id;
        
                Console.WriteLine($"Created test card: ID={_testCard.Id}, UserId={_testCard.UserId}");
        
                // Verify card
                var dbCard = _cardRepository.GetCard(_testCard.Id);
                if (dbCard == null)
                {
                    Console.WriteLine("Failed to retrieve test card from database");
                }
            }
            else
            {
                Console.WriteLine("Test user 1 is null!");
            }
        }
        
        [Test]
        public void CreateTrade_ShouldCreateNewTrade()
        {
            Console.WriteLine($"Test card details - Id: {_testCard.Id}, UserId: {_testUser1.Id}");
    
            // Verify card exists
            var cardInDb = _cardRepository.GetCard(_testCard.Id);
            Assert.That(cardInDb, Is.Not.Null, "Card should exist in database before creating trade");

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
            var trades = _tradeRepository.GetAllTrades().ToList();

            // Debug
            Console.WriteLine($"Created trade - Id: {trade.Id}, CardId: {trade.CardId}");
            Console.WriteLine($"Found {trades.Count} trades");

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
            var tradeCard = new Card("TradeCard", 50, ElementType.Water) 
            { 
                CardType = "Monster"  // Set explicitly
            };
            var offeredCard = new Card("OfferedCard", 60, ElementType.Fire) 
            { 
                CardType = "Spell"    // Set explicitly
            };

            Console.WriteLine("\nInitial card setup:");
            Console.WriteLine($"Trade card type: {tradeCard.CardType}");
            Console.WriteLine($"Offered card type: {offeredCard.CardType}");

            // Add cards and verify their IDs
            tradeCard.Id = _cardRepository.AddCard(tradeCard, _testUser1.Id);
            offeredCard.Id = _cardRepository.AddCard(offeredCard, _testUser2.Id);

            // Verify cards were stored correctly
            var storedTradeCard = _cardRepository.GetCard(tradeCard.Id);
            var storedOfferedCard = _cardRepository.GetCard(offeredCard.Id);

            Console.WriteLine("\nStored card verification:");
            Console.WriteLine($"Stored trade card type: {storedTradeCard?.CardType}");
            Console.WriteLine($"Stored offered card type: {storedOfferedCard?.CardType}");

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
            Assert.That(result, Is.True, "Trade execution should succeed");
            var finalCard = _cardRepository.GetCard(tradeCard.Id);
            Assert.That(finalCard.UserId, Is.EqualTo(_testUser2.Id));
        }
        
        [Test]
        public void ExecuteTrade_WithInvalidCard_ShouldFail()
        {
            // Arrange
            var tradeCard = new Card("TradeCard", 50, ElementType.Water)
            {
                CardType = "Monster",
                UserId = _testUser1.Id
            };
            var weakCard = new Card("WeakCard", 20, ElementType.Fire)
            {
                CardType = "Spell",
                UserId = _testUser2.Id
            };

            tradeCard.Id = _cardRepository.AddCard(tradeCard, _testUser1.Id);
            weakCard.Id = _cardRepository.AddCard(weakCard, _testUser2.Id);

            Console.WriteLine($"Created trade card - Id: {tradeCard.Id}, UserId: {_testUser1.Id}");
            Console.WriteLine($"Created weak card - Id: {weakCard.Id}, UserId: {_testUser2.Id}");

            var trade = new Trade
            {
                CardId = tradeCard.Id,
                UserId = _testUser1.Id,
                RequiredType = "Spell",
                MinimumDamage = 50
            };

            _tradeRepository.CreateTrade(trade);
            Console.WriteLine($"Created trade - Id: {trade.Id}");

            // Act
            bool result = _tradeRepository.ExecuteTrade(trade.Id, weakCard.Id, _testUser2.Id);

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