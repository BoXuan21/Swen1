using Npgsql;
using Dapper;
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
        
            // Get admin user id (or create a system user if needed)
            var adminId = connection.QuerySingle<int>("SELECT id FROM users WHERE username = 'admin'");
        
            // Create package
            var packageId = connection.QuerySingle<int>(
                "INSERT INTO packages (is_sold) VALUES (false) RETURNING id");
            Console.WriteLine($"Created package with ID: {packageId}");

            // Add cards to package
            foreach (var card in cards)
            {
                DetermineCardTypes(card);
                Console.WriteLine($"Adding card {card.Name} to package {packageId}");
                var cardId = _cardRepository.AddCard(card, adminId);  // Use admin ID instead of null
                connection.Execute(
                    "INSERT INTO package_cards (package_id, card_id) VALUES (@PackageId, @CardId)",
                    new { PackageId = packageId, CardId = cardId },
                    transaction);
            }

            transaction.Commit();
            Console.WriteLine("Package creation completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating package: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            transaction.Rollback();
            throw;
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
            var packageId = connection.QuerySingleOrDefault<int?>(
                "SELECT id FROM packages WHERE is_sold = false ORDER BY id LIMIT 1");

            if (!packageId.HasValue)
                return null;

            var cards = connection.Query<Card>(@"
                SELECT c.* 
                FROM cards c
                JOIN package_cards pc ON pc.card_id = c.id
                WHERE pc.package_id = @PackageId",
                new { PackageId = packageId });

            return new Package
            {
                Id = packageId.Value,
                Cards = cards.ToList(),
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
            connection.Execute(@"
            UPDATE packages
            SET is_sold = true,
                bought_by_user_id = @UserId,
                purchase_date = CURRENT_TIMESTAMP
            WHERE id = @PackageId",
                new { PackageId = packageId, UserId = userId },
                transaction);

            // Update cards table with existing user ID from package_cards
            connection.Execute(@"
            UPDATE cards
            SET user_id = @UserId
            WHERE id IN (
                SELECT card_id
                FROM package_cards
                WHERE package_id = @PackageId
            )",
                new { PackageId = packageId, UserId = userId },
                transaction);

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