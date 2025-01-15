using NUnit.Framework;
using Npgsql;

namespace MCTG.Tests
{
    [TestFixture]
    public class CardRepositoryTests
    {
        private string _connectionString = "Host=localhost;Database=MCTG;Username=postgres;Password=postgres;";
        private ICardRepository _repository;
        private int _testUserId;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Create test user
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "INSERT INTO users (username, password) VALUES (@username, @password) RETURNING id",
                conn);
            cmd.Parameters.AddWithValue("@username", "testcards_user");
            cmd.Parameters.AddWithValue("@password", "test_password");
            _testUserId = (int)cmd.ExecuteScalar();

            // Insert test cards
            using var cardCmd = new NpgsqlCommand(@"
                INSERT INTO cards (name, damage, element_type, card_type, user_id)
                VALUES 
                    (@Name1, @Damage1, @ElementType1, @CardType1, @UserId),
                    (@Name2, @Damage2, @ElementType2, @CardType2, @UserId)",
                conn);

            cardCmd.Parameters.AddWithValue("@Name1", "Dragon");
            cardCmd.Parameters.AddWithValue("@Damage1", 50);
            cardCmd.Parameters.AddWithValue("@ElementType1", 1);
            cardCmd.Parameters.AddWithValue("@CardType1", "Monster");
            cardCmd.Parameters.AddWithValue("@Name2", "WaterSpell");
            cardCmd.Parameters.AddWithValue("@Damage2", 20);
            cardCmd.Parameters.AddWithValue("@ElementType2", 0);
            cardCmd.Parameters.AddWithValue("@CardType2", "Spell");
            cardCmd.Parameters.AddWithValue("@UserId", _testUserId);

            cardCmd.ExecuteNonQuery();
        }

        [SetUp]
        public void Setup()
        {
            _repository = new CardRepository(_connectionString);
        }

        [Test]
        public void GetUserCards_ReturnsAllUserCards()
        {
            // Act
            var cards = _repository.GetUserCards(_testUserId).ToList();

            // Assert
            Assert.That(cards, Has.Count.EqualTo(2));
            Assert.That(cards[0].Name, Is.EqualTo("Dragon"));
            Assert.That(cards[1].Name, Is.EqualTo("WaterSpell"));
        }

        [Test]
        public void GetUserCards_NonExistentUser_ReturnsEmptyList()
        {
            // Arrange
            var nonExistentUserId = 99999;

            // Act
            var cards = _repository.GetUserCards(nonExistentUserId);

            // Assert
            Assert.That(cards, Is.Empty);
        }

        [Test]
        public void GetUserCards_VerifyCardProperties()
        {
            // Act
            var cards = _repository.GetUserCards(_testUserId).ToList();
            var dragon = cards.First(c => c.Name == "Dragon");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(dragon.Damage, Is.EqualTo(50));
                Assert.That(dragon.CardType, Is.EqualTo("Monster"));
                Assert.That(dragon.ElementType, Is.EqualTo(ElementType.Fire));
                Assert.That(dragon.UserId, Is.EqualTo(_testUserId));
            });
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                // Delete test cards
                using (var cmd = new NpgsqlCommand(
                    "DELETE FROM cards WHERE user_id = @UserId",
                    conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@UserId", _testUserId);
                    cmd.ExecuteNonQuery();
                }

                // Delete test user
                using (var cmd = new NpgsqlCommand(
                    "DELETE FROM users WHERE id = @UserId",
                    conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@UserId", _testUserId);
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