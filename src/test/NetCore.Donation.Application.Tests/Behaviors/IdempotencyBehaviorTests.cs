using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NetCore.Donation.Application.Behaviors;
using NetCore.Donation.Domain.Entities;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Application.Tests.Behaviors;

[TestClass]
public class IdempotencyBehaviorTests
{
    private Mock<ILogger<IdempotencyBehavior<TestCommand, TestResponse>>>? loggerMock;
    private Mock<IIdempotencyRepository>? repositoryMock;
    private Mock<ICorrelationIdAccessor>? correlationIdAccessorMock;

    [TestInitialize]
    public void Setup()
    {
        loggerMock = new Mock<ILogger<IdempotencyBehavior<TestCommand, TestResponse>>>();
        repositoryMock = new Mock<IIdempotencyRepository>();
        correlationIdAccessorMock = new Mock<ICorrelationIdAccessor>();
    }

    [TestMethod]
    public async Task Handle_BypassesIdempotency_WhenRepositoryNull()
    {
        // Arrange
        var behavior = new IdempotencyBehavior<TestCommand, TestResponse>(loggerMock!.Object, null, null);
        var called = false;

        RequestHandlerDelegate<TestResponse> next = async (cancellationToken) =>
        {
            called = true;
            return new TestResponse { Message = "Response" };
        };

        var command = new TestCommand { Value = "test" };

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        Assert.IsTrue(called);
        Assert.IsNotNull(result);
        Assert.AreEqual("Response", result.Message);
    }

    [TestMethod]
    public async Task Handle_ReturnsCachedResponse_WhenIdempotencyLogExists()
    {
        // Arrange
        var correlationId = "test-correlation-id-123";
        var cachedResponse = new TestResponse { Message = "Cached Response" };
        var cachedLog = new IdempotencyLog
        {
            Id = Guid.NewGuid(),
            CorrelationId = correlationId,
            RequestType = "TestCommand",
            HttpMethod = "POST",
            RequestPath = "/test",
            ResponseData = System.Text.Json.JsonSerializer.Serialize(cachedResponse),
            ResponseStatusCode = 200,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(1440),
            IsExpired = false,
        };

        correlationIdAccessorMock!.Setup(x => x.CorrelationId).Returns(correlationId);
        repositoryMock!
            .Setup(x => x.GetByCorrelationIdAsync(correlationId, "TestCommand", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedLog);

        var behavior = new IdempotencyBehavior<TestCommand, TestResponse>(
            loggerMock!.Object,
            repositoryMock.Object,
            correlationIdAccessorMock.Object);

        var handlerCalled = false;
        RequestHandlerDelegate<TestResponse> next = async (cancellationToken) =>
        {
            handlerCalled = true;
            return new TestResponse { Message = "Fresh Response" };
        };

        var command = new TestCommand { Value = "test" };

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        Assert.IsFalse(handlerCalled, "Handler should not have been called");
        Assert.AreEqual("Cached Response", result.Message);
        repositoryMock.Verify(
            x => x.GetByCorrelationIdAsync(correlationId, "TestCommand", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_StoresIdempotencyLog_WhenNoExistingLog()
    {
        // Arrange
        var correlationId = "new-correlation-id";
        var newResponse = new TestResponse { Message = "New Response" };

        correlationIdAccessorMock!.Setup(x => x.CorrelationId).Returns(correlationId);
        repositoryMock!
            .Setup(x => x.GetByCorrelationIdAsync(correlationId, "TestCommand", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdempotencyLog?)null);

        var behavior = new IdempotencyBehavior<TestCommand, TestResponse>(
            loggerMock!.Object,
            repositoryMock.Object,
            correlationIdAccessorMock.Object);

        RequestHandlerDelegate<TestResponse> next = async (cancellationToken) => newResponse;

        var command = new TestCommand { Value = "test" };

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        Assert.AreEqual("New Response", result.Message);

        repositoryMock.Verify(
            x => x.AddAsync(It.Is<IdempotencyLog>(
                log => log.CorrelationId == correlationId && log.RequestType == "TestCommand"),
            It.IsAny<CancellationToken>()),
            Times.Once);

        repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_ContinuesOnRepository_WhenRepositoryThrows()
    {
        // Arrange
        var correlationId = "error-correlation-id";

        correlationIdAccessorMock!.Setup(x => x.CorrelationId).Returns(correlationId);
        repositoryMock!
            .Setup(x => x.GetByCorrelationIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Repository error"));

        var behavior = new IdempotencyBehavior<TestCommand, TestResponse>(
            loggerMock!.Object,
            repositoryMock.Object,
            correlationIdAccessorMock.Object);

        var handlerCalled = false;
        RequestHandlerDelegate<TestResponse> next = async (cancellationToken) =>
        {
            handlerCalled = true;
            return new TestResponse { Message = "Handler response" };
        };

        var command = new TestCommand { Value = "test" };

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        Assert.IsTrue(handlerCalled, "Handler should have been called despite repository error");
        Assert.AreEqual("Handler response", result.Message);
    }

    // Test helpers
    public class TestCommand
    {
        public string Value { get; set; } = string.Empty;
    }

    public class TestResponse
    {
        public string Message { get; set; } = string.Empty;
    }
}