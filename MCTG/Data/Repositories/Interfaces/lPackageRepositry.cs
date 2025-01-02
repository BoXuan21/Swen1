namespace MCTG;

public interface IPackageRepository
{
    void CreatePackage(List<Card> cards);
    Package GetAvailablePackage();
    void MarkPackageAsSold(int packageId, int userId);
    IEnumerable<Package> GetUserPurchaseHistory(int userId);
}