using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Text.Json;
using NUnit.Framework;

namespace MCTG.Tests
{
    [TestFixture]
    public class HandleGetCardsAsyncTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<ICardRepository> _cardRepositoryMock;
        private readonly TcpServer _server;

        public HandleGetCardsAsyncTests()
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

            var context = new DefaultHttpContext();
            context.Items["Username"] = "testuser";

            using var memoryStream = new MemoryStream();

            // Act
            await _server.HandleGetCardsAsync(memoryStream, context);

            // Assert
            _userRepositoryMock.Verify(repo => repo.GetByUsername("testuser"), Times.Exactly(2));
            _cardRepositoryMock.Verify(repo => repo.GetUserCards(user.Id), Times.Once);

            memoryStream.Position = 0;
            using var streamReader = new StreamReader(memoryStream);
            var response = await streamReader.ReadToEndAsync();
    
            StringAssert.Contains("HTTP/1.1 200 OK", response);
            StringAssert.Contains("Content-Type: application/json", response);

            // Deserialize the response body
            var responseBody = JsonSerializer.Deserialize<List<Card>>(response.Split("\r\n\r\n")[1]);

            // Assert the properties of the response body
            Assert.AreEqual(2, responseBody.Count);
            Assert.AreEqual(1, responseBody[0].Id);
            Assert.AreEqual(2, responseBody[1].Id);
        }

        [Test]
        public async Task UserNotFound_ReturnsNotFound()
        {
            // Arrange
            _userRepositoryMock.Setup(repo => repo.GetByUsername("testuser")).Returns((User)null);

            var context = new DefaultHttpContext();
            context.Items["Username"] = "testuser";

            using var memoryStream = new MemoryStream();

            // Act
            await _server.HandleGetCardsAsync(memoryStream, context);

            // Assert
            memoryStream.Position = 0;
            using var streamReader = new StreamReader(memoryStream);
            var response = await streamReader.ReadToEndAsync();
            
            StringAssert.Contains("HTTP/1.1 404 Not Found", response);
            StringAssert.Contains("User not found", response);
        }

        [Test]
        public async Task AuthenticationRequired_ReturnsUnauthorized()
        {
            // Arrange
            var context = new DefaultHttpContext();

            using var memoryStream = new MemoryStream();

            // Act
            await _server.HandleGetCardsAsync(memoryStream, context);

            // Assert
            memoryStream.Position = 0;
            using var streamReader = new StreamReader(memoryStream);
            var response = await streamReader.ReadToEndAsync();
            
            StringAssert.Contains("HTTP/1.1 401 Unauthorized", response);
            StringAssert.Contains("Authentication required", response);
        }
    }
}