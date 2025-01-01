using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;

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
            _server = new TcpServer(
                10001,
                _userRepositoryMock.Object,
                _cardRepositoryMock.Object,
                _tradeRepositoryMock.Object,
                _jwtServiceMock.Object
            );
        }

        [Test]
        public async Task HandleLoginAsync_ValidCredentials_ReturnsOkResponseWithToken()
        {
            var user = new User { Username = "testuser", Password = "password" };
            _userRepositoryMock
                .Setup(repo => repo.ValidateCredentials(
                    It.Is<string>(u => u == "testuser"),
                    It.Is<string>(p => p == "password")))
                .Returns(true);

            _jwtServiceMock
                .Setup(service => service.GenerateToken("testuser"))
                .Returns("test-token");

            var stream = new MemoryStream();
            await _server.HandleLoginAsync(stream, JsonSerializer.Serialize(user));

            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("200 OK"));
            Assert.That(response, Contains.Substring("test-token"));
        }

        [Test]
        public async Task HandleBattleAsync_ValidBattle_ReturnsBattleLog()
        {
            // Arrange
            var user1 = new User
            {
                Id = 1,
                Username = "player1",
                Elo = 100,
                Stack = new Stack()
            };
            var user2 = new User
            {
                Id = 2,
                Username = "player2",
                Elo = 100,
                Stack = new Stack()
            };

            var deck1Cards = new List<Card>
            {
                new Card("Card1", 50, ElementType.Fire)
                {
                    CardType = "Monster",
                    UserId = 1
                }
            };
            var deck2Cards = new List<Card>
            {
                new Card("Card2", 40, ElementType.Water)
                {
                    CardType = "Spell",
                    UserId = 2
                }
            };

            _userRepositoryMock.Setup(r => r.GetByUsername("player1")).Returns(user1);
            _userRepositoryMock.Setup(r => r.GetByUsername("player2")).Returns(user2);
            _cardRepositoryMock.Setup(r => r.GetUserDeck(1)).Returns(deck1Cards);
            _cardRepositoryMock.Setup(r => r.GetUserDeck(2)).Returns(deck2Cards);

            var context = new DefaultHttpContext();
            var identity = new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, "player1") },
                "test"
            );
            context.User = new ClaimsPrincipal(identity);

            var battleRequest = new BattleRequest { OpponentUsername = "player2" };
            var body = JsonSerializer.Serialize(battleRequest);
            var stream = new MemoryStream();

            // Act
            await _server.HandleBattleAsync(stream, context, body);

            // Assert
            _userRepositoryMock.Verify(r => r.GetByUsername("player1"), Times.Once);
            _userRepositoryMock.Verify(r => r.GetByUsername("player2"), Times.Once);
            _userRepositoryMock.Verify(r => r.Update(It.IsAny<User>()), Times.Exactly(2));

            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("200 OK"));
            Assert.That(response, Contains.Substring("\"Rounds\""));
            Assert.That(response, Contains.Substring("\"Winner\""));
            Assert.That(response, Contains.Substring("\"FinalScore1\""));
            Assert.That(response, Contains.Substring("\"FinalScore2\""));
        }

        [Test]
        public async Task HandleBattleAsync_UserNotFound_Returns404()
        {
            // Arrange
            _userRepositoryMock.Setup(r => r.GetByUsername(It.IsAny<string>()))
                .Returns((User)null);

            var context = new DefaultHttpContext();
            var identity = new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, "player1") },
                "test"
            );
            context.User = new ClaimsPrincipal(identity);

            var body = JsonSerializer.Serialize(new BattleRequest { OpponentUsername = "player2" });
            var stream = new MemoryStream();

            // Act
            await _server.HandleBattleAsync(stream, context, body);

            // Assert
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("404 Not Found"));
        }

        [Test]
        public async Task HandleGetCardsAsync_UserExists_ReturnsOkResponseWithCards()
        {
            var user = new User { Id = 1, Username = "testuser" };
            _userRepositoryMock
                .Setup(repo => repo.GetByUsername(It.Is<string>(u => u == "testuser")))
                .Returns(user);

            var cards = new[] { new Card { Id = 1, Name = "Card1" } };
            _cardRepositoryMock
                .Setup(repo => repo.GetUserCards(1))
                .Returns(cards);

            var context = new DefaultHttpContext();
            var identity = new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, "testuser") },
                "test"
            );
            context.User = new ClaimsPrincipal(identity);

            var stream = new MemoryStream();
            await _server.HandleGetCardsAsync(stream, context);

            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("200 OK"));
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