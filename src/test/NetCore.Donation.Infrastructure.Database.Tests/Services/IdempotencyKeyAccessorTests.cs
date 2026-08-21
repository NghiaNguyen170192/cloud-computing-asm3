using Microsoft.AspNetCore.Http;
using Moq;
using NetCore.Donation.Infrastructure.Database.Services;

namespace NetCore.Donation.Infrastructure.Database.Tests.Services;

[TestClass]
public class IdempotencyKeyAccessorTests
{
    [TestMethod]
    public void IdempotencyKey_WhenStoredInItems_ReturnsKey()
    {
        var httpContextMock = new Mock<HttpContext>();
        var items = new Dictionary<object, object?> { { IdempotencyKeyAccessor.ItemKey, "idem-123" } };
        httpContextMock.Setup(x => x.Items).Returns(items);

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        var accessor = new IdempotencyKeyAccessor(httpContextAccessorMock.Object);

        Assert.AreEqual("idem-123", accessor.IdempotencyKey);
    }

    [TestMethod]
    public void IdempotencyKey_WhenHeaderPresent_ReturnsHeader()
    {
        var httpContextMock = new Mock<HttpContext>();
        var items = new Dictionary<object, object?>();
        var headers = new HeaderDictionary
        {
            [IdempotencyKeyAccessor.HeaderName] = "header-key",
        };

        httpContextMock.Setup(x => x.Items).Returns(items);
        httpContextMock.Setup(x => x.Request.Headers).Returns(headers);

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        var accessor = new IdempotencyKeyAccessor(httpContextAccessorMock.Object);

        Assert.AreEqual("header-key", accessor.IdempotencyKey);
    }

    [TestMethod]
    public void IdempotencyKey_WhenHttpContextNull_ReturnsNull()
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var accessor = new IdempotencyKeyAccessor(httpContextAccessorMock.Object);

        Assert.IsNull(accessor.IdempotencyKey);
    }
}
