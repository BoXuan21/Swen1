using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;

namespace MCTG.Tests
{
    [TestFixture]
    public class HandleBattleAsyncTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<ICardRepository> _cardRepositoryMock;
        private readonly Mock<IBattleRepository> _battleRepositoryMock;
        private readonly TcpServer _server;

        public HandleBattleAsyncTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _cardRepositoryMock = new Mock<ICardRepository>();
            _battleRepositoryMock = new Mock<IBattleRepository>();
            _server = new TcpServer(0, _userRepositoryMock.Object, _cardRepositoryMock.Object, 
                null, null, _battleRepositoryMock.Object, null, null, "");
        }

        [Test]
        public async Task ValidBattle_SavesBattleLog()
        {
            // Arrange
            var user1 = new User { Id = 1, Username = "user1", Elo = 100 };
            var user2 = new User { Id = 2, Username = "user2", Elo = 100 };
            var battleRequest = new BattleRequest { OpponentUsername = "user2" };
            var deck1 = new List<Card> { new Card(), new Card(), new Card(), new Card() };
            var deck2 = new List<Card> { new Card(), new Card(), new Card(), new Card() };

            _userRepositoryMock.Setup(repo => repo.GetByUsername("user1")).Returns(user1);
            _userRepositoryMock.Setup(repo => repo.GetByUsername("user2")).Returns(user2);
            _cardRepositoryMock.Setup(repo => repo.GetUserDeck(user1.Id)).Returns(deck1);
            _cardRepositoryMock.Setup(repo => repo.GetUserDeck(user2.Id)).Returns(deck2);

            var context = new DefaultHttpContext();
            context.Items["Username"] = "user1";

            using var memoryStream = new MemoryStream();
            var body = System.Text.Json.JsonSerializer.Serialize(battleRequest);

            // Act
            await _server.HandleBattleAsync(memoryStream, context, body);

            // Assert
            _battleRepositoryMock.Verify(repo => 
                repo.SaveBattleHistory(It.Is<BattleHistory>(history =>
                    history.Player1Id == user1.Id &&
                    history.Player2Id == user2.Id &&
                    history.BattleLog != null)), Times.Once);

            memoryStream.Position = 0;
            using var streamReader = new StreamReader(memoryStream);
            var response = await streamReader.ReadToEndAsync();
            
            StringAssert.Contains("HTTP/1.1 200 OK", response);
            StringAssert.Contains("Content-Type: application/json", response);
            StringAssert.Contains("\"Winner\":", response);
        }
        
        [Test]
        public async Task InsufficientCardsInDeck_ReturnsBadRequest()
        {
            // Arrange
            var user1 = new User { Id = 1, Username = "user1", Elo = 100 };
            var user2 = new User { Id = 2, Username = "user2", Elo = 100 };
            var battleRequest = new BattleRequest { OpponentUsername = "user2" };
            var deck1 = new List<Card> { new Card(), new Card() }; // Insufficient cards

            _userRepositoryMock.Setup(repo => repo.GetByUsername("user1")).Returns(user1);
            _userRepositoryMock.Setup(repo => repo.GetByUsername("user2")).Returns(user2);
            _cardRepositoryMock.Setup(repo => repo.GetUserDeck(user1.Id)).Returns(deck1);

            var context = new DefaultHttpContext();
            context.Items["Username"] = "user1";

            using var memoryStream = new MemoryStream();
            var body = System.Text.Json.JsonSerializer.Serialize(battleRequest);

            // Act
            await _server.HandleBattleAsync(memoryStream, context, body);

            // Assert
            memoryStream.Position = 0;
            using var streamReader = new StreamReader(memoryStream);
            var response = await streamReader.ReadToEndAsync();
            
            StringAssert.Contains("HTTP/1.1 400 Bad Request", response);
            StringAssert.Contains("Both users must have at least 4 cards in their deck", response);
        }
    }
}