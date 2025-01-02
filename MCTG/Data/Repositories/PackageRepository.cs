using Npgsql;
using Dapper;
namespace MCTG;

public class Package
{
    public int Id { get; set; }
    public List<Card> Cards { get; set; }
    public bool IsSold { get; set; }
    public int? BoughtByUserId { get; set; }
    public DateTime? PurchaseDate { get; set; }
}

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
        using var transaction = connection.BeginTransaction();

        try
        {
            // Create package
            var packageId = connection.QuerySingle<int>(
                "INSERT INTO packages DEFAULT VALUES RETURNING id");

            // Add cards to package
            foreach (var card in cards)
            {
                var cardId = _cardRepository.AddCard(card, 0); // null userId means card is in package
                connection.Execute(
                    "INSERT INTO package_cards (package_id, card_id) VALUES (@PackageId, @CardId)",
                    new { PackageId = packageId, CardId = cardId });
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public Package GetAvailablePackage()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var packageId = connection.QuerySingleOrDefault<int?>(
            "SELECT id FROM packages WHERE is_sold = false LIMIT 1");

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

    public void MarkPackageAsSold(int packageId, int userId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Execute(@"
            UPDATE packages 
            SET is_sold = true, 
                bought_by_user_id = @UserId,
                purchase_date = CURRENT_TIMESTAMP
            WHERE id = @PackageId",
            new { PackageId = packageId, UserId = userId });
    }

    public IEnumerable<Package> GetUserPurchaseHistory(int userId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return connection.Query<Package>(@"
            SELECT * FROM packages
            WHERE bought_by_user_id = @UserId
            ORDER BY purchase_date DESC",
            new { UserId = userId });
    }
}