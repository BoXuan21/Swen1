namespace MCTG_IF23B049{
using Xunit;

    public class UserTest
    {
        /// Testet, ob der Kauf eines Pakets die Anzahl der Münzen reduziert und Karten zum Stapel hinzufügt.
        [Fact]
        public void BuyPackage_ShouldDecreaseCoinAndAddCardsToStack()
        {
            // Arrange
            User user = new User();

            // Act
            user.BuyPackage();

            // Assert
            Assert.Equal(15, user.Coin); // Annahme: Paket kostet 5 Münzen und der Startwert ist 20
            Assert.Equal(5, user.Stack.Cards.Count); // Paket fügt 5 Karten zum Stapel hinzu
        }

        /// Testet, ob das Hinzufügen von Karten zum Deck diese vom Stack ins Deck verschiebt.
        [Fact]
        public void AddCardsToDeck_ShouldMoveCardsFromStackToDeck()
        {
            // Arrange
            User user = new User();
            user.Stack.AddRandomCards(5);

            // Act
            user.AddCardsToDeck();

            // Assert
            Assert.Equal(5, user.Deck.Count); // Alle 5 Karten sollten ins Deck verschoben worden sein
            Assert.Empty(user.Stack.Cards); // Der Stapel sollte keine Karten mehr enthalten
        }
        
        /// Testet Deck verfügt nicht mehr als 10 Karten.
        [Fact]
        public void AddCardsToDeck_ShouldNotExceedDeckLimit()
        {
            // Arrange
            User user = new User();

            // Simuliert 10 Karten im Deck
            user.Deck.AddRange(new List<Card>(new Card[10]));

            user.Stack.AddRandomCards(5); // 5 Karten zum Stapel hinzufügen

            // Act
            user.AddCardsToDeck();

            // Assert
            Assert.Equal(10, user.Deck.Count); // Deck sollte immer noch 10 Karten enthalten
            Assert.Equal(5, user.Stack.Cards.Count); // Stapel sollte immer noch 5 Karten enthalten, da Deck voll ist
        }

        /// Testet ob ein Benutzer ein Paket kaufen kann wenn er nicht genug Münzen hat.
        [Fact]
        public void BuyPackage_ShouldNotBuyIfInsufficientCoins()
        {
            // Arrange
            User user = new User();
            user.Coin = 4; // Weniger als die Kosten des Pakets

            // Act
            user.BuyPackage();

            // Assert
            Assert.Equal(4, user.Coin); // Münzen sollten unverändert bleiben
            Assert.Empty(user.Stack.Cards); // Keine Karten sollten zum Stapel hinzugefügt werden
        }
    }
}