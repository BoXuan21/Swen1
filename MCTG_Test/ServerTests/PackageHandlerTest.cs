using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;

namespace MCTG
{
    [TestFixture]
    public class PackageHandlerTests
    {
        private Mock<IUserRepository> _userRepositoryMock;
        private Mock<ICardRepository> _cardRepositoryMock;
        private Mock<IJwtService> _jwtServiceMock;
        private Mock<IPackageRepository> _packageRepositoryMock;
        private Mock<IBattleRepository> _battleRepositoryMock;
        private Mock<ITradeRepository> _tradeRepositoryMock;
        private Mock<IUserStatsRepository> _userStatsRepositoryMock;
        private TcpServer _server;

        [SetUp]
        public void Setup()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _cardRepositoryMock = new Mock<ICardRepository>();
            _jwtServiceMock = new Mock<IJwtService>();
            _packageRepositoryMock = new Mock<IPackageRepository>();
            _battleRepositoryMock = new Mock<IBattleRepository>();
            _tradeRepositoryMock = new Mock<ITradeRepository>();
            _userStatsRepositoryMock = new Mock<IUserStatsRepository>();

            _server = new TcpServer(
                port: 10001,
                userRepository: _userRepositoryMock.Object,
                cardRepository: _cardRepositoryMock.Object,
                jwtService: _jwtServiceMock.Object,
                packageRepository: _packageRepositoryMock.Object,
                battleRepository: _battleRepositoryMock.Object,
                tradeRepository: _tradeRepositoryMock.Object,
                userStatsRepository: _userStatsRepositoryMock.Object,
                connectionString: "mock_connection_string"
            );
        }

        [Test]
        public async Task HandleBuyPackageAsync_NoAuth_ReturnsUnauthorized()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var stream = new MemoryStream();

            // Act
            await _server.HandleBuyPackageAsync(stream, "", context);

            // Assert
            stream.Position = 0;
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("HTTP/1.1 401 Unauthorized"));
        }

        [Test]
        public async Task HandleBuyPackageAsync_NotEnoughCoins_ReturnsForbidden()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Items["Username"] = "testuser";

            _userRepositoryMock.Setup(r => r.GetByUsername("testuser"))
                .Returns(new User { Id = 1, Username = "testuser", Coins = 3 });

            var stream = new MemoryStream();

            // Act
            await _server.HandleBuyPackageAsync(stream, "", context);

            // Assert
            stream.Position = 0;
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("HTTP/1.1 403 Forbidden"));
        }

        [Test]
        public async Task HandleBuyPackageAsync_NoPackagesAvailable_ReturnsNotFound()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Items["Username"] = "testuser";

            _userRepositoryMock.Setup(r => r.GetByUsername("testuser"))
                .Returns(new User { Id = 1, Username = "testuser", Coins = 10 });

            _packageRepositoryMock.Setup(r => r.GetAvailablePackage())
                .Returns((Package)null);

            var stream = new MemoryStream();

            // Act
            await _server.HandleBuyPackageAsync(stream, "", context);

            // Assert
            stream.Position = 0;
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("HTTP/1.1 404 Not Found"));
        }

        [Test]
        public async Task HandleBuyPackageAsync_SuccessfulPurchase_ReturnsOk()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Items["Username"] = "testuser";
            var user = new User { Id = 1, Username = "testuser", Coins = 10 };
            var cards = new List<Card> { new Card { Id = 1, Name = "TestCard" } };
            var package = new Package { Id = 1, Cards = cards };

            _userRepositoryMock.Setup(r => r.GetByUsername("testuser"))
                .Returns(user);
            _packageRepositoryMock.Setup(r => r.GetAvailablePackage())
                .Returns(package);

            var stream = new MemoryStream();

            // Act
            await _server.HandleBuyPackageAsync(stream, "", context);

            // Assert
            stream.Position = 0;
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("HTTP/1.1 200 OK"));
            _packageRepositoryMock.Verify(r => r.MarkPackageAsSold(package.Id, user.Id), Times.Once);
        }

        [Test]
        public async Task HandleCreatePackageAsync_NonAdmin_ReturnsForbidden()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Items["Username"] = "normaluser";
            var stream = new MemoryStream();

            // Act
            await _server.HandleCreatePackageAsync(stream, "", context);

            // Assert
            stream.Position = 0;
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("HTTP/1.1 403 Forbidden"));
        }

        [Test]
        public async Task HandleCreatePackageAsync_InvalidJson_ReturnsBadRequest()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Items["Username"] = "admin";
            var stream = new MemoryStream();
            var invalidJson = "invalid json";

            // Act
            await _server.HandleCreatePackageAsync(stream, invalidJson, context);

            // Assert
            stream.Position = 0;
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("HTTP/1.1 400 Bad Request"));
        }

        [Test]
        public async Task HandleCreatePackageAsync_ValidPackage_ReturnsCreated()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Items["Username"] = "admin";
            var cards = new[]
            {
                new { Id = "845f0dc7-37d0-426e-994e-43fc3ac83c08", Name = "WaterGoblin", Damage = 10 }
            };

            var stream = new MemoryStream();

            // Act
            await _server.HandleCreatePackageAsync(stream, JsonSerializer.Serialize(cards), context);

            // Assert
            stream.Position = 0;
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("HTTP/1.1 201 Created"));
            _packageRepositoryMock.Verify(r => r.CreatePackage(It.Is<List<Card>>(
                cardList => cardList.Count == 1 && 
                           cardList[0].Name == "WaterGoblin" && 
                           cardList[0].Damage == 10)), 
                Times.Once);
        }

        [Test]
        public async Task HandleCreatePackageAsync_NoCards_ReturnsBadRequest()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Items["Username"] = "admin";
            var emptyCards = new List<object>();
            var stream = new MemoryStream();

            // Act
            await _server.HandleCreatePackageAsync(stream, JsonSerializer.Serialize(emptyCards), context);

            // Assert
            stream.Position = 0;
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("HTTP/1.1 400 Bad Request"));
        }

        [TearDown]
        public void Teardown()
        {
            _userRepositoryMock.Reset();
            _cardRepositoryMock.Reset();
            _jwtServiceMock.Reset();
            _packageRepositoryMock.Reset();
            _battleRepositoryMock.Reset();
            _tradeRepositoryMock.Reset();
            _userStatsRepositoryMock.Reset();
        }
    }
}