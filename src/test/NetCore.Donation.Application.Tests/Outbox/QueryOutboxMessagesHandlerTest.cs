using Microsoft.EntityFrameworkCore;
using NetCore.Donation.Application.Outbox.QueryOutboxMessages;
using NetCore.Donation.Domain.Entities;
using NetCore.Donation.Infrastructure.Database;
using NetCore.Donation.Infrastructure.Database.Repositories;

namespace NetCore.Donation.Application.Tests.Outbox;

[TestClass]
public class QueryOutboxMessagesHandlerTest : BaseTest
{
    [TestMethod]
    public async Task Handle_WhenTwoMessagesShareCorrelationId_ReturnsBoth()
    {
        var context = await GetContext();
        await using (context)
        {
            const string correlationId = "shared-correlation";
            context.OutboxMessages.Add(OutboxMessage.Create(
                "NetCore.Donation.Domain.Events.CountryCreatedDomainEvent",
                """{"countryId":"00000000-0000-0000-0000-000000000001","name":"A"}""",
                correlationId,
                correlationId));
            context.OutboxMessages.Add(OutboxMessage.Create(
                "NetCore.Donation.Domain.Events.JournalEntryCreatedDomainEvent",
                """{"contactId":"00000000-0000-0000-0000-000000000002","amount":10}""",
                correlationId,
                correlationId));
            context.OutboxMessages.Add(OutboxMessage.Create(
                "NetCore.Donation.Domain.Events.CountryCreatedDomainEvent",
                """{"countryId":"00000000-0000-0000-0000-000000000003","name":"B"}""",
                "other-correlation",
                "other-correlation"));
            await context.SaveChangesAsync(CancellationToken.None);

            var handler = new QueryOutboxMessagesHandler(new OutboxMessageRepository(context));
            var result = (await handler.Handle(new QueryOutboxMessages(correlationId), CancellationToken.None)).ToList();

            Assert.HasCount(2, result);
            Assert.IsTrue(result.All(message => message.CorrelationId == correlationId));
        }
    }
}
