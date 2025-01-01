namespace MCTG;

public interface ITradeRepository
{
    void CreateTrade(Trade trade);
    IEnumerable<Trade> GetAllTrades();
    Trade GetTradeById(int tradeId);
    void DeleteTrade(int tradeId);
    bool ExecuteTrade(int tradeId, int offeredCardId, int newOwnerId);
    
}