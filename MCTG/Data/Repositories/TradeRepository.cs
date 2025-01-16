using Npgsql;
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
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            Console.WriteLine($"Creating trade for CardId={trade.CardId}, UserId={trade.UserId}");
    
            // First verify the card exists and belongs to the user
            using var verifyCmd = new NpgsqlCommand(
                "SELECT id FROM cards WHERE id = @CardId AND user_id = @UserId",
                connection,
                transaction);

            verifyCmd.Parameters.AddWithValue("@CardId", trade.CardId);
            verifyCmd.Parameters.AddWithValue("@UserId", trade.UserId);

            var cardExists = verifyCmd.ExecuteScalar();
            if (cardExists == null)
            {
                throw new Exception("Card not found or doesn't belong to user");
            }

            using var createCmd = new NpgsqlCommand(@"
                INSERT INTO trades (card_id, user_id, required_type, minimum_damage) 
                VALUES (@CardId, @UserId, @RequiredType, @MinimumDamage)
                RETURNING id",
                connection,
                transaction);

            createCmd.Parameters.AddWithValue("@CardId", trade.CardId);
            createCmd.Parameters.AddWithValue("@UserId", trade.UserId);
            createCmd.Parameters.AddWithValue("@RequiredType", trade.RequiredType);
            createCmd.Parameters.AddWithValue("@MinimumDamage", trade.MinimumDamage);

            var id = (int)createCmd.ExecuteScalar();
            trade.Id = id;
            Console.WriteLine($"Trade created successfully with ID: {id}");

            transaction.Commit();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
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
        var trade = GetTradeById(tradeId);
        Console.WriteLine($"Found trade: {trade?.Id}, CardId: {trade?.CardId}");

        if (trade == null)
        {
            Console.WriteLine("Trade not found");
            return false;
        }

        // Get traded card
        using var tradedCardCmd = new NpgsqlCommand(@"
            SELECT id, name, damage, element_type, card_type, user_id
            FROM cards 
            WHERE id = @CardId",
            connection, 
            transaction);
        tradedCardCmd.Parameters.AddWithValue("@CardId", trade.CardId);
        
        Card tradedCard = null;
        using (var reader = tradedCardCmd.ExecuteReader())
        {
            if (reader.Read())
            {
                Console.WriteLine("Reading traded card details:");
                Console.WriteLine($"ElementType column type: {reader.GetDataTypeName(reader.GetOrdinal("element_type"))}");
                
                tradedCard = new Card
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Damage = reader.GetInt32(reader.GetOrdinal("damage")),
                    ElementType = reader.IsDBNull(reader.GetOrdinal("element_type")) ? 
                        ElementType.Normal : 
                        Enum.Parse<ElementType>(reader.GetString(reader.GetOrdinal("element_type"))),
                    CardType = reader.GetString(reader.GetOrdinal("card_type")),
                    UserId = reader.GetInt32(reader.GetOrdinal("user_id"))
                };
            }
        }

        // Get offered card
        using var offeredCardCmd = new NpgsqlCommand(@"
            SELECT id, name, damage, element_type, card_type, user_id
            FROM cards 
            WHERE id = @CardId",
            connection, 
            transaction);
        offeredCardCmd.Parameters.AddWithValue("@CardId", offeredCardId);
        
        Card offeredCard = null;
        using (var reader = offeredCardCmd.ExecuteReader())
        {
            if (reader.Read())
            {
                Console.WriteLine("Reading offered card details:");
                Console.WriteLine($"ElementType column type: {reader.GetDataTypeName(reader.GetOrdinal("element_type"))}");
                
                offeredCard = new Card
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Damage = reader.GetInt32(reader.GetOrdinal("damage")),
                    ElementType = reader.IsDBNull(reader.GetOrdinal("element_type")) ? 
                        ElementType.Normal : 
                        Enum.Parse<ElementType>(reader.GetString(reader.GetOrdinal("element_type"))),
                    CardType = reader.GetString(reader.GetOrdinal("card_type")),
                    UserId = reader.GetInt32(reader.GetOrdinal("user_id"))
                };
            }
        }

        if (tradedCard == null || offeredCard == null)
        {
            Console.WriteLine("One or both cards not found");
            return false;
        }

        Console.WriteLine($"Found cards - Traded: {tradedCard.Id} ({tradedCard.CardType}), " +
                       $"Offered: {offeredCard.Id} ({offeredCard.CardType})");

        if (!ValidateTrade(trade, offeredCard))
        {
            Console.WriteLine("Trade validation failed");
            return false;
        }

        // Update card ownerships
        using var updateTradedCardCmd = new NpgsqlCommand(
            "UPDATE cards SET user_id = @NewOwnerId WHERE id = @CardId",
            connection,
            transaction);
        updateTradedCardCmd.Parameters.AddWithValue("@NewOwnerId", newOwnerId);
        updateTradedCardCmd.Parameters.AddWithValue("@CardId", trade.CardId);
        updateTradedCardCmd.ExecuteNonQuery();

        using var updateOfferedCardCmd = new NpgsqlCommand(
            "UPDATE cards SET user_id = @NewOwnerId WHERE id = @CardId",
            connection,
            transaction);
        updateOfferedCardCmd.Parameters.AddWithValue("@NewOwnerId", trade.UserId);
        updateOfferedCardCmd.Parameters.AddWithValue("@CardId", offeredCardId);
        updateOfferedCardCmd.ExecuteNonQuery();

        // Delete the trade
        using var deleteTradeCmd = new NpgsqlCommand(
            "DELETE FROM trades WHERE id = @TradeId",
            connection,
            transaction);
        deleteTradeCmd.Parameters.AddWithValue("@TradeId", tradeId);
        deleteTradeCmd.ExecuteNonQuery();

        transaction.Commit();
        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Trade execution error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        transaction.Rollback();
        return false;
    }
}

    public IEnumerable<Trade> GetAllTrades()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        using var cmd = new NpgsqlCommand(@"
        SELECT id, card_id, user_id, required_type, minimum_damage
        FROM trades 
        ORDER BY id",
            connection);

        var trades = new List<Trade>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            trades.Add(new Trade
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                CardId = reader.GetInt32(reader.GetOrdinal("card_id")),
                UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                RequiredType = reader.GetString(reader.GetOrdinal("required_type")),  // Changed from GetInt32 to GetString
                MinimumDamage = reader.GetInt32(reader.GetOrdinal("minimum_damage"))
            });
        }

        Console.WriteLine($"Retrieved {trades.Count} trades");
        foreach (var trade in trades)
        {
            Console.WriteLine($"Trade: Id={trade.Id}, CardId={trade.CardId}, RequiredType={trade.RequiredType}");
        }
        return trades;
    }

    public Trade GetTradeById(int tradeId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        using var cmd = new NpgsqlCommand(@"
        SELECT id, card_id, user_id, required_type, minimum_damage
        FROM trades 
        WHERE id = @TradeId",
            connection);
        cmd.Parameters.AddWithValue("@TradeId", tradeId);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            var trade = new Trade
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                CardId = reader.GetInt32(reader.GetOrdinal("card_id")),
                UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                RequiredType = reader.GetString(reader.GetOrdinal("required_type")),  // Changed from GetInt32 to GetString
                MinimumDamage = reader.GetInt32(reader.GetOrdinal("minimum_damage"))
            };
            Console.WriteLine($"Retrieved trade: {trade.Id}, CardId: {trade.CardId}, RequiredType: {trade.RequiredType}");
            return trade;
        }

        return null;
    }

    public void DeleteTrade(int tradeId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        using var cmd = new NpgsqlCommand(
            "DELETE FROM trades WHERE id = @TradeId",
            connection);
        cmd.Parameters.AddWithValue("@TradeId", tradeId);
        cmd.ExecuteNonQuery();
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

        // Check if the offered card matches the required type
        bool typeMatches = false;
        if (string.Equals(trade.RequiredType, "monster", StringComparison.OrdinalIgnoreCase))
        {
            // Monster
            var monsterTypes = new[] { "Goblin", "Dragon", "Wizard", "Ork", "Knight", "Kraken", "FireElves" };
            typeMatches = monsterTypes.Contains(offeredCard.CardType, StringComparer.OrdinalIgnoreCase);
        }
        else if (string.Equals(trade.RequiredType, "spell", StringComparison.OrdinalIgnoreCase))
        {
            //spell
            typeMatches = offeredCard.CardType.EndsWith("Spell", StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            typeMatches = string.Equals(offeredCard.CardType, trade.RequiredType, StringComparison.OrdinalIgnoreCase);
        }

        var damageOk = offeredCard.Damage >= trade.MinimumDamage;

        Console.WriteLine($"Type match: {typeMatches}");
        Console.WriteLine($"Damage requirement met: {damageOk}");

        return typeMatches && damageOk;
    }
}