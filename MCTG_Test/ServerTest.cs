using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;

namespace MCTG
{
    [TestFixture]
    public class ServerTests
    {
        private Mock<IUserRepository> _userRepositoryMock;
        private Mock<ICardRepository> _cardRepositoryMock;
        private Mock<ITradeRepository> _tradeRepositoryMock;
        private Mock<IJwtService> _jwtServiceMock;
        private TcpServer _server;

        [SetUp]
        public void Setup()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _cardRepositoryMock = new Mock<ICardRepository>();
            _tradeRepositoryMock = new Mock<ITradeRepository>();
            _jwtServiceMock = new Mock<IJwtService>();
            _server = new TcpServer(10001, _userRepositoryMock.Object, _cardRepositoryMock.Object,
                _tradeRepositoryMock.Object, _jwtServiceMock.Object);
        }

        [Test]
        public async Task HandleLoginAsync_ValidCredentials_ReturnsOkResponseWithToken()
        {
            // Arrange
            var stream = new MemoryStream();
            var body = "{\"username\":\"testuser\",\"password\":\"password\"}";

            _userRepositoryMock
                .Setup(repo => repo.ValidateCredentials("testuser", "password"))
                .Returns(true);

            _jwtServiceMock
                .Setup(service => service.GenerateToken("testuser"))
                .Returns("test-token");

            // Act
            await _server.HandleLoginAsync(stream, body);

            // Verify the ValidateCredentials method was called
            _userRepositoryMock.Verify(repo => repo.ValidateCredentials("testuser", "password"), Times.Once);

            // Verify the GenerateToken method was called
            _jwtServiceMock.Verify(service => service.GenerateToken("testuser"), Times.Once);

            // Debugging assistance
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Console.WriteLine("Response Written to Stream:\n" + response);

            // Assert
            Assert.That(response, Contains.Substring("HTTP/1.1 200 OK"));
            Assert.That(response, Contains.Substring("test-token"));
        }




        [Test]
        public async Task HandleGetCardsAsync_UserExists_ReturnsOkResponseWithCards()
        {
            // Arrange
            var stream = new MemoryStream();
            var context = new DefaultHttpContext();
            context.User = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(
                    new[] { new System.Security.Claims.Claim("username", "testuser") }, "mock"));

            var user = new User { Id = 1, Username = "testuser" };
            _userRepositoryMock
                .Setup(repo => repo.GetByUsername("testuser"))
                .Returns(user);

            var cards = new[]
            {
                new Card { Id = 1, Name = "Card1" },
                new Card { Id = 2, Name = "Card2" }
            };
            _cardRepositoryMock
                .Setup(repo => repo.GetUserCards(user.Id))
                .Returns(cards);

            // Act
            await _server.HandleGetCardsAsync(stream, context);

            // Verify the GetByUsername method was called
            _userRepositoryMock.Verify(repo => repo.GetByUsername("testuser"), Times.Once);

            // Verify the GetUserCards method was called
            _cardRepositoryMock.Verify(repo => repo.GetUserCards(user.Id), Times.Once);

            // Debugging assistance
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Console.WriteLine("Response Written to Stream:\n" + response);

            // Assert
            Assert.That(response, Contains.Substring("HTTP/1.1 200 OK"));
            Assert.That(response, Contains.Substring(JsonSerializer.Serialize(cards)));
        }

        
        [Test]
        public async Task HandleRegistrationAsync_ValidUser_ReturnsCreatedResponse()
        {
            // Arrange
            var stream = new MemoryStream();
            var body = "{\"username\":\"testuser\",\"password\":\"password\"}";

            // Act
            await _server.HandleRegistrationAsync(stream, body);

            // Assert
            _userRepositoryMock.Verify(repo => repo.Add(It.IsAny<User>()), Times.Once);
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("HTTP/1.1 201 Created"));
            Assert.That(response, Contains.Substring("User created successfully"));
        }
        
        [Test]
        public void ValidateCredentials_Mock_ReturnsExpectedValue()
        {
            _userRepositoryMock
                .Setup(repo => repo.ValidateCredentials("testuser", "password"))
                .Returns(true);

            var result = _userRepositoryMock.Object.ValidateCredentials("testuser", "password");
            Assert.That(result, Is.True, "Mocked ValidateCredentials should return true.");
        }

        [Test]
        public void GetByUsername_Mock_ReturnsExpectedUser()
        {
            var user = new User { Id = 1, Username = "testuser" };
            _userRepositoryMock
                .Setup(repo => repo.GetByUsername("testuser"))
                .Returns(user);

            var result = _userRepositoryMock.Object.GetByUsername("testuser");
            Assert.That(result, Is.EqualTo(user), "Mocked GetByUsername should return the expected user.");
        }

        [Test]
        public void GetUserCards_Mock_ReturnsExpectedCards()
        {
            var cards = new[]
            {
                new Card { Id = 1, Name = "Card1" },
                new Card { Id = 2, Name = "Card2" }
            };
            _cardRepositoryMock
                .Setup(repo => repo.GetUserCards(1))
                .Returns(cards);

            var result = _cardRepositoryMock.Object.GetUserCards(1);
            Assert.That(result, Is.EqualTo(cards), "Mocked GetUserCards should return the expected cards.");
        }

        [TearDown]
        public void Teardown()
        {
            // Reset mocks to avoid cross-test contamination
            _userRepositoryMock.Reset();
            _cardRepositoryMock.Reset();
            _tradeRepositoryMock.Reset();
            _jwtServiceMock.Reset();
        }
    }
}
