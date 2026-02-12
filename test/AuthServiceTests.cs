using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using BankingService.Data;
using BankingService.DTOs;
using BankingService.Models;
using BankingService.Services;

namespace BankingService.Tests;

public class AuthServiceTests : IDisposable
{
    private readonly BankingDbContext _context;
    private readonly AuthService _service;
    private readonly IConfiguration _configuration;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<BankingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new BankingDbContext(options);
        var logger = new Mock<ILogger<AuthService>>().Object;

        var configDict = new Dictionary<string, string>
        {
            {"Jwt:Key", "YourSuperSecretKeyThatIsAtLeast32CharactersLongForHS256"},
            {"Jwt:Issuer", "BankingService"},
            {"Jwt:Audience", "BankingServiceClient"}
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict!)
            .Build();

        _service = new AuthService(_context, _configuration, logger);
    }

    [Fact]
    public async Task SignUp_ValidRequest_ReturnsAuthResponse()
    {
        // Arrange
        var request = new SignUpRequest
        {
            Email = "newuser@example.com",
            Password = "SecurePass123",
            FirstName = "Jane",
            LastName = "Smith"
        };

        // Act
        var result = await _service.SignUpAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        Assert.Equal("newuser@example.com", result.User.Email);
    }

    [Fact]
    public async Task SignUp_DuplicateEmail_ThrowsException()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "existing@example.com",
            PasswordHash = "hash",
            FirstName = "John",
            LastName = "Doe"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new SignUpRequest
        {
            Email = "existing@example.com",
            Password = "SecurePass123",
            FirstName = "Jane",
            LastName = "Smith"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.SignUpAsync(request));
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var signUpRequest = new SignUpRequest
        {
            Email = "logintest@example.com",
            Password = "SecurePass123",
            FirstName = "Test",
            LastName = "User"
        };
        await _service.SignUpAsync(signUpRequest);

        var loginRequest = new LoginRequest
        {
            Email = "logintest@example.com",
            Password = "SecurePass123"
        };

        // Act
        var result = await _service.LoginAsync(loginRequest);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ThrowsException()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _service.LoginAsync(request));
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}