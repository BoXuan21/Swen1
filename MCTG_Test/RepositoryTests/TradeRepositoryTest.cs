using NUnit.Framework;
using Npgsql;
using Moq;

namespace MCTG.Tests
{
    [TestFixture]
    public class TradeRepositoryTests
    {
        private string _connectionString = "Host=localhost;Database=MCTG;Username=postgres;Password=postgres;";
        private ITradeRepository _repository;
        private Mock<ICardRepository> _cardRepositoryMock;
        private int _testUserId1;
        private int _testUserId2;
        private int _testCardId1;
        private int _testCardId2;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Create test users and cards
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                // Create test users
                using (var cmd = new NpgsqlCommand(
                    "INSERT INTO users (username, password) VALUES (@username1, @password1), (@username2, @password2) RETURNING id",
                    conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@username1", "testtrade_user1");
                    cmd.Parameters.AddWithValue("@password1", "test_password1");
                    cmd.Parameters.AddWithValue("@username2", "testtrade_user2");
                    cmd.Parameters.AddWithValue("@password2", "test_password2");
                    
                    using var reader = cmd.ExecuteReader();
                    reader.Read();
                    _testUserId1 = reader.GetInt32(0);
                    reader.Read();
                    _testUserId2 = reader.GetInt32(0);
                }

                // Create test cards
                using (var cmd = new NpgsqlCommand(@"
                    INSERT INTO cards (name, damage, element_type, card_type, user_id) 
                    VALUES 
                        (@Name1, @Damage1, @ElementType1, @CardType1, @UserId1),
                        (@Name2, @Damage2, @ElementType2, @CardType2, @UserId2)
                    RETURNING id",
                    conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@Name1", "Dragon");
                    cmd.Parameters.AddWithValue("@Damage1", 50);
                    cmd.Parameters.AddWithValue("@ElementType1", "Fire");
                    cmd.Parameters.AddWithValue("@CardType1", "Dragon");
                    cmd.Parameters.AddWithValue("@UserId1", _testUserId1);
                    
                    cmd.Parameters.AddWithValue("@Name2", "WaterSpell");
                    cmd.Parameters.AddWithValue("@Damage2", 20);
                    cmd.Parameters.AddWithValue("@ElementType2", "Water");
                    cmd.Parameters.AddWithValue("@CardType2", "Spell");
                    cmd.Parameters.AddWithValue("@UserId2", _testUserId2);

                    using var reader = cmd.ExecuteReader();
                    reader.Read();
                    _testCardId1 = reader.GetInt32(0);
                    reader.Read();
                    _testCardId2 = reader.GetInt32(0);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        [SetUp]
        public void Setup()
        {
            _cardRepositoryMock = new Mock<ICardRepository>();
            _repository = new TradeRepository(_connectionString, _cardRepositoryMock.Object);

            // Clean existing trades
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand("DELETE FROM trades", conn);
            cmd.ExecuteNonQuery();
        }

        [Test]
        public void CreateTrade_ValidTrade_CreatesSuccessfully()
        {
            // Arrange
            var trade = new Trade
            {
                CardId = _testCardId1,
                UserId = _testUserId1,
                RequiredType = "Spell",
                MinimumDamage = 20
            };

            // Act & Assert
            Assert.DoesNotThrow(() => _repository.CreateTrade(trade));
            Assert.That(trade.Id, Is.GreaterThan(0));
        }

        [Test]
        public void CreateTrade_CardNotOwnedByUser_ThrowsException()
        {
            // Arrange
            var trade = new Trade
            {
                CardId = _testCardId1,
                UserId = _testUserId2, // Wrong user
                RequiredType = "Spell",
                MinimumDamage = 20
            };

            // Act & Assert
            var ex = Assert.Throws<Exception>(() => _repository.CreateTrade(trade));
            Assert.That(ex.Message, Is.EqualTo("Card not found or doesn't belong to user"));
        }
        

        [Test]
        public void DeleteTrade_ExistingTrade_DeletesSuccessfully()
        {
            // Arrange
            var trade = new Trade
            {
                CardId = _testCardId1,
                UserId = _testUserId1,
                RequiredType = "Spell",
                MinimumDamage = 20
            };
            _repository.CreateTrade(trade);

            // Act
            Assert.DoesNotThrow(() => _repository.DeleteTrade(trade.Id));

            // Assert
            var trades = _repository.GetAllTrades();
            Assert.That(trades, Is.Empty);
        }
        
        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                // Clean up trades (if any remaining)
                using (var cmd = new NpgsqlCommand("DELETE FROM trades", conn, transaction))
                {
                    cmd.ExecuteNonQuery();
                }

                // Clean up cards
                using (var cmd = new NpgsqlCommand(
                    "DELETE FROM cards WHERE user_id IN (@UserId1, @UserId2)",
                    conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@UserId1", _testUserId1);
                    cmd.Parameters.AddWithValue("@UserId2", _testUserId2);
                    cmd.ExecuteNonQuery();
                }

                // Clean up users
                using (var cmd = new NpgsqlCommand(
                    "DELETE FROM users WHERE id IN (@UserId1, @UserId2)",
                    conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@UserId1", _testUserId1);
                    cmd.Parameters.AddWithValue("@UserId2", _testUserId2);
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}