using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetCore.Donation.Application.Donation.CreateJournalEntry;
using NetCore.Donation.Application.Donation.GenerateDonationReceipt;
using NetCore.Donation.Domain.Events;

namespace NetCore.Donation.Application.Donation.Events;

public class TransactionCompletedDomainEventHandler(
    IServiceScopeFactory scopeFactory,
    ILogger<TransactionCompletedDomainEventHandler> logger)
    : INotificationHandler<TransactionCompletedDomainEvent>
{
    public async Task Handle(TransactionCompletedDomainEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Transaction {TransactionId} completed; dispatching receipt and journal commands",
            notification.TransactionId);

        await Task.WhenAll(
            SendInNewScope(new GenerateDonationReceiptCommand(notification.TransactionId), cancellationToken),
            SendInNewScope(new CreateJournalEntryCommand(notification.TransactionId), cancellationToken));
    }

    private async Task SendInNewScope<TResponse>(IRequest<TResponse> command, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(command, cancellationToken);
    }
}
