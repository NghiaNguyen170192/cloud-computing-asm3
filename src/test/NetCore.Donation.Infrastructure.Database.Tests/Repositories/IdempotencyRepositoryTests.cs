using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetCore.Donation.Domain.Entities;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Infrastructure.Database.Repositories;

namespace NetCore.Donation.Infrastructure.Database.Tests.Repositories;

[TestClass]
public class IdempotencyRepositoryTests
{
    private ApplicationDatabaseContext? dbContext;

    [TestInitialize]
    public async Task TestInitialize()
    {
        var options = new DbContextOptionsBuilder<ApplicationDatabaseContext>()
            .UseInMemoryDatabase(databaseName: $"IdempotencyRepositoryTests_{Guid.NewGuid()}")
            .Options;

        dbContext = new ApplicationDatabaseContext(options);
        await dbContext.Database.EnsureCreatedAsync();
    }

    [TestCleanup]
    public async Task TestCleanup()
    {
        if (dbContext != null)
        {
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task AddAsync_AddsIdempotencyLog()
    {
        // Arrange
        var repository = new IdempotencyRepository(dbContext!);
        var idempotencyLog = new IdempotencyLog
        {
            Id = Guid.NewGuid(),
            CorrelationId = "test-correlation-id",
            RequestType = "CreateCountriesCommand",
            HttpMethod = "POST",
            RequestPath = "/api/v1/countries",
            ResponseData = @"{""ids"":[""id1"",""id2""]}",
            ResponseStatusCode = 201,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(1440),
            IsExpired = false,
        };

        // Act
        await repository.AddAsync(idempotencyLog);
        await repository.SaveAsync();

        // Assert
        var stored = await dbContext!.IdempotencyLogs.FirstOrDefaultAsync(
            x => x.CorrelationId == "test-correlation-id");
        Assert.IsNotNull(stored);
        Assert.AreEqual("CreateCountriesCommand", stored.RequestType);
    }

    [TestMethod]
    public async Task GetByCorrelationIdAsync_ReturnsLog_WhenExists()
    {
        // Arrange
        var repository = new IdempotencyRepository(dbContext!);
        var idempotencyLog = new IdempotencyLog
        {
            Id = Guid.NewGuid(),
            CorrelationId = "test-correlation-id-123",
            RequestType = "CreateCountriesCommand",
            HttpMethod = "POST",
            RequestPath = "/api/v1/countries",
            ResponseData = @"{""ids"":[""id1""]}",
            ResponseStatusCode = 201,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(1440),
            IsExpired = false,
        };

        await repository.AddAsync(idempotencyLog);
        await repository.SaveAsync();

        // Act
        var result = await repository.GetByCorrelationIdAsync("test-correlation-id-123", "CreateCountriesCommand");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("test-correlation-id-123", result.CorrelationId);
        Assert.AreEqual("CreateCountriesCommand", result.RequestType);
    }

    [TestMethod]
    public async Task GetByCorrelationIdAsync_ReturnsNull_WhenNotExists()
    {
        // Arrange
        var repository = new IdempotencyRepository(dbContext!);

        // Act
        var result = await repository.GetByCorrelationIdAsync("non-existent", "CreateCountriesCommand");

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetByCorrelationIdAsync_ReturnsNull_WhenExpired()
    {
        // Arrange
        var repository = new IdempotencyRepository(dbContext!);
        var expiredLog = new IdempotencyLog
        {
            Id = Guid.NewGuid(),
            CorrelationId = "expired-correlation-id",
            RequestType = "CreateCountriesCommand",
            HttpMethod = "POST",
            RequestPath = "/api/v1/countries",
            ResponseData = @"{""ids"":[""id1""]}",
            ResponseStatusCode = 201,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            IsExpired = true,
        };

        await repository.AddAsync(expiredLog);
        await repository.SaveAsync();

        // Act
        var result = await repository.GetByCorrelationIdAsync("expired-correlation-id", "CreateCountriesCommand");

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task DeleteExpiredAsync_DeletesExpiredRecords()
    {
        // Arrange
        var repository = new IdempotencyRepository(dbContext!);

        var expiredLog = new IdempotencyLog
        {
            Id = Guid.NewGuid(),
            CorrelationId = "expired-id",
            RequestType = "CreateCountriesCommand",
            HttpMethod = "POST",
            RequestPath = "/api/v1/countries",
            ResponseData = "{}",
            ResponseStatusCode = 201,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            ExpiresAt = DateTime.UtcNow.AddSeconds(-1),
            IsExpired = false,
        };

        var validLog = new IdempotencyLog
        {
            Id = Guid.NewGuid(),
            CorrelationId = "valid-id",
            RequestType = "CreateCountriesCommand",
            HttpMethod = "POST",
            RequestPath = "/api/v1/countries",
            ResponseData = "{}",
            ResponseStatusCode = 201,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(1440),
            IsExpired = false,
        };

        await repository.AddAsync(expiredLog);
        await repository.AddAsync(validLog);
        await repository.SaveAsync();

        // Act
        var deletedCount = await repository.DeleteExpiredAsync();

        // Assert
        Assert.AreEqual(1, deletedCount);

        var remaining = await dbContext!.IdempotencyLogs.ToListAsync();
        Assert.AreEqual(1, remaining.Count);
        Assert.AreEqual("valid-id", remaining[0].CorrelationId);
    }

    [TestMethod]
    public async Task GetByCorrelationIdAsync_ThrowsArgumentException_WhenCorrelationIdNull()
    {
        // Arrange
        var repository = new IdempotencyRepository(dbContext!);

        // Act & Assert
        try
        {
            await repository.GetByCorrelationIdAsync(null!, "CreateCountriesCommand");
            Assert.Fail("Expected ArgumentException was not thrown.");
        }
        catch (ArgumentException)
        {
            // Expected
        }
    }

    [TestMethod]
    public async Task GetByCorrelationIdAsync_ThrowsArgumentException_WhenRequestTypeNull()
    {
        // Arrange
        var repository = new IdempotencyRepository(dbContext!);

        // Act & Assert
        try
        {
            await repository.GetByCorrelationIdAsync("test-id", null!);
            Assert.Fail("Expected ArgumentException was not thrown.");
        }
        catch (ArgumentException)
        {
            // Expected
        }
    }
}
