using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Moq;

namespace MCTG
{
    [TestFixture]
    public class ServerTests
    {
        private Mock<IUserRepository> _userRepositoryMock;
        private Mock<ICardRepository> _cardRepositoryMock;
        private Mock<ITradeRepository> _tradeRepositoryMock;
        private Mock<IJwtService> _jwtServiceMock;
        private Mock<IBattleRepository> _battleRepositoryMock;
        private Mock<IPackageRepository> _packageRepositoryMock;   
        private Mock<IUserStatsRepository> _userStatsRepositoryMock;  
        private TcpServer _server;

        [SetUp]
        public void Setup()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _cardRepositoryMock = new Mock<ICardRepository>();
            _tradeRepositoryMock = new Mock<ITradeRepository>();
            _battleRepositoryMock = new Mock<IBattleRepository>();
            _packageRepositoryMock = new Mock<IPackageRepository>();
            _userStatsRepositoryMock = new Mock<IUserStatsRepository>(); 
            _jwtServiceMock = new Mock<IJwtService>();
    
            _server = new TcpServer(
                10001,
                _userRepositoryMock.Object,
                _cardRepositoryMock.Object,
                _tradeRepositoryMock.Object,
                _battleRepositoryMock.Object,
                _jwtServiceMock.Object,
                _userStatsRepositoryMock.Object,
                _packageRepositoryMock.Object
            );
        }

        [Test]
        public async Task HandleLoginAsync_ValidCredentials_ReturnsOkResponseWithToken()
        {
            // Arrange
            var stream = new MemoryStream();
            var user = new User { Username = "testuser", Password = "password" };

            _userRepositoryMock
                .Setup(repo => repo.ValidateCredentials("testuser", "password"))
                .Returns(true);

            _jwtServiceMock
                .Setup(service => service.GenerateToken("testuser"))
                .Returns("test-token");

            // Act
            await _server.HandleLoginAsync(stream, JsonSerializer.Serialize(user));

            // Assert
            stream.Position = 0;
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("HTTP/1.1 200 OK"));
            Assert.That(response, Contains.Substring("test-token"));
        }

        [Test]
        public async Task HandleBattleAsync_UserNotFound_Returns404()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Items["Username"] = "player1";

            _userRepositoryMock.Setup(r => r.GetByUsername(It.IsAny<string>()))
                .Returns((User)null);

            var body = JsonSerializer.Serialize(new BattleRequest { OpponentUsername = "player2" });
            var stream = new MemoryStream();

            // Act
            await _server.HandleBattleAsync(stream, context, body);

            // Assert
            stream.Position = 0;
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("HTTP/1.1 404 Not Found"));
        }

        [Test]
        public async Task HandleGetCardsAsync_UserExists_ReturnsOkResponseWithCards()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Items["Username"] = "testuser";

            var user = new User { Id = 1, Username = "testuser" };
            _userRepositoryMock
                .Setup(repo => repo.GetByUsername("testuser"))
                .Returns(user);

            var cards = new[] { new Card { Id = 1, Name = "Card1" } };
            _cardRepositoryMock
                .Setup(repo => repo.GetUserCards(1))
                .Returns(cards);

            var stream = new MemoryStream();

            // Act
            await _server.HandleGetCardsAsync(stream, context);

            // Assert
            stream.Position = 0;
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("HTTP/1.1 200 OK"));
            Assert.That(response, Contains.Substring("Card1"));
        }

        [Test]
        public async Task HandleGetBattleHistoryAsync_ReturnsUserBattleHistory()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Items["Username"] = "testuser";

            var user = new User { Id = 1, Username = "testuser" };
            var battleHistory = new[] 
            {
                new BattleHistory 
                { 
                    Id = 1, 
                    Player1Id = 1, 
                    Player2Id = 2, 
                    WinnerId = 1 
                }
            };

            _userRepositoryMock
                .Setup(repo => repo.GetByUsername("testuser"))
                .Returns(user);
            _battleRepositoryMock
                .Setup(repo => repo.GetUserBattleHistory(1))
                .Returns(battleHistory);

            var stream = new MemoryStream();

            // Act
            await _server.HandleGetBattleHistoryAsync(stream, context);

            // Assert
            stream.Position = 0;
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("HTTP/1.1 200 OK"));
            _battleRepositoryMock.Verify(repo => repo.GetUserBattleHistory(1), Times.Once);
        }

        [Test]
        public async Task HandleGetProfileAsync_UserExists_ReturnsOkWithProfile()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Items["Username"] = "testuser";

            var user = new User { Id = 1, Username = "testuser" };
            var profile = new UserProfile 
            { 
                UserId = 1, 
                Name = "Test User", 
                Bio = "Test Bio", 
                Image = "test.jpg" 
            };

            _userRepositoryMock.Setup(r => r.GetByUsername("testuser")).Returns(user);
            _userRepositoryMock.Setup(r => r.GetUserProfile(1)).Returns(profile);

            var stream = new MemoryStream();

            // Act
            await _server.HandleGetProfileAsync(stream, "testuser", context);

            // Assert
            stream.Position = 0;
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("HTTP/1.1 200 OK"));
            Assert.That(response, Contains.Substring("Test User"));
            Assert.That(response, Contains.Substring("Test Bio"));
        }

        [Test]
        public async Task HandleGetProfileAsync_UnauthorizedAccess_Returns403()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Items["Username"] = "differentuser";

            var user = new User { Id = 1, Username = "testuser" };
            _userRepositoryMock.Setup(r => r.GetByUsername("testuser")).Returns(user);

            var stream = new MemoryStream();

            // Act
            await _server.HandleGetProfileAsync(stream, "testuser", context);

            // Assert
            stream.Position = 0;
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("HTTP/1.1 403 Forbidden"));
        }

        [Test]
        public async Task HandleUpdateProfileAsync_ValidProfile_ReturnsOk()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Items["Username"] = "testuser";

            var user = new User { Id = 1, Username = "testuser" };
            var profile = new UserProfile 
            { 
                UserId = 1, 
                Name = "Updated Name", 
                Bio = "Updated Bio", 
                Image = "updated.jpg" 
            };

            _userRepositoryMock.Setup(r => r.GetByUsername("testuser")).Returns(user);
            _userRepositoryMock.Setup(r => r.UpdateProfile(It.IsAny<UserProfile>())).Verifiable();

            var stream = new MemoryStream();
            var body = JsonSerializer.Serialize(profile);

            // Act
            await _server.HandleUpdateProfileAsync(stream, "testuser", body, context);

            // Assert
            stream.Position = 0;
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("HTTP/1.1 200 OK"));
            _userRepositoryMock.Verify(r => r.UpdateProfile(It.Is<UserProfile>(p => 
                p.Name == "Updated Name" && 
                p.Bio == "Updated Bio" && 
                p.Image == "updated.jpg")), Times.Once);
        }

        [Test]
        public async Task HandleRegistrationAsync_InvalidUser_ReturnsBadRequest()
        {
            // Arrange
            var stream = new MemoryStream();
            var invalidUser = new User { Username = "", Password = "" };

            // Act
            await _server.HandleRegistrationAsync(stream, JsonSerializer.Serialize(invalidUser));

            // Assert
            stream.Position = 0;
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("HTTP/1.1 400 Bad Request"));
        }

        [Test]
        public async Task HandleLoginAsync_InvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var stream = new MemoryStream();
            var user = new User { Username = "testuser", Password = "wrongpass" };

            _userRepositoryMock
                .Setup(repo => repo.ValidateCredentials("testuser", "wrongpass"))
                .Returns(false);

            // Act
            await _server.HandleLoginAsync(stream, JsonSerializer.Serialize(user));

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

            var user = new User { Id = 1, Username = "testuser", Coins = 3 };
            _userRepositoryMock.Setup(r => r.GetByUsername("testuser")).Returns(user);

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

            var user = new User { Id = 1, Username = "testuser", Coins = 10 };
            _userRepositoryMock.Setup(r => r.GetByUsername("testuser")).Returns(user);
            _packageRepositoryMock.Setup(r => r.GetAvailablePackage()).Returns((Package)null);

            var stream = new MemoryStream();

            // Act
            await _server.HandleBuyPackageAsync(stream, "", context);

            // Assert
            stream.Position = 0;
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("HTTP/1.1 404 Not Found"));
        }

        [Test]
        public async Task HandleBuyPackageAsync_SuccessfulPurchase_UpdatesUserCoins()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Items["Username"] = "testuser";

            var user = new User { Id = 1, Username = "testuser", Coins = 10 };
            var package = new Package { Id = 1, Cards = new List<Card> { new Card { Id = 1, Name = "TestCard" } } };

            _userRepositoryMock.Setup(r => r.GetByUsername("testuser")).Returns(user);
            _packageRepositoryMock.Setup(r => r.GetAvailablePackage()).Returns(package);

            var stream = new MemoryStream();

            // Act
            await _server.HandleBuyPackageAsync(stream, "", context);

            // Assert
            _userRepositoryMock.Verify(r => r.Update(It.Is<User>(u => u.Coins == 5)), Times.Once);
        }

        [Test]
        public async Task HandleGetDeckAsync_PlainFormat_ReturnsPlainTextResponse()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Items["Username"] = "testuser";
            context.Request.Path = "/deck?format=plain";

            var user = new User { Id = 1, Username = "testuser" };
            var deck = new List<Card> { new Card { Id = 1, Name = "TestCard", Damage = 10 } };

            _userRepositoryMock.Setup(r => r.GetByUsername("testuser")).Returns(user);
            _cardRepositoryMock.Setup(r => r.GetUserDeck(1)).Returns(deck);

            var stream = new MemoryStream();

            // Act
            await _server.HandleGetDeckAsync(stream, context);

            // Assert
            stream.Position = 0;
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("HTTP/1.1 200 OK"));
            Assert.That(response, Contains.Substring("TestCard: 10 damage"));
        }

        [Test]
        public async Task HandleConfigureDeckAsync_InvalidCardCount_ReturnsBadRequest()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Items["Username"] = "testuser";

            var user = new User { Id = 1, Username = "testuser" };
            _userRepositoryMock.Setup(r => r.GetByUsername("testuser")).Returns(user);

            var stream = new MemoryStream();
            var invalidDeck = new List<int> { 1, 2, 3 }; // Only 3 cards instead of 4

            // Act
            await _server.HandleConfigureDeckAsync(stream, JsonSerializer.Serialize(invalidDeck), context);

            // Assert
            stream.Position = 0;
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("HTTP/1.1 400 Bad Request"));
        }

        [Test]
        public async Task HandleGetStatsAsync_ValidUser_ReturnsStats()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Items["Username"] = "testuser";

            var user = new User { Id = 1, Username = "testuser", Elo = 100, Coins = 20 };
            _userRepositoryMock.Setup(r => r.GetByUsername("testuser")).Returns(user);

            var stream = new MemoryStream();

            // Act
            await _server.HandleGetStatsAsync(stream, context);

            // Assert
            stream.Position = 0;
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("HTTP/1.1 200 OK"));
            Assert.That(response, Contains.Substring("\"Elo\":100"));
            Assert.That(response, Contains.Substring("\"Coins\":20"));
        }

        [Test]
        public async Task HandleGetScoreboardAsync_ReturnsOrderedScoreboard()
        {
            // Arrange
            var users = new List<User>
            {
                new User { Username = "user1", Elo = 100 },
                new User { Username = "user2", Elo = 200 },
                new User { Username = "user3", Elo = 150 }
            };

            _userRepositoryMock.Setup(r => r.GetAllUsers()).Returns(users);

            var stream = new MemoryStream();

            // Act
            await _server.HandleGetScoreboardAsync(stream);

            // Assert
            stream.Position = 0;
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("HTTP/1.1 200 OK"));
            Assert.That(response.IndexOf("user2"), Is.LessThan(response.IndexOf("user3")));
            Assert.That(response.IndexOf("user3"), Is.LessThan(response.IndexOf("user1")));
        }

        [Test]
        public async Task HandleCreateTradeAsync_InvalidTrade_ReturnsBadRequest()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Items["Username"] = "testuser";

            var user = new User { Id = 1, Username = "testuser" };
            _userRepositoryMock.Setup(r => r.GetByUsername("testuser")).Returns(user);

            _tradeRepositoryMock
                .Setup(r => r.CreateTrade(It.IsAny<Trade>()))
                .Throws(new Exception("Invalid trade"));

            var stream = new MemoryStream();
            var trade = new Trade { Id = 1, CardId = 1, UserId = 1 };

            // Act
            await _server.HandleCreateTradeAsync(stream, JsonSerializer.Serialize(trade), context);

            // Assert
            stream.Position = 0;
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("HTTP/1.1 400 Bad Request"));
        }

        [Test]
        public async Task HandleDeleteTradeAsync_UnauthorizedDelete_ReturnsForbidden()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Items["Username"] = "testuser";

            var user = new User { Id = 1, Username = "testuser" };
            var trade = new Trade { Id = 1, UserId = 2 }; // Trade belongs to different user

            _userRepositoryMock.Setup(r => r.GetByUsername("testuser")).Returns(user);
            _tradeRepositoryMock.Setup(r => r.GetTradeById(1)).Returns(trade);

            var stream = new MemoryStream();

            // Act
            await _server.HandleDeleteTradeAsync(stream, "/tradings/1", context);

            // Assert
            stream.Position = 0;
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("HTTP/1.1 403 Forbidden"));
        }

        [Test]
        public async Task HandleExecuteTradeAsync_TradeWithSelf_ReturnsForbidden()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Items["Username"] = "testuser";

            var user = new User { Id = 1, Username = "testuser" };
            var trade = new Trade { Id = 1, UserId = 1 }; // Trade belongs to same user

            _userRepositoryMock.Setup(r => r.GetByUsername("testuser")).Returns(user);
            _tradeRepositoryMock.Setup(r => r.GetTradeById(1)).Returns(trade);

            var stream = new MemoryStream();
            var offeredCardId = 2;

            // Act
            await _server.HandleExecuteTradeAsync(stream, "/tradings/1", JsonSerializer.Serialize(offeredCardId), context);

            // Assert
            stream.Position = 0;
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("HTTP/1.1 403 Forbidden"));
        }

        [Test]
        public async Task HandleCreatePackageAsync_ValidPackage_ReturnsCreated()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var cards = new List<CardDTO>
            {
                new CardDTO { Id = "1", Name = "Card1", Damage = 10 },
                new CardDTO { Id = "2", Name = "Card2", Damage = 20 }
            };

            var stream = new MemoryStream();

            // Act
            await _server.HandleCreatePackageAsync(stream, context, JsonSerializer.Serialize(cards));

            // Assert
            stream.Position = 0;
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("HTTP/1.1 201 Created"));
            _packageRepositoryMock.Verify(r => r.CreatePackage(It.IsAny<List<Card>>()), Times.Once);
        }

        [Test]
        public async Task HandleBattleAsync_InsufficientCards_ReturnsBadRequest()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Items["Username"] = "player1";

            var user1 = new User { Id = 1, Username = "player1" };
            var user2 = new User { Id = 2, Username = "player2" };
            var stats1 = new UserStats { UserId = 1 };
            var stats2 = new UserStats { UserId = 2 };

            _userRepositoryMock.Setup(r => r.GetByUsername("player1")).Returns(user1);
            _userRepositoryMock.Setup(r => r.GetByUsername("player2")).Returns(user2);
            _userStatsRepositoryMock.Setup(r => r.GetUserStats(1)).Returns(stats1);
            _userStatsRepositoryMock.Setup(r => r.GetUserStats(2)).Returns(stats2);
            _cardRepositoryMock.Setup(r => r.GetUserDeck(It.IsAny<int>())).Returns(new List<Card> { new Card() }); // Only 1 card

            var stream = new MemoryStream();
            var battleRequest = new BattleRequest { OpponentUsername = "player2" };

            // Act
            await _server.HandleBattleAsync(stream, context, JsonSerializer.Serialize(battleRequest));

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
            _tradeRepositoryMock.Reset();
            _battleRepositoryMock.Reset();
            _packageRepositoryMock.Reset();
            _userStatsRepositoryMock.Reset();
            _jwtServiceMock.Reset();
        }
    }
}