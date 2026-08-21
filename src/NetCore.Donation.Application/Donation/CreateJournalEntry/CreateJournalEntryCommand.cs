using MediatR;

namespace NetCore.Donation.Application.Donation.CreateJournalEntry;

public sealed record CreateJournalEntryCommand(Guid TransactionId) : IRequest<Guid>;
