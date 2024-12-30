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
    public void GenerateToken_ShouldCreateValidToken()
    {
        // Arrange
        var username = "testuser4";

        // Act
        var token = _jwtService.GenerateToken(username);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSecretKey);
        tokenHandler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        }, out SecurityToken validatedToken);

        var jwtToken = (JwtSecurityToken)validatedToken;
        Assert.That(jwtToken.Claims.First(x => x.Type == "username").Value, Is.EqualTo(username));
    }
    
    [TearDown]
    public void Cleanup()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Execute("DELETE FROM users WHERE username LIKE 'testuser%'");
    }
}