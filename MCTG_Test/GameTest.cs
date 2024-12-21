using System;
using Xunit;
using Moq;

namespace MCTG
{
    namespace MCTG_Test
    {
        public class GameTests
        {
            private readonly Game game;
            private readonly Mock<UserController> mockUserController;
            private readonly Mock<User> mockUser1;
            private readonly Mock<User> mockUser2;
            private readonly Mock<Deck> mockDeck1;
            private readonly Mock<Deck> mockDeck2;

            public GameTests()
            {
                // Mock all dependencies
                mockUserController = new Mock<UserController>();
                mockUser1 = new Mock<User>();
                mockUser2 = new Mock<User>();
                mockDeck1 = new Mock<Deck>(mockUser1.Object.Stack);
                mockDeck2 = new Mock<Deck>(mockUser2.Object.Stack);

                // Set up Game with mocked UserController
                game = new Game();
            }

            [Fact]
            public void CreateGame_ShouldInitializeUsersAndDecks()
            {
                // Arrange
                mockUser1.Setup(u => u.Stack.AddRandomCards(30));
                mockUser2.Setup(u => u.Stack.AddRandomCards(30));

                // Act
                game.CreateGame();

                //Assert
                mockUser1.Verify(u => u.Stack.AddRandomCards(30), Times.Once);
                mockUser2.Verify(u => u.Stack.AddRandomCards(30), Times.Once);

                Xunit.Assert.Equal(30, mockDeck1.Object.GetCurrentCardCount());
                Xunit.Assert.Equal(30, mockDeck2.Object.GetCurrentCardCount());
            }

            [Fact]
            public void StartGame_ShouldPlayRoundsUntilWinner()
            {
                // Arrange
                var user1Wins = 0;
                var user2Wins = 0;

                mockDeck1.Setup(d => d.GetCurrentCardCount()).Returns(0); // Simulate user 1 losing
                mockDeck2.Setup(d => d.GetCurrentCardCount()).Returns(5);

                // Act
                game.StartGame();

                // Assert
                Xunit.Assert.True(user2Wins > user1Wins); // User 2 should win when User 1's deck is empty
            }

            [Fact]
            public void BuyCards_ShouldDeductCoinsAndAddCards()
            {
                // Arrange
                mockUser1.Setup(u => u.BuyPackage()).Verifiable();
                mockUser2.Setup(u => u.BuyPackage()).Verifiable();

                Console.SetIn(new System.IO.StringReader("1")); // Simulate user input for deck 1

                // Act
                game.BuyCards();

                // Assert
                mockUser1.Verify(u => u.BuyPackage(), Times.Once);
                mockUser2.Verify(u => u.BuyPackage(), Times.Never);
            }

            [Fact]
            public void StartScreen_ShouldExitOnInvalidLogin()
            {
                // Arrange
                mockUserController.Setup(uc => uc.Login()).Returns(false);

                // Capture console output
                var consoleOutput = new System.IO.StringWriter();
                Console.SetOut(consoleOutput);

                // Act
                game.StartScreen();

                // Assert
                var output = consoleOutput.ToString();
                Xunit.Assert.Contains("Login failed. Exiting game...", output);
            }
        }
    }
}