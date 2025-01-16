using NUnit.Framework;
using Npgsql;

namespace MCTG.Tests
{
    [TestFixture]
    public class UserRepositoryTests
    {
        private string _connectionString = "Host=localhost;Database=MCTG;Username=postgres;Password=postgres;";
        private IUserRepository _repository;

        [SetUp]
        public void Setup()
        {
            _repository = new UserRepository(_connectionString);
            
            // Clean up any test data
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(
                @"DELETE FROM user_profiles WHERE user_id IN (SELECT id FROM users WHERE username LIKE 'test_%');
                  DELETE FROM users WHERE username LIKE 'test_%'",
                conn);
            cmd.ExecuteNonQuery();
        }

        [Test]
        public void Add_NewUser_AddsSuccessfully()
        {
            // Arrange
            var user = new User
            {
                Username = "test_user1",
                Password = "test123",
                Coins = 20,
                Elo = 100
            };

            // Act
            Assert.DoesNotThrow(() => _repository.Add(user));

            // Assert
            var addedUser = _repository.GetByUsername("test_user1");
            Assert.That(addedUser, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(addedUser.Username, Is.EqualTo(user.Username));
                Assert.That(addedUser.Coins, Is.EqualTo(20));
                Assert.That(addedUser.Elo, Is.EqualTo(100));
            });
        }

        [Test]
        public void GetByUsername_NonExistentUser_ReturnsNull()
        {
            // Act
            var result = _repository.GetByUsername("nonexistent_user");

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void Update_ExistingUser_UpdatesSuccessfully()
        {
            // Arrange
            var user = new User
            {
                Username = "test_user2",
                Password = "test123",
                Coins = 20,
                Elo = 100
            };
            _repository.Add(user);
            var addedUser = _repository.GetByUsername(user.Username);

            // Act
            addedUser.Coins = 25;
            addedUser.Elo = 110;
            _repository.Update(addedUser);

            // Assert
            var updatedUser = _repository.GetByUsername(user.Username);
            Assert.Multiple(() =>
            {
                Assert.That(updatedUser.Coins, Is.EqualTo(25));
                Assert.That(updatedUser.Elo, Is.EqualTo(110));
            });
        }
        
        
        [Test]
        public void UserProfile_CreateAndRetrieve_WorksCorrectly()
        {
            // Arrange
            var user = new User
            {
                Username = "test_user7",
                Password = "test123",
                Coins = 20,
                Elo = 100
            };
            _repository.Add(user);
            var userId = _repository.GetByUsername(user.Username).Id;

            var profile = new UserProfile
            {
                UserId = userId,
                Name = "Test Name",
                Bio = "Test Bio",
                Image = "test.jpg"
            };

            // Act
            _repository.UpdateProfile(profile);
            var retrievedProfile = _repository.GetUserProfile(userId);

            // Assert
            Assert.That(retrievedProfile, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(retrievedProfile.Name, Is.EqualTo("Test Name"));
                Assert.That(retrievedProfile.Bio, Is.EqualTo("Test Bio"));
                Assert.That(retrievedProfile.Image, Is.EqualTo("test.jpg"));
            });
        }

        [TearDown]
        public void Cleanup()
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                using (var cmd = new NpgsqlCommand(
                    @"DELETE FROM user_profiles WHERE user_id IN (SELECT id FROM users WHERE username LIKE 'test_%');
                      DELETE FROM users WHERE username LIKE 'test_%'",
                    conn, transaction))
                {
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