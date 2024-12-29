namespace MCTG;
using Npgsql;
using Dapper;

public class UserRepositoryTests
{
    private readonly string _connectionString = "Host=localhost;Database=MCTG;Username=postgres;Password=postgres;";
    private IUserRepository _userRepository;

    [SetUp]
    public void Setup()
    {
        _userRepository = new UserRepository(_connectionString);
    }

    [Test]
    public void Add_ShouldCreateNewUser()
    {
        // Arrange
        var user = new User { Username = "testuser1", Password = "test123" };

        // Act
        _userRepository.Add(user);
        var retrievedUser = _userRepository.GetByUsername("testuser1");

        // Assert
        Assert.That(retrievedUser, Is.Not.Null);
        Assert.That(retrievedUser.Username, Is.EqualTo("testuser1"));
    }

    [Test]
    public void ValidateCredentials_ShouldReturnTrueForValidCredentials()
    {
        // Arrange
        var user = new User { Username = "testuser2", Password = "test123" };
        _userRepository.Add(user);

        // Act
        var isValid = _userRepository.ValidateCredentials("testuser2", "test123");

        // Assert
        Assert.That(isValid, Is.True);
    }

    [Test]
    public void ValidateCredentials_ShouldReturnFalseForInvalidCredentials()
    {
        // Arrange
        var user = new User { Username = "testuser3", Password = "test123" };
        _userRepository.Add(user);

        // Act
        var isValid = _userRepository.ValidateCredentials("testuser3", "wrongpassword");

        // Assert
        Assert.That(isValid, Is.False);
    }

    [Test]
    public void GenerateToken_ShouldCreateValidToken()
    {
        // Arrange
        var username = "testuser4";

        // Act
        var token = _userRepository.GenerateToken(username);

        // Assert
        Assert.That(token, Is.EqualTo($"{username}-mtcgToken"));
    }

    [TearDown]
    public void Cleanup()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Execute("DELETE FROM users WHERE username LIKE 'testuser%'");
    }
}