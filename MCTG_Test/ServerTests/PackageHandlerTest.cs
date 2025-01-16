using System.Text.Json;
using Moq;
using NUnit.Framework;

namespace MCTG.Tests
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
        public async Task HandleBuyPackageAsync_NotEnoughCoins_ReturnsForbidden()
        {
            // Arrange
            var context = new CustomHttpContext();
            context.Items["Username"] = "testuser";

            _userRepositoryMock.Setup(r => r.GetByUsername("testuser"))
                .Returns(new User { Id = 1, Username = "testuser", Coins = 3 });

            // Act
            await _server.HandleBuyPackageAsync(context);

            // Assert
            Assert.That(context.Response.StatusCode, Is.EqualTo(403));
            Assert.That(context.Response.StatusDescription, Is.EqualTo("Forbidden"));
            Assert.That(context.Response.Body, Is.EqualTo("Not enough coins"));
        }

        [Test]
        public async Task HandleBuyPackageAsync_NoPackagesAvailable_ReturnsNotFound()
        {
            // Arrange
            var context = new CustomHttpContext();
            context.Items["Username"] = "testuser";

            _userRepositoryMock.Setup(r => r.GetByUsername("testuser"))
                .Returns(new User { Id = 1, Username = "testuser", Coins = 10 });

            _packageRepositoryMock.Setup(r => r.GetAvailablePackage())
                .Returns((Package)null);

            // Act
            await _server.HandleBuyPackageAsync(context);

            // Assert
            Assert.That(context.Response.StatusCode, Is.EqualTo(404));
            Assert.That(context.Response.StatusDescription, Is.EqualTo("Not Found"));
            Assert.That(context.Response.Body, Is.EqualTo("No packages available"));
        }

        [Test]
        public async Task HandleBuyPackageAsync_SuccessfulPurchase_ReturnsOk()
        {
            // Arrange
            var context = new CustomHttpContext();
            context.Items["Username"] = "testuser";
            var user = new User { Id = 1, Username = "testuser", Coins = 10 };
            var cards = new List<Card> { new Card { Id = 1, Name = "TestCard" } };
            var package = new Package { Id = 1, Cards = cards };

            _userRepositoryMock.Setup(r => r.GetByUsername("testuser"))
                .Returns(user);
            _packageRepositoryMock.Setup(r => r.GetAvailablePackage())
                .Returns(package);

            // Act
            await _server.HandleBuyPackageAsync(context);

            // Assert
            Assert.That(context.Response.StatusCode, Is.EqualTo(200));
            Assert.That(context.Response.StatusDescription, Is.EqualTo("OK"));
            _packageRepositoryMock.Verify(r => r.MarkPackageAsSold(package.Id, user.Id), Times.Once);
            
            var responseCards = JsonSerializer.Deserialize<List<Card>>(context.Response.Body);
            Assert.That(responseCards, Has.Count.EqualTo(1));
            Assert.That(responseCards[0].Name, Is.EqualTo("TestCard"));
        }

        [Test]
        public async Task HandleCreatePackageAsync_NonAdmin_ReturnsForbidden()
        {
            // Arrange
            var context = new CustomHttpContext();
            context.Items["Username"] = "normaluser";

            // Act
            await _server.HandleCreatePackageAsync(context);

            // Assert
            Assert.That(context.Response.StatusCode, Is.EqualTo(403));
            Assert.That(context.Response.StatusDescription, Is.EqualTo("Forbidden"));
            Assert.That(context.Response.Body, Is.EqualTo("Only admin can create packages"));
        }

        [Test]
        public async Task HandleCreatePackageAsync_ValidPackage_ReturnsCreated()
        {
            // Arrange
            var context = new CustomHttpContext();
            context.Items["Username"] = "admin";
            var cards = new[]
            {
                new { Id = "845f0dc7-37d0-426e-994e-43fc3ac83c08", Name = "WaterGoblin", Damage = 10 }
            };
            context.Request.Body = JsonSerializer.Serialize(cards);

            // Act
            await _server.HandleCreatePackageAsync(context);

            // Assert
            Assert.That(context.Response.StatusCode, Is.EqualTo(201));
            Assert.That(context.Response.StatusDescription, Is.EqualTo("Created"));
            _packageRepositoryMock.Verify(r => r.CreatePackage(It.Is<List<Card>>(
                cardList => cardList.Count == 1 && 
                           cardList[0].Name == "WaterGoblin" && 
                           cardList[0].Damage == 10)), 
                Times.Once);
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