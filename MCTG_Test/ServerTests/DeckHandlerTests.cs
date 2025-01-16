using System.Text;
using System.Text.Json;
using Moq;
using NUnit.Framework;

namespace MCTG.Tests
{
    [TestFixture]
    public class DeckHandlerTests
    {
        private Mock<IUserRepository> _userRepositoryMock;
        private Mock<ICardRepository> _cardRepositoryMock;
        private TcpServer _server;

        [SetUp]
        public void Setup()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _cardRepositoryMock = new Mock<ICardRepository>();
            _server = new TcpServer(0, _userRepositoryMock.Object, _cardRepositoryMock.Object, 
                null, null, null, null, null, "");
        }

        [Test]
        public async Task HandleGetDeckAsync_ValidUser_ReturnsJsonDeck()
        {
            // Arrange
            var user = new User { Id = 1, Username = "testuser" };
            var cards = new List<Card>
            {
                new Card { Id = 1, Name = "Card 1", Damage = 10 },
                new Card { Id = 2, Name = "Card 2", Damage = 20 }
            };

            _userRepositoryMock.Setup(repo => repo.GetByUsername("testuser")).Returns(user);
            _cardRepositoryMock.Setup(repo => repo.GetUserDeck(user.Id)).Returns(cards);

            var context = new CustomHttpContext();
            context.Items["Username"] = "testuser";
            context.Request.Path = "/deck";

            // Act
            await _server.HandleGetDeckAsync(context);

            // Assert
            _userRepositoryMock.Verify(repo => repo.GetByUsername("testuser"), Times.Once);
            _cardRepositoryMock.Verify(repo => repo.GetUserDeck(user.Id), Times.Once);

            Assert.That(context.Response.StatusCode, Is.EqualTo(200));
            Assert.That(context.Response.StatusDescription, Is.EqualTo("OK"));

            var responseCards = JsonSerializer.Deserialize<List<Card>>(context.Response.Body);
            Assert.That(responseCards, Has.Count.EqualTo(2));
            Assert.That(responseCards[0].Name, Is.EqualTo("Card 1"));
            Assert.That(responseCards[0].Damage, Is.EqualTo(10));
            Assert.That(responseCards[1].Name, Is.EqualTo("Card 2"));
            Assert.That(responseCards[1].Damage, Is.EqualTo(20));
        }
        
        [Test]
        public async Task HandleConfigureDeckAsync_ValidRequest_ConfiguresDeck()
        {
            // Arrange
            var user = new User { Id = 1, Username = "testuser" };
            var cardIds = new List<int> { 1, 2, 3, 4 };

            _userRepositoryMock.Setup(repo => repo.GetByUsername("testuser")).Returns(user);

            var context = new CustomHttpContext();
            context.Items["Username"] = "testuser";
            context.Request.Body = JsonSerializer.Serialize(cardIds);

            // Act
            await _server.HandleConfigureDeckAsync(context);

            // Assert
            _userRepositoryMock.Verify(repo => repo.GetByUsername("testuser"), Times.Once);
            _cardRepositoryMock.Verify(repo => repo.UpdateDeck(user.Id, cardIds), Times.Once);

            Assert.That(context.Response.StatusCode, Is.EqualTo(200));
            Assert.That(context.Response.StatusDescription, Is.EqualTo("OK"));
            Assert.That(context.Response.Body, Is.EqualTo("Deck configured successfully"));
        }
        
        [Test]
        public async Task HandleConfigureDeckAsync_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            var cardIds = new List<int> { 1, 2, 3, 4 };
            _userRepositoryMock.Setup(repo => repo.GetByUsername("testuser")).Returns((User)null);

            var context = new CustomHttpContext();
            context.Items["Username"] = "testuser";
            context.Request.Body = JsonSerializer.Serialize(cardIds);

            // Act
            await _server.HandleConfigureDeckAsync(context);

            // Assert
            Assert.That(context.Response.StatusCode, Is.EqualTo(404));
            Assert.That(context.Response.StatusDescription, Is.EqualTo("Not Found"));
            Assert.That(context.Response.Body, Is.EqualTo("User not found"));
        }

        [Test]
        public async Task HandleConfigureDeckAsync_AuthenticationRequired_ReturnsUnauthorized()
        {
            // Arrange
            var cardIds = new List<int> { 1, 2, 3, 4 };
            var context = new CustomHttpContext();
            context.Request.Body = JsonSerializer.Serialize(cardIds);

            // Act
            await _server.HandleConfigureDeckAsync(context);

            // Assert
            Assert.That(context.Response.StatusCode, Is.EqualTo(401));
            Assert.That(context.Response.StatusDescription, Is.EqualTo("Unauthorized"));
            Assert.That(context.Response.Body, Is.EqualTo("Authentication required"));
        }
    }
}