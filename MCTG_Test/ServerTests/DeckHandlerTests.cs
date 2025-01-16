using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Moq;

namespace MCTG.Tests
{
    [TestFixture]
    public class DeckHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<ICardRepository> _cardRepositoryMock;
        private readonly TcpServer _server;

        public DeckHandlerTests()
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

            var context = new DefaultHttpContext();
            context.Items["Username"] = "testuser";
            context.Request.Path = "/deck";

            using var memoryStream = new MemoryStream();

            // Act
            await _server.HandleGetDeckAsync(memoryStream, context);

            // Assert
            _userRepositoryMock.Verify(repo => repo.GetByUsername("testuser"), Times.AtLeastOnce);
            _cardRepositoryMock.Verify(repo => repo.GetUserDeck(user.Id), Times.Once);

            memoryStream.Position = 0;
            using var streamReader = new StreamReader(memoryStream);
            var response = await streamReader.ReadToEndAsync();

            StringAssert.Contains("HTTP/1.1 200 OK", response);
            StringAssert.Contains("Content-Type: application/json", response);

            var responseBody = JsonSerializer.Deserialize<List<Card>>(response.Split("\r\n\r\n")[1]);
            Assert.AreEqual(2, responseBody.Count);
            Assert.AreEqual("Card 1", responseBody[0].Name);
            Assert.AreEqual(10, responseBody[0].Damage);
            Assert.AreEqual("Card 2", responseBody[1].Name);
            Assert.AreEqual(20, responseBody[1].Damage);
        }
        
        
        [Test]
        public async Task HandleConfigureDeckAsync_ValidRequest_ConfiguresDeck()
        {
            // Arrange
            var user = new User { Id = 1, Username = "testuser" };
            var cardIds = new List<int> { 1, 2, 3, 4 };
            var requestBody = JsonSerializer.Serialize(cardIds);

            _userRepositoryMock.Setup(repo => repo.GetByUsername("testuser")).Returns(user);

            var context = new DefaultHttpContext();
            context.Items["Username"] = "testuser";

            using var memoryStream = new MemoryStream();

            // Act
            await _server.HandleConfigureDeckAsync(memoryStream, requestBody, context);

            // Assert
            _userRepositoryMock.Verify(repo => repo.GetByUsername("testuser"), Times.AtLeastOnce);
            _cardRepositoryMock.Verify(repo => repo.UpdateDeck(user.Id, cardIds), Times.Once);

            memoryStream.Position = 0;
            using var streamReader = new StreamReader(memoryStream);
            var response = await streamReader.ReadToEndAsync();

            StringAssert.Contains("HTTP/1.1 200 OK", response);
            StringAssert.Contains("Deck configured successfully", response);
        }
        
        [Test]
        public async Task HandleConfigureDeckAsync_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            var cardIds = new List<int> { 1, 2, 3, 4 };
            var requestBody = JsonSerializer.Serialize(cardIds);

            _userRepositoryMock.Setup(repo => repo.GetByUsername("testuser")).Returns((User)null);

            var context = new DefaultHttpContext();
            context.Items["Username"] = "testuser";

            using var memoryStream = new MemoryStream();

            // Act
            await _server.HandleConfigureDeckAsync(memoryStream, requestBody, context);

            // Assert
            memoryStream.Position = 0;
            using var streamReader = new StreamReader(memoryStream);
            var response = await streamReader.ReadToEndAsync();
            
            StringAssert.Contains("HTTP/1.1 404 Not Found", response);
            StringAssert.Contains("User not found", response);
        }

        [Test]
        public async Task HandleConfigureDeckAsync_AuthenticationRequired_ReturnsUnauthorized()
        {
            // Arrange
            var cardIds = new List<int> { 1, 2, 3, 4 };
            var requestBody = JsonSerializer.Serialize(cardIds);

            var context = new DefaultHttpContext();

            using var memoryStream = new MemoryStream();

            // Act
            await _server.HandleConfigureDeckAsync(memoryStream, requestBody, context);

            // Assert
            memoryStream.Position = 0;
            using var streamReader = new StreamReader(memoryStream);
            var response = await streamReader.ReadToEndAsync();
            
            StringAssert.Contains("HTTP/1.1 401 Unauthorized", response);
            StringAssert.Contains("Authentication required", response);
        }
    }
}