using Npgsql;
using Dapper;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace MCTG;

public class UserRepositoryTests
{
    private readonly string _connectionString = "Host=localhost;Database=MCTG;Username=postgres;Password=postgres;";
    private readonly string _jwtSecretKey = "your-secret-key-at-least-16-chars";
    private IUserRepository _userRepository;
    private JwtService _jwtService;

    [SetUp]
    public void Setup()
    {
        _userRepository = new UserRepository(_connectionString);
        _jwtService = new JwtService(_jwtSecretKey);
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
    
    [TearDown]
    public void Cleanup()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Execute("DELETE FROM users WHERE username LIKE 'testuser%'");
    }
}