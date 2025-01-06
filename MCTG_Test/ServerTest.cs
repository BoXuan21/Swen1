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
        
        [Test]
        public async Task HandleGetBattleHistoryAsync_ReturnsUserBattleHistory()
        {
            // Arrange
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

            var context = new DefaultHttpContext();
            var identity = new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, "testuser") },
                "test"
            );
            context.User = new ClaimsPrincipal(identity);

            var stream = new MemoryStream();

            // Act
            await _server.HandleGetBattleHistoryAsync(stream, context);

            // Assert
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("200 OK"));
            _battleRepositoryMock.Verify(repo => repo.GetUserBattleHistory(1), Times.Once);
        }
        
        [Test]
        public async Task HandleBuyPackageAsync_NoPackagesAvailable_Returns404()
        {
            // Arrange
            var user = new User { Id = 1, Username = "testuser", Coins = 10 };
            _userRepositoryMock.Setup(r => r.GetByUsername("testuser")).Returns(user);
            _packageRepositoryMock.Setup(r => r.GetAvailablePackage()).Returns((Package)null);

            var context = new DefaultHttpContext();
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "test");
            context.User = new ClaimsPrincipal(identity);

            var stream = new MemoryStream();

            // Act
            await _server.HandleBuyPackageAsync(stream, "", context);

            // Assert
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("404 Not Found"));
        }

        [Test]
        public async Task HandleBuyPackageAsync_NotEnoughCoins_Returns403()
        {
            // Arrange
            var user = new User { Id = 1, Username = "testuser", Coins = 3 };
            _userRepositoryMock.Setup(r => r.GetByUsername("testuser")).Returns(user);

            var context = new DefaultHttpContext();
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "test");
            context.User = new ClaimsPrincipal(identity);

            var stream = new MemoryStream();

            // Act
            await _server.HandleBuyPackageAsync(stream, "", context);

            // Assert
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(response, Contains.Substring("403 Forbidden"));
        }
        
        [Test]
public async Task HandleBattleAsync_UpdatesUserStats_AfterBattle()
{
   // Arrange
   var user1 = new User { Id = 1, Username = "player1", Elo = 100 };
   var user2 = new User { Id = 2, Username = "player2", Elo = 100 };
   
   var stats1 = new UserStats { UserId = 1, GamesPlayed = 0, Wins = 0, Losses = 0, Elo = 100 };
   var stats2 = new UserStats { UserId = 2, GamesPlayed = 0, Wins = 0, Losses = 0, Elo = 100 };

   _userRepositoryMock.Setup(r => r.GetByUsername("player1")).Returns(user1);
   _userRepositoryMock.Setup(r => r.GetByUsername("player2")).Returns(user2);
   _userStatsRepositoryMock.Setup(r => r.GetUserStats(1)).Returns(stats1);
   _userStatsRepositoryMock.Setup(r => r.GetUserStats(2)).Returns(stats2);

   var deck1Cards = new List<Card> { new Card("Card1", 50, ElementType.Fire) { CardType = "Monster", UserId = 1 } };
   var deck2Cards = new List<Card> { new Card("Card2", 40, ElementType.Water) { CardType = "Spell", UserId = 2 } };

   _cardRepositoryMock.Setup(r => r.GetUserDeck(1)).Returns(deck1Cards);
   _cardRepositoryMock.Setup(r => r.GetUserDeck(2)).Returns(deck2Cards);

   var context = new DefaultHttpContext();
   var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "player1") }, "test");
   context.User = new ClaimsPrincipal(identity);

   var battleRequest = new BattleRequest { OpponentUsername = "player2" };
   var stream = new MemoryStream();

   // Act
   await _server.HandleBattleAsync(stream, context, JsonSerializer.Serialize(battleRequest));

   // Assert
   _userStatsRepositoryMock.Verify(r => r.UpdateStats(It.Is<UserStats>(s => 
       s.GamesPlayed == 1 &&
       (s.Wins == 1 || s.Losses == 1))), 
       Times.Exactly(2));
}

[Test]
public async Task HandleBattleAsync_UpdatesElo_AfterBattle()
{
   // Arrange
   var user1 = new User { Id = 1, Username = "player1", Elo = 100 };
   var user2 = new User { Id = 2, Username = "player2", Elo = 100 };
   
   var stats1 = new UserStats { UserId = 1, GamesPlayed = 10, Wins = 5, Elo = 100 };
   var stats2 = new UserStats { UserId = 2, GamesPlayed = 10, Wins = 5, Elo = 100 };

   _userRepositoryMock.Setup(r => r.GetByUsername("player1")).Returns(user1);
   _userRepositoryMock.Setup(r => r.GetByUsername("player2")).Returns(user2);
   _userStatsRepositoryMock.Setup(r => r.GetUserStats(1)).Returns(stats1);
   _userStatsRepositoryMock.Setup(r => r.GetUserStats(2)).Returns(stats2);

   var deck1Cards = new List<Card> { new Card("Card1", 50, ElementType.Fire) { CardType = "Monster", UserId = 1 } };
   var deck2Cards = new List<Card> { new Card("Card2", 40, ElementType.Water) { CardType = "Spell", UserId = 2 } };

   _cardRepositoryMock.Setup(r => r.GetUserDeck(1)).Returns(deck1Cards);
   _cardRepositoryMock.Setup(r => r.GetUserDeck(2)).Returns(deck2Cards);

   var context = new DefaultHttpContext();
   var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "player1") }, "test");
   context.User = new ClaimsPrincipal(identity);

   var battleRequest = new BattleRequest { OpponentUsername = "player2" };
   var stream = new MemoryStream();

   // Act
   await _server.HandleBattleAsync(stream, context, JsonSerializer.Serialize(battleRequest));

   // Assert
   _userStatsRepositoryMock.Verify(r => r.UpdateStats(It.Is<UserStats>(s => 
       Math.Abs(s.Elo - 100) == 3 || Math.Abs(s.Elo - 100) == 5)), 
       Times.Exactly(2));
}

[Test]
public async Task HandleBattleAsync_Draw_UpdatesStats()
{
   // Arrange
   var user1 = new User { Id = 1, Username = "player1", Elo = 100 };
   var user2 = new User { Id = 2, Username = "player2", Elo = 100 };
   
   var stats1 = new UserStats { UserId = 1, GamesPlayed = 0, Draws = 0, Elo = 100 };
   var stats2 = new UserStats { UserId = 2, GamesPlayed = 0, Draws = 0, Elo = 100 };

   _userRepositoryMock.Setup(r => r.GetByUsername("player1")).Returns(user1);
   _userRepositoryMock.Setup(r => r.GetByUsername("player2")).Returns(user2);
   _userStatsRepositoryMock.Setup(r => r.GetUserStats(1)).Returns(stats1);
   _userStatsRepositoryMock.Setup(r => r.GetUserStats(2)).Returns(stats2);

   // Setup identical decks to force a draw
   var deck1Cards = new List<Card> { new Card("Card1", 50, ElementType.Fire) { CardType = "Monster", UserId = 1 } };
   var deck2Cards = new List<Card> { new Card("Card2", 50, ElementType.Fire) { CardType = "Monster", UserId = 2 } };

   _cardRepositoryMock.Setup(r => r.GetUserDeck(1)).Returns(deck1Cards);
   _cardRepositoryMock.Setup(r => r.GetUserDeck(2)).Returns(deck2Cards);

   var context = new DefaultHttpContext();
   var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "player1") }, "test");
   context.User = new ClaimsPrincipal(identity);

   var battleRequest = new BattleRequest { OpponentUsername = "player2" };
   var stream = new MemoryStream();

   // Act
   await _server.HandleBattleAsync(stream, context, JsonSerializer.Serialize(battleRequest));

   // Assert
   _userStatsRepositoryMock.Verify(r => r.UpdateStats(It.Is<UserStats>(s => 
       s.GamesPlayed == 1 && s.Draws == 1)), 
       Times.Exactly(2));
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