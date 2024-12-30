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
        Console.WriteLine($"Creating trade for CardId={trade.CardId}, UserId={trade.UserId}");
    
        // First verify the card exists and belongs to the user
        var card = connection.QuerySingleOrDefault<Card>(
            "SELECT * FROM cards WHERE id = @CardId AND user_id = @UserId",
            new { trade.CardId, trade.UserId });
        
        if (card == null)
        {
            throw new Exception("Card not found or doesn't belong to user");
        }

        var sql = @"
        INSERT INTO trades (card_id, user_id, required_type, minimum_damage) 
        VALUES (@CardId, @UserId, @RequiredType, @MinimumDamage)
        RETURNING id";
        
        try
        {
            var id = connection.QuerySingle<int>(sql, trade);
            trade.Id = id;
            Console.WriteLine($"Trade created successfully with ID: {id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating trade: {ex.Message}");
            throw;
        }
    }

    public bool ExecuteTrade(int tradeId, int offeredCardId, int newOwnerId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // Get trade info with explicit column mapping
            var trade = connection.QuerySingleOrDefault<Trade>(@"
                SELECT id, card_id as CardId, user_id as UserId, required_type as RequiredType, minimum_damage as MinimumDamage 
                FROM trades 
                WHERE id = @tradeId", 
                new { tradeId },
                transaction);

            Console.WriteLine($"Found trade: {trade?.Id}, CardId: {trade?.CardId}");

            if (trade == null)
            {
                Console.WriteLine("Trade not found");
                return false;
            }

            // Get both cards within the transaction
            var tradedCard = connection.QuerySingleOrDefault<Card>(@"
                SELECT * FROM cards WHERE id = @CardId", 
                new { CardId = trade.CardId },
                transaction);

            var offeredCard = connection.QuerySingleOrDefault<Card>(@"
                SELECT * FROM cards WHERE id = @CardId", 
                new { CardId = offeredCardId },
                transaction);

            Console.WriteLine($"Found cards - Traded: {tradedCard?.Id}, Offered: {offeredCard?.Id}");

            if (tradedCard == null || offeredCard == null)
            {
                Console.WriteLine("One or both cards not found");
                return false;
            }

            if (!ValidateTrade(trade, offeredCard))
            {
                Console.WriteLine("Trade validation failed");
                return false;
            }

            // Update card ownerships
            connection.Execute(@"
                UPDATE cards 
                SET user_id = @NewOwnerId 
                WHERE id = @CardId",
                new { NewOwnerId = newOwnerId, CardId = trade.CardId },
                transaction);

            connection.Execute(@"
                UPDATE cards 
                SET user_id = @NewOwnerId 
                WHERE id = @CardId",
                new { NewOwnerId = trade.UserId, CardId = offeredCardId },
                transaction);

            // Delete the trade
            connection.Execute(@"
                DELETE FROM trades 
                WHERE id = @TradeId",
                new { TradeId = tradeId },
                transaction);

            transaction.Commit();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Trade execution error: {ex.Message}");
            transaction.Rollback();
            return false;
        }
    }

    public IEnumerable<Trade> GetAllTrades()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return connection.Query<Trade>("SELECT * FROM trades ORDER BY id");
    }

    public Trade GetTradeById(int tradeId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var trade = connection.QuerySingleOrDefault<Trade>(@"
            SELECT id, card_id as CardId, user_id as UserId, required_type as RequiredType, minimum_damage as MinimumDamage 
            FROM trades 
            WHERE id = @tradeId", 
            new { tradeId });
            
        Console.WriteLine($"Retrieved trade: {trade?.Id}, CardId: {trade?.CardId}");
        return trade;
    }

    public void DeleteTrade(int tradeId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Execute("DELETE FROM trades WHERE id = @tradeId", new { tradeId });
    }

    private bool ValidateTrade(Trade trade, Card offeredCard)
    {
        Console.WriteLine("\nTrade Validation Details:");
        Console.WriteLine($"Trade object: {(trade == null ? "null" : "exists")}");
        Console.WriteLine($"Offered card object: {(offeredCard == null ? "null" : "exists")}");

        if (trade == null || offeredCard == null)
        {
            Console.WriteLine("Trade or offered card is null");
            return false;
        }

        Console.WriteLine($"Trade ID: {trade.Id}");
        Console.WriteLine($"Trade Required Type: '{trade.RequiredType}'");
        Console.WriteLine($"Offered Card ID: {offeredCard.Id}");
        Console.WriteLine($"Offered Card Type: '{offeredCard.CardType}'");
        Console.WriteLine($"Minimum Required Damage: {trade.MinimumDamage}");
        Console.WriteLine($"Offered Card Damage: {offeredCard.Damage}");

        var typeMatches = string.Equals(offeredCard.CardType?.Trim(), trade.RequiredType?.Trim(), StringComparison.OrdinalIgnoreCase);
        var damageOk = offeredCard.Damage >= trade.MinimumDamage;

        Console.WriteLine($"Type match: {typeMatches}");
        Console.WriteLine($"Damage requirement met: {damageOk}");

        return typeMatches && damageOk;
    }
}