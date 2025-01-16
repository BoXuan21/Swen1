using System.Text.Json;
using Moq;
using NUnit.Framework;

namespace MCTG.Tests
{
    [TestFixture]
    public class HandleBattleAsyncTests
    {
        private Mock<IUserRepository> _userRepositoryMock;
        private Mock<ICardRepository> _cardRepositoryMock;
        private Mock<IBattleRepository> _battleRepositoryMock;
        private TcpServer _server;

        [SetUp]
        public void Setup()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _cardRepositoryMock = new Mock<ICardRepository>();
            _battleRepositoryMock = new Mock<IBattleRepository>();
            _server = new TcpServer(0, _userRepositoryMock.Object, _cardRepositoryMock.Object, 
                null, null, _battleRepositoryMock.Object, null, null, "");
        }

        [Test]
        public async Task ValidBattle_SavesBattleLog()
        {
            // Arrange
            var user1 = new User { Id = 1, Username = "user1", Elo = 100 };
            var user2 = new User { Id = 2, Username = "user2", Elo = 100 };
            var battleRequest = new BattleRequest { OpponentUsername = "user2" };
            var deck1 = new List<Card> { new Card(), new Card(), new Card(), new Card() };
            var deck2 = new List<Card> { new Card(), new Card(), new Card(), new Card() };

            _userRepositoryMock.Setup(repo => repo.GetByUsername("user1")).Returns(user1);
            _userRepositoryMock.Setup(repo => repo.GetByUsername("user2")).Returns(user2);
            _cardRepositoryMock.Setup(repo => repo.GetUserDeck(user1.Id)).Returns(deck1);
            _cardRepositoryMock.Setup(repo => repo.GetUserDeck(user2.Id)).Returns(deck2);

            var context = new CustomHttpContext();
            context.Items["Username"] = "user1";
            context.Request.Body = JsonSerializer.Serialize(battleRequest);

            // Act
            await _server.HandleBattleAsync(context);

            // Assert
            _battleRepositoryMock.Verify(repo => 
                repo.SaveBattleHistory(It.Is<BattleHistory>(history =>
                    history.Player1Id == user1.Id &&
                    history.Player2Id == user2.Id &&
                    history.BattleLog != null)), Times.Once);

            Assert.That(context.Response.StatusCode, Is.EqualTo(200));
            Assert.That(context.Response.StatusDescription, Is.EqualTo("OK"));
            Assert.That(context.Response.Body, Does.Contain("Winner"));
        }
        
        [Test]
        public async Task InsufficientCardsInDeck_ReturnsBadRequest()
        {
            // Arrange
            var user1 = new User { Id = 1, Username = "user1", Elo = 100 };
            var user2 = new User { Id = 2, Username = "user2", Elo = 100 };
            var battleRequest = new BattleRequest { OpponentUsername = "user2" };
            var deck1 = new List<Card> { new Card(), new Card() }; // Insufficient cards

            _userRepositoryMock.Setup(repo => repo.GetByUsername("user1")).Returns(user1);
            _userRepositoryMock.Setup(repo => repo.GetByUsername("user2")).Returns(user2);
            _cardRepositoryMock.Setup(repo => repo.GetUserDeck(user1.Id)).Returns(deck1);

            var context = new CustomHttpContext();
            context.Items["Username"] = "user1";
            context.Request.Body = JsonSerializer.Serialize(battleRequest);

            // Act
            await _server.HandleBattleAsync(context);

            // Assert
            Assert.That(context.Response.StatusCode, Is.EqualTo(400));
            Assert.That(context.Response.StatusDescription, Is.EqualTo("Bad Request"));
            Assert.That(context.Response.Body, Is.EqualTo("Both users must have at least 4 cards in their deck"));
        }

        [Test]
        public async Task UserNotFound_ReturnsNotFound()
        {
            // Arrange
            var battleRequest = new BattleRequest { OpponentUsername = "user2" };
            _userRepositoryMock.Setup(repo => repo.GetByUsername("user1")).Returns((User)null);

            var context = new CustomHttpContext();
            context.Items["Username"] = "user1";
            context.Request.Body = JsonSerializer.Serialize(battleRequest);

            // Act
            await _server.HandleBattleAsync(context);

            // Assert
            Assert.That(context.Response.StatusCode, Is.EqualTo(404));
            Assert.That(context.Response.StatusDescription, Is.EqualTo("Not Found"));
            Assert.That(context.Response.Body, Is.EqualTo("User not found"));
        }
    }
}