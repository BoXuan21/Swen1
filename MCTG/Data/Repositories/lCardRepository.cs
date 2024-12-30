// ICardRepository.cs
namespace MCTG
{
    public interface ICardRepository
    {
        int AddCard(Card card, int userId);
        IEnumerable<Card> GetUserCards(int userId);
        void AddPackage(int userId);
        Card GetCard(int cardId);
        IEnumerable<Card> GetUserDeck(int userId);
        void UpdateDeck(int userId, List<int> cardIds);
    }
}