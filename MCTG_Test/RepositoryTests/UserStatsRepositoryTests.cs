﻿using NUnit.Framework;
using Npgsql;

namespace MCTG.Tests
{
    [TestFixture]
    public class UserStatsRepositoryTests
    {
        private string _connectionString = "Host=localhost;Database=MCTG;Username=postgres;Password=postgres;";
        private IUserStatsRepository _repository;
        private int _testUserId;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Create a test user in the database
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "INSERT INTO users (username, password) VALUES (@username, @password) RETURNING id",
                conn);
            cmd.Parameters.AddWithValue("@username", "teststats_user");
            cmd.Parameters.AddWithValue("@password", "test_password");
            _testUserId = (int)cmd.ExecuteScalar();
        }

        [SetUp]
        public void Setup()
        {
            _repository = new UserStatsRepository(_connectionString);
            
            // Clean up any existing stats for test user
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "DELETE FROM user_stats WHERE user_id = @UserId",
                conn);
            cmd.Parameters.AddWithValue("@UserId", _testUserId);
            cmd.ExecuteNonQuery();
        }

        [Test]
        public void GetUserStats_NewUser_ReturnsAndCreatesDefaultStats()
        {
            // Act
            var stats = _repository.GetUserStats(_testUserId);

            // Assert
            Assert.That(stats, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(stats.UserId, Is.EqualTo(_testUserId));
                Assert.That(stats.GamesPlayed, Is.EqualTo(0));
                Assert.That(stats.Wins, Is.EqualTo(0));
                Assert.That(stats.Losses, Is.EqualTo(0));
                Assert.That(stats.Draws, Is.EqualTo(0));
                Assert.That(stats.Elo, Is.EqualTo(100));
            });

            // Verify stats were created in database
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM user_stats WHERE user_id = @UserId",
                conn);
            cmd.Parameters.AddWithValue("@UserId", _testUserId);
            var count = (long)cmd.ExecuteScalar();
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void UpdateStats_ValidStats_UpdatesSuccessfully()
        {
            // Arrange - First get default stats
            var initialStats = _repository.GetUserStats(_testUserId);
            var updatedStats = new UserStats
            {
                UserId = _testUserId,
                GamesPlayed = 5,
                Wins = 3,
                Losses = 1,
                Draws = 1,
                Elo = 120
            };

            // Act
            _repository.UpdateStats(updatedStats);

            // Assert
            var retrievedStats = _repository.GetUserStats(_testUserId);
            Assert.Multiple(() =>
            {
                Assert.That(retrievedStats.GamesPlayed, Is.EqualTo(5));
                Assert.That(retrievedStats.Wins, Is.EqualTo(3));
                Assert.That(retrievedStats.Losses, Is.EqualTo(1));
                Assert.That(retrievedStats.Draws, Is.EqualTo(1));
                Assert.That(retrievedStats.Elo, Is.EqualTo(120));
            });
        }

        [Test]
        public void GetUserStats_ExistingStats_ReturnsCorrectStats()
        {
            // Arrange - Insert test stats directly into DB
            var expectedStats = new UserStats
            {
                UserId = _testUserId,
                GamesPlayed = 10,
                Wins = 5,
                Losses = 3,
                Draws = 2,
                Elo = 115
            };

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using var cmd = new NpgsqlCommand(@"
                    INSERT INTO user_stats (user_id, games_played, wins, losses, draws, elo)
                    VALUES (@UserId, @GamesPlayed, @Wins, @Losses, @Draws, @Elo)",
                    conn);

                cmd.Parameters.AddWithValue("@UserId", expectedStats.UserId);
                cmd.Parameters.AddWithValue("@GamesPlayed", expectedStats.GamesPlayed);
                cmd.Parameters.AddWithValue("@Wins", expectedStats.Wins);
                cmd.Parameters.AddWithValue("@Losses", expectedStats.Losses);
                cmd.Parameters.AddWithValue("@Draws", expectedStats.Draws);
                cmd.Parameters.AddWithValue("@Elo", expectedStats.Elo);

                cmd.ExecuteNonQuery();
            }

            // Act
            var actualStats = _repository.GetUserStats(_testUserId);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(actualStats.GamesPlayed, Is.EqualTo(expectedStats.GamesPlayed));
                Assert.That(actualStats.Wins, Is.EqualTo(expectedStats.Wins));
                Assert.That(actualStats.Losses, Is.EqualTo(expectedStats.Losses));
                Assert.That(actualStats.Draws, Is.EqualTo(expectedStats.Draws));
                Assert.That(actualStats.Elo, Is.EqualTo(expectedStats.Elo));
            });
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            // Clean up test user and their stats
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                using (var cmd = new NpgsqlCommand(
                    "DELETE FROM user_stats WHERE user_id = @UserId",
                    conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@UserId", _testUserId);
                    cmd.ExecuteNonQuery();
                }

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