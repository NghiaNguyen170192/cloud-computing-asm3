using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetCore.Donation.Application.Donation.CreateJournalEntry;
using NetCore.Donation.Application.Donation.GenerateDonationReceipt;
using NetCore.Donation.Domain.Events;

namespace NetCore.Donation.Application.Donation.Events;

public class TransactionSucceededDomainEventHandler(
    IServiceScopeFactory scopeFactory,
    ILogger<TransactionSucceededDomainEventHandler> logger)
    : INotificationHandler<TransactionSucceededDomainEvent>
{
    public async Task Handle(TransactionSucceededDomainEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Transaction {TransactionId} succeeded; dispatching receipt and journal commands",
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
