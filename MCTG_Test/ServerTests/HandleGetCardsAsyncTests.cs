using System.Text.Json;
using Moq;
using NUnit.Framework;

namespace MCTG.Tests
{
    [TestFixture]
    public class HandleGetCardsAsyncTests
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
        public async Task ValidUser_ReturnsCards()
        {
            // Arrange
            var user = new User { Id = 1, Username = "testuser" };
            var cards = new List<Card> { new Card { Id = 1 }, new Card { Id = 2 } };

            _userRepositoryMock.Setup(repo => repo.GetByUsername("testuser")).Returns(user);
            _cardRepositoryMock.Setup(repo => repo.GetUserCards(user.Id)).Returns(cards);

            var context = new CustomHttpContext();
            context.Items["Username"] = "testuser";

            // Act
            await _server.HandleGetCardsAsync(context);

            // Assert
            _userRepositoryMock.Verify(repo => repo.GetByUsername("testuser"), Times.Once);
            _cardRepositoryMock.Verify(repo => repo.GetUserCards(user.Id), Times.Once);

            Assert.That(context.Response.StatusCode, Is.EqualTo(200));
            Assert.That(context.Response.StatusDescription, Is.EqualTo("OK"));

            var responseCards = JsonSerializer.Deserialize<List<Card>>(context.Response.Body);
            Assert.That(responseCards, Has.Count.EqualTo(2));
            Assert.That(responseCards[0].Id, Is.EqualTo(1));
            Assert.That(responseCards[1].Id, Is.EqualTo(2));
        }

        [Test]
        public async Task UserNotFound_ReturnsNotFound()
        {
            // Arrange
            _userRepositoryMock.Setup(repo => repo.GetByUsername("testuser")).Returns((User)null);

            var context = new CustomHttpContext();
            context.Items["Username"] = "testuser";

            // Act
            await _server.HandleGetCardsAsync(context);

            // Assert
            Assert.That(context.Response.StatusCode, Is.EqualTo(404));
            Assert.That(context.Response.StatusDescription, Is.EqualTo("Not Found"));
            Assert.That(context.Response.Body, Is.EqualTo("User not found"));
        }
    }
}