using Npgsql;
using Dapper;
namespace MCTG;

public class TradeRepository : ITradeRepository
{
    private readonly string _connectionString;
    private readonly ICardRepository _cardRepository;

    public TradeRepository(string connectionString, ICardRepository cardRepository)
    {
        _connectionString = connectionString;
        _cardRepository = cardRepository;
    }

    public void CreateTrade(Trade trade)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Execute(@"
            INSERT INTO trades (card_id, user_id, required_type, minimum_damage) 
            VALUES (@CardId, @UserId, @RequiredType, @MinimumDamage)",
            trade);
    }

    public IEnumerable<Trade> GetAllTrades()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return connection.Query<Trade>("SELECT * FROM trades");
    }

    public Trade GetTradeById(int tradeId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return connection.QuerySingleOrDefault<Trade>(
            "SELECT * FROM trades WHERE id = @tradeId",
            new { tradeId });
    }

    public void DeleteTrade(int tradeId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Execute("DELETE FROM trades WHERE id = @tradeId", new { tradeId });
    }

    public bool ExecuteTrade(int tradeId, int offeredCardId, int newOwnerId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var trade = GetTradeById(tradeId);
            var offeredCard = _cardRepository.GetCard(offeredCardId);

            // Validate trade requirements
            if (!ValidateTrade(trade, offeredCard))
            {
                return false;
            }

            // Update card ownerships
            connection.Execute(
                "UPDATE cards SET user_id = @NewOwnerId WHERE id = @CardId",
                new { NewOwnerId = newOwnerId, CardId = trade.CardId });

            connection.Execute(
                "UPDATE cards SET user_id = @NewOwnerId WHERE id = @CardId",
                new { NewOwnerId = trade.UserId, CardId = offeredCardId });

            // Delete the trade
            DeleteTrade(tradeId);

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            return false;
        }
    }

    private bool ValidateTrade(Trade trade, Card offeredCard)
    {
        return offeredCard.CardType == trade.RequiredType && 
               offeredCard.Damage >= trade.MinimumDamage;
    }
}