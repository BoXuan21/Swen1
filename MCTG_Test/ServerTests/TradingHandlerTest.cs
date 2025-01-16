using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Moq;

namespace MCTG
{
    [TestFixture]
    public class TradingHandlerTests
    {
        private Mock<IUserRepository> _userRepositoryMock;
        private Mock<ICardRepository> _cardRepositoryMock;
        private Mock<IJwtService> _jwtServiceMock;
        private Mock<IPackageRepository> _packageRepositoryMock;
        private Mock<IBattleRepository> _battleRepositoryMock;
        private Mock<ITradeRepository> _tradeRepositoryMock;
        private Mock<IUserStatsRepository> _userStatsRepositoryMock;
        private TcpServer _server;
        private const string TestConnectionString = "TestConnectionString";

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
                connectionString: TestConnectionString
            );
        }

        [Test]
        public async Task HandleGetTradingsAsync_ReturnsAllTrades()
        {
            // Arrange
            var trades = new List<Trade>
            {
                new Trade { Id = 1, CardId = 1, UserId = 1, RequiredType = "Monster", MinimumDamage = 50 },
                new Trade { Id = 2, CardId = 2, UserId = 2, RequiredType = "Spell", MinimumDamage = 30 }
            };

            _tradeRepositoryMock.Setup(r => r.GetAllTrades()).Returns(trades);
            var stream = new MemoryStream();

            // Act
            await _server.HandleGetTradingsAsync(stream);

            // Assert
            stream.Position = 0;
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("HTTP/1.1 200 OK"));
            Assert.That(response, Contains.Substring("Monster"));
            Assert.That(response, Contains.Substring("Spell"));
            _tradeRepositoryMock.Verify(r => r.GetAllTrades(), Times.Once);
        }

        [Test]
        public async Task HandleCreateTradeAsync_Unauthorized_Returns401()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var stream = new MemoryStream();
            var trade = new Trade { CardId = 1, RequiredType = "Monster", MinimumDamage = 50 };

            // Act
            await _server.HandleCreateTradeAsync(stream, JsonSerializer.Serialize(trade), context);

            // Assert
            stream.Position = 0;
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("HTTP/1.1 401 Unauthorized"));
        }

        [Test]
        public async Task HandleCreateTradeAsync_UserNotFound_Returns404()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Items["Username"] = "testuser";
            var stream = new MemoryStream();
            var trade = new Trade { CardId = 1, RequiredType = "Monster", MinimumDamage = 50 };

            _userRepositoryMock.Setup(r => r.GetByUsername("testuser")).Returns((User)null);

            // Act
            await _server.HandleCreateTradeAsync(stream, JsonSerializer.Serialize(trade), context);

            // Assert
            stream.Position = 0;
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("HTTP/1.1 404 Not Found"));
        }

        [Test]
        public async Task HandleCreateTradeAsync_ValidTrade_ReturnsCreated()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Items["Username"] = "testuser";
            var user = new User { Id = 1, Username = "testuser" };
            var trade = new Trade { CardId = 1, RequiredType = "Monster", MinimumDamage = 50 };

            _userRepositoryMock.Setup(r => r.GetByUsername("testuser")).Returns(user);
            var stream = new MemoryStream();

            // Act
            await _server.HandleCreateTradeAsync(stream, JsonSerializer.Serialize(trade), context);

            // Assert
            stream.Position = 0;
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("HTTP/1.1 201 Created"));
            _tradeRepositoryMock.Verify(r => r.CreateTrade(It.Is<Trade>(t => 
                t.CardId == trade.CardId && 
                t.RequiredType == trade.RequiredType && 
                t.UserId == user.Id)), Times.Once);
        }

        
        [Test]
        public async Task HandleExecuteTradeAsync_TradeWithSelf_ReturnsForbidden()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Items["Username"] = "testuser";
            var user = new User { Id = 1, Username = "testuser" };
            var trade = new Trade { Id = 1, UserId = 1 }; // Same user ID

            _userRepositoryMock.Setup(r => r.GetByUsername("testuser")).Returns(user);
            _tradeRepositoryMock.Setup(r => r.GetTradeById(1)).Returns(trade);

            var stream = new MemoryStream();

            // Act
            await _server.HandleExecuteTradeAsync(stream, "/tradings/1", "2", context);

            // Assert
            stream.Position = 0;
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("HTTP/1.1 403 Forbidden"));
        }

        [Test]
        public async Task HandleExecuteTradeAsync_SuccessfulTrade_ReturnsOk()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Items["Username"] = "testuser";
            var user = new User { Id = 1, Username = "testuser" };
            var trade = new Trade { Id = 1, UserId = 2 }; // Different user ID

            _userRepositoryMock.Setup(r => r.GetByUsername("testuser")).Returns(user);
            _tradeRepositoryMock.Setup(r => r.GetTradeById(1)).Returns(trade);
            _tradeRepositoryMock.Setup(r => r.ExecuteTrade(1, 2, 1)).Returns(true);

            var stream = new MemoryStream();

            // Act
            await _server.HandleExecuteTradeAsync(stream, "/tradings/1", "2", context);

            // Assert
            stream.Position = 0;
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("HTTP/1.1 200 OK"));
            _tradeRepositoryMock.Verify(r => r.ExecuteTrade(1, 2, 1), Times.Once);
        }
        
        [Test]
        public async Task HandleDeleteTradeAsync_SuccessfulDelete_ReturnsOk()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Items["Username"] = "testuser";
            var user = new User { Id = 1, Username = "testuser" };
            var trade = new Trade { Id = 1, UserId = 1 }; // Same user ID

            _userRepositoryMock.Setup(r => r.GetByUsername("testuser")).Returns(user);
            _tradeRepositoryMock.Setup(r => r.GetTradeById(1)).Returns(trade);

            var stream = new MemoryStream();

            // Act
            await _server.HandleDeleteTradeAsync(stream, "/tradings/1", context);

            // Assert
            stream.Position = 0;
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("HTTP/1.1 200 OK"));
            _tradeRepositoryMock.Verify(r => r.DeleteTrade(1), Times.Once);
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