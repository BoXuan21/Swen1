using Npgsql;
namespace MCTG;

public class PackageRepository : IPackageRepository
{
    private readonly string _connectionString;
    private readonly ICardRepository _cardRepository;

    public PackageRepository(string connectionString, ICardRepository cardRepository)
    {
        _connectionString = connectionString;
        _cardRepository = cardRepository;
    }

   public void CreatePackage(List<Card> cards)
{
    using var connection = new NpgsqlConnection(_connectionString);
    connection.Open();
    using var transaction = connection.BeginTransaction();

    try
    {
        Console.WriteLine("Creating new package...");
        
        // Get admin user id
        using var adminCmd = new NpgsqlCommand(
            "SELECT id FROM users WHERE username = 'admin'", 
            connection, 
            transaction);
        var adminId = (int)adminCmd.ExecuteScalar();
        
        // Create package
        using var packageCmd = new NpgsqlCommand(
            "INSERT INTO packages (is_sold) VALUES (false) RETURNING id", 
            connection, 
            transaction);
        var packageId = (int)packageCmd.ExecuteScalar();
        Console.WriteLine($"Created package with ID: {packageId}");

        // Add cards to package
        using var cardCmd = new NpgsqlCommand(
            "INSERT INTO package_cards (package_id, card_id) VALUES (@PackageId, @CardId)",
            connection,
            transaction);
        cardCmd.Parameters.AddWithValue("@PackageId", packageId);
        var cardIdParam = cardCmd.Parameters.Add("@CardId", NpgsqlTypes.NpgsqlDbType.Integer);

        foreach (var card in cards)
        {
            DetermineCardTypes(card);
            Console.WriteLine($"Adding card {card.Name} to package {packageId}");
            
            // Generate a new card_id for each card
            var cardId = _cardRepository.AddCard(card, adminId);
            
            // Check if the (package_id, card_id) combination already exists
            using var existsCmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM package_cards WHERE package_id = @PackageId AND card_id = @CardId",
                connection,
                transaction);
            existsCmd.Parameters.AddWithValue("@PackageId", packageId);
            existsCmd.Parameters.AddWithValue("@CardId", cardId);
            var exists = (long)existsCmd.ExecuteScalar() > 0;
            
            if (!exists)
            {
                // Insert the card into the package_cards table
                cardIdParam.Value = cardId;
                cardCmd.ExecuteNonQuery();
            }
            else
            {
                Console.WriteLine($"Card {cardId} already exists in package {packageId}. Skipping insertion.");
            }
        }

        transaction.Commit();
        Console.WriteLine("Package creation completed successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error creating package: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        transaction.Rollback();
        throw new Exception("Failed to create package", ex);
    }
}

    private void DetermineCardTypes(Card card)
    {
        // Determine element type
        if (card.Name.Contains("Water"))
            card.ElementType = ElementType.Water;
        else if (card.Name.Contains("Fire"))
            card.ElementType = ElementType.Fire;
        else if (card.Name.Contains("Regular"))
            card.ElementType = ElementType.Regular;
        else
            card.ElementType = ElementType.Normal;

        // Determine card type
        if (card.Name.Contains("Spell"))
            card.CardType = "Spell";
        else if (card.Name.Contains("Goblin"))
            card.CardType = "Goblin";
        else if (card.Name.Contains("Dragon"))
            card.CardType = "Dragon";
        else if (card.Name.Contains("Wizard"))
            card.CardType = "Wizard";
        else if (card.Name.Contains("Ork"))
            card.CardType = "Ork";
        else if (card.Name.Contains("Knight"))
            card.CardType = "Knight";
        else if (card.Name.Contains("Kraken"))
            card.CardType = "Kraken";
        else if (card.Name.Contains("Elf"))
            card.CardType = "Elf";
        else
            card.CardType = "Monster";
    }

    public Package? GetAvailablePackage()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
    
        try
        {
            // Get first available package
            using var packageCmd = new NpgsqlCommand(
                "SELECT id FROM packages WHERE is_sold = false ORDER BY id LIMIT 1",
                connection);
        
            var packageId = packageCmd.ExecuteScalar() as int?;
            if (!packageId.HasValue)
                return null;

            // Get cards for package
            using var cardsCmd = new NpgsqlCommand(@"
            SELECT c.id, c.name, c.damage, c.element_type, c.card_type, c.user_id 
            FROM cards c
            JOIN package_cards pc ON pc.card_id = c.id
            WHERE pc.package_id = @PackageId",
                connection);
            cardsCmd.Parameters.AddWithValue("@PackageId", packageId.Value);

            var cards = new List<Card>();
            using var reader = cardsCmd.ExecuteReader();
            while (reader.Read())
            {
                cards.Add(new Card
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Damage = reader.GetInt32(reader.GetOrdinal("damage")),
                    ElementType = (ElementType)Enum.Parse(typeof(ElementType), reader.GetString(reader.GetOrdinal("element_type"))),
                    CardType = reader.GetString(reader.GetOrdinal("card_type")),
                    UserId = !reader.IsDBNull(reader.GetOrdinal("user_id")) ? 
                        reader.GetInt32(reader.GetOrdinal("user_id")) : null
                });
            }

            return new Package
            {
                Id = packageId.Value,
                Cards = cards,
                IsSold = false
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting available package: {ex.Message}");
            throw;
        }
    }

    public void MarkPackageAsSold(int packageId, int userId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // Update package status
            using var packageCmd = new NpgsqlCommand(@"
                UPDATE packages
                SET is_sold = true,
                    bought_by_user_id = @UserId,
                    purchase_date = CURRENT_TIMESTAMP
                WHERE id = @PackageId",
                connection, 
                transaction);
            
            packageCmd.Parameters.AddWithValue("@PackageId", packageId);
            packageCmd.Parameters.AddWithValue("@UserId", userId);
            packageCmd.ExecuteNonQuery();

            // Update card ownership
            using var cardsCmd = new NpgsqlCommand(@"
                UPDATE cards
                SET user_id = @UserId
                WHERE id IN (
                    SELECT card_id
                    FROM package_cards
                    WHERE package_id = @PackageId
                )",
                connection,
                transaction);
            
            cardsCmd.Parameters.AddWithValue("@PackageId", packageId);
            cardsCmd.Parameters.AddWithValue("@UserId", userId);
            cardsCmd.ExecuteNonQuery();

            transaction.Commit();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error marking package as sold: {ex.Message}");
            transaction.Rollback();
            throw;
        }
    }
}